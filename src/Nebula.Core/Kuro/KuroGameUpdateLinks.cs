using Nebula.Core;

namespace Nebula.Core.Kuro;

/// <summary>
/// 库洛已知游戏的官方 CDN 更新地址。
///
/// 库洛目前没有公开的全量游戏目录，因而这里只配置已支持的鸣潮区服。
/// </summary>
public static class KuroGameUpdateLinks
{
    // 这些 appKey 来自鸣潮官方启动器配置。它们属于可轮换的发布配置，不能用于推断未知游戏。
    private const string WutheringWavesChinaAppKey = "Y8xXrXk65DqFHEDgApn3cpK5lfczpFx5";
    private const string WutheringWavesGlobalAppKey = "obOHXFrFanqsaIEOmuKroCcbZkQRBC7c";

    public static GameUpdateInfo? Get(GameBiz gameBiz) => gameBiz.Value switch
    {
        GameBiz.wutheringwaves_cn => Create(
            "https://prod-cn-alicdn-gamestarter.kurogame.com",
            "G152", "10003", WutheringWavesChinaAppKey),
        GameBiz.wutheringwaves_global => Create(
            "https://prod-alicdn-gamestarter.kurogame.com",
            "G153", "50004", WutheringWavesGlobalAppKey),
        _ => null,
    };

    private static GameUpdateInfo Create(string baseUrl, string gameId, string appId, string appKey) => new()
    {
        Provider = "Kuro",
        ManifestUrl = $"{baseUrl}/launcher/game/{gameId}/{appId}_{appKey}/index.json",
        LauncherUrl = $"{baseUrl}/launcher/launcher/{appId}_{appKey}/{gameId}/index.json",
    };
}
