using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Nebula.Core.HoYoPlay;

namespace Nebula.Core.Hypergryph;

/// <summary>
/// 从森空岛官方产品配置发现游戏，再查询鹰角启动器的图片与 PC 发行信息。
/// 社区版区列表仅用于补充图片，不能作为产品目录（其中包含非游戏版区）。
/// </summary>
public sealed class HypergryphClient(HttpClient httpClient)
{
    public const string CatalogUrl = "https://assets.skland.com/common-config/json/game-config.json";
    public const string CommunityUrl = "https://zonai.skland.com/web/v1/game";
    public const string ArtworkUrl = "https://launcher.hypergryph.com/api/proxy/web/batch_proxy";
    private const string LauncherUrl = "https://launcher.hypergryph.com/api/game/get_latest";
    private const string TransparentIcon = "ms-appx:///Assets/Image/Transparent.png";

    public async Task<List<GameInfo>> GetGamesAsync(
        IReadOnlyList<GameInfo>? cachedGames = null, CancellationToken cancellationToken = default)
    {
        using var catalog = await GetJsonAsync(CatalogUrl, cancellationToken);
        if (catalog.RootElement.ValueKind != JsonValueKind.Object)
            throw new JsonException("Invalid Hypergryph product catalog.");

        using var community = await TryGetCommunityAsync(cancellationToken);
        var games = new List<GameInfo>();
        foreach (var property in catalog.RootElement.EnumerateObject())
        {
            var product = property.Value;
            string appCode = ReadString(product, "appCode"), name = ReadString(product, "name");
            if (string.IsNullOrWhiteSpace(appCode) || string.IsNullOrWhiteSpace(name))
                continue;

            var biz = ToGameBiz(property.Name);
            var cached = cachedGames?.FirstOrDefault(x => x.GameBiz == biz && x.Hypergryph?.AppCode == appCode);
            var display = FindCommunityGame(community, name);
            var icon = FirstNonEmpty(ReadString(display, "pcIconUrl"), ReadString(display, "iconUrl"),
                cached?.Display.Icon.Url, biz.Game switch
                {
                    GameBiz.arknights => "ms-appx:///Assets/Image/icon_arknights.ico",
                    GameBiz.endfield => "ms-appx:///Assets/Image/icon_endfield.ico",
                    _ => "ms-appx:///Assets/NebulaLogo.png",
                });
            var background = FirstNonEmpty(cached?.Hypergryph?.LauncherBackground, cached?.Hypergryph?.LauncherCover,
                ReadHttpsUrl(display, "pcBackgroundUrl"), ReadHttpsUrl(display, "backgroundUrl"),
                cached?.Display.Background?.Url, biz.Game switch
                {
                    GameBiz.arknights => "ms-appx:///Assets/Image/cover_arknights.jpg",
                    GameBiz.endfield => "ms-appx:///Assets/Image/cover_endfield.jpg",
                    _ => icon,
                });
            var thumbnail = FirstNonEmpty(cached?.Hypergryph?.LauncherCover,
                cached?.Hypergryph?.LauncherBackground, background);

            games.Add(new GameInfo
            {
                // 复用旧 ID，保留已固定的游戏、安装路径及抽卡记录关联。
                Id = GameId.FromGameBiz(biz)?.Id ?? $"hypergryph_{appCode}_1_1",
                GameBiz = biz,
                Display = new GameInfoDisplay
                {
                    Language = "zh-cn", Name = name, Title = name, Subtitle = "",
                    Icon = new GameImage { Url = icon },
                    Background = new GameImage { Url = background },
                    Thumbnail = new GameImage { Url = thumbnail },
                    Logo = new GameImage { Url = TransparentIcon },
                },
                DisplayStatus = GameInfoDisplayStatus.LAUNCHER_GAME_DISPLAY_STATUS_AVAILABLE,
                GameServerConfigs = [],
                Hypergryph = new HypergryphGameInfo
                {
                    AppCode = appCode, Slug = property.Name,
                    Website = GetWebsite(property.Name, ReadString(product, "iosDeeplink")),
                    SupportsPc = cached?.Hypergryph?.SupportsPc,
                    Version = cached?.Hypergryph?.Version,
                    LauncherBackground = cached?.Hypergryph?.LauncherBackground,
                    LauncherCover = cached?.Hypergryph?.LauncherCover,
                },
            });
        }

        // 空/错误响应不能覆盖上一次的可用目录。
        if (games.Count == 0)
            throw new JsonException("Hypergryph returned no valid products.");

        using var concurrency = new SemaphoreSlim(4);
        await Task.WhenAll(games.Select(async game =>
        {
            await concurrency.WaitAsync(cancellationToken);
            try
            {
                await Task.WhenAll(UpdatePcAvailabilityAsync(game.Hypergryph!, cancellationToken),
                    UpdateArtworkAsync(game, cancellationToken));
            }
            finally { concurrency.Release(); }
        }));
        return games.DistinctBy(x => x.GameBiz).ToList();
    }

    public static GameBiz ToGameBiz(string slug) => slug switch
    {
        "arknights" => GameBiz.arknights_cn,
        "endfield" => GameBiz.endfield_cn,
        // 未适配的新产品仍有稳定的 ID；下划线必须转义，以免被误读为区服分隔符。
        _ => $"hg-{Uri.EscapeDataString(slug).Replace("_", "%5F")}_cn",
    };

    private async Task UpdateArtworkAsync(GameInfo game, CancellationToken cancellationToken)
    {
        var metadata = game.Hypergryph!;
        try
        {
            using var content = JsonContent.Create(new HypergryphArtworkRequest(metadata.AppCode),
                HypergryphArtworkJsonContext.Default.HypergryphArtworkRequest);
            using var response = await httpClient.PostAsync(ArtworkUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (json.RootElement.ValueKind != JsonValueKind.Object
                || !json.RootElement.TryGetProperty("proxy_rsps", out var items)
                || items.ValueKind != JsonValueKind.Array)
                return;

            foreach (var item in items.EnumerateArray())
            {
                switch (ReadString(item, "kind"))
                {
                    case "get_main_bg_image":
                        var background = ReadObject(ReadObject(item, "get_main_bg_image_rsp"), "main_bg_image");
                        metadata.LauncherBackground = ReadHttpsUrl(background, "url") ?? metadata.LauncherBackground;
                        break;
                    case "get_game_image":
                        var artwork = ReadObject(item, "get_game_image_rsp");
                        if (ReadString(artwork, "appcode") == metadata.AppCode)
                            metadata.LauncherCover = ReadHttpsUrl(artwork, "cover")
                                ?? ReadHttpsUrl(artwork, "bg") ?? metadata.LauncherCover;
                        break;
                }
            }

            // 列表封面与主背景分开；官方未提供封面时复用启动器背景。
            game.Display.Background = new GameImage
            {
                Url = FirstNonEmpty(metadata.LauncherBackground, metadata.LauncherCover, game.Display.Background?.Url),
            };
            game.Display.Thumbnail = new GameImage
            {
                Url = FirstNonEmpty(metadata.LauncherCover, metadata.LauncherBackground, game.Display.Thumbnail?.Url),
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            // 图片失败不能丢失目录，也不能用社区占位背景覆盖已缓存的启动器图片。
        }
    }

    private async Task UpdatePcAvailabilityAsync(HypergryphGameInfo game, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{LauncherUrl}?appcode={Uri.EscapeDataString(game.AppCode)}&channel=1&sub_channel=1&version=&launcher_appcode=abYeZZ16BPluCFyT";
            using var response = await httpClient.GetAsync(url, cancellationToken);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException("Invalid Hypergryph launcher response.");
            if (response.StatusCode == HttpStatusCode.NotFound
                && ReadString(root, "reason") == "RESOURCE_NOT_FOUND"
                && ReadString(root, "message") == "game not exist")
            {
                game.SupportsPc = false;
                game.Version = null;
                return;
            }
            response.EnsureSuccessStatusCode();
            var version = ReadString(root, "version");
            if (root.TryGetProperty("pkg", out var pkg)
                && Uri.TryCreate(ReadString(pkg, "file_path"), UriKind.Absolute, out var files)
                && files.Scheme == Uri.UriSchemeHttps
                && files.AbsolutePath.Contains("/Windows/", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(version))
            {
                game.SupportsPc = true;
                game.Version = version;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            // 单个产品故障不影响其他游戏；保留上一次能力判断，不把超时当作“不支持 PC”。
        }
    }

    private async Task<JsonDocument?> TryGetCommunityAsync(CancellationToken cancellationToken)
    {
        try { return await GetJsonAsync(CommunityUrl, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException) { return null; }
    }

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private static JsonElement FindCommunityGame(JsonDocument? community, string name)
    {
        if (community?.RootElement is JsonElement root && root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.Number
            && code.TryGetInt32(out int result) && result == 0
            && root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object
            && data.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
                if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("game", out var game)
                    && ReadString(game, "name") == name)
                    return game;
        }
        return default;
    }

    private static string GetWebsite(string slug, string deepLink)
    {
        if (Uri.TryCreate(deepLink, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.EndsWith(".hypergryph.com", StringComparison.OrdinalIgnoreCase))
            return uri.GetLeftPart(UriPartial.Authority) + "/";
        return slug switch
        {
            "exastris" => "https://exa.hypergryph.com/",
            "popucom" => "https://popucom.hypergryph.com/",
            _ => "https://www.hypergryph.com/",
        };
    }

    private static string ReadString(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    private static JsonElement ReadObject(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.Object ? value : default;

    private static string? ReadHttpsUrl(JsonElement element, string key) =>
        Uri.TryCreate(ReadString(element, key), UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps ? uri.AbsoluteUri : null;

    private static string FirstNonEmpty(params string?[] values) => values.First(x => !string.IsNullOrWhiteSpace(x))!;
}
