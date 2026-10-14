namespace Nebula.Core.Hypergryph;

/// <summary>鹰角 PC 启动器的发行渠道；未适配的渠道不能回退到国服。</summary>
public static class HypergryphGameUpdateLinks
{
    public static string? GetManifestUrl(GameBiz gameBiz, string version = "")
    {
        var source = gameBiz.Value switch
        {
            GameBiz.arknights_cn => ("hypergryph", "GzD1CpaWgmSq1wew", "abYeZZ16BPluCFyT", 1),
            GameBiz.arknights_bilibili => ("hypergryph", "GzD1CpaWgmSq1wew", "abYeZZ16BPluCFyT", 2),
            GameBiz.endfield_cn => ("hypergryph", "6LL0KJuqHBVz33WK", "abYeZZ16BPluCFyT", 1),
            GameBiz.endfield_global => ("gryphline", "YDUTE5gscDZ229CW", "YDUTE5gscDZ229CW", 6),
            _ => default,
        };
        if (source.Item1 is null)
            return null;
        return $"https://launcher.{source.Item1}.com/api/game/get_latest?appcode={source.Item2}"
            + $"&channel={source.Item4}&sub_channel={source.Item4}&version={Uri.EscapeDataString(version)}"
            + $"&launcher_appcode={source.Item3}";
    }
}
