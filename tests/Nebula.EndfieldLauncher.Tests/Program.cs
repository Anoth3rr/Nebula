using Nebula.Core;
using Nebula.Features.GameLauncher;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

// Harmless child modes exercise process creation without a game, UAC or network.
if (args.FirstOrDefault() == "--native-parent")
{
    Console.ReadLine();
    return;
}
if (args.FirstOrDefault() == "--native-child")
{
    var snapshot = new ChildResult(Native.GetParentId(), Environment.ProcessId, Environment.CurrentDirectory, args[2..]);
    File.WriteAllText(args[1], JsonSerializer.Serialize(snapshot));
    return;
}

int passed = 0;
void Check(string name, bool condition)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine($"PASS {name}");
    passed++;
}
void Rejects(string name, Action action)
{
    try { action(); }
    catch (ArgumentException) { Check(name, true); return; }
    throw new Exception($"{name}: expected invalid arguments to be rejected");
}

Check("CN uses the official parent", EndfieldLauncher.RequiresOfficialLauncher(GameBiz.endfield_cn, false));
Check("Global uses the official parent", EndfieldLauncher.RequiresOfficialLauncher(GameBiz.endfield_global, false));
Check("Third-party tools own their launch chain", !EndfieldLauncher.RequiresOfficialLauncher(GameBiz.endfield_cn, true));
Check("Arknights is not rerouted", !EndfieldLauncher.RequiresOfficialLauncher(GameBiz.arknights_cn, false));
Check("Unknown Endfield channels are not rerouted", !EndfieldLauncher.RequiresOfficialLauncher("endfield_unknown", false));
Check("CN and global registrations are separate", !EndfieldLauncher.GetProtocols(GameBiz.endfield_cn)
    .Intersect(EndfieldLauncher.GetProtocols(GameBiz.endfield_global)).Any());
Check("Legacy global registration remains supported", EndfieldLauncher.GetProtocols(GameBiz.endfield_global)
    .Contains("gryphlinelauncher-YDUTE5gscDZ229CW"));
Check("Quoted registry path preserves spaces", EndfieldLauncher.ParseRegisteredExecutable("\"D:\\Games Folder\\Launcher.exe\" \"%1\"")
    == "D:\\Games Folder\\Launcher.exe");
Check("Unquoted registry commands are rejected", EndfieldLauncher.ParseRegisteredExecutable("D:\\Games Folder\\Launcher.exe %1") == null);
Check("Relative registry path is rejected", EndfieldLauncher.ParseRegisteredExecutable("\"Launcher.exe\" \"%1\"") == null);
Check("Unterminated registry command is rejected", EndfieldLauncher.ParseRegisteredExecutable("\"D:\\Games\\Launcher.exe") == null);
Check("Unrelated registered executable is rejected", EndfieldLauncher.ParseRegisteredExecutable("\"C:\\Windows\\cmd.exe\" /c anything") == null);
Check("Missing registry command is rejected", EndfieldLauncher.ParseRegisteredExecutable(null) == null);
Check("UAC cancellation remains a cancellation", EndfieldLauncher.CreateFailure(EndfieldLauncher.Failure.Cancelled) is Win32Exception { NativeErrorCode: 1223 });
Check("Missing launcher tells users how to recover", EndfieldLauncher.CreateFailure(EndfieldLauncher.Failure.MissingLauncher).Message.Contains("GRYPHLINK"));

Check("Executable with spaces is quoted and custom arguments stay intact",
    LauncherNativeProcess.BuildCommandLine("D:\\Game Folder\\Endfield.exe", "-log=\"path with spaces\" -force-d3d11")
    == "\"D:\\Game Folder\\Endfield.exe\" -log=\"path with spaces\" -force-d3d11");
Rejects("Relative executable cannot reach CreateProcess", () => LauncherNativeProcess.BuildCommandLine("Endfield.exe", ""));
Rejects("Quotes in executable are rejected", () => LauncherNativeProcess.BuildCommandLine("D:\\End\"field.exe", ""));
Rejects("NUL in arguments is rejected", () => LauncherNativeProcess.BuildCommandLine("D:\\Endfield.exe", "-flag\0ignored"));
Rejects("Overlong command line is rejected", () => LauncherNativeProcess.BuildCommandLine("D:\\Endfield.exe", new string('x', 32767)));

string root = Path.Combine(Path.GetTempPath(), "Nebula.EndfieldLauncher.Tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    foreach (string version in new[] { "1.9.0", "1.10.0", "backup", "1.11.0" })
    {
        Directory.CreateDirectory(Path.Combine(root, version));
        if (version != "1.11.0") File.WriteAllText(Path.Combine(root, version, "Games.exe"), "test fixture, not an executable");
    }
    File.WriteAllText(Path.Combine(root, "Games.exe"), "not a signed executable");
    string[] candidates = EndfieldLauncher.GetCandidates([root, root, Path.Combine(root, "missing")]);
    Check("Launcher versions are sorted numerically and incomplete versions ignored", candidates.SequenceEqual(new[]
    {
        Path.Combine(root, "1.10.0", "Games.exe"), Path.Combine(root, "1.9.0", "Games.exe"), Path.Combine(root, "Games.exe"),
    }));
    Check("Unsigned Games.exe is rejected", !OfficialLauncherSignature.Verify(Path.Combine(root, "Games.exe"), false));

    string executable = Environment.ProcessPath!;
    using var current = Process.GetCurrentProcess();
    using (var currentHandle = LauncherNativeProcess.Open(current.Id))
        Check("Unicode image path query returns the executable", LauncherNativeProcess.GetPath(currentHandle).Equals(executable, StringComparison.OrdinalIgnoreCase));

    var parentInfo = new ProcessStartInfo(executable)
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardInput = true,
    };
    parentInfo.ArgumentList.Add("--native-parent");
    using var parent = Process.Start(parentInfo)!;
    try
    {
        using var parentHandle = LauncherNativeProcess.Open(parent.Id, forChildCreation: true);
        string output = Path.Combine(root, "child result 中文.json");
        int childId = LauncherNativeProcess.Start(parentHandle, executable, $"--native-child \"{output}\" \"argument with spaces\" -flag=\"custom value\"");
        for (int i = 0; i < 100 && !File.Exists(output); i++) await Task.Delay(50);
        // Wait for the child's writer to close before reading its report.
        try { using var child = Process.GetProcessById(childId); await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); }
        catch (ArgumentException) { }
        var report = JsonSerializer.Deserialize<ChildResult>(File.ReadAllText(output))!;
        Check("Native creation uses the selected parent, not the calling process", report.ParentId == parent.Id && report.ParentId != current.Id);
        Check("Native creation returns the actual child PID", report.ProcessId == childId);
        Check("Native creation sets the executable directory", report.Directory.Equals(Path.GetDirectoryName(executable), StringComparison.OrdinalIgnoreCase));
        Check("Native creation preserves quoted custom arguments", report.Arguments.SequenceEqual(new[] { "argument with spaces", "-flag=custom value" }));
    }
    finally
    {
        parent.StandardInput.Close();
        await parent.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
    }

    int verifyOption = Array.IndexOf(args, "--verify-installed");
    if (verifyOption >= 0 && verifyOption + 1 < args.Length)
    {
        string installed = args[verifyOption + 1];
        Check("Installed CN official launcher signature is accepted", OfficialLauncherSignature.Verify(installed, false));
        Check("CN signature cannot stand in for global publisher", !OfficialLauncherSignature.Verify(installed, true));
        string changed = Path.Combine(root, "Games.exe");
        File.Copy(installed, changed, true);
        using (var stream = File.Open(changed, FileMode.Open, FileAccess.ReadWrite))
        {
            stream.Position = 0x40; // DOS stub is covered by the Authenticode digest.
            int original = stream.ReadByte();
            stream.Position = 0x40;
            stream.WriteByte((byte)(original ^ 1));
        }
        Check("Tampered official image is rejected", !OfficialLauncherSignature.Verify(changed, false));
    }
}
finally
{
    Directory.Delete(root, recursive: true);
}
Console.WriteLine($"{passed} Endfield launcher checks passed.");

record ChildResult(int ParentId, int ProcessId, string Directory, string[] Arguments);

static class Native
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation
    {
        public nint Reserved1, PebAddress, Reserved2, Reserved3, ProcessId, ParentId;
    }
    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(nint process, int informationClass,
        out ProcessBasicInformation information, int size, out int returnedLength);

    internal static int GetParentId()
    {
        using var current = Process.GetCurrentProcess();
        int status = NtQueryInformationProcess(current.Handle, 0, out var info, Marshal.SizeOf<ProcessBasicInformation>(), out _);
        if (status != 0) throw new Exception($"NtQueryInformationProcess failed: {status:X}");
        return checked((int)info.ParentId);
    }
}
