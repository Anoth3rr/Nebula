using Nebula.Setup.Core;
using Nebula.Setup.Core.Github;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

var tests = new List<(string Name, Func<Task> Run)>();
void Test(string name, Action action) => tests.Add((name, () => { action(); return Task.CompletedTask; }));
void TestAsync(string name, Func<Task> action) => tests.Add((name, action));
void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}.");
}
async Task Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}
GithubRelease Release(string tag, bool preview = false, bool draft = false) => new()
{
    TagName = tag, Prerelease = preview, Draft = draft, Assets = [],
    HtmlUrl = $"https://github.com/Anoth3rr/Nebula/releases/tag/{tag}",
};
GithubAsset Asset(string name, string? digest = null) => new()
{
    Name = name, Size = 100, State = "uploaded", Digest = digest ?? "sha256:" + new string('a', 64),
    BrowserDownloadUrl = $"https://github.com/Anoth3rr/Nebula/releases/download/v1.2.0/{name}",
};

Test("stable channel ignores draft and prerelease tags", () =>
{
    var values = new[] { Release("v9.0.0", draft: true), Release("v3.0.0-preview.1"), Release("2.1.0", preview: true), Release("v2.0.0"), Release("nonsense") };
    Equal("v2.0.0", GithubReleaseSource.SelectLatest(values, false)?.TagName);
});
Test("preview channel includes the newer stable version", () =>
    Equal("v2.0.0", GithubReleaseSource.SelectLatest([Release("v2.0.0-rc.1", true), Release("v2.0.0")], true)?.TagName));
Test("semantic versions sort numerically", () =>
    Equal("v1.10.0", GithubReleaseSource.SelectLatest([Release("v1.9.0"), Release("v1.10.0")], false)?.TagName));
Test("preview channel accepts newer prerelease", () =>
    Equal("v2.0.0-beta.1", GithubReleaseSource.SelectLatest([Release("1.0.0"), Release("v2.0.0-beta.1", true)], true)?.TagName));
Test("empty or invalid releases have no latest version", () =>
    Equal<GithubRelease?>(null, GithubReleaseSource.SelectLatest([Release("assets")], false)));
Test("select exact architecture and package type", () =>
{
    var release = Release("v1.2.0");
    release.Assets = [Asset("Nebula_Setup_1.2.0_arm64.exe"), Asset("Nebula_Portable_1.2.0_x64.zip")];
    var info = GithubReleaseSource.ToReleaseInfo(release);
    Equal(2, info.Releases.Count);
    Equal(false, info.Releases["x64-portable"].DisableAutoUpdate);
    Equal("Nebula_Setup_1.2.0_arm64.exe", info.Releases["arm64-setup"].Setup?.FileName);
    Equal(release.HtmlUrl, info.Releases["x64-portable"].ReleaseUrl);
    Equal(false, info.TryGetReleaseInfoDetail(Architecture.X64, InstallType.Setup, out _));
});
Test("missing checksum requires manual download", () =>
{
    var release = Release("v1.2.0");
    release.Assets = [Asset("Nebula_Setup_1.2.0_x64.exe", "")];
    var detail = GithubReleaseSource.ToReleaseInfo(release).Releases["x64-setup"];
    Equal(true, detail.DisableAutoUpdate);
    Equal<ReleaseSetup?>(null, detail.Setup);
});
Test("legacy 7z packages remain available as manual downloads", () =>
{
    var release = Release("v1.2.0");
    release.Assets = [Asset("Nebula_Portable_1.2.0_x64.7z")];
    Equal(true, GithubReleaseSource.ToReleaseInfo(release).Releases["x64-portable"].DisableAutoUpdate);
});
TestAsync("reject package URLs outside this repository", async () =>
{
    var release = Release("v1.2.0");
    var asset = Asset("Nebula_Setup_1.2.0_x64.exe");
    asset.BrowserDownloadUrl = "https://github.com/other/project/releases/download/v1/update.exe";
    release.Assets = [asset];
    await Throws<FormatException>(() => Task.FromResult(GithubReleaseSource.ToReleaseInfo(release)));
});
TestAsync("unpublished repository has a distinct result", async () =>
{
    using var client = new HttpClient(new Handler((request, _) =>
    {
        Equal(true, request.Headers.UserAgent.Count > 0);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });
    }));
    await Throws<ReleaseNotFoundException>(() => new ReleaseClient(client).GetLatestReleaseInfoAsync(false, "1.0.0"));
});
TestAsync("GitHub errors do not become an up-to-date result", async () =>
{
    using var client = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden))));
    await Throws<HttpRequestException>(() => new ReleaseClient(client).GetLatestReleaseInfoAsync(false, "1.0.0"));
});
TestAsync("release checks pass cancellation through HTTP", async () =>
{
    using var cts = new CancellationTokenSource();
    using var client = new HttpClient(new Handler(async (_, token) =>
    {
        cts.Cancel();
        await Task.Delay(Timeout.Infinite, token);
        return new HttpResponseMessage(HttpStatusCode.OK);
    }));
    await Throws<OperationCanceledException>(() => new ReleaseClient(client).GetLatestReleaseInfoAsync(false, "1.0.0", cts.Token));
});

byte[] payload = Encoding.UTF8.GetBytes("Nebula verified update package contents");
string hash = Convert.ToHexString(SHA256.HashData(payload));
foreach (bool useRange in new[] { false, true })
{
    TestAsync($"resume download when server {(useRange ? "honors" : "ignores")} Range", async () =>
    {
        using var temp = new TempFolder();
        string path = Path.Combine(temp.Path, "package");
        await File.WriteAllBytesAsync(path, payload[..8]);
        using var client = new HttpClient(new Handler((request, _) =>
        {
            Equal(8L, request.Headers.Range?.Ranges.Single().From);
            var response = new HttpResponseMessage(useRange ? HttpStatusCode.PartialContent : HttpStatusCode.OK)
            { Content = new ByteArrayContent(useRange ? payload[8..] : payload) };
            if (useRange) response.Content.Headers.ContentRange = new ContentRangeHeaderValue(8, payload.Length - 1, payload.Length);
            return Task.FromResult(response);
        }));
        await UpdatePackage.DownloadAsync(client, "https://example.test/package", path, payload.Length, hash);
        Equal(hash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))));
    });
}
TestAsync("corrupt cached package is redownloaded", async () =>
{
    using var temp = new TempFolder();
    string path = Path.Combine(temp.Path, "package");
    await File.WriteAllBytesAsync(path, new byte[payload.Length]);
    int requests = 0;
    using var client = new HttpClient(new Handler((request, _) =>
    {
        requests++;
        Equal<RangeHeaderValue?>(null, request.Headers.Range);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) });
    }));
    await UpdatePackage.DownloadAsync(client, "https://example.test/package", path, payload.Length, hash);
    Equal(1, requests);
});
TestAsync("repeated hash mismatch fails and clears corrupt data", async () =>
{
    using var temp = new TempFolder();
    string path = Path.Combine(temp.Path, "package");
    int requests = 0;
    using var client = new HttpClient(new Handler((_, _) =>
    {
        requests++;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[payload.Length]) });
    }));
    await Throws<InvalidDataException>(() => UpdatePackage.DownloadAsync(client, "https://example.test/package", path, payload.Length, hash));
    Equal(3, requests);
    Equal(0L, new FileInfo(path).Length);
});
TestAsync("invalid range is rejected", async () =>
{
    using var temp = new TempFolder();
    string path = Path.Combine(temp.Path, "package");
    await File.WriteAllBytesAsync(path, payload[..8]);
    using var client = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.PartialContent)
    { Content = new ByteArrayContent(payload) })));
    await Throws<InvalidDataException>(() => UpdatePackage.DownloadAsync(client, "https://example.test/package", path, payload.Length, hash));
});
TestAsync("cancelled download preserves the partial file", async () =>
{
    using var temp = new TempFolder();
    string path = Path.Combine(temp.Path, "package");
    await File.WriteAllBytesAsync(path, payload[..8]);
    using var cts = new CancellationTokenSource();
    cts.Cancel();
    using var client = new HttpClient();
    await Throws<OperationCanceledException>(() => UpdatePackage.DownloadAsync(client, "https://example.test/package", path, payload.Length, hash, cancellationToken: cts.Token));
    Equal(8L, new FileInfo(path).Length);
});

async Task<string> Package(string root, Dictionary<string, string>? extra = null)
{
    string path = Path.Combine(root, "update.zip");
    using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
    var files = new Dictionary<string, string>
    {
        ["Nebula.exe"] = "new launcher", ["version.ini"] = "version=1.2.0\n",
        ["app-1.2.0/Nebula.exe"] = "new app", ["app-1.2.0/lib.dll"] = "library",
    };
    foreach (var item in extra ?? []) files[item.Key] = item.Value;
    foreach (var item in files)
    {
        using var writer = new StreamWriter(zip.CreateEntry(item.Key).Open());
        await writer.WriteAsync(item.Value);
    }
    return path;
}
TestAsync("portable upgrade keeps old version and user data", async () =>
{
    using var temp = new TempFolder();
    string target = Path.Combine(temp.Path, "installed");
    Directory.CreateDirectory(Path.Combine(target, "app-1.0.0"));
    await File.WriteAllTextAsync(Path.Combine(target, "Nebula.exe"), "old launcher");
    await File.WriteAllTextAsync(Path.Combine(target, "version.ini"), "version=1.0.0");
    await File.WriteAllTextAsync(Path.Combine(target, "config.ini"), "user config");
    await File.WriteAllTextAsync(Path.Combine(target, "app-1.0.0/Nebula.exe"), "old app");
    await UpdatePackage.InstallPortableAsync(await Package(temp.Path), target, "1.2.0");
    Equal("version=1.2.0", (await File.ReadAllTextAsync(Path.Combine(target, "version.ini"))).Trim());
    Equal("new app", await File.ReadAllTextAsync(Path.Combine(target, "app-1.2.0/Nebula.exe")));
    Equal("old app", await File.ReadAllTextAsync(Path.Combine(target, "app-1.0.0/Nebula.exe")));
    Equal("user config", await File.ReadAllTextAsync(Path.Combine(target, "config.ini")));
});
foreach (string entry in new[] { "app-1.2.0/../../outside.txt", "config.ini", "app-1.2.0/C:/outside.txt", "app-1.2.0/.. /.. /outside.txt" })
{
    TestAsync($"reject unsafe package entry {entry}", async () =>
    {
        using var temp = new TempFolder();
        string target = Path.Combine(temp.Path, "installed");
        Directory.CreateDirectory(target);
        await File.WriteAllTextAsync(Path.Combine(target, "Nebula.exe"), "old launcher");
        string archive = await Package(temp.Path, new() { [entry] = "bad" });
        await Throws<InvalidDataException>(() => UpdatePackage.InstallPortableAsync(archive, target, "1.2.0"));
        Equal("old launcher", await File.ReadAllTextAsync(Path.Combine(target, "Nebula.exe")));
        Equal(false, File.Exists(Path.Combine(temp.Path, "outside.txt")));
    });
}
TestAsync("mismatched version does not change installed files", async () =>
{
    using var temp = new TempFolder();
    string path = await Package(temp.Path, new() { ["version.ini"] = "version=9.0.0" });
    await Throws<InvalidDataException>(() => UpdatePackage.InstallPortableAsync(path, Path.Combine(temp.Path, "installed"), "1.2.0"));
    Equal(false, File.Exists(Path.Combine(temp.Path, "installed/Nebula.exe")));
});
TestAsync("file replacement failure rolls back earlier replacements", async () =>
{
    using var temp = new TempFolder();
    string source = Path.Combine(temp.Path, "source");
    string target = Path.Combine(temp.Path, "target");
    Directory.CreateDirectory(source);
    Directory.CreateDirectory(Path.Combine(target, "b.dll")); // Directory conflicts with the second package file.
    await File.WriteAllTextAsync(Path.Combine(source, "a.dll"), "new a");
    await File.WriteAllTextAsync(Path.Combine(source, "b.dll"), "new b");
    await File.WriteAllTextAsync(Path.Combine(source, "version.ini"), "new version");
    await File.WriteAllTextAsync(Path.Combine(target, "a.dll"), "old a");
    await File.WriteAllTextAsync(Path.Combine(target, "version.ini"), "old version");
    await Throws<IOException>(() => Task.Run(() => UpdatePackage.ApplyDirectory(source, target)));
    Equal("old a", await File.ReadAllTextAsync(Path.Combine(target, "a.dll")));
    Equal("old version", await File.ReadAllTextAsync(Path.Combine(target, "version.ini")));
});

int failed = 0;
foreach (var test in tests)
{
    try { await test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception error) { failed++; Console.Error.WriteLine($"FAIL {test.Name}: {error}"); }
}
Console.WriteLine($"{tests.Count - failed}/{tests.Count} passed");
return failed == 0 ? 0 : 1;

sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
}

sealed class TempFolder : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Nebula.Update.Tests-{Guid.NewGuid():N}");
    public TempFolder() => Directory.CreateDirectory(Path);
    public void Dispose() => Directory.Delete(Path, true);
}
