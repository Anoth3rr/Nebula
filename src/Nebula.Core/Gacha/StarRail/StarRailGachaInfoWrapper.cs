using System.Text.Json.Serialization;

namespace Nebula.Core.Gacha.StarRail;

internal class StarRailGachaInfoWrapper
{

    [JsonPropertyName("list")]
    public List<StarRailGachaInfo> List { get; set; }
}

