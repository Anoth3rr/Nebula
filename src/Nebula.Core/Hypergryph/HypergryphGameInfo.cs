namespace Nebula.Core.Hypergryph;

/// <summary>鹰角官方产品目录附加信息；PC 可用性未知时不提供本地安装入口。</summary>
public sealed class HypergryphGameInfo
{
    public string AppCode { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Website { get; set; } = "https://www.hypergryph.com/";
    public bool? SupportsPc { get; set; }
    public string? Version { get; set; }
}
