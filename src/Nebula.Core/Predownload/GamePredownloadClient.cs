using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nebula.Core.Hypergryph;
using Nebula.Core.Kuro;

namespace Nebula.Core.Predownload;

/// <summary>鹰角和库洛官方 PC 预下载源。HoYoPlay 保留原有分块/差分下载流程。</summary>
public sealed class GamePredownloadClient(HttpClient httpClient)
{
    public async Task<GamePredownloadPlan?> GetAsync(GameBiz gameBiz, string installPath,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            return await GetCoreAsync(gameBiz, installPath, timeout.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            throw new TimeoutException("Checking predownload resources timed out.", ex);
        }
    }

    private async Task<GamePredownloadPlan?> GetCoreAsync(GameBiz gameBiz, string installPath, CancellationToken cancellationToken)
    {
        if (HypergryphGameUpdateLinks.GetManifestUrl(gameBiz) is not null)
        {
            string? localVersion = await ReadHypergryphVersionAsync(installPath, cancellationToken);
            // 增量包依赖安装版本；无法读取版本时不能请求一份猜测的补丁。
            if (localVersion is null)
                return null;
            using var json = await GetJsonAsync(HypergryphGameUpdateLinks.GetManifestUrl(gameBiz, localVersion)!, cancellationToken);
            return ParseHypergryph(gameBiz, localVersion, json.RootElement);
        }
        if (KuroGameUpdateLinks.Get(gameBiz)?.ManifestUrl is string url)
            return await GetKuroAsync(gameBiz, url, cancellationToken);
        return null;
    }

    private static GamePredownloadPlan? ParseHypergryph(GameBiz gameBiz, string localVersion, JsonElement root)
    {
        var pre = Property(root, "pre_patch");
        string version = String(pre, "version");
        if (!IsNewer(version, String(root, "version")) || !IsNewer(version, localVersion))
            return null;
        var files = new List<GamePredownloadFile>();
        var patches = Property(pre, "patches");
        if (patches.ValueKind == JsonValueKind.Array && patches.GetArrayLength() > 0)
        {
            foreach (var patch in patches.EnumerateArray())
                files.Add(ParseHypergryphFile(patch));
        }
        else if (!string.IsNullOrWhiteSpace(String(pre, "url")))
        {
            files.Add(ParseHypergryphFile(pre));
        }
        return files.Count == 0 ? null : new(gameBiz, version, files);
    }

    private static GamePredownloadFile ParseHypergryphFile(JsonElement pack)
    {
        string url = String(pack, "url");
        if (!GamePredownloadPlan.IsHttpsUrl(url))
            throw new InvalidDataException("Invalid Hypergryph predownload URL.");
        string path = Uri.UnescapeDataString(new Uri(url).AbsolutePath.Split('/').Last());
        return new(path, url, Size(pack, "package_size"), String(pack, "md5"));
    }

    private async Task<GamePredownloadPlan?> GetKuroAsync(GameBiz gameBiz, string url, CancellationToken token)
    {
        using var json = await GetJsonAsync(url, token);
        var root = json.RootElement;
        var pre = Property(root, "predownload");
        var current = Property(root, "default");
        // 开关不代表资源已发布；只接受明确的未来版本和预下载资源清单。
        if (String(root, "predownloadSwitch") == "0" || Property(root, "predownloadSwitch").ValueKind == JsonValueKind.False
            || !IsNewer(String(pre, "version"), String(current, "version")))
            return null;
        string resource = String(pre, "resources"), basePath = String(pre, "resourcesBasePath");
        if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(basePath))
            return null;
        var cdns = Property(pre, "cdnList");
        if (cdns.ValueKind != JsonValueKind.Array || cdns.GetArrayLength() == 0)
            cdns = Property(current, "cdnList");
        if (cdns.ValueKind != JsonValueKind.Array)
            return null;
        string? cdn = cdns.EnumerateArray().Select(x => String(x, "url"))
            .FirstOrDefault(GamePredownloadPlan.IsHttpsUrl);
        if (cdn is null)
            return null;
        string resourcesUrl = CombineUrl(cdn, resource);
        string downloadBase = CombineUrl(cdn, basePath).TrimEnd('/') + "/";
        using var resources = await GetJsonAsync(resourcesUrl, token);
        var entries = Property(resources.RootElement, "resource");
        if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() == 0)
            return null;
        var files = new List<GamePredownloadFile>();
        foreach (var item in entries.EnumerateArray())
        {
            // 旧版鸣潮清单的 dest 以单个 / 开头，语义仍是相对资源目录的路径。
            string destination = String(item, "dest");
            if (destination.StartsWith('/'))
                destination = destination[1..];
            string path = GamePredownloadPlan.ValidateRelativePath(destination);
            string escaped = string.Join('/', path.Split('/').Select(Uri.EscapeDataString));
            files.Add(new(path, downloadBase + escaped, Size(item, "size"), String(item, "md5")));
        }
        return new(gameBiz, String(pre, "version"), files);
    }

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken token)
    {
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        // 部分库洛 CDN 即使请求未携带 Accept-Encoding，也返回 gzip。
        // HttpClient 自动解压时会移除此头，因此不会重复解压。
        Stream decoded = stream;
        foreach (string encoding in response.Content.Headers.ContentEncoding.Reverse())
        {
            decoded = encoding.ToLowerInvariant() switch
            {
                "gzip" => new GZipStream(decoded, CompressionMode.Decompress),
                "deflate" => new DeflateStream(decoded, CompressionMode.Decompress),
                "br" => new BrotliStream(decoded, CompressionMode.Decompress),
                "identity" => decoded,
                _ => throw new InvalidDataException("Unsupported predownload manifest encoding."),
            };
        }
        await using (decoded)
            return await JsonDocument.ParseAsync(decoded, cancellationToken: token);
    }

    private static string CombineUrl(string cdn, string path)
    {
        if (GamePredownloadPlan.IsHttpsUrl(path))
            return path;
        path = GamePredownloadPlan.ValidateRelativePath(path.TrimEnd('/'));
        return new Uri(new Uri(cdn.TrimEnd('/') + "/"), path).AbsoluteUri;
    }

    private static bool IsNewer(string version, string baseline) => Version.TryParse(version, out var target)
        && Version.TryParse(baseline, out var current) && target > current;

    private static JsonElement Property(JsonElement value, string name) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var result) ? result : default;

    private static string String(JsonElement value, string name)
    {
        var result = Property(value, name);
        return result.ValueKind is JsonValueKind.String or JsonValueKind.Number ? result.ToString() : "";
    }

    private static long Size(JsonElement value, string name) =>
        long.TryParse(String(value, name), NumberStyles.None, CultureInfo.InvariantCulture, out long size) && size >= 0
            ? size : throw new InvalidDataException("Invalid predownload size.");

    public static async Task<string?> ReadHypergryphVersionAsync(string installPath, CancellationToken token = default)
    {
        string path = Path.Combine(installPath, "config.ini");
        if (!File.Exists(path) || new FileInfo(path).Length > 1024 * 1024)
            return null;
        byte[] bytes = await File.ReadAllBytesAsync(path, token);
        string? version = ParseVersion(Encoding.UTF8.GetString(bytes));
        if (version is not null)
            return version;
        try
        {
            // 官方启动器 config.ini 的公开固定文件格式；只读，不回写配置。
            using var aes = Aes.Create();
            aes.Key = Convert.FromHexString("C0F30E1CE763BBC21CC355A34303AC50399444BFF68C4A22AF398C0A166EE143");
            byte[] iv = Convert.FromHexString("33467861192750649501937264608400");
            return ParseVersion(Encoding.UTF8.GetString(aes.DecryptCbc(bytes, iv)));
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private static string? ParseVersion(string content)
    {
        foreach (string line in content.Split('\n'))
        {
            var entry = line.Trim().Split('=', 2, StringSplitOptions.TrimEntries);
            if (entry.Length == 2 && entry[0] == "version" && Version.TryParse(entry[1], out var version))
                return version.ToString();
        }
        return null;
    }
}
