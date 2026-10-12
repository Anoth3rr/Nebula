using Nebula.Core.HoYoPlay;
using Nebula.Core.Hypergryph;
using Nebula.Core.Kuro;

namespace Nebula.Core;

/// <summary>为已接入的游戏生成官方更新源配置。</summary>
public static class GameUpdateLinks
{
    private static readonly IReadOnlyDictionary<string, string> HypergryphAppCodes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [GameBiz.arknights] = "GzD1CpaWgmSq1wew",
            [GameBiz.endfield] = "6LL0KJuqHBVz33WK",
        };

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

        if (gameId.GameBiz.IsHypergryphGame()
            && HypergryphAppCodes.TryGetValue(gameId.GameBiz.Game, out string? appCode))
        {
            return new GameUpdateInfo
            {
                Provider = "Hypergryph",
                ManifestUrl = HypergryphClient.GetLatestUrl(appCode),
            };
        }

        return null;
    }
}
