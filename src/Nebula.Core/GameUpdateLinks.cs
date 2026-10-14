using Nebula.Core.HoYoPlay;
using Nebula.Core.Hypergryph;
using Nebula.Core.Kuro;

namespace Nebula.Core;

/// <summary>为已接入的游戏生成官方更新源配置。</summary>
public static class GameUpdateLinks
{
    public static GameUpdateInfo? Get(GameId gameId)
    {
        if (gameId.GameBiz.IsHoYoPlayGame() && !string.IsNullOrWhiteSpace(gameId.Id))
        {
            return new GameUpdateInfo
            {
                Provider = "HoYoPlay",
                ManifestUrl = HoYoPlayClient.GetGamePackagesUrl(gameId.GameBiz, gameId.Id),
            };
        }

        if (KuroGameUpdateLinks.Get(gameId.GameBiz) is GameUpdateInfo kuro)
            return kuro;

        if (HypergryphGameUpdateLinks.GetManifestUrl(gameId.GameBiz) is string manifestUrl)
        {
            return new GameUpdateInfo
            {
                Provider = "Hypergryph",
                ManifestUrl = manifestUrl,
            };
        }

        return null;
    }
}
