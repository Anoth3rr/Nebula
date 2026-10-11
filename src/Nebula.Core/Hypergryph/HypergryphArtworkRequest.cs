using System.Text.Json.Serialization;

namespace Nebula.Core.Hypergryph;

internal sealed class HypergryphArtworkRequest(string appCode)
{
    [JsonPropertyName("proxy_reqs")]
    public HypergryphArtworkQuery[] Queries { get; } =
    [
        new() { Kind = "get_main_bg_image", Background = new(appCode) },
        new() { Kind = "get_game_image", Cover = new(appCode) },
    ];
}

internal sealed class HypergryphArtworkQuery
{
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("get_main_bg_image_req")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HypergryphArtworkParameters? Background { get; init; }

    [JsonPropertyName("get_game_image_req")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HypergryphArtworkParameters? Cover { get; init; }
}

internal sealed class HypergryphArtworkParameters(string appCode)
{
    [JsonPropertyName("appcode")]
    public string AppCode { get; } = appCode;
    [JsonPropertyName("language")]
    public string Language => "zh-cn";
    [JsonPropertyName("channel")]
    public string Channel => "1";
    [JsonPropertyName("sub_channel")]
    public string SubChannel => "1";
    [JsonPropertyName("platform")]
    public string Platform => "Windows";
    [JsonPropertyName("source")]
    public string Source => "launcher";
}
