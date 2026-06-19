param(
    [Parameter(Mandatory = $true)]
    [string] $Version,

    [ValidateSet("x64", "arm64")]
    [string] $Architecture = "x64",

    [string] $OutputDir = "publish/stable",

    [string] $SevenZipPath = "",

    [switch] $NativeAot,

    [switch] $NoClean,

    [switch] $KeepPayload
)

$ErrorActionPreference = "Stop"

if ($Version.Contains("-")) {
    throw "Stable package versions must not contain a prerelease suffix. Use a version like 1.2.3."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$repoRoot = $repoRoot.Path

function Resolve-SevenZip {
    param([string] $RequestedPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        if (Test-Path -LiteralPath $RequestedPath) {
            return (Resolve-Path -LiteralPath $RequestedPath).Path
        }
        throw "7-Zip was not found at '$RequestedPath'."
    }

    $fromPath = Get-Command "7z.exe" -ErrorAction SilentlyContinue
    if ($fromPath) {
        return $fromPath.Source
    }

    $candidates = @(
        "${env:ProgramFiles}\7-Zip\7z.exe",
        "${env:ProgramFiles(x86)}\7-Zip\7z.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    return $null
}

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FileName,

        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    Write-Host ">" $FileName ($Arguments -join " ")
    & $FileName @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FileName"
    }
}

function Publish-Setup {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RuntimeIdentifier,

        [Parameter(Mandatory = $true)]
        [string] $OutputPath,

        [Parameter(Mandatory = $true)]
        [string] $PackageVersion,

        [switch] $UseNativeAot
    )

    $baseArgs = @(
        "publish", "src/Nebula.Setup",
        "-c", "Release",
        "-r", $RuntimeIdentifier,
        "-o", $OutputPath,
        "-p:Version=$PackageVersion"
    )

    $singleFileArgs = $baseArgs + @(
        "-p:PublishAot=false",
        "-p:SelfContained=true",
        "-p:PublishSingleFile=true",
        "-p:PublishTrimmed=false",
        "-p:IncludeNativeLibrariesForSelfExtract=true"
    )

    if (-not $UseNativeAot) {
        Invoke-Checked "dotnet" $singleFileArgs
        return
    }

    try {
        Invoke-Checked "dotnet" $baseArgs
    }
    catch {
        Write-Warning "NativeAOT setup publish failed. Falling back to self-contained single-file setup."
        if (Test-Path -LiteralPath $OutputPath) {
            Remove-Item -LiteralPath $OutputPath -Recurse -Force
        }
        Invoke-Checked "dotnet" $singleFileArgs
    }
}

function New-SevenZipArchive {
    param(
        [Parameter(Mandatory = $true)]
        [string] $SourceDirectory,

        [Parameter(Mandatory = $true)]
        [string] $ArchivePath,

        [string] $SevenZipExe,

        [Parameter(Mandatory = $true)]
        [string] $HelperDirectory
    )

    if (-not [string]::IsNullOrWhiteSpace($SevenZipExe)) {
        Push-Location $SourceDirectory
        try {
            Invoke-Checked $SevenZipExe @("a", "-t7z", "-mx=9", $ArchivePath, ".\*")
        }
        finally {
            Pop-Location
        }
        return
    }

    Write-Host "7-Zip was not found; using SharpSevenZip via dotnet to create the payload archive."
    New-Item -ItemType Directory -Force -Path $HelperDirectory | Out-Null

    $helperProject = Join-Path $HelperDirectory "PackSevenZip.csproj"
    $helperProgram = Join-Path $HelperDirectory "Program.cs"

    Set-Content -LiteralPath $helperProject -Encoding UTF8 -Value @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="SharpSevenZip" Version="2.0.47" />
  </ItemGroup>
</Project>
'@

    Set-Content -LiteralPath $helperProgram -Encoding UTF8 -Value @'
using SharpSevenZip;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: PackSevenZip <source-directory> <archive-path>");
    return 2;
}

string sourceDirectory = Path.GetFullPath(args[0]);
string archivePath = Path.GetFullPath(args[1]);

if (!Directory.Exists(sourceDirectory))
{
    Console.Error.WriteLine($"Source directory does not exist: {sourceDirectory}");
    return 3;
}

Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
if (File.Exists(archivePath))
{
    File.Delete(archivePath);
}

var compressor = new SharpSevenZipCompressor
{
    CompressionLevel = SharpSevenZip.CompressionLevel.Ultra,
};

compressor.CompressDirectory(sourceDirectory, archivePath);
return 0;
'@

    Invoke-Checked "dotnet" @(
        "run",
        "--project", $helperProject,
        "-c", "Release",
        "--",
        $SourceDirectory,
        $ArchivePath
    )
}

$rid = "win-$Architecture"
$outputRoot = Join-Path $repoRoot $OutputDir
$workRoot = Join-Path $outputRoot "work\$Architecture"
$appPayloadDir = Join-Path $workRoot "Nebula\app-$Version"
$setupStubDir = Join-Path $workRoot "setup-stub"
$finalSetupDir = Join-Path $workRoot "setup-final"
$packHelperDir = Join-Path $workRoot "pack-sevenzip"
$setupAssetsDir = Join-Path $repoRoot "src\Nebula.Setup\Assets"
$payloadArchive = Join-Path $setupAssetsDir "Nebula.7z"
$finalPackage = Join-Path $outputRoot "Nebula_Setup_${Version}_${Architecture}.exe"
$sevenZip = Resolve-SevenZip $SevenZipPath

if (-not $NoClean) {
    if (Test-Path -LiteralPath $workRoot) {
        Remove-Item -LiteralPath $workRoot -Recurse -Force
    }
    if (Test-Path -LiteralPath $finalPackage) {
        Remove-Item -LiteralPath $finalPackage -Force
    }
}

New-Item -ItemType Directory -Force -Path $outputRoot, $appPayloadDir, $setupStubDir, $finalSetupDir, $setupAssetsDir | Out-Null

try {
    Push-Location $repoRoot

    Write-Host "Publishing Nebula $Version ($Architecture) payload..."
    Invoke-Checked "dotnet" @(
        "publish", "src/Nebula",
        "-c", "Release",
        "-r", $rid,
        "-o", $appPayloadDir,
        "-p:Platform=$Architecture",
        "-p:Version=$Version"
    )

    Write-Host "Publishing setup bootstrapper..."
    Publish-Setup -RuntimeIdentifier $rid -OutputPath $setupStubDir -PackageVersion $Version -UseNativeAot:$NativeAot

    Copy-Item -LiteralPath (Join-Path $setupStubDir "Nebula.Setup.exe") -Destination (Join-Path $appPayloadDir "Nebula.Setup.exe") -Force

    if (Test-Path -LiteralPath $payloadArchive) {
        Remove-Item -LiteralPath $payloadArchive -Force
    }

    Write-Host "Creating embedded full-package payload..."
    New-SevenZipArchive -SourceDirectory $appPayloadDir -ArchivePath $payloadArchive -SevenZipExe $sevenZip -HelperDirectory $packHelperDir

    Write-Host "Publishing final stable setup executable..."
    Publish-Setup -RuntimeIdentifier $rid -OutputPath $finalSetupDir -PackageVersion $Version -UseNativeAot:$NativeAot

    Copy-Item -LiteralPath (Join-Path $finalSetupDir "Nebula.Setup.exe") -Destination $finalPackage -Force

    Write-Host ""
    Write-Host "Stable installer created:"
    Write-Host "  $finalPackage"
}
finally {
    Pop-Location
    if (-not $KeepPayload -and (Test-Path -LiteralPath $payloadArchive)) {
        Remove-Item -LiteralPath $payloadArchive -Force
    }
}
