using Microsoft.Extensions.Logging;
using Nebula.Core;
using Nebula.Core.HoYoPlay;
using Nebula.Features.GameSetting;
using Nebula.Features.HoYoPlay;
using Nebula.Features.PlayTime;
using Nebula.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nebula.Features.GameLauncher;

internal partial class GameLauncherService
{


    private readonly ILogger<GameLauncherService> _logger;


    private readonly HoYoPlayService _hoYoPlayService;

    private readonly PlayTimeService _playTimeService;

    private readonly GameAuthLoginService _gameAuthLoginService;


    public GameLauncherService(ILogger<GameLauncherService> logger, HoYoPlayService hoYoPlayService, PlayTimeService playTimeService, GameAuthLoginService gameAuthLoginService)
    {
        _logger = logger;
        _hoYoPlayService = hoYoPlayService;
        _playTimeService = playTimeService;
        _gameAuthLoginService = gameAuthLoginService;
    }





    /// <summary>
    /// 游戏安装目录，为空时未找到
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public static string? GetGameInstallPath(GameId gameId)
    {
        return GetGameInstallPath(gameId.GameBiz);
    }


    /// <summary>
    /// 游戏安装目录，为空时未找到
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public static string? GetGameInstallPath(GameBiz gameBiz)
    {
        var path = AppConfig.GetGameInstallPath(gameBiz);
        if (string.IsNullOrWhiteSpace(path) && GetSharedChinaLauncherGameBiz(gameBiz) is GameBiz sharedGameBiz)
        {
            path = AppConfig.GetGameInstallPath(sharedGameBiz);
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }
        path = GetFullPathIfRelativePath(path);
        if (Directory.Exists(path))
        {
            return Path.GetFullPath(path);
        }
        else if (AppConfig.GetGameInstallPathRemovable(gameBiz))
        {
            return path;
        }
        else
        {
            ChangeGameInstallPath(gameBiz, null);
            return null;
        }
    }



    /// <summary>
    /// 游戏安装目录，为空时未找到
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="storageRemoved">可移动存储设备已移除</param>
    /// <returns></returns>
    public static string? GetGameInstallPath(GameId gameId, out bool storageRemoved)
    {
        storageRemoved = false;
        var path = AppConfig.GetGameInstallPath(gameId.GameBiz);
        GameBiz? sharedGameBiz = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            sharedGameBiz = GetSharedChinaLauncherGameBiz(gameId.GameBiz);
            if (sharedGameBiz is GameBiz shared)
            {
                path = AppConfig.GetGameInstallPath(shared);
            }
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }
        path = GetFullPathIfRelativePath(path);
        if (Directory.Exists(path))
        {
            return path;
        }
        else if (AppConfig.GetGameInstallPathRemovable(sharedGameBiz ?? gameId.GameBiz))
        {
            storageRemoved = true;
            return path;
        }
        else
        {
            ChangeGameInstallPath(gameId, null);
            return null;
        }
    }



    /// <summary>
    /// 本地游戏版本
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<Version?> GetLocalGameVersionAsync(GameId gameId, string? installPath = null)
    {
        return await GetLocalGameVersionAsync(gameId.GameBiz, installPath);
    }



    /// <summary>
    /// 本地游戏版本
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<Version?> GetLocalGameVersionAsync(GameBiz gameBiz, string? installPath = null)
    {
        if (!gameBiz.IsHoYoPlayGame())
        {
            return null;
        }
        installPath ??= GetGameInstallPath(gameBiz);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return null;
        }
        var config = Path.Join(installPath, "config.ini");
        if (File.Exists(config))
        {
            var str = await File.ReadAllTextAsync(config);
            var matches = GameVersionRegex().Matches(str);
            Version? version = null;
            if (matches.Count > 0)
            {
                _ = Version.TryParse(matches[^1].Groups[1].Value, out version);
            }
            return version;
        }
        else
        {
            _logger.LogWarning("config.ini not found: {path}", config);
            return null;
        }
    }


    [GeneratedRegex(@"game_version=(.+)")]
    private static partial Regex GameVersionRegex();



    /// <summary>
    /// 最新游戏版本
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public async Task<(Version? Latest, Version? Predownload)> GetLatestGameVersionAsync(GameId gameId)
    {
        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId);
        if (config is null)
        {
            throw new ArgumentOutOfRangeException($"Game config is null ({gameId.Id}, {gameId.GameBiz}).");
        }
        if (config.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_CHUNK or DownloadMode.DOWNLOAD_MODE_LDIFF)
        {
            GameBranch? gameBranch = await _hoYoPlayService.GetGameBranchAsync(gameId);
            if (gameBranch is null)
            {
                throw new ArgumentOutOfRangeException($"Game branch is null ({gameId.Id}, {gameId.GameBiz}).");
            }
            _ = Version.TryParse(gameBranch.Main.Tag, out Version? latestVersion);
            _ = Version.TryParse(gameBranch.PreDownload?.Tag, out Version? predownloadVersion);
            return (latestVersion, predownloadVersion);
        }
        else
        {
            GamePackage package = await _hoYoPlayService.GetGamePackageAsync(gameId);
            _ = Version.TryParse(package.Main.Major?.Version, out Version? latestVersion);
            _ = Version.TryParse(package.PreDownload.Major?.Version, out Version? predownloadVersion);
            return (latestVersion, predownloadVersion);
        }
    }




    /// <summary>
    /// 游戏进程名，带 .exe 扩展名
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public async Task<string> GetGameExeNameAsync(GameId gameId)
    {
        string? name = GetGameExeName(gameId.GameBiz);
        if (string.IsNullOrWhiteSpace(name))
        {
            var config = await _hoYoPlayService.GetGameConfigAsync(gameId);
            name = config?.ExeFileName;
        }
        return name ?? throw new ArgumentOutOfRangeException($"Unknown game ({gameId.Id}, {gameId.GameBiz}).");
    }



    /// <summary>
    /// 游戏进程名，带 .exe 扩展名
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public static string? GetGameExeName(GameBiz gameBiz)
    {
        string? name = gameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => "YuanShen.exe",
            GameBiz.hk4e_global => "GenshinImpact.exe",
            GameBiz.arknights_cn or GameBiz.arknights_bilibili or GameBiz.arknights_global => FindGameExeName(gameBiz) ?? "Arknights.exe",
            GameBiz.endfield_cn or GameBiz.endfield_global => FindGameExeName(gameBiz) ?? "Endfield.exe",
            GameBiz.wutheringwaves_cn or GameBiz.wutheringwaves_global => FindGameExeName(gameBiz) ?? "Wuthering Waves.exe",
            _ => gameBiz.Game switch
            {
                GameBiz.hkrpg => "StarRail.exe",
                GameBiz.bh3 => "BH3.exe",
                GameBiz.nap => "ZenlessZoneZero.exe",
                _ => null,
            },
        };
        return name;
    }


    private static readonly IReadOnlyDictionary<string, string[]> GameExeCandidates = new Dictionary<string, string[]>
    {
        [GameBiz.arknights] = ["Arknights.exe", "ArknightsGame.exe", "明日方舟.exe"],
        [GameBiz.endfield] = ["Endfield.exe", "ArknightsEndfield.exe", "Arknights Endfield.exe", "明日方舟终末地.exe", "明日方舟：终末地.exe"],
        [GameBiz.wutheringwaves] = ["Wuthering Waves.exe", @"Wuthering Waves Game\Wuthering Waves.exe", @"Client\Binaries\Win64\Client-Win64-Shipping.exe", @"Wuthering Waves Game\Client\Binaries\Win64\Client-Win64-Shipping.exe", "launcher.exe"],
    };


    private static string? FindGameExeName(GameBiz gameBiz)
    {
        string? installPath = GetGameInstallPath(gameBiz);
        if (!Directory.Exists(installPath))
        {
            return null;
        }
        if (GameExeCandidates.TryGetValue(gameBiz.Game, out string[]? candidates))
        {
            foreach (var candidate in candidates)
            {
                if (File.Exists(Path.Join(installPath, candidate)))
                {
                    return candidate;
                }
            }
        }
        return Directory.EnumerateFiles(installPath, "*.exe", SearchOption.TopDirectoryOnly)
                        .Select(Path.GetFileName)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }



    /// <summary>
    /// 游戏进程文件是否存在
    /// </summary>
    /// <param name="biz"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<bool> IsGameExeExistsAsync(GameId gameId, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(gameId);
        if (!string.IsNullOrWhiteSpace(installPath))
        {
            var exe = Path.Join(installPath, await GetGameExeNameAsync(gameId));
            return File.Exists(exe);
        }
        return false;
    }



    /// <summary>
    /// 获取游戏进程
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public async Task<Process?> GetGameProcessAsync(GameId gameId)
    {
        int currentSessionId = Process.GetCurrentProcess().SessionId;
        var name = (await GetGameExeNameAsync(gameId)).Replace(".exe", "");
        return Process.GetProcessesByName(name).Where(x => x.SessionId == currentSessionId && !IsProcessPending(x)).FirstOrDefault();
    }



    /// <summary>
    /// 检测进程挂起
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    public static bool IsProcessPending(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return false;
            }
            foreach (ProcessThread thread in process.Threads)
            {
                if (thread.ThreadState is not ThreadState.Wait)
                {
                    return false;
                }
                else if (thread.WaitReason is not ThreadWaitReason.Suspended)
                {
                    return false;
                }
            }
            return true;
        }
        catch { }
        return true;
    }



    /// <summary>
    /// 启动游戏
    /// </summary>
    /// <returns></returns>
    public async Task<Process?> StartGameAsync(GameId gameId, string? installPath = null)
    {
        const int ERROR_CANCELLED = 0x000004C7;
        try
        {
            if (await GetGameProcessAsync(gameId) is Process existingProcess)
            {
                throw new Exception($"Game is running: {existingProcess.ProcessName}.exe ({existingProcess.Id}).");
            }
            string? exe = null, arg = null, verb = null;
            if (Directory.Exists(installPath))
            {
                var e = Path.Join(installPath, await GetGameExeNameAsync(gameId));
                if (File.Exists(e))
                {
                    exe = e;
                }
            }
            bool thirdPartyTool = false;
            if (string.IsNullOrWhiteSpace(exe) && AppConfig.GetEnableThirdPartyTool(gameId.GameBiz))
            {
                exe = GetThirdPartyToolPath(gameId);
                if (File.Exists(exe))
                {
                    thirdPartyTool = true;
                    verb = Path.GetExtension(exe) is ".exe" or ".bat" ? "runas" : "";
                }
                else
                {
                    exe = null;
                    SetThirdPartyToolPath(gameId, null);
                    _logger.LogWarning("Third party tool not found: {path}", exe);
                }
            }
            if (string.IsNullOrWhiteSpace(exe))
            {
                var folder = GetGameInstallPath(gameId);
                var name = await GetGameExeNameAsync(gameId);
                exe = Path.Join(folder, name);
                verb = "runas";
                if (!File.Exists(exe))
                {
                    _logger.LogWarning("Game exe not found: {path}", exe);
                    throw new FileNotFoundException("Game exe not found", name);
                }
            }
            string? configInstallPath = Directory.Exists(installPath) ? installPath : GetGameInstallPath(gameId);
            if (!string.IsNullOrWhiteSpace(configInstallPath))
            {
                await SetChinaLauncherGameConfigAsync(gameId, configInstallPath);
                ApplyLocalSwitcherFiles(gameId.GameBiz, configInstallPath);
            }
            arg = AppConfig.GetStartArgument(gameId.GameBiz)?.Trim();
            if (AppConfig.EnableLoginAuthTicket is true)
            {
                string? ticket = await _gameAuthLoginService.CreateAuthTicketByGameBiz(gameId);
                if (!string.IsNullOrWhiteSpace(ticket))
                {
                    arg += $" login_auth_ticket={ticket}";
                }
            }
            if (AppConfig.GetUsePopupWindow(gameId.GameBiz))
            {
                arg += " -popupwindow";
            }

            if (gameId.GameBiz.Game is GameBiz.hk4e)
            {
                GameSettingService.SetGenshinEnableHDR(gameId.GameBiz, AppConfig.EnableGenshinHDR);
            }
            if (!thirdPartyTool && AppConfig.StartGameWithCMD)
            {
                arg = $"""/c start "" /d "{Path.GetDirectoryName(exe)}" "{exe}" {arg}""";
                exe = "cmd.exe";
            }
            _logger.LogInformation("Start game ({biz})\r\npath: {exe}\r\narg: {arg}", gameId, exe, arg);
            var info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arg,
                UseShellExecute = true,
                Verb = verb,
                WorkingDirectory = Path.GetDirectoryName(exe),
            };
            Process? process = Process.Start(info);
            if (process != null)
            {
                if (thirdPartyTool || AppConfig.StartGameWithCMD)
                {
                    return await _playTimeService.StartProcessToLogAsync(gameId);
                }
                else
                {
                    await _playTimeService.StartProcessToLogAsync(gameId, process.Id);
                    return process;
                }
            }
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
        {
            // Operation canceled
            _logger.LogInformation("Start game operation canceled.");
        }
        return null;
    }




    /// <summary>
    /// 修改游戏安装目录
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string? ChangeGameInstallPath(GameId gameId, string? path)
    {
        return ChangeGameInstallPath(gameId.GameBiz, path);
    }


    /// <summary>
    /// 修改游戏安装目录
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string? ChangeGameInstallPath(GameBiz gameBiz, string? path)
    {
        if (Directory.Exists(path))
        {
            path = Path.GetFullPath(path);
            string relativePath = GetRelativePathIfInRemovableStorage(path, out bool removable);
            AppConfig.SetGameInstallPath(gameBiz, relativePath);
            AppConfig.SetGameInstallPathRemovable(gameBiz, removable);
            InstallLocalPluginPackage(gameBiz, path);
            if (GetSharedChinaLauncherGameBiz(gameBiz) is GameBiz sharedGameBiz)
            {
                AppConfig.SetGameInstallPath(sharedGameBiz, relativePath);
                AppConfig.SetGameInstallPathRemovable(sharedGameBiz, removable);
            }
        }
        else
        {
            path = null;
            AppConfig.SetGameInstallPath(gameBiz, null);
            AppConfig.SetGameInstallPathRemovable(gameBiz, false);
            if (GetSharedChinaLauncherGameBiz(gameBiz) is GameBiz sharedGameBiz)
            {
                AppConfig.SetGameInstallPath(sharedGameBiz, null);
                AppConfig.SetGameInstallPathRemovable(sharedGameBiz, false);
            }
        }
        return path;
    }



    private static GameBiz? GetSharedChinaLauncherGameBiz(GameBiz gameBiz)
    {
        return gameBiz.Value switch
        {
            GameBiz.hk4e_cn => GameBiz.hk4e_bilibili,
            GameBiz.hk4e_bilibili => GameBiz.hk4e_cn,
            GameBiz.hkrpg_cn => GameBiz.hkrpg_bilibili,
            GameBiz.hkrpg_bilibili => GameBiz.hkrpg_cn,
            GameBiz.arknights_cn => GameBiz.arknights_bilibili,
            GameBiz.arknights_bilibili => GameBiz.arknights_cn,
            _ => null,
        };
    }



    private static void InstallLocalPluginPackage(GameBiz gameBiz, string installPath, ILogger? logger = null)
    {
        if (GetLocalPluginPackage(gameBiz) is not (string packageFileName, string dataFolder))
        {
            return;
        }

        string packagePath = Path.Join(AppContext.BaseDirectory, "Plugins", packageFileName);
        if (!File.Exists(packagePath))
        {
            logger?.LogWarning("Local plugin package not found: {path}", packagePath);
            return;
        }

        try
        {
            string? pcGameSdkPath = string.IsNullOrWhiteSpace(dataFolder) ? null : Path.Join(installPath, dataFolder, "Plugins", "PCGameSDK");
            if (pcGameSdkPath is not null)
            {
                UnlockPCGameSDK(pcGameSdkPath);
            }
            ZipFile.ExtractToDirectory(packagePath, installPath, true);
            if (pcGameSdkPath is not null)
            {
                LockPCGameSDK(pcGameSdkPath);
            }
            logger?.LogInformation("Installed local plugin package ({GameBiz}) from {package} to {installPath}", gameBiz, packagePath, installPath);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Install local plugin package failed ({GameBiz}) from {package} to {installPath}", gameBiz, packagePath, installPath);
        }
    }



    private static (string PackageFileName, string DataFolder)? GetLocalPluginPackage(GameBiz gameBiz)
    {
        return gameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => ("原神.zip", "YuanShen_Data"),
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => ("崩坏星穹铁道.zip", "StarRail_Data"),
            GameBiz.nap_cn or GameBiz.nap_bilibili => ("绝区零.zip", "ZenlessZoneZero_Data"),
            GameBiz.arknights_cn or GameBiz.arknights_bilibili => ("明日方舟.zip", ""),
            _ => null,
        };
    }



    private static void ApplyLocalSwitcherFiles(GameBiz gameBiz, string installPath)
    {
        string? switchFolder = gameBiz.Value switch
        {
            GameBiz.arknights_cn => "B2C",
            GameBiz.arknights_bilibili => "C2B",
            _ => null,
        };
        if (switchFolder is null)
        {
            return;
        }

        string source = Path.Join(installPath, switchFolder);
        if (!Directory.Exists(source))
        {
            return;
        }
        CopyDirectory(source, installPath);
    }



    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Join(target, relativePath));
        }
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(source, file);
            string targetFile = Path.Join(target, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            if (File.Exists(targetFile))
            {
                File.SetAttributes(targetFile, File.GetAttributes(targetFile) & ~FileAttributes.ReadOnly);
            }
            File.Copy(file, targetFile, true);
        }
    }



    private static void UnlockPCGameSDK(string path)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            return;
        }

        SetReadOnly(path, false);
        RemoveWriteDenyRules(path);
    }



    private static void LockPCGameSDK(string path)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            return;
        }

        SetReadOnly(path, true);
        AddWriteDenyRules(path);
    }



    private static void SetReadOnly(string path, bool readOnly)
    {
        if (File.Exists(path))
        {
            SetFileReadOnly(path, readOnly);
            return;
        }

        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (string item in Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories))
        {
            if (File.Exists(item))
            {
                SetFileReadOnly(item, readOnly);
            }
            else if (Directory.Exists(item))
            {
                SetDirectoryReadOnly(item, readOnly);
            }
        }
        SetDirectoryReadOnly(path, readOnly);
    }



    private static void SetFileReadOnly(string path, bool readOnly)
    {
        FileAttributes attributes = File.GetAttributes(path);
        attributes = readOnly ? attributes | FileAttributes.ReadOnly : attributes & ~FileAttributes.ReadOnly;
        File.SetAttributes(path, attributes);
    }



    private static void SetDirectoryReadOnly(string path, bool readOnly)
    {
        FileAttributes attributes = File.GetAttributes(path);
        attributes = readOnly ? attributes | FileAttributes.ReadOnly : attributes & ~FileAttributes.ReadOnly;
        File.SetAttributes(path, attributes);
    }



    private static void RemoveWriteDenyRules(string path)
    {
        FileSystemSecurity security = GetFileSystemSecurity(path);
        foreach (IdentityReference identity in GetWriteDenyIdentities())
        {
            FileSystemAccessRule rule = CreateWriteDenyRule(identity);
            security.RemoveAccessRuleAll(rule);
        }
        SetFileSystemSecurity(path, security);
    }



    private static void AddWriteDenyRules(string path)
    {
        FileSystemSecurity security = GetFileSystemSecurity(path);
        foreach (IdentityReference identity in GetWriteDenyIdentities())
        {
            security.RemoveAccessRuleAll(CreateWriteDenyRule(identity));
            security.AddAccessRule(CreateWriteDenyRule(identity));
        }
        SetFileSystemSecurity(path, security);
    }



    private static FileSystemSecurity GetFileSystemSecurity(string path)
    {
        return Directory.Exists(path)
            ? new DirectoryInfo(path).GetAccessControl()
            : new FileInfo(path).GetAccessControl();
    }



    private static void SetFileSystemSecurity(string path, FileSystemSecurity security)
    {
        if (Directory.Exists(path) && security is DirectorySecurity directorySecurity)
        {
            new DirectoryInfo(path).SetAccessControl(directorySecurity);
        }
        else if (File.Exists(path) && security is FileSecurity fileSecurity)
        {
            new FileInfo(path).SetAccessControl(fileSecurity);
        }
    }



    private static IEnumerable<IdentityReference> GetWriteDenyIdentities()
    {
        yield return new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
        yield return new SecurityIdentifier(WellKnownSidType.WorldSid, null);
    }



    private static FileSystemAccessRule CreateWriteDenyRule(IdentityReference identity)
    {
        const FileSystemRights rights =
            FileSystemRights.Write |
            FileSystemRights.CreateFiles |
            FileSystemRights.CreateDirectories |
            FileSystemRights.AppendData |
            FileSystemRights.WriteData |
            FileSystemRights.WriteAttributes |
            FileSystemRights.WriteExtendedAttributes |
            FileSystemRights.Delete |
            FileSystemRights.DeleteSubdirectoriesAndFiles |
            FileSystemRights.ChangePermissions |
            FileSystemRights.TakeOwnership;

        return new FileSystemAccessRule(
            identity,
            rights,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Deny);
    }



    private static async Task SetChinaLauncherGameConfigAsync(GameId gameId, string installPath)
    {
        if (gameId.GameBiz.Value is not (GameBiz.hk4e_cn or GameBiz.hk4e_bilibili or GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili))
        {
            return;
        }

        string path = Path.Join(installPath, "config.ini");
        if (!File.Exists(path))
        {
            return;
        }

        Dictionary<string, string> values = gameId.GameBiz.Server switch
        {
            "cn" => new()
            {
                ["channel"] = "1",
                ["sub_channel"] = "1",
                ["cps"] = "mihoyo",
            },
            "bilibili" => new()
            {
                ["channel"] = "14",
                ["sub_channel"] = "0",
                ["cps"] = "bilibili",
            },
            _ => [],
        };
        if (values.Count == 0)
        {
            return;
        }

        string[] lines = await File.ReadAllLinesAsync(path);
        HashSet<string> existingKeys = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            int index = line.IndexOf('=');
            if (index <= 0)
            {
                continue;
            }
            string key = line[..index].Trim();
            if (values.TryGetValue(key, out string? value))
            {
                lines[i] = $"{key}={value}";
                existingKeys.Add(key);
            }
        }

        await using StreamWriter writer = new(path, false);
        foreach (string line in lines)
        {
            await writer.WriteLineAsync(line);
        }
        foreach ((string key, string value) in values)
        {
            if (!existingKeys.Contains(key))
            {
                await writer.WriteLineAsync($"{key}={value}");
            }
        }
    }



    /// <summary>
    /// 如果安装在可移动存储设备中，获取相对路径
    /// </summary>
    /// <param name="path"></param>
    /// <param name="removableStorage"></param>
    /// <returns></returns>
    public static string GetRelativePathIfInRemovableStorage(string path, out bool removableStorage)
    {
        removableStorage = DriveHelper.IsDeviceRemovableOrOnUSB(path);
        if (removableStorage && Path.GetPathRoot(AppConfig.NebulaExecutePath) == Path.GetPathRoot(path))
        {
            path = Path.GetRelativePath(Path.GetDirectoryName(AppConfig.ConfigPath)!, path);
        }
        return path;
    }



    /// <summary>
    /// 如果安装在可移动存储设备中，获取完整路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string GetFullPathIfRelativePath(string path)
    {
        if (Path.IsPathFullyQualified(path))
        {
            return Path.GetFullPath(path);
        }
        else
        {
            return Path.GetFullPath(path, Path.GetDirectoryName(AppConfig.ConfigPath)!);
        }
    }




    /// <summary>
    /// 获取第三方工具路径
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public static string? GetThirdPartyToolPath(GameId gameId)
    {
        string? path = AppConfig.GetThirdPartyToolPath(gameId.GameBiz);
        if (!string.IsNullOrWhiteSpace(path))
        {
            path = GetFullPathIfRelativePath(path);
        }
        if (File.Exists(path))
        {
            return path;
        }
        else
        {
            AppConfig.SetThirdPartyToolPath(gameId.GameBiz, null);
            return null;
        }
    }


    /// <summary>
    /// 设置第三方工具路径
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string? SetThirdPartyToolPath(GameId gameId, string? path)
    {
        if (File.Exists(path))
        {
            path = Path.GetFullPath(path);
            string relativePath = GetRelativePathIfInRemovableStorage(path, out bool removable);
            AppConfig.SetThirdPartyToolPath(gameId.GameBiz, relativePath);
        }
        else
        {
            path = null;
            AppConfig.SetThirdPartyToolPath(gameId.GameBiz, null);
        }
        return path;
    }




}
