using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Nebula.Core;
using Nebula.Core.HoYoPlay;
using Nebula.Core.Hypergryph;
using Nebula.Features.Hypergryph;

if (args.Contains("--live"))
{
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    var games = await new HypergryphClient(http).GetGamesAsync();
    foreach (var game in games)
        Console.WriteLine($"{game.GameBiz}: {game.Display.Name}; PC={game.Hypergryph!.SupportsPc}; version={game.Hypergryph.Version}");
    return;
}

int passed = 0;
await Test("Official catalog, not community boards, defines the games", async () =>
{
    var games = await Client().GetGamesAsync();
    Assert(games.Count == 5 && games.All(x => x.Hypergryph is not null), "five products expected");
    Assert(games.All(x => x.Display.Name is not "纳斯特港" and not "开拓芯"), "community boards are not games");
    Assert(games.Count(x => x.Hypergryph!.SupportsPc == true) == 3, "three PC games expected");
    Assert(games.Count(x => x.Hypergryph!.SupportsPc == false) == 2, "mobile products must not offer PC installation");
    Assert(games.First(x => x.GameBiz == GameBiz.arknights_cn).Display.Icon.Url.StartsWith("https://"), "official artwork expected");
});
await Test("Existing identities remain compatible with installed games", async () =>
{
    var games = await Client().GetGamesAsync();
    foreach (GameBiz biz in new GameBiz[] { GameBiz.arknights_cn, GameBiz.endfield_cn })
        Assert(games.Single(x => x.GameBiz == biz).Id == GameId.FromGameBiz(biz)!.Id, "legacy game ID changed");
});
await Test("A newly published product appears without a compiled game list", async () =>
{
    var config = JsonNode.Parse(Fixture("catalog"))!;
    config["future_game"] = new JsonObject { ["appCode"] = "Future2026", ["name"] = "Future Game" };
    var games = await Client(catalog: config.ToJsonString()).GetGamesAsync();
    var future = games.Single(x => x.Hypergryph!.AppCode == "Future2026");
    Assert(future.GameBiz.Game == "hg-future%5Fgame" && future.GameBiz.Server == "cn", "stable game/region split");
    Assert(future.GameBiz.IsHypergryphGame() && !future.GameBiz.IsHoYoPlayGame(), "provider routing");
    Assert(future.Hypergryph!.SupportsPc == true, "new PC game must be discovered");
});
await Test("Artwork failure keeps the catalog usable", async () =>
{
    var games = await Client(overrideResponse: req => req.RequestUri!.AbsoluteUri == HypergryphClient.CommunityUrl
        ? Reply("unavailable", HttpStatusCode.ServiceUnavailable) : null).GetGamesAsync();
    Assert(games.Count == 5 && games.All(x => !string.IsNullOrWhiteSpace(x.Display.Icon.Url)), "fallback artwork");
});
await Test("One launcher failure preserves cached capability and other games update", async () =>
{
    var cached = await Client().GetGamesAsync();
    var games = await Client(overrideResponse: req => req.RequestUri!.Query.Contains("appcode=Vp2jukCCSUGztqli")
        ? Reply("unavailable", HttpStatusCode.ServiceUnavailable) : null).GetGamesAsync(cached);
    var popucom = games.Single(x => x.Hypergryph!.Slug == "popucom");
    Assert(popucom.Hypergryph!.SupportsPc == true && popucom.Hypergryph.Version == "1.2.1", "transient error erased capability");
    Assert(games.Single(x => x.GameBiz == GameBiz.endfield_cn).Hypergryph!.SupportsPc == true, "unrelated product lost");
});
await Test("Unknown capability is not advertised as a PC game", async () =>
{
    var games = await Client(overrideResponse: req => req.RequestUri!.Host == "launcher.hypergryph.com"
        ? Reply("null") : null).GetGamesAsync();
    Assert(games.All(x => x.Hypergryph!.SupportsPc is null), "malformed response must not imply PC support");
});
await Test("Only the launcher's product-not-found response means no PC version", async () =>
{
    var games = await Client(overrideResponse: req => req.RequestUri!.Host == "launcher.hypergryph.com"
        ? Reply("{\"reason\":\"RESOURCE_NOT_FOUND\",\"message\":\"endpoint not found\"}", HttpStatusCode.NotFound) : null).GetGamesAsync();
    Assert(games.All(x => x.Hypergryph!.SupportsPc is null), "endpoint failure must not remove PC support");
});
await Test("Invalid and empty catalogs are rejected", async () =>
{
    foreach (string catalog in new[] { "{}", "[]", "null", "{\"code\":500}", "<html>error</html>" })
        await Throws<JsonException>(() => Client(catalog: catalog).GetGamesAsync());
});
await Test("Cache survives restart and catalog outage", async () =>
{
    Nebula.AppConfig.CachedHypergryphGameInfo = null;
    var initial = new HypergryphService(Client(), NullLogger<HypergryphService>.Instance);
    await initial.UpdateGameInfoListAsync();
    var serialized = Nebula.AppConfig.CachedHypergryphGameInfo;
    var restarted = new HypergryphService(Client(overrideResponse: req => req.RequestUri!.AbsoluteUri == HypergryphClient.CatalogUrl
        ? Reply("upstream unavailable", HttpStatusCode.BadGateway) : null), NullLogger<HypergryphService>.Instance);
    var cached = await restarted.UpdateGameInfoListAsync();
    Assert(cached.Count == 5 && cached.Count(x => x.Hypergryph!.SupportsPc == true) == 3, "offline catalog missing");
    Assert(Nebula.AppConfig.CachedHypergryphGameInfo == serialized, "failure overwrote persisted cache");
    Assert(cached.Any(x => x.GameBiz == HypergryphClient.ToGameBiz("popucom")), "dynamic pin ID did not round trip");
});
await Test("Empty refresh and corrupt cache do not break subsequent refresh", async () =>
{
    var prior = Nebula.AppConfig.CachedHypergryphGameInfo;
    var empty = new HypergryphService(Client(catalog: "{}"), NullLogger<HypergryphService>.Instance);
    Assert((await empty.UpdateGameInfoListAsync()).Count == 5, "empty response lost cached games");
    Assert(Nebula.AppConfig.CachedHypergryphGameInfo == prior, "empty response overwrote cache");
    Nebula.AppConfig.CachedHypergryphGameInfo = "broken json";
    var service = new HypergryphService(Client(), NullLogger<HypergryphService>.Instance);
    Assert(service.GetCachedGames().Count == 0, "corrupt cache should be ignored");
    Assert((await service.UpdateGameInfoListAsync()).Count == 5, "refresh should repair cache");
});
await Test("Caller cancellation is propagated", async () =>
{
    using var cts = new CancellationTokenSource();
    cts.Cancel();
    await Throws<OperationCanceledException>(() => Client().GetGamesAsync(cancellationToken: cts.Token));
});
Console.WriteLine($"Passed {passed} Hypergryph catalog tests.");

async Task Test(string name, Func<Task> test)
{
    await test();
    passed++;
    Console.WriteLine($"PASS {name}");
}

static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".json"));
static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
static async Task Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); }
    catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}
static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body) };

static HypergryphClient Client(string? catalog = null, Func<HttpRequestMessage, HttpResponseMessage?>? overrideResponse = null)
{
    return new HypergryphClient(new HttpClient(new FakeHandler(req =>
    {
        var overridden = overrideResponse?.Invoke(req);
        if (overridden is not null) return overridden;
        var uri = req.RequestUri!;
        if (uri.AbsoluteUri == HypergryphClient.CatalogUrl) return Reply(catalog ?? Fixture("catalog"));
        if (uri.AbsoluteUri == HypergryphClient.CommunityUrl) return Reply(Fixture("community"));
        Assert(req.Method == HttpMethod.Get && uri.Host == "launcher.hypergryph.com" && uri.AbsolutePath == "/api/game/get_latest", "unexpected endpoint");
        Assert(uri.Query.Contains("channel=1&sub_channel=1") && uri.Query.Contains("launcher_appcode=abYeZZ16BPluCFyT"), "incorrect channel");
        if (uri.Query.Contains("appcode=Djtofbl7KCIkZtft") || uri.Query.Contains("appcode=EjOB8xSdBmtLnzCX"))
            return Reply("{\"code\":404,\"reason\":\"RESOURCE_NOT_FOUND\",\"message\":\"game not exist\"}", HttpStatusCode.NotFound);
        return Reply("{\"version\":\"1.2.1\",\"pkg\":{\"file_path\":\"https://popucom.hycdn.cn/example/Windows/1.2.1/files\"}}");
    })));
}

sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(respond(request));
    }
}

namespace Nebula
{
    // 替代 WinUI/SQLite 外壳，验证实际服务的持久化与重启恢复逻辑。
    public static class AppConfig
    {
        public static string? CachedHypergryphGameInfo { get; set; }
    }
}
