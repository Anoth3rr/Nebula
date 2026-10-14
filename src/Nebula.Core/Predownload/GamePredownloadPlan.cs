using System.Security.Cryptography;
using System.Text;

namespace Nebula.Core.Predownload;

public sealed record GamePredownloadFile(string RelativePath, string Url, long Size, string MD5);

/// <summary>只包含预下载资源，不会写入现有游戏文件或更改已安装版本。</summary>
public sealed class GamePredownloadPlan
{
    public GameBiz GameBiz { get; }
    public string Version { get; }
    public IReadOnlyList<GamePredownloadFile> Files { get; }
    public long DownloadSize { get; }
    public string Fingerprint { get; }

    public GamePredownloadPlan(GameBiz gameBiz, string version, IEnumerable<GamePredownloadFile> files)
    {
        if (!gameBiz.IsKnown() || !System.Version.TryParse(version, out _))
            throw new InvalidDataException("Invalid predownload game or version.");
        GameBiz = gameBiz;
        Version = version;
        var validated = files.Select(file => file with
        {
            RelativePath = ValidateRelativePath(file.RelativePath),
            MD5 = NormalizeMD5(file.MD5),
        }).OrderBy(file => file.RelativePath, StringComparer.Ordinal).ToArray();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in validated)
        {
            if (file.Size < 0 || !IsHttpsUrl(file.Url) || !paths.Add(file.RelativePath))
                throw new InvalidDataException("Invalid or duplicate predownload file.");
            DownloadSize = checked(DownloadSize + file.Size);
        }
        foreach (var file in validated)
        {
            // 下载临时文件和目录不能与清单中的另一个文件重叠。
            if (paths.Contains(file.RelativePath + "_tmp"))
                throw new InvalidDataException("Conflicting predownload temporary file.");
            var parent = file.RelativePath;
            while (parent.LastIndexOf('/') is int slash && slash > 0)
            {
                parent = parent[..slash];
                if (paths.Contains(parent) || paths.Contains(parent + "_tmp"))
                    throw new InvalidDataException("Conflicting predownload directory.");
            }
        }
        if (validated.Length == 0 || DownloadSize <= 0)
            throw new InvalidDataException("Empty predownload package.");
        Files = Array.AsReadOnly(validated);
        // 签名 URL 会轮换；内容标识仅取版本、路径、长度和校验值。
        string identity = $"{gameBiz}\n{version}\n" + string.Join('\n', validated.Select(
            file => $"{file.RelativePath}\t{file.Size}\t{file.MD5}"));
        Fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    public static bool IsHttpsUrl(string? url) => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo);

    public static string NormalizeMD5(string value)
    {
        // 库洛清单以十六进制整数输出 MD5，可能省略前导 0。
        if (value.Length is < 1 or > 32 || value.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidDataException("Invalid predownload checksum.");
        return value.PadLeft(32, '0').ToLowerInvariant();
    }

    public static string ValidateRelativePath(string path)
    {
        path = path.Replace('\\', '/');
        var parts = path.Split('/');
        if (parts.Any(part => string.IsNullOrWhiteSpace(part) || part is "." or ".."
            || part.EndsWith('.') || part.EndsWith(' ')
            || part.Any(c => c < 32 || "<>:\"|?*".Contains(c))
            || IsDeviceName(part)))
            throw new InvalidDataException("Unsafe predownload path.");
        return path;
    }

    private static bool IsDeviceName(string part)
    {
        var name = part.Split('.')[0].ToUpperInvariant();
        return name is "CON" or "PRN" or "AUX" or "NUL"
            || name.Length == 4 && (name.StartsWith("COM") || name.StartsWith("LPT"))
                && name[3] is >= '0' and <= '9';
    }
}
