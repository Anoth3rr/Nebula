using System.Text.Json.Serialization;

namespace Nebula.Core.Gacha.Endfield;

public class EndfieldGachaInfo
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Icon { get; set; } = "";

    public int CatalogueId { get; set; }
}


internal class SklandWikiCatalogResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("data")]
    public SklandWikiCatalogData? Data { get; set; }
}


internal class SklandWikiCatalogData
{
    [JsonPropertyName("catalog")]
    public List<SklandWikiCatalogMain> Catalog { get; set; } = [];
}


internal class SklandWikiCatalogMain
{
    [JsonPropertyName("typeSub")]
    public List<SklandWikiCatalogSubType> TypeSub { get; set; } = [];
}


internal class SklandWikiCatalogSubType
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("items")]
    public List<SklandWikiCatalogItem> Items { get; set; } = [];
}


internal class SklandWikiCatalogItem
{
    [JsonPropertyName("itemId")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int ItemId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("brief")]
    public SklandWikiCatalogItemBrief? Brief { get; set; }
}


internal class SklandWikiCatalogItemBrief
{
    [JsonPropertyName("cover")]
    public string? Cover { get; set; }
}


internal class SklandAuthResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("data")]
    public SklandAuthData? Data { get; set; }
}


internal class SklandAuthData
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
