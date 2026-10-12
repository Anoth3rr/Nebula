using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Nebula.Core;
using Nebula.Core.HoYoPlay;
using Nebula.Core.Hypergryph;
using Nebula.Features.Background;
using Nebula.Features.GameLauncher;
using Nebula.Features.Hypergryph;

if (args.Contains("--live"))
{
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    var games = await new HypergryphClient(http).GetGamesAsync();
    foreach (var game in games)
        Console.WriteLine($"{game.GameBiz}: {game.Display.Name}; supported={game.GameBiz.IsKnown()}; PC={game.Hypergryph!.SupportsPc}; version={game.Hypergryph.Version}\n  cover={game.Display.Thumbnail?.Url}\n  background={game.Display.Background?.Url}");
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
await Test("Every supported catalog entry exposes its official update source", async () =>
{
    var games = await Client().GetGamesAsync();
    foreach (var game in games)
    {
        Assert(game.Update is { Provider: "Hypergryph", ManifestUrl: not null } && game.Hypergryph is not null,
            "catalog entry missing Hypergryph provider");
        Assert(game.Update!.ManifestUrl == HypergryphClient.GetLatestUrl(game.Hypergryph!.AppCode),
            "catalog entry has an unexpected latest endpoint");
    }

    var arknights = games.Single(x => x.GameBiz == GameBiz.arknights_cn);
    var endfield = games.Single(x => x.GameBiz == GameBiz.endfield_cn);
    Assert(arknights.Update!.PackageUrl == arknights.Hypergryph!.PackageUrl
        && arknights.Update.PackageUrl!.Contains("/Windows/", StringComparison.OrdinalIgnoreCase),
        "Windows package URL was not copied into the update source");
    Assert(endfield.Update!.PackageUrl == endfield.Hypergryph!.PackageUrl,
        "second PC product package URL was not copied into the update source");
    Assert(games.Where(x => x.Hypergryph?.SupportsPc == false)
        .All(x => x.Update!.PackageUrl is null), "mobile-only product exposes a package URL");
});
await Test("Known vendor update links use official endpoint formats", () =>
{
    var hoyo = GameUpdateLinks.Get(new GameId { Id = "1Z8W5NHUQb", GameBiz = GameBiz.hk4e_cn });
    Assert(hoyo?.Provider == "HoYoPlay" && hoyo.ManifestUrl!.Contains("getGamePackages")
        && hoyo.ManifestUrl.Contains("game_ids[]=1Z8W5NHUQb"), "HoYoPlay endpoint is not configured");

    var kuroCn = GameUpdateLinks.Get(GameId.FromGameBiz(GameBiz.wutheringwaves_cn)!);
    var kuroGlobal = GameUpdateLinks.Get(GameId.FromGameBiz(GameBiz.wutheringwaves_global)!);
    Assert(kuroCn?.Provider == "Kuro" && kuroCn.ManifestUrl!.Contains("G152/10003_")
        && kuroCn.LauncherUrl!.Contains("/launcher/launcher/10003_"), "Kuro CN endpoint is not configured");
    Assert(kuroGlobal?.Provider == "Kuro" && kuroGlobal.ManifestUrl!.Contains("G153/50004_"),
        "Kuro global endpoint is not configured");
    return Task.CompletedTask;
});
await Test("Only Arknights and Endfield are in the supported Hypergryph whitelist", async () =>
{
    var games = await Client().GetGamesAsync();
    Assert(games.Where(x => x.GameBiz.IsHypergryphGame()).Select(x => x.Hypergryph!.Slug)
        .Order().SequenceEqual(new[] { "arknights", "endfield" }), "unexpected supported game");
    foreach (var game in games)
    {
        var action = GameCatalogAction.GetDisplayOnlyState(game.GameBiz, game);
        Assert(game.GameBiz.IsKnown() ? action is null : action == GameState.OpenWebsite,
            "PC availability must not bypass the whitelist");
    }
    foreach (var biz in GameBiz.AllGameBizs.Where(x => x.IsHypergryphGame()))
        Assert(GameCatalogAction.GetDisplayOnlyState(biz, null) is null, "offline/channel support regressed");
});
await Test("Unadapted HoYoPlay and Hypergryph entries share the same display-only actions", () =>
{
    var upcoming = new GameInfo { GameBiz = "future_cn", Reservation = new() { Link = "https://example.com/register" } };
    Assert(GameCatalogAction.GetDisplayOnlyState(upcoming.GameBiz, upcoming) == GameState.OpenWebsite, "reservation link missing");
    upcoming.Reservation = null;
    Assert(GameCatalogAction.GetDisplayOnlyState(upcoming.GameBiz, upcoming) == GameState.ComingSoon, "unknown game enabled");
    upcoming.Hypergryph = new() { Website = "file:///C:/game.exe", SupportsPc = true };
    Assert(GameCatalogAction.GetDisplayOnlyState(upcoming.GameBiz, upcoming) == GameState.ComingSoon, "unsafe URL accepted");
    return Task.CompletedTask;
});
await Test("Launcher artwork takes precedence and cover is separate from background", async () =>
{
    var games = await Client().GetGamesAsync();
    var arknights = games.Single(x => x.GameBiz == GameBiz.arknights_cn);
    var endfield = games.Single(x => x.GameBiz == GameBiz.endfield_cn);
    Assert(arknights.Display.Background!.Url.Contains("7b26c916"), "launcher main background not selected");
    Assert(arknights.Display.Thumbnail!.Url.Contains("75e6a3b6"), "launcher catalog cover not selected");
    Assert(endfield.Display.Thumbnail!.Url == endfield.Display.Background!.Url
        && endfield.Display.Background.Url.Contains("e9b80ea8"), "missing cover must fall back to launcher background");
});
await Test("Launcher artwork outage preserves cached images and new responses refresh them", async () =>
{
    var cached = await Client().GetGamesAsync();
    foreach (string failure in new[] { "{\"proxy_rsps\":[]}", "{\"proxy_rsps\":[null]}", "null", "<html>unavailable</html>" })
    {
        var games = await Client(overrideResponse: req => req.RequestUri!.AbsoluteUri == HypergryphClient.ArtworkUrl
            ? Reply(failure) : null).GetGamesAsync(cached);
        foreach (var game in games.Where(x => x.Hypergryph!.LauncherBackground is not null))
            Assert(game.Display.Background!.Url == cached.Single(x => x.GameBiz == game.GameBiz).Display.Background!.Url,
                "community fallback replaced a cached launcher image");
    }
    const string newArtwork = "{\"proxy_rsps\":[{\"kind\":\"get_main_bg_image\",\"get_main_bg_image_rsp\":{\"main_bg_image\":{\"url\":\"https://example.com/new-version.png\"}}}]}";
    var refreshed = await Client(overrideResponse: req => req.RequestUri!.AbsoluteUri == HypergryphClient.ArtworkUrl
        ? Reply(newArtwork) : null).GetGamesAsync(cached);
    Assert(refreshed.All(x => x.Display.Background!.Url == "https://example.com/new-version.png"), "new poster was not applied");
    Assert(cached.Single(x => x.GameBiz == GameBiz.endfield_cn).Display.Background!.Url.Contains("e9b80ea8"), "refresh mutated old snapshot");
});
await Test("Foreign and invalid artwork does not replace a game's cached cover", async () =>
{
    var cached = await Client().GetGamesAsync();
    const string invalidArtwork = "{\"proxy_rsps\":[{\"kind\":\"get_game_image\",\"get_game_image_rsp\":{\"appcode\":\"another-product\",\"cover\":\"https://example.com/foreign.png\"}},{\"kind\":\"get_main_bg_image\",\"get_main_bg_image_rsp\":{\"main_bg_image\":{\"url\":\"file:///C:/image.png\"}}}]}";
    var games = await Client(overrideResponse: req => req.RequestUri!.AbsoluteUri == HypergryphClient.ArtworkUrl
        ? Reply(invalidArtwork) : null).GetGamesAsync(cached);
    Assert(games.All(x => x.Display.Thumbnail!.Url == cached.Single(old => old.GameBiz == x.GameBiz).Display.Thumbnail!.Url),
        "invalid cover overwrote cached artwork");
});
await Test("A newly published product appears without a compiled game list", async () =>
{
    var config = JsonNode.Parse(Fixture("catalog"))!;
    config["future_game"] = new JsonObject { ["appCode"] = "Future2026", ["name"] = "Future Game" };
    var games = await Client(catalog: config.ToJsonString()).GetGamesAsync();
    var future = games.Single(x => x.Hypergryph!.AppCode == "Future2026");
    Assert(future.GameBiz.Game == "hg-future%5Fgame" && future.GameBiz.Server == "cn", "stable game/region split");
    Assert(future.GameBiz.IsHypergryphCatalogGame() && !future.GameBiz.IsHypergryphGame()
        && !future.GameBiz.IsHoYoPlayGame(), "discovery must not extend the supported whitelist");
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
    var initial = Service(Client());
    await initial.UpdateGameInfoListAsync();
    var serialized = Nebula.AppConfig.CachedHypergryphGameInfo;
    var restarted = Service(Client(overrideResponse: req => req.RequestUri!.AbsoluteUri == HypergryphClient.CatalogUrl
        ? Reply("upstream unavailable", HttpStatusCode.BadGateway) : null));
    var cached = await restarted.UpdateGameInfoListAsync();
    Assert(cached.Count == 5 && cached.Count(x => x.Hypergryph!.SupportsPc == true) == 3, "offline catalog missing");
    Assert(Nebula.AppConfig.CachedHypergryphGameInfo == serialized, "failure overwrote persisted cache");
    Assert(cached.Any(x => x.GameBiz == HypergryphClient.ToGameBiz("popucom")), "dynamic pin ID did not round trip");
});
await Test("Empty refresh and corrupt cache do not break subsequent refresh", async () =>
{
    var prior = Nebula.AppConfig.CachedHypergryphGameInfo;
    var empty = Service(Client(catalog: "{}"));
    Assert((await empty.UpdateGameInfoListAsync()).Count == 5, "empty response lost cached games");
    Assert(Nebula.AppConfig.CachedHypergryphGameInfo == prior, "empty response overwrote cache");
    Nebula.AppConfig.CachedHypergryphGameInfo = "broken json";
    var service = Service(Client());
    Assert(service.GetCachedGames().Count == 0, "corrupt cache should be ignored");
    Assert((await service.UpdateGameInfoListAsync()).Count == 5, "refresh should repair cache");
});
await Test("Caller cancellation is propagated", async () =>
{
    using var cts = new CancellationTokenSource();
    cts.Cancel();
    await Throws<OperationCanceledException>(() => Client().GetGamesAsync(cancellationToken: cts.Token));
});
await Test("Legacy community-only cache is upgraded and artwork survives a service restart", async () =>
{
    var legacy = await Client(overrideResponse: req => req.RequestUri!.AbsoluteUri == HypergryphClient.ArtworkUrl
        ? Reply("unavailable", HttpStatusCode.ServiceUnavailable) : null).GetGamesAsync();
    Nebula.AppConfig.CachedHypergryphGameInfo = JsonSerializer.Serialize(legacy, HypergryphJsonContext.Default.ListGameInfo);
    var service = Service(Client());
    var games = await service.UpdateGameInfoListAsync();
    Assert(games.Single(x => x.GameBiz == GameBiz.arknights_cn).Display.Thumbnail!.Url.Contains("75e6a3b6"), "legacy cache blocked update");
    var restored = Service(Client());
    Assert(restored.GetGameInfo(GameBiz.endfield_cn)!.Hypergryph!.LauncherBackground!.Contains("e9b80ea8"), "artwork lost after restart");
    Assert(restored.GetGameDisplay(GameBiz.endfield_global)!.Background!.Url.Contains("e9b80ea8"), "existing server artwork fallback missing");
    Assert(restored.GetGameInfo(GameBiz.endfield_global) is null, "CN artwork must not fabricate global server metadata");
});
await Test("Shared poster cache deduplicates downloads and preserves files on failure", async () =>
{
    string folder = Path.Combine(Path.GetTempPath(), "Nebula-poster-test-" + Guid.NewGuid().ToString("N"));
    Nebula.AppConfig.CacheFolder = folder;
    int requests = 0;
    using var http = new HttpClient(new FakeHandler(_ => { requests++; return Reply("poster bytes"); }));
    var cache = new GamePosterCache(http, NullLogger<GamePosterCache>.Instance);
    var games = new List<GameInfo>();
    foreach (GameBiz biz in new GameBiz[] { GameBiz.hk4e_cn, GameBiz.arknights_cn })
        games.Add(new() { GameBiz = biz, Display = new() { Background = new() { Url = "https://example.com/poster.png?version=1" } } });
    try
    {
        await cache.DownloadAsync(games);
        Assert(requests == 1 && games.All(x => Nebula.AppConfig.VersionPosters[x.GameBiz] == "poster.png"), "shared poster was not cached once");
        await cache.DownloadAsync(games);
        Assert(requests == 1, "cached poster downloaded again");
        games[1].Display.Background!.Url = "https://example.com/new-poster.png";
        using var unavailable = new HttpClient(new FakeHandler(_ => Reply("unavailable", HttpStatusCode.BadGateway)));
        await new GamePosterCache(unavailable, NullLogger<GamePosterCache>.Instance).DownloadAsync(games);
        Assert(Nebula.AppConfig.VersionPosters[GameBiz.arknights_cn] == "poster.png"
            && File.ReadAllText(Path.Combine(folder, "bg", "poster.png")) == "poster bytes", "failed download replaced valid cache");
        Assert(Directory.GetFiles(Path.Combine(folder, "bg")).Length == 1, "incomplete image left in cache");
    }
    finally
    {
        Nebula.AppConfig.CacheFolder = null;
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
    }
});
Console.WriteLine($"Passed {passed} Hypergryph catalog tests.");

async Task Test(string name, Func<Task> test)
{
    await test();
    passed++;
    Console.WriteLine($"PASS {name}");
}

static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".json"));
static HypergryphService Service(HypergryphClient client) => new(client, NullLogger<HypergryphService>.Instance,
    new GamePosterCache(new HttpClient(new FakeHandler(_ => Reply("unused"))), NullLogger<GamePosterCache>.Instance));
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
        if (uri.AbsoluteUri == HypergryphClient.ArtworkUrl)
        {
            Assert(req.Method == HttpMethod.Post, "artwork API requires POST");
            using var body = JsonDocument.Parse(req.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            var queries = body.RootElement.GetProperty("proxy_reqs");
            string? appCode = null;
            foreach (var query in queries.EnumerateArray())
            {
                var parameters = query.GetProperty(query.GetProperty("kind").GetString() + "_req");
                appCode = parameters.GetProperty("appcode").GetString();
                Assert(parameters.GetProperty("platform").GetString() == "Windows"
                    && parameters.GetProperty("source").GetString() == "launcher", "required artwork parameters missing");
            }
            using var artwork = JsonDocument.Parse(Fixture("launcher-artwork"));
            return Reply(artwork.RootElement.TryGetProperty(appCode!, out var result) ? result.GetRawText() : "{\"proxy_rsps\":[]}");
        }
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
        public static string? CacheFolder { get; set; }
        public static System.Collections.Concurrent.ConcurrentDictionary<GameBiz, string> VersionPosters { get; } = new();
        public static void SetVersionPoster(GameBiz gameBiz, string name) => VersionPosters[gameBiz] = name;
    }
}
