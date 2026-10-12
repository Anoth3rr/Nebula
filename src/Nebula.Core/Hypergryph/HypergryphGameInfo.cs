namespace Nebula.Core.Hypergryph;

/// <summary>鹰角官方目录信息；发行平台信息不代表 Nebula 已适配该游戏。</summary>
public sealed class HypergryphGameInfo
{
    public string AppCode { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Website { get; set; } = "https://www.hypergryph.com/";
    public bool? SupportsPc { get; set; }
    public string? Version { get; set; }
    /// <summary>当前 Windows 完整包地址；随版本更新，仅用于诊断和后续更新器。</summary>
    public string? PackageUrl { get; set; }
    public string? LauncherBackground { get; set; }
    public string? LauncherCover { get; set; }
}
