using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Nebula.Features.GameLauncher;

internal static class OfficialLauncherSignature
{
    internal static bool Verify(string path, bool global)
    {
        if (!File.Exists(path) || !Path.GetFileName(path).Equals("Games.exe", StringComparison.OrdinalIgnoreCase))
            return false;

        var file = new WinTrustFileInfo { Size = (uint)Marshal.SizeOf<WinTrustFileInfo>(), FilePath = path };
        nint filePointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>());
        Marshal.StructureToPtr(file, filePointer, false);
        var data = new WinTrustData
        {
            Size = (uint)Marshal.SizeOf<WinTrustData>(),
            UiChoice = 2, // WTD_UI_NONE
            UnionChoice = 1, // WTD_CHOICE_FILE
            FileInfo = filePointer,
            StateAction = 1, // WTD_STATEACTION_VERIFY
            ProviderFlags = 0x80, // WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT; honor timestamped signatures
        };
        var action = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
        try
        {
            if (WinVerifyTrust(new nint(-1), ref action, ref data) != 0)
                return false;
            // The loader APIs do not extract Authenticode signers from PE files.
#pragma warning disable SYSLIB0057
            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
#pragma warning restore SYSLIB0057
            string expected = global ? "GRYPH FRONTIER PTE. LTD." : "Shanghai Hypergryph Network Technology Co., Ltd.";
            return string.Equals(certificate.GetNameInfo(X509NameType.SimpleName, false), expected, StringComparison.OrdinalIgnoreCase);
        }
        catch (CryptographicException)
        {
            return false;
        }
        finally
        {
            data.StateAction = 2; // WTD_STATEACTION_CLOSE, including when verification failed
            WinVerifyTrust(new nint(-1), ref action, ref data);
            Marshal.DestroyStructure<WinTrustFileInfo>(filePointer);
            Marshal.FreeHGlobal(filePointer);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustFileInfo
    {
        public uint Size;
        [MarshalAs(UnmanagedType.LPWStr)] public string FilePath;
        public nint FileHandle, KnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustData
    {
        public uint Size;
        public nint PolicyCallbackData, SipClientData;
        public uint UiChoice, RevocationChecks, UnionChoice;
        public nint FileInfo;
        public uint StateAction;
        public nint StateData, UrlReference;
        public uint ProviderFlags, UiContext;
        public nint SignatureSettings;
    }

    [DllImport("wintrust.dll", ExactSpelling = true)]
    private static extern int WinVerifyTrust(nint window, ref Guid action, ref WinTrustData data);
}
