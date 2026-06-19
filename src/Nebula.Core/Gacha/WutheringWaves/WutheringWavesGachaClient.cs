using System.Net.Http.Json;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;

namespace Nebula.Core.Gacha.WutheringWaves;

public class WutheringWavesGachaClient : GachaLogClient
{
    private static readonly Regex GachaUrlRegex = new("""https://aki-gm-resources(-oversea)?\.aki-game\.(net|com)/aki/gacha/index\.html#/record[^\s"']*""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private const int GachaRecordPageSize = 10;


    public override IReadOnlyCollection<IGachaType> QueryGachaTypes { get; init; } = new WutheringWavesGachaType[] { 1, 2, 3, 4, 5 }.Cast<IGachaType>().ToList().AsReadOnly();


    public WutheringWavesGachaClient(HttpClient? httpClient = null) : base(httpClient)
    {

    }


    public static string? GetGachaUrlFromGameLogs(string installPath)
    {
        foreach (var file in GetCandidateLogFiles(installPath))
        {
            if (!File.Exists(file))
            {
                continue;
            }
            var text = ReadLogText(file, decrypt: file.EndsWith("Client.log", StringComparison.OrdinalIgnoreCase));
            var url = FindGachaUrl(text);
            if (!string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }
        return null;
    }


    public override async Task<long> GetUidByGachaUrlAsync(string gachaUrl)
    {
        var query = ParseUrl(gachaUrl);
        if (long.TryParse(query.PlayerId, out long uid))
        {
            return uid;
        }
        return await base.GetUidByGachaUrlAsync(gachaUrl);
    }


    protected override string GetGachaUrlPrefix(string gachaUrl, string? lang = null)
    {
        if (GachaUrlRegex.IsMatch(gachaUrl))
        {
            return gachaUrl;
        }
        throw new ArgumentException(CoreLang.Gacha_CannotParseTheWishRecordURL);
    }


    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = new List<GachaLogItem>();
        foreach (var type in QueryGachaTypes)
        {
            result.AddRange(await GetGachaLogAsync(gachaUrl, type, endId, lang, progress, cancellationToken));
        }
        return result;
    }


    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, IGachaType gachaType, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        var query = ParseUrl(gachaUrl);
        var result = new List<GachaLogItem>();
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report((gachaType, 1));
        var items = await GetPageAsync(query, gachaType.Value, lang, cancellationToken);
        result.AddRange(items.Select((x, index) => ToGachaLogItem(query, gachaType.Value, x, GetSubId(index))));
        return result;
    }


    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaLogQuery query, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync(gachaUrl, query.GachaType, query.EndId, cancellationToken: cancellationToken);
    }


    private async Task<List<WutheringWavesGachaRecord>> GetPageAsync(WutheringWavesQuery query, int gachaType, string? lang, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 300), cancellationToken);
        var host = query.IsOversea ? "https://gmserver-api.aki-game2.net/gacha/record/query" : "https://gmserver-api.aki-game2.com/gacha/record/query";
        var payload = new WutheringWavesGachaRequest
        {
            ServerId = query.ServerId,
            PlayerId = query.PlayerId,
            CardPoolType = gachaType,
            RecordId = query.RecordId,
            CardPoolId = query.CardPoolId,
            LanguageCode = FilterLanguage(lang) ?? query.LanguageCode,
        };
        var response = await _httpClient.PostAsJsonAsync(host, payload, cancellationToken);
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (root.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            throw new miHoYoApiException(-1, "Response body is null");
        }
        int code = root.TryGetProperty("code", out var codeElement) && codeElement.TryGetInt32(out var c) ? c : -1;
        string? message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;
        if (code != 0)
        {
            throw new miHoYoApiException(code, message);
        }
        if (!root.TryGetProperty("data", out var dataElement) || dataElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }
        if (dataElement.ValueKind != JsonValueKind.Array)
        {
            throw new miHoYoApiException(-1, "Unexpected convene record response.");
        }
        var list = new List<WutheringWavesGachaRecord>();
        foreach (var item in dataElement.EnumerateArray())
        {
            list.Add(ParseGachaRecord(item));
        }
        return list;
    }


    private static WutheringWavesQuery ParseUrl(string url)
    {
        var match = GachaUrlRegex.Match(url);
        if (!match.Success)
        {
            throw new ArgumentException(CoreLang.Gacha_CannotParseTheWishRecordURL);
        }
        var fragment = match.Value.Split('#').ElementAtOrDefault(1) ?? "";
        var queryText = fragment.Split('?').ElementAtOrDefault(1) ?? "";
        var values = HttpUtility.ParseQueryString(queryText);
        return new WutheringWavesQuery
        {
            IsOversea = match.Groups[1].Success || match.Value.Contains("aki-game.net", StringComparison.OrdinalIgnoreCase),
            ServerId = values["svr_id"] ?? values["server_id"] ?? values["serverId"] ?? "",
            PlayerId = values["player_id"] ?? values["playerId"] ?? "",
            RecordId = values["record_id"] ?? values["recordId"] ?? "",
            CardPoolId = values["resources_id"] ?? values["resourcesId"] ?? "",
            LanguageCode = values["lang"] ?? values["language_code"] ?? values["languageCode"] ?? "zh-Hans",
        };
    }


    private static string? FilterLanguage(string? lang)
    {
        return lang?.ToLowerInvariant() switch
        {
            "zh-cn" or "zh-hans" => "zh-Hans",
            "zh-tw" or "zh-hant" => "zh-Hant",
            "ja" or "ja-jp" => "ja",
            "ko" or "ko-kr" => "ko",
            "en" or "en-us" => "en",
            _ => null,
        };
    }


    private static GachaLogItem ToGachaLogItem(WutheringWavesQuery query, int gachaType, WutheringWavesGachaRecord item, int subId)
    {
        long uid = long.TryParse(query.PlayerId, out var value) ? value : 0;
        var time = ParseKuroTime(item.Time);
        return new GachaLogItem
        {
            Uid = uid,
            Id = item.Id > 0 ? item.Id : StableId(gachaType, time, subId),
            Name = item.Name ?? "",
            Time = time,
            ItemId = 0,
            ItemType = item.ResourceTypeText ?? item.ResourceType ?? "",
            RankType = item.QualityLevel,
            GachaType = gachaType,
            Count = 1,
            Lang = query.LanguageCode,
        };
    }


    private static WutheringWavesGachaRecord ParseGachaRecord(JsonElement element)
    {
        return new WutheringWavesGachaRecord
        {
            Id = GetInt64(element, "id"),
            Name = GetString(element, "name"),
            Time = GetPropertyOrDefault(element, "time"),
            QualityLevel = GetInt32(element, "qualityLevel"),
            ResourceType = GetString(element, "resourceType"),
            ResourceTypeText = GetString(element, "resourceTypeText"),
        };
    }


    private static JsonElement GetPropertyOrDefault(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value.Clone() : default;
    }


    private static string? GetString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };
    }


    private static int GetInt32(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return 0;
        }
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
        {
            return intValue;
        }
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue) ? intValue : 0;
    }


    private static long GetInt64(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return 0;
        }
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var longValue))
        {
            return longValue;
        }
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue) ? longValue : 0;
    }


    private static DateTime ParseKuroTime(JsonElement element)
    {
        string? text = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null,
        };
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new miHoYoApiException(-1, "The convene record time is empty.");
        }
        text = text.Trim();
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long unix))
        {
            try
            {
                return (Math.Abs(unix) >= 10_000_000_000
                        ? DateTimeOffset.FromUnixTimeMilliseconds(unix)
                        : DateTimeOffset.FromUnixTimeSeconds(unix)).LocalDateTime;
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }
        string[] formats =
        [
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/M/d H:mm:ss",
            "yyyy/M/dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
            "O",
        ];
        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var exactTime)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out exactTime)
            || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out exactTime))
        {
            return exactTime;
        }
        throw new miHoYoApiException(-1, $"Cannot parse convene record time: {text}");
    }


    private static int GetSubId(int index)
    {
        return GachaRecordPageSize - index % GachaRecordPageSize;
    }


    private static long StableId(int gachaType, DateTime time, int subId)
    {
        return new DateTimeOffset(time).ToUnixTimeSeconds() * 1000 + gachaType * 10 + subId;
    }


    private static string? FindGachaUrl(string text)
    {
        var matches = GachaUrlRegex.Matches(text);
        return matches.Count == 0 ? null : matches[^1].Value;
    }


    private static string ReadLogText(string path, bool decrypt)
    {
        using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        var bytes = ms.ToArray();
        if (decrypt)
        {
            var decrypted = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                decrypted[i] = (byte)((bytes[i] & 0x0F) % 2 == 1 ? bytes[i] ^ 0xA5 : bytes[i] ^ 0xEF);
            }
            var text = Encoding.UTF8.GetString(decrypted);
            if (GachaUrlRegex.IsMatch(text))
            {
                return text;
            }
        }
        return Encoding.UTF8.GetString(bytes);
    }


    private static IEnumerable<string> GetCandidateLogFiles(string installPath)
    {
        yield return Path.Join(installPath, @"Client\Saved\Logs\Client.log");
        yield return Path.Join(installPath, @"Client\Binaries\Win64\ThirdParty\KrPcSdk_Global\KRSDKRes\KRSDKWebView\debug.log");
    }


    private sealed class WutheringWavesQuery
    {
        public bool IsOversea { get; init; }

        public string ServerId { get; init; } = "";

        public string PlayerId { get; init; } = "";

        public string RecordId { get; init; } = "";

        public string CardPoolId { get; init; } = "";

        public string LanguageCode { get; init; } = "zh-Hans";
    }


    private sealed class WutheringWavesGachaRequest
    {
        [JsonPropertyName("serverId")]
        public string ServerId { get; init; } = "";

        [JsonPropertyName("playerId")]
        public string PlayerId { get; init; } = "";

        [JsonPropertyName("cardPoolType")]
        public int CardPoolType { get; init; }

        [JsonPropertyName("recordId")]
        public string RecordId { get; init; } = "";

        [JsonPropertyName("cardPoolId")]
        public string CardPoolId { get; init; } = "";

        [JsonPropertyName("languageCode")]
        public string LanguageCode { get; init; } = "zh-Hans";

    }


    private sealed class WutheringWavesGachaRecord
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("time")]
        public JsonElement Time { get; set; }

        [JsonPropertyName("qualityLevel")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int QualityLevel { get; set; }

        [JsonPropertyName("resourceType")]
        public string? ResourceType { get; set; }

        [JsonPropertyName("resourceTypeText")]
        public string? ResourceTypeText { get; set; }
    }
}
