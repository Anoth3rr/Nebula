using System.Text.Json.Serialization;

namespace Nebula.Core;

/// <summary>
/// 游戏更新源。地址由官方启动器协议提供，包地址可能随版本变化。
/// </summary>
public sealed class GameUpdateInfo
{
    /// <summary>更新协议提供方，例如 HoYoPlay、Hypergryph 或 Kuro。</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "";

    /// <summary>查询游戏版本和资源清单的地址。</summary>
    [JsonPropertyName("manifest_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ManifestUrl { get; set; }

    /// <summary>官方启动器自身的更新配置地址。</summary>
    [JsonPropertyName("launcher_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LauncherUrl { get; set; }

    /// <summary>当前版本的完整包地址；它不是永久配置，刷新版本时会被替换。</summary>
    [JsonPropertyName("package_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PackageUrl { get; set; }
}
