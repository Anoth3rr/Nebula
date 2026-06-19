using System.Text.Json.Serialization;

namespace Nebula.Core.Gacha.WutheringWaves;

public class WutheringWavesGachaInfo
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Icon { get; set; } = "";

    public int CatalogueId { get; set; }

    public int Rarity { get; set; }
}


internal class WutheringWavesCataloguePage
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public WutheringWavesCataloguePageData? Data { get; set; }
}


internal class WutheringWavesCataloguePageData
{
    [JsonPropertyName("results")]
    public WutheringWavesCataloguePageResults? Results { get; set; }
}


internal class WutheringWavesCataloguePageResults
{
    [JsonPropertyName("records")]
    public List<WutheringWavesCatalogueItem> Records { get; set; } = [];
}


internal class WutheringWavesCatalogueItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("content")]
    public WutheringWavesCatalogueItemContent? Content { get; set; }
}


internal class WutheringWavesCatalogueItemContent
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("contentUrl")]
    public string? ContentUrl { get; set; }

    [JsonPropertyName("star")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Rarity { get; set; }
}
