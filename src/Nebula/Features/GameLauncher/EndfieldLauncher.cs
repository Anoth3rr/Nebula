using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Nebula.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Nebula.Features.GameLauncher;

internal static class EndfieldLauncher
{
    internal const string HelperCommand = "--nebula-endfield-launch";

    // Positive helper exit codes contain the exact game PID; failures are negative.
    internal enum Failure
    {
        Unknown = -1,
        MissingLauncher = -2,
        InvalidSignature = -3,
        AccessDenied = -4,
        LauncherTimeout = -5,
        InvalidRequest = -6,
        Cancelled = -7,
    }

    internal static bool RequiresOfficialLauncher(GameBiz biz, bool thirdPartyTool) =>
        !thirdPartyTool && biz.Value is GameBiz.endfield_cn or GameBiz.endfield_global;

    internal static async Task<Process> StartAsync(GameBiz biz, string executable, string? arguments, string nebulaExecutable)
    {
        ValidateRequest(biz, executable, arguments);
        int processId;
        if (IsElevated())
        {
            processId = await Task.Run(() => StartCoreAsync(biz, executable, arguments));
        }
        else
        {
            // Both Games.exe and Endfield.exe require elevation. Keep the UI at its
            // original privilege level and use a short-lived copy of Nebula as helper.
            if (GetCandidates(GetRegisteredRoots(biz)).Length == 0)
                throw CreateFailure(Failure.MissingLauncher);
            var start = new ProcessStartInfo(nebulaExecutable)
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = AppContext.BaseDirectory,
            };
            start.ArgumentList.Add(HelperCommand);
            start.ArgumentList.Add(biz.Value);
            start.ArgumentList.Add(Path.GetFullPath(executable));
            start.ArgumentList.Add(arguments ?? "");
            using var helper = await Task.Run(() => Process.Start(start))
                ?? throw CreateFailure(Failure.Unknown);
            await helper.WaitForExitAsync();
            processId = helper.ExitCode;
            if (processId <= 0)
                throw CreateFailure((Failure)processId);
        }

        try
        {
            var process = Process.GetProcessById(processId);
            using var current = Process.GetCurrentProcess();
            if (process.SessionId == current.SessionId
                && process.ProcessName.Equals(Path.GetFileNameWithoutExtension(executable), StringComparison.OrdinalIgnoreCase))
                return process;
            process.Dispose();
        }
        catch (ArgumentException) { /* Game exited before the helper returned. */ }
        throw new InvalidOperationException("终末地进程已退出，请检查游戏文件和官方启动器后重试。");
    }

    // Runs before WinUI/application initialization; no second launcher window or
    // activation redirection should consume this internal command.
    internal static async Task<int> RunHelperAsync(string[] args)
    {
        try
        {
            if (args.Length != 4 || args[0] != HelperCommand)
                throw CreateFailure(Failure.InvalidRequest);
            GameBiz biz = args[1];
            ValidateRequest(biz, args[2], args[3]);
            if (!IsElevated())
                throw CreateFailure(Failure.AccessDenied);
            return await StartCoreAsync(biz, args[2], args[3]);
        }
        catch (Exception ex)
        {
            try
            {
                string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nebula", "log");
                Directory.CreateDirectory(logDirectory);
                File.AppendAllText(Path.Combine(logDirectory, "endfield-launch.log"), $"[{DateTimeOffset.Now:O}] {ex}\n");
            }
            catch { /* Logging must not turn a failure into a successful PID. */ }
            return (int)(ex is LaunchException launch ? launch.Reason : ex is Win32Exception win32 ? win32.NativeErrorCode switch
            {
                5 or 740 => Failure.AccessDenied,
                1223 => Failure.Cancelled,
                _ => Failure.Unknown,
            } : Failure.Unknown);
        }
    }

    private static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    internal static void ValidateRequest(GameBiz biz, string executable, string? arguments)
    {
        string[] gameNames = ["Endfield.exe", "ArknightsEndfield.exe", "Arknights Endfield.exe", "明日方舟终末地.exe", "明日方舟：终末地.exe"];
        if (!RequiresOfficialLauncher(biz, false) || !Path.IsPathFullyQualified(executable)
            || !gameNames.Contains(Path.GetFileName(executable), StringComparer.OrdinalIgnoreCase) || !File.Exists(executable))
            throw CreateFailure(Failure.InvalidRequest);
        _ = LauncherNativeProcess.BuildCommandLine(executable, arguments);
    }

    internal static string[] GetProtocols(GameBiz biz) => biz.Value switch
    {
        GameBiz.endfield_cn => ["hypergryphlauncher-abYeZZ16BPluCFyT"],
        GameBiz.endfield_global => ["gryphlinklauncher-TiaytKBUIEdoEwRT", "gryphlinelauncher-YDUTE5gscDZ229CW"],
        _ => [],
    };

    internal static string? ParseRegisteredExecutable(string? command)
    {
        command = Environment.ExpandEnvironmentVariables(command?.Trim() ?? "");
        if (!command.StartsWith('"'))
            return null;
        int closingQuote = command.IndexOf('"', 1);
        if (closingQuote <= 1)
            return null;
        string path = command[1..closingQuote];
        return Path.IsPathFullyQualified(path) && !path.Contains('\0')
            && Path.GetFileName(path).Equals("Launcher.exe", StringComparison.OrdinalIgnoreCase) ? path : null;
    }

    private static IEnumerable<string> GetRegisteredRoots(GameBiz biz)
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var classes = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, view);
            foreach (string protocol in GetProtocols(biz))
            {
                using var key = classes.OpenSubKey($"{protocol}\\shell\\open\\command");
                string? executable = ParseRegisteredExecutable(key?.GetValue("") as string);
                if (executable != null && File.Exists(executable))
                    roots.Add(Path.GetDirectoryName(executable)!);
            }
        }
        return roots;
    }

    internal static string[] GetCandidates(IEnumerable<string> roots)
    {
        var candidates = new List<string>();
        foreach (string root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(root))
                continue;
            var versions = Directory.EnumerateDirectories(root)
                .Select(path => (Path: path, Version: Version.TryParse(Path.GetFileName(path), out var version) ? version : null))
                .Where(item => item.Version != null)
                .OrderByDescending(item => item.Version);
            candidates.AddRange(versions.Select(item => Path.Combine(item.Path, "Games.exe")).Where(File.Exists));
            string unversioned = Path.Combine(root, "Games.exe");
            if (File.Exists(unversioned)) candidates.Add(unversioned);
        }
        return candidates.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static async Task<int> StartCoreAsync(GameBiz biz, string executable, string? arguments)
    {
        string[] candidates = GetCandidates(GetRegisteredRoots(biz));
        if (candidates.Length == 0)
            throw CreateFailure(Failure.MissingLauncher);
        bool global = biz.Value == GameBiz.endfield_global;
        string[] trusted = candidates.Where(path => OfficialLauncherSignature.Verify(path, global)).ToArray();
        if (trusted.Length == 0)
            throw CreateFailure(Failure.InvalidSignature);

        using var parent = await GetParentAsync(trusted, global);
        string parentPath = LauncherNativeProcess.GetPath(parent);
        // Lock the selected image against replacement through verification and creation.
        using var image = File.Open(parentPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (!OfficialLauncherSignature.Verify(parentPath, global))
            throw CreateFailure(Failure.InvalidSignature);
        return LauncherNativeProcess.Start(parent, Path.GetFullPath(executable), arguments);
    }

    private static async Task<SafeProcessHandle> GetParentAsync(string[] candidates, bool global)
    {
        var parent = FindParent(candidates);
        if (parent != null)
            return parent;

        using (var image = File.Open(candidates[0], FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (!OfficialLauncherSignature.Verify(candidates[0], global))
                throw CreateFailure(Failure.InvalidSignature);
            using var started = Process.Start(new ProcessStartInfo(candidates[0])
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(candidates[0]),
                WindowStyle = ProcessWindowStyle.Hidden,
            });
        }

        for (int attempt = 0; attempt < 60; attempt++)
        {
            await Task.Delay(250);
            parent = FindParent(candidates);
            if (parent != null)
                return parent;
        }
        throw CreateFailure(Failure.LauncherTimeout);
    }

    private static SafeProcessHandle? FindParent(string[] candidates)
    {
        using var current = Process.GetCurrentProcess();
        var processes = Process.GetProcessesByName("Games");
        try
        {
            foreach (var process in processes)
            {
                SafeProcessHandle? handle = null;
                bool matched = false;
                try
                {
                    if (process.SessionId != current.SessionId) continue;
                    using var query = LauncherNativeProcess.Open(process.Id);
                    if (!candidates.Contains(LauncherNativeProcess.GetPath(query), StringComparer.OrdinalIgnoreCase)) continue;
                    matched = true;
                    handle = LauncherNativeProcess.Open(process.Id, forChildCreation: true);
                    // Check the creation handle too in case the enumerated PID exited.
                    if (!candidates.Contains(LauncherNativeProcess.GetPath(handle), StringComparer.OrdinalIgnoreCase)) continue;
                    var result = handle;
                    handle = null;
                    return result;
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 5 && matched)
                {
                    throw CreateFailure(Failure.AccessDenied);
                }
                catch (Win32Exception) { /* Inaccessible or exiting unrelated process. */ }
                catch (InvalidOperationException) { /* Process exited during discovery. */ }
                finally { handle?.Dispose(); }
            }
        }
        finally { foreach (var process in processes) process.Dispose(); }
        return null;
    }

    internal static Exception CreateFailure(Failure reason) => reason == Failure.Cancelled
        ? new Win32Exception(1223)
        : new LaunchException(reason, reason switch
        {
            Failure.MissingLauncher => "未找到此区服的官方启动器。请先安装并运行鹰角启动器（国服）或 GRYPHLINK（国际服），再通过 Nebula 启动终末地。",
            Failure.InvalidSignature => "官方启动器 Games.exe 的数字签名或发行者验证失败，请使用官方安装包修复启动器。",
            Failure.AccessDenied => "无法访问官方启动器进程，请确认允许 Nebula 的管理员权限请求后重试。",
            Failure.LauncherTimeout => "等待官方启动器超时，请手动打开对应区服的官方启动器后重试。",
            Failure.InvalidRequest => "终末地游戏路径或启动请求无效，请重新定位游戏目录。",
            _ => "启动终末地失败。请查看 Nebula 日志目录中的 endfield-launch.log，并确认官方启动器可以正常启动游戏。",
        });

    private sealed class LaunchException(Failure reason, string message) : Exception(message)
    {
        internal Failure Reason { get; } = reason;
    }
}
