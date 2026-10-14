using System.Net;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nebula.Core;
using Nebula.Core.HoYoPlay;
using Nebula.Core.Hypergryph;
using Nebula.Core.Predownload;

int passed = 0;
string testRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Nebula.Predownload.Tests", Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(testRoot);
string configPath = Path.Combine(testRoot, "config.ini");
await File.WriteAllTextAsync(configPath, "[game]\nversion=1.5.3\n");
byte[] payload = Encoding.UTF8.GetBytes("verified predownload bytes");
string md5 = Convert.ToHexStringLower(MD5.HashData(payload));
var file = new GamePredownloadFile("patch.zip.001", "https://cdn.example.test/patch.zip.001", payload.Length, md5);

try
{
    await Test("Channels keep their own endpoints", () =>
    {
        Assert(HypergryphGameUpdateLinks.GetManifestUrl(GameBiz.arknights_bilibili)!.Contains("channel=2&sub_channel=2"));
        Assert(HypergryphGameUpdateLinks.GetManifestUrl(GameBiz.endfield_global)!.Contains("launcher.gryphline.com"));
        Assert(HypergryphGameUpdateLinks.GetManifestUrl(GameBiz.endfield_global)!.Contains("channel=6&sub_channel=6"));
        Assert(HypergryphGameUpdateLinks.GetManifestUrl(GameBiz.arknights_global) is null);
        Assert(GameUpdateLinks.Get(GameId.FromGameBiz(GameBiz.arknights_global)!) is null);
        Assert(GameUpdateLinks.Get(GameId.FromGameBiz(GameBiz.endfield_cn)!)!.ManifestUrl!
            .Contains("appcode=6LL0KJuqHBVz33WK"));
        return Task.CompletedTask;
    });
    await Test("HoYo compressed packages require actual future resources", () =>
    {
        var package = HoyoPackage();
        Assert(HoYoPlayPredownload.IsAvailable(package, new Version("1.0.0")));
        Assert(!HoYoPlayPredownload.IsAvailable(package, new Version("2.0.0")));
        package.PreDownload.Major!.GamePackages.Clear();
        Assert(!HoYoPlayPredownload.IsAvailable(package, new Version("1.0.0")));
        package.PreDownload.Major = null;
        Assert(!HoYoPlayPredownload.IsAvailable(package, new Version("1.0.0")));
        return Task.CompletedTask;
    });
    await Test("HoYo does not mistake an already released version for preload", () =>
    {
        var package = HoyoPackage();
        package.Main.Major!.Version = "2.0.0";
        Assert(!HoYoPlayPredownload.IsAvailable(package, new Version("1.0.0")));
        return Task.CompletedTask;
    });
    await Test("HoYo chunk branch needs a package and future tag", () =>
    {
        var branch = new GameBranch
        {
            Main = new() { Tag = "1.0.0" },
            PreDownload = new() { Tag = "2.0.0", PackageId = "future", Branch = "predownload" },
        };
        Assert(HoYoPlayPredownload.IsAvailable(branch, new Version("1.0.0")));
        branch.PreDownload.PackageId = "";
        Assert(!HoYoPlayPredownload.IsAvailable(branch, new Version("1.0.0")));
        branch.PreDownload = null;
        Assert(!HoYoPlayPredownload.IsAvailable(branch, new Version("1.0.0")));
        return Task.CompletedTask;
    });
    await Test("HG multipart pre_patch uses local version and ignores current packages", async () =>
    {
        foreach (var biz in new[] { GameBiz.arknights_cn, GameBiz.arknights_bilibili, GameBiz.endfield_cn, GameBiz.endfield_global })
        {
            using var http = Http((request, _) =>
            {
                Assert(request.RequestUri!.Query.Contains("version=1.5.3"));
                return Json(HgReply());
            });
            var plan = await new GamePredownloadClient(http).GetAsync(biz, testRoot);
            Assert(plan?.Files.Count == 1 && plan.Version == "1.6.0" && plan.DownloadSize == payload.Length);
            Assert(plan!.Files[0].Url == file.Url);
        }
    });
    await Test("HG single predownload archive is supported", async () =>
    {
        using var http = Http((_, _) => Json(JsonSerializer.Serialize(new
        {
            version = "1.5.3",
            pre_patch = new { version = "1.6.0", url = file.Url, md5, package_size = payload.Length.ToString() },
        })));
        Assert((await new GamePredownloadClient(http).GetAsync(GameBiz.endfield_cn, testRoot))?.Files.Count == 1);
    });
    await Test("HG empty, unavailable and released pre_patch remain hidden", async () =>
    {
        foreach (string json in new[] { "{\"version\":\"1.5.3\",\"pre_patch\":null}",
            "{\"version\":\"1.5.3\",\"pre_patch\":{\"version\":\"1.6.0\",\"patches\":[]}}", HgReply("1.5.3") })
        {
            using var http = Http((_, _) => Json(json));
            Assert(await new GamePredownloadClient(http).GetAsync(GameBiz.endfield_cn, testRoot) is null);
        }
    });
    await Test("HG encrypted local config is read without modifying it", async () =>
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromHexString("C0F30E1CE763BBC21CC355A34303AC50399444BFF68C4A22AF398C0A166EE143");
        byte[] encrypted = aes.EncryptCbc(Encoding.UTF8.GetBytes("[game]\nversion=1.5.3\n"),
            Convert.FromHexString("33467861192750649501937264608400"));
        await File.WriteAllBytesAsync(configPath, encrypted);
        Assert(await GamePredownloadClient.ReadHypergryphVersionAsync(testRoot) == "1.5.3");
        Assert((await File.ReadAllBytesAsync(configPath)).SequenceEqual(encrypted));
        await File.WriteAllTextAsync(configPath, "version=1.5.3");
    });
    await Test("HG unknown local version never requests a guessed patch", async () =>
    {
        await File.WriteAllTextAsync(configPath, "corrupt config");
        using var http = Http((_, _) => throw new Exception("Unexpected request"));
        Assert(await new GamePredownloadClient(http).GetAsync(GameBiz.endfield_cn, testRoot) is null);
        await File.WriteAllTextAsync(configPath, "version=1.5.3");
    });
    await Test("Unsupported games never fall back to a different channel", async () =>
    {
        using var http = Http((_, _) => throw new Exception("Unexpected request"));
        foreach (string biz in new[] { GameBiz.arknights_global, "hg-popucom_cn", "endfield_bilibili", "wutheringwaves_bilibili" })
            Assert(await new GamePredownloadClient(http).GetAsync(biz, testRoot) is null);
    });
    await Test("Kuro selects predownload resources and encodes filenames", async () =>
    {
        foreach (string biz in new[] { GameBiz.wutheringwaves_cn, GameBiz.wutheringwaves_global })
        {
            int requests = 0;
            using var http = Http((request, _) =>
            {
                requests++;
                if (requests == 1)
                {
                    Assert(request.RequestUri!.AbsolutePath.Contains(biz == GameBiz.wutheringwaves_cn ? "G152/10003_" : "G153/50004_"));
                    return Json(KuroReply());
                }
                Assert(request.RequestUri!.AbsoluteUri == "https://cdn.example.test/future/resources.json");
                return Json(JsonSerializer.Serialize(new { resource = new[] { new
                {
                    dest = biz == GameBiz.wutheringwaves_cn ? "Content/with space.pak" : "/Content/with space.pak",
                    size = 12, md5 = "abc",
                } } }));
            });
            var plan = await new GamePredownloadClient(http).GetAsync(biz, testRoot);
            Assert(requests == 2 && plan?.Version == "3.8.0" && plan.DownloadSize == 12);
            Assert(plan!.Files[0].Url == "https://cdn.example.test/future/files/Content/with%20space.pak");
            Assert(plan.Files[0].MD5 == "00000000000000000000000000000abc");
        }
    });
    await Test("Kuro switch without resources does not expose a button", async () =>
    {
        foreach (string json in new[] { "{\"predownloadSwitch\":1,\"default\":{\"version\":\"3.7.0\"}}",
            KuroReply(0), KuroReply(1, "3.7.0"), KuroReply(1, "3.8.0", ""),
            KuroReply().Replace("\"predownloadSwitch\":1", "\"predownloadSwitch\":false") })
        {
            int requests = 0;
            using var http = Http((_, _) => { requests++; return Json(json); });
            Assert(await new GamePredownloadClient(http).GetAsync(GameBiz.wutheringwaves_cn, testRoot) is null);
            Assert(requests == 1);
        }
    });
    await Test("Kuro gzip manifests work with clients without automatic decompression", async () =>
    {
        int requests = 0;
        using var http = Http((_, _) =>
        {
            string json = ++requests == 1 ? KuroReply()
                : JsonSerializer.Serialize(new { resource = new[] { new { dest = file.RelativePath, size = file.Size, md5 } } });
            using var buffer = new MemoryStream();
            using (var gzip = new GZipStream(buffer, CompressionMode.Compress, leaveOpen: true))
                gzip.Write(Encoding.UTF8.GetBytes(json));
            var response = Bytes(buffer.ToArray());
            response.Content.Headers.ContentEncoding.Add("gzip");
            return response;
        });
        var plan = await new GamePredownloadClient(http).GetAsync(GameBiz.wutheringwaves_cn, testRoot);
        Assert(requests == 2 && plan?.Files.Count == 1 && plan.DownloadSize == payload.Length);
    });
    await Test("Malformed resource, HTTP failure and cancellation cannot indicate availability", async () =>
    {
        using var malformed = Http((_, _) => Json("not json"));
        await Throws<JsonException>(() => new GamePredownloadClient(malformed).GetAsync(GameBiz.wutheringwaves_cn, testRoot));
        using var failed = Http((_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        await Throws<HttpRequestException>(() => new GamePredownloadClient(failed).GetAsync(GameBiz.wutheringwaves_cn, testRoot));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Throws<OperationCanceledException>(() => new GamePredownloadClient(failed).GetAsync(GameBiz.wutheringwaves_cn, testRoot, cancelled.Token));
    });
    await Test("Unsafe paths and invalid package entries are rejected", async () =>
    {
        foreach (string path in new[] { "../x", "/x", "C:/x", "a/../../x", "a\\..\\x", "a:stream", "NUL.txt", "a.", "a//b" })
            await Throws<InvalidDataException>(() => Task.FromResult(Plan(file with { RelativePath = path })));
        foreach (var invalid in new[] { file with { Url = "http://cdn.example.test/file" }, file with { MD5 = "" }, file with { Size = -1 } })
            await Throws<InvalidDataException>(() => Task.FromResult(Plan(invalid)));
        foreach (var conflict in new[] { file with { RelativePath = "PATCH.ZIP.001" }, file with { RelativePath = file.RelativePath + "_tmp" }, file with { RelativePath = file.RelativePath + "/child" } })
            await Throws<InvalidDataException>(() => Task.FromResult(new GamePredownloadPlan(GameBiz.endfield_cn, "1.6.0", [file, conflict])));
    });
    await Test("Signed URL rotation does not invalidate completed content identity", () =>
    {
        Assert(Plan(file).Fingerprint == Plan(file with { Url = file.Url + "?auth_key=changed" }).Fingerprint);
        Assert(Plan(file).Fingerprint != Plan(file with { MD5 = new string('0', 32) }).Fingerprint);
        return Task.CompletedTask;
    });
    await Test("Completion receipt requires every file and detects deletion or modification", async () =>
    {
        var plan = Plan(file);
        string path = GamePredownloadCache.GetFilePath(plan, testRoot, file);
        Assert(!await GamePredownloadCache.IsFinishedAsync(plan, testRoot));
        await Throws<InvalidDataException>(() => GamePredownloadCache.MarkFinishedAsync(plan, testRoot));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, payload);
        await GamePredownloadCache.MarkFinishedAsync(plan, testRoot);
        Assert(await GamePredownloadCache.IsFinishedAsync(plan, testRoot));
        Assert(GamePredownloadCache.GetRemainingBytes(plan, testRoot) == 0);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
        Assert(!await GamePredownloadCache.IsFinishedAsync(plan, testRoot));
        File.Delete(path);
        Assert(!await GamePredownloadCache.IsFinishedAsync(plan, testRoot));
        Assert(GamePredownloadCache.GetRemainingBytes(plan, testRoot) == payload.Length);
    });
    await Test("Download validates checksum and reuses completed files", async () =>
    {
        string path = TransferPath();
        await File.WriteAllBytesAsync(path, new byte[payload.Length]);
        int requests = 0;
        long progress = 0;
        using var http = Http((_, _) => { requests++; return Bytes(payload); });
        var transfer = new PredownloadFileTransfer(http);
        await transfer.DownloadAsync(file, path, (bytes, _) => progress += bytes);
        Assert(progress == payload.Length && (await File.ReadAllBytesAsync(path)).SequenceEqual(payload));
        progress = 0;
        await transfer.DownloadAsync(file, path, (bytes, _) => progress += bytes);
        Assert(requests == 1 && progress == payload.Length);
    });
    await Test("Interrupted downloads resume using validated byte ranges", async () =>
    {
        string path = TransferPath();
        int calls = 0;
        long progress = 0;
        using var http = Http((request, _) =>
        {
            if (++calls == 1)
                return Bytes(payload[..5]);
            Assert(request.Headers.Range!.Ranges.Single().From == 5);
            var response = Bytes(payload[5..], HttpStatusCode.PartialContent);
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(5, payload.Length - 1, payload.Length);
            return response;
        });
        var transfer = new PredownloadFileTransfer(http);
        await Throws<EndOfStreamException>(() => transfer.DownloadAsync(file, path, (bytes, _) => progress += bytes));
        Assert(!File.Exists(path) && new FileInfo(path + "_tmp").Length == 5 && progress == 0);
        await transfer.DownloadAsync(file, path, (bytes, _) => progress += bytes);
        Assert(progress == payload.Length && (await File.ReadAllBytesAsync(path)).SequenceEqual(payload));
    });
    await Test("Servers ignoring Range restart safely", async () =>
    {
        string path = TransferPath();
        await File.WriteAllBytesAsync(path + "_tmp", payload[..7]);
        using var http = Http((request, _) => { Assert(request.Headers.Range is not null); return Bytes(payload); });
        await new PredownloadFileTransfer(http).DownloadAsync(file, path);
        Assert((await File.ReadAllBytesAsync(path)).SequenceEqual(payload));
    });
    await Test("Full temporary files are verified without redownloading", async () =>
    {
        string path = TransferPath();
        await File.WriteAllBytesAsync(path + "_tmp", payload);
        using var http = Http((_, _) => throw new Exception("Unexpected request"));
        long progress = 0;
        await new PredownloadFileTransfer(http).DownloadAsync(file, path, (bytes, _) => progress += bytes);
        Assert(progress == payload.Length && File.Exists(path));
    });
    await Test("Bad checksum never creates a final file or completed marker", async () =>
    {
        string path = TransferPath();
        using var http = Http((_, _) => Bytes(new byte[payload.Length]));
        long progress = 0;
        await Throws<InvalidDataException>(() => new PredownloadFileTransfer(http).DownloadAsync(file, path, (bytes, _) => progress += bytes));
        Assert(!File.Exists(path) && !File.Exists(path + "_tmp") && progress == 0);
    });
    await Test("Wrong Content-Range cannot corrupt cached bytes", async () =>
    {
        string path = TransferPath();
        await File.WriteAllBytesAsync(path + "_tmp", payload[..5]);
        using var http = Http((_, _) =>
        {
            var response = Bytes(payload[2..], HttpStatusCode.PartialContent);
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(2, payload.Length - 1, payload.Length);
            return response;
        });
        await Throws<InvalidDataException>(() => new PredownloadFileTransfer(http).DownloadAsync(file, path));
        Assert((await File.ReadAllBytesAsync(path + "_tmp")).SequenceEqual(payload[..5]));
    });
    await Test("Pause retains partial data and rolls back attempt progress", async () =>
    {
        string path = TransferPath();
        using var cancellation = new CancellationTokenSource();
        using var http = Http((_, _) => Bytes(payload));
        long progress = 0;
        await Throws<OperationCanceledException>(() => new PredownloadFileTransfer(http).DownloadAsync(file, path,
            (bytes, _) => { progress += bytes; if (bytes > 0) cancellation.Cancel(); }, cancellationToken: cancellation.Token));
        Assert(!File.Exists(path) && File.Exists(path + "_tmp") && progress == 0);
        await new PredownloadFileTransfer(http).DownloadAsync(file, path);
        Assert(File.Exists(path));
    });
    Console.WriteLine($"Passed {passed} predownload regression tests.");
}
finally
{
    // testRoot 是本次测试创建的唯一临时目录。
    Directory.Delete(testRoot, recursive: true);
}

async Task Test(string name, Func<Task> body)
{
    await body();
    passed++;
    Console.WriteLine($"PASS {name}");
}
static void Assert(bool result)
{
    if (!result) throw new Exception("Assertion failed.");
}
static async Task Throws<T>(Func<Task> body) where T : Exception
{
    try { await body(); }
    catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}
static HttpClient Http(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) => new(new Handler(handler));
static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
static HttpResponseMessage Bytes(byte[] bytes, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new ByteArrayContent(bytes) };
GamePredownloadPlan Plan(GamePredownloadFile entry) => new(GameBiz.endfield_cn, "1.6.0", [entry]);
string TransferPath() => Path.Combine(testRoot, Guid.NewGuid().ToString("N") + ".zip");
string HgReply(string version = "1.6.0") => JsonSerializer.Serialize(new
{
    version = "1.5.3",
    pkg = new { packs = new[] { new { url = "https://cdn.example.test/current.zip" } } },
    pre_patch = new { version, patches = new[] { new { url = file.Url, md5, package_size = payload.Length.ToString() } } },
});
string KuroReply(int toggle = 1, string version = "3.8.0", string resources = "future/resources.json") => JsonSerializer.Serialize(new
{
    predownloadSwitch = toggle,
    @default = new { version = "3.7.0", cdnList = new[] { new { url = "https://cdn.example.test/" } }, resources = "current/resources.json" },
    predownload = new { version, resources, resourcesBasePath = "future/files" },
});
GamePackage HoyoPackage() => new()
{
    Main = new() { Major = new() { Version = "1.0.0" } },
    PreDownload = new()
    {
        Major = new() { Version = "2.0.0", GamePackages = [new() { Url = file.Url, Size = payload.Length, MD5 = md5 }] },
        Patches = [],
    },
};

sealed class Handler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handle) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(handle(request, cancellationToken));
    }
}
