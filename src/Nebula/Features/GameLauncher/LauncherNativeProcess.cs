using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Nebula.Features.GameLauncher;

/// <summary>Creates a game with the installed launcher's process as its parent.</summary>
internal static class LauncherNativeProcess
{
    internal static SafeProcessHandle Open(int processId, bool forChildCreation = false)
    {
        const uint QueryLimitedInformation = 0x1000;
        const uint CreateProcessAccess = 0x0080;
        var handle = OpenProcess(QueryLimitedInformation | (forChildCreation ? CreateProcessAccess : 0), false, processId);
        if (!handle.IsInvalid)
            return handle;
        int error = Marshal.GetLastWin32Error();
        handle.Dispose();
        throw new Win32Exception(error);
    }

    internal static string GetPath(SafeProcessHandle process)
    {
        var buffer = new StringBuilder(32768);
        int length = buffer.Capacity;
        if (!QueryFullProcessImageNameW(process, 0, buffer, ref length))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return buffer.ToString();
    }

    internal static string BuildCommandLine(string executable, string? arguments)
    {
        if (!Path.IsPathFullyQualified(executable) || executable.IndexOfAny(['"', '\0']) >= 0
            || arguments?.Contains('\0') is true)
            throw new ArgumentException("游戏路径或启动参数无效。");
        string command = $"\"{executable}\"" + (string.IsNullOrWhiteSpace(arguments) ? "" : $" {arguments}");
        if (command.Length >= 32767)
            throw new ArgumentException("游戏启动参数过长。");
        return command;
    }

    internal static int Start(SafeProcessHandle parent, string executable, string? arguments)
    {
        var command = new StringBuilder(BuildCommandLine(executable, arguments));
        nint attributes = 0, parentValue = 0;
        bool initialized = false, parentReferenced = false;
        ProcessInformation created = default;
        try
        {
            parent.DangerousAddRef(ref parentReferenced);
            nuint bytes = 0;
            InitializeProcThreadAttributeList(0, 1, 0, ref bytes);
            if (bytes == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            attributes = Marshal.AllocHGlobal(checked((int)bytes));
            if (!InitializeProcThreadAttributeList(attributes, 1, 0, ref bytes))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            initialized = true;
            parentValue = Marshal.AllocHGlobal(nint.Size);
            Marshal.WriteIntPtr(parentValue, parent.DangerousGetHandle());
            const nuint ParentProcessAttribute = 0x00020000;
            if (!UpdateProcThreadAttribute(attributes, 0, ParentProcessAttribute, parentValue, (nuint)nint.Size, 0, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            var startup = new StartupInfoEx
            {
                StartupInfo = new StartupInfo { Size = Marshal.SizeOf<StartupInfoEx>() },
                AttributeList = attributes,
            };
            const uint ExtendedStartupInfoPresent = 0x00080000;
            const uint CreateNoWindow = 0x08000000;
            if (!CreateProcessW(executable, command, 0, 0, false, ExtendedStartupInfoPresent | CreateNoWindow,
                0, Path.GetDirectoryName(executable)!, ref startup, out created))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return created.ProcessId;
        }
        finally
        {
            if (created.Thread != 0) CloseHandle(created.Thread);
            if (created.Process != 0) CloseHandle(created.Process);
            if (initialized) DeleteProcThreadAttributeList(attributes);
            if (attributes != 0) Marshal.FreeHGlobal(attributes);
            if (parentValue != 0) Marshal.FreeHGlobal(parentValue);
            if (parentReferenced) parent.DangerousRelease();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfo
    {
        public int Size;
        public nint Reserved, Desktop, Title;
        public uint X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
        public ushort ShowWindow, ReservedSize;
        public nint ReservedBytes, StandardInput, StandardOutput, StandardError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfoEx
    {
        public StartupInfo StartupInfo;
        public nint AttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public nint Process, Thread;
        public int ProcessId, ThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageNameW(SafeProcessHandle process, uint flags, [Out] StringBuilder name, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InitializeProcThreadAttributeList(nint list, int count, uint flags, ref nuint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateProcThreadAttribute(nint list, uint flags, nuint attribute, nint value, nuint size, nint previous, nint returned);

    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(nint list);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessW(string application, StringBuilder command, nint processAttributes,
        nint threadAttributes, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint flags, nint environment,
        string directory, ref StartupInfoEx startup, out ProcessInformation information);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);
}
