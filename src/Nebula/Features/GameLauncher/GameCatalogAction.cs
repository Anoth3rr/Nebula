using Nebula.Core;
using Nebula.Core.HoYoPlay;
using System;

namespace Nebula.Features.GameLauncher;

/// <summary>动态发现但未适配的游戏共用展示/官网入口，不启用本地安装和启动。</summary>
internal static class GameCatalogAction
{
    public static GameState? GetDisplayOnlyState(GameBiz gameBiz, GameInfo? info)
    {
        if (gameBiz.IsKnown())
            return null;
        return GetWebsite(info) is null ? GameState.ComingSoon : GameState.OpenWebsite;
    }

    public static Uri? GetWebsite(GameInfo? info)
    {
        string? url = info?.Reservation?.Link ?? info?.Hypergryph?.Website;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri : null;
    }
}
