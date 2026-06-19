using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;

namespace Nebula.Core.Gacha.Endfield;

public class EndfieldGachaClient : GachaLogClient
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.6422.112 Safari/537.36";

    private static readonly Regex GachaUrlRegex = new("""https://ef-webview\.(hypergryph|gryphline)\.com/page/gacha_[^\s"']*""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly (string Key, EndfieldGachaType Type)[] CharacterPoolTypes =
    [
        ("E_CharacterGachaPoolType_Special", EndfieldGachaType.SpecialCharacter),
        ("E_CharacterGachaPoolType_Joint", EndfieldGachaType.JointCharacter),
        ("E_CharacterGachaPoolType_Standard", EndfieldGachaType.StandardCharacter),
        ("E_CharacterGachaPoolType_Beginner", EndfieldGachaType.BeginnerCharacter),
    ];


    public override IReadOnlyCollection<IGachaType> QueryGachaTypes { get; init; } =
    [
        new EndfieldGachaType(EndfieldGachaType.SpecialCharacter),
        new EndfieldGachaType(EndfieldGachaType.JointCharacter),
        new EndfieldGachaType(EndfieldGachaType.StandardCharacter),
        new EndfieldGachaType(EndfieldGachaType.BeginnerCharacter),
        new EndfieldGachaType(EndfieldGachaType.Weapon),
    ];


    public EndfieldGachaClient(HttpClient? httpClient = null) : base(httpClient)
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
    }


    public static string? GetGachaUrlFromGameLogs(GameBiz gameBiz)
    {
        var provider = gameBiz.Value switch
        {
            GameBiz.endfield_global => "gryphline",
            GameBiz.endfield_cn => "hypergryph",
            _ => null,
        };
        if (provider is not null)
        {
            var url = GetGachaUrlFromGameLogs(provider);
            if (!string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }
        return GetGachaUrlFromGameLogs("hypergryph") ?? GetGachaUrlFromGameLogs("gryphline");
    }


    public static string? GetGachaUrlFromGameLogs(string provider)
    {
        foreach (var file in GetCandidateLogFiles(provider))
        {
            if (!File.Exists(file))
            {
                continue;
            }
            var text = ReadLogText(file);
            var url = FindGachaUrl(text);
            if (!string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }
        return null;
    }


    public static string BuildGachaUrl(string provider, string u8Token, string serverId, string? lang = null)
    {
        provider = NormalizeProvider(provider);
        var values = HttpUtility.ParseQueryString("");
        values["pool_id"] = "nebula";
        values["u8_token"] = u8Token;
        values["server_id"] = string.IsNullOrWhiteSpace(serverId) && provider == "hypergryph" ? "1" : serverId;
        values["lang"] = FilterLanguage(lang) ?? "zh-cn";
        return $"https://ef-webview.{provider}.com/page/gacha_nebula?{values}";
    }


    public static string ExtractAccountToken(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }
        text = text.Trim();
        if ((text.StartsWith('{') && text.EndsWith('}')) || (text.StartsWith('[') && text.EndsWith(']')))
        {
            try
            {
                using var document = JsonDocument.Parse(text);
                if (TryFindString(document.RootElement, ["token", "account_token", "content"], out var value))
                {
                    return value;
                }
            }
            catch
            {
            }
        }
        return text.Trim('"');
    }


    public async Task<string> GetOAuthTokenAsync(string provider, string loginToken, CancellationToken cancellationToken = default)
    {
        provider = NormalizeProvider(provider);
        loginToken = ExtractAccountToken(loginToken);
        var appCode = provider == "gryphline" ? "3dacefa138426cfe" : "be36d44aa36bfb5b";
        using var req = new HttpRequestMessage(HttpMethod.Post, $"https://as.{provider}.com/user/oauth2/v2/grant");
        req.Content = JsonContent.Create(new
        {
            token = loginToken,
            appCode,
            type = 1,
        });
        using var res = await _httpClient.SendAsync(req, cancellationToken);
        var text = await res.Content.ReadAsStringAsync(cancellationToken);
        using var document = ParseJsonResponse(text, "Endfield OAuth response");
        var root = document.RootElement;
        var status = GetInt32(root, "status", -1);
        if (status != 0)
        {
            throw new miHoYoApiException(status, GetString(root, "msg") ?? "Cannot get Endfield OAuth token.");
        }
        var token = root.TryGetProperty("data", out var data) ? GetString(data, "token") : null;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new miHoYoApiException(-1, "Endfield OAuth token is empty.");
        }
        return token;
    }


    public async Task<string> GetU8TokenByUidAsync(long uid, string loginToken, string provider, CancellationToken cancellationToken = default)
    {
        provider = NormalizeProvider(provider);
        var oauthToken = await GetOAuthTokenAsync(provider, loginToken, cancellationToken);
        using var req = new HttpRequestMessage(HttpMethod.Post, $"https://binding-api-account-prod.{provider}.com/account/binding/v1/u8_token_by_uid");
        req.Content = JsonContent.Create(new
        {
            uid = uid.ToString(CultureInfo.InvariantCulture),
            token = oauthToken,
        });
        using var res = await _httpClient.SendAsync(req, cancellationToken);
        var text = await res.Content.ReadAsStringAsync(cancellationToken);
        using var document = ParseJsonResponse(text, "Endfield u8 token response");
        var root = document.RootElement;
        var status = GetInt32(root, "status", -1);
        if (status != 0)
        {
            throw new miHoYoApiException(status, GetString(root, "msg") ?? "Cannot get Endfield u8 token.");
        }
        var token = root.TryGetProperty("data", out var data) ? GetString(data, "token") : null;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new miHoYoApiException(-1, "Endfield u8 token is empty.");
        }
        return token;
    }


    public async Task<List<EndfieldBindingAccount>> GetBindingsByTokenAsync(string loginToken, string provider, CancellationToken cancellationToken = default)
    {
        provider = NormalizeProvider(provider);
        var oauthToken = await GetOAuthTokenAsync(provider, loginToken, cancellationToken);
        var values = HttpUtility.ParseQueryString("");
        values["token"] = oauthToken;
        values["appCode"] = "endfield";
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://binding-api-account-prod.{provider}.com/account/binding/v1/binding_list?{values}");
        using var res = await _httpClient.SendAsync(req, cancellationToken);
        var text = await res.Content.ReadAsStringAsync(cancellationToken);
        using var document = ParseJsonResponse(text, "Endfield binding list response");
        var root = document.RootElement;
        var status = GetInt32(root, "status", -1);
        if (status != 0)
        {
            throw new miHoYoApiException(status, GetString(root, "msg") ?? "Cannot get Endfield account list.");
        }
        var result = new List<EndfieldBindingAccount>();
        if (!root.TryGetProperty("data", out var data)
            || !data.TryGetProperty("list", out var appList)
            || appList.ValueKind != JsonValueKind.Array)
        {
            return result;
        }
        var appInfo = appList.EnumerateArray().FirstOrDefault(x => string.Equals(GetString(x, "appCode"), "endfield", StringComparison.OrdinalIgnoreCase));
        if (appInfo.ValueKind is JsonValueKind.Undefined)
        {
            appInfo = appList.EnumerateArray().FirstOrDefault();
        }
        if (!appInfo.TryGetProperty("bindingList", out var bindingList) || bindingList.ValueKind != JsonValueKind.Array)
        {
            return result;
        }
        foreach (var binding in bindingList.EnumerateArray())
        {
            var uidText = GetString(binding, "uid") ?? "";
            if (!long.TryParse(uidText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uid))
            {
                continue;
            }
            if (binding.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array && roles.GetArrayLength() > 0)
            {
                foreach (var role in roles.EnumerateArray())
                {
                    result.Add(new EndfieldBindingAccount
                    {
                        Uid = uid,
                        Provider = provider,
                        ServerId = provider == "hypergryph" ? "1" : GetString(role, "serverId") ?? "",
                        ServerName = GetString(role, "serverName") ?? "",
                        RoleId = GetString(role, "roleId") ?? "",
                        NickName = GetString(role, "nickName") ?? "",
                    });
                }
            }
            else
            {
                result.Add(new EndfieldBindingAccount
                {
                    Uid = uid,
                    Provider = provider,
                    ServerId = provider == "hypergryph" ? "1" : "",
                });
            }
        }
        return result;
    }


    public override async Task<long> GetUidByGachaUrlAsync(string gachaUrl)
    {
        var query = ParseUrl(gachaUrl);
        var role = await QueryRoleListAsync(query.Provider, query.U8Token, query.ServerId);
        return long.TryParse(role.Uid, out var uid) ? uid : 0;
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
        var query = ParseUrl(gachaUrl, lang);
        var uid = await GetUidByGachaUrlAsync(gachaUrl);
        if (gachaType.Value == EndfieldGachaType.Weapon)
        {
            return await GetWeaponGachaLogAsync(query, uid, endId, lang, progress, cancellationToken);
        }
        var poolType = CharacterPoolTypes.FirstOrDefault(x => x.Type.Value == gachaType.Value);
        if (string.IsNullOrWhiteSpace(poolType.Key))
        {
            return [];
        }
        return await GetCharacterGachaLogAsync(query, uid, poolType.Key, poolType.Type, endId, lang, progress, cancellationToken);
    }


    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaLogQuery query, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync(gachaUrl, query.GachaType, query.EndId, cancellationToken: cancellationToken);
    }


    private async Task<List<GachaLogItem>> GetCharacterGachaLogAsync(EndfieldQuery query, long uid, string poolType, EndfieldGachaType gachaType, long endId, string? lang, IProgress<(IGachaType GachaType, int Page)>? progress, CancellationToken cancellationToken)
    {
        var url = $"https://ef-webview.{query.Provider}.com/api/record/char";
        var result = new List<GachaLogItem>();
        await foreach (var page in FetchPaginatedDataAsync(url, query, new Dictionary<string, string> { ["pool_type"] = poolType }, gachaType, progress, cancellationToken))
        {
            var shouldStop = false;
            foreach (var item in page)
            {
                var record = ToCharacterGachaLogItem(uid, query, gachaType.Value, item);
                if (endId > 0 && record.Id <= endId)
                {
                    shouldStop = true;
                    continue;
                }
                result.Add(record);
            }
            if (shouldStop)
            {
                break;
            }
        }
        return result;
    }


    private async Task<List<GachaLogItem>> GetWeaponGachaLogAsync(EndfieldQuery query, long uid, long endId, string? lang, IProgress<(IGachaType GachaType, int Page)>? progress, CancellationToken cancellationToken)
    {
        var pools = await GetWeaponPoolsAsync(query, cancellationToken);
        var result = new List<GachaLogItem>();
        foreach (var pool in pools)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var url = $"https://ef-webview.{query.Provider}.com/api/record/weapon";
            await foreach (var page in FetchPaginatedDataAsync(url, query, new Dictionary<string, string> { ["pool_id"] = pool.PoolId }, new EndfieldGachaType(EndfieldGachaType.Weapon), progress, cancellationToken))
            {
                var shouldStop = false;
                foreach (var item in page)
                {
                    var record = ToWeaponGachaLogItem(uid, query, item, pool);
                    if (endId > 0 && record.Id <= endId)
                    {
                        shouldStop = true;
                        continue;
                    }
                    result.Add(record);
                }
                if (shouldStop)
                {
                    break;
                }
            }
        }
        return result;
    }


    private async IAsyncEnumerable<List<JsonElement>> FetchPaginatedDataAsync(string url, EndfieldQuery query, Dictionary<string, string> extraParams, IGachaType gachaType, IProgress<(IGachaType GachaType, int Page)>? progress, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var seqId = "";
        var hasMore = true;
        var page = 0;
        while (hasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();
            page++;
            progress?.Report((gachaType, page));
            await Task.Delay(Random.Shared.Next(500, 1000), cancellationToken);

            var values = HttpUtility.ParseQueryString("");
            values["lang"] = query.Language;
            values["token"] = query.U8Token;
            values["server_id"] = query.ServerId;
            foreach (var item in extraParams)
            {
                values[item.Key] = item.Value;
            }
            if (!string.IsNullOrWhiteSpace(seqId))
            {
                values["seq_id"] = seqId;
            }

            using var document = await GetJsonDocumentWithRetryAsync($"{url}?{values}", cancellationToken);
            var root = document.RootElement;
            var code = GetInt32(root, "code", -1);
            if (code != 0)
            {
                throw new miHoYoApiException(code, GetString(root, "msg") ?? "Endfield gacha API returned an error.");
            }
            if (!root.TryGetProperty("data", out var data) || data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                yield break;
            }
            if (!data.TryGetProperty("list", out var listElement) || listElement.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            var list = listElement.EnumerateArray().Select(x => x.Clone()).ToList();
            if (list.Count == 0)
            {
                yield break;
            }
            yield return list;

            hasMore = GetBoolean(data, "hasMore");
            seqId = GetString(list[^1], "seqId") ?? "";
        }
    }


    private async Task<List<EndfieldWeaponPool>> GetWeaponPoolsAsync(EndfieldQuery query, CancellationToken cancellationToken)
    {
        var values = HttpUtility.ParseQueryString("");
        values["lang"] = query.Language;
        values["token"] = query.U8Token;
        values["server_id"] = query.ServerId;
        using var document = await GetJsonDocumentWithRetryAsync($"https://ef-webview.{query.Provider}.com/api/record/weapon/pool?{values}", cancellationToken);
        var root = document.RootElement;
        var code = GetInt32(root, "code", -1);
        if (code != 0)
        {
            throw new miHoYoApiException(code, GetString(root, "msg") ?? "Cannot get Endfield weapon pools.");
        }
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }
        return data.EnumerateArray()
                   .Select(x => new EndfieldWeaponPool(GetString(x, "poolId") ?? "", GetString(x, "poolName") ?? ""))
                   .Where(x => !string.IsNullOrWhiteSpace(x.PoolId))
                   .ToList();
    }


    private async Task<EndfieldRoleInfo> QueryRoleListAsync(string provider, string u8Token, string serverId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"https://u8.{provider}.com/game/role/v1/query_role_list");
        req.Content = JsonContent.Create(new { token = u8Token, serverId });
        using var res = await _httpClient.SendAsync(req);
        var text = await res.Content.ReadAsStringAsync();
        using var document = ParseJsonResponse(text, "Endfield role list response");
        var root = document.RootElement;
        var status = GetInt32(root, "status", -1);
        if (status != 0)
        {
            throw new miHoYoApiException(status, GetString(root, "msg") ?? "Cannot query Endfield role list.");
        }
        var data = root.GetProperty("data");
        var uid = GetString(data, "uid") ?? "";
        var role = data.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array
            ? roles.EnumerateArray().FirstOrDefault(x => string.Equals(GetString(x, "serverId"), serverId, StringComparison.OrdinalIgnoreCase))
            : default;
        if (role.ValueKind is JsonValueKind.Undefined && roles.ValueKind == JsonValueKind.Array)
        {
            role = roles.EnumerateArray().FirstOrDefault();
        }
        return new EndfieldRoleInfo(uid, GetString(role, "roleId") ?? "", GetString(role, "nickName") ?? GetString(role, "nickname") ?? "", GetString(role, "serverName") ?? "");
    }


    private async Task<JsonDocument> GetJsonDocumentWithRetryAsync(string url, CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var res = await _httpClient.GetAsync(url, cancellationToken);
                var text = await res.Content.ReadAsStringAsync(cancellationToken);
                if (!res.IsSuccessStatusCode)
                {
                    throw new miHoYoApiException((int)res.StatusCode, text);
                }
                return ParseJsonResponse(text, "Endfield gacha response");
            }
            catch (Exception ex) when (attempt < 3)
            {
                lastException = ex;
                await Task.Delay(Random.Shared.Next(500, 900), cancellationToken);
            }
        }
        throw lastException ?? new miHoYoApiException(-1, "Cannot get Endfield gacha response.");
    }


    private static EndfieldQuery ParseUrl(string url, string? lang = null)
    {
        var match = GachaUrlRegex.Match(url);
        if (!match.Success)
        {
            throw new ArgumentException(CoreLang.Gacha_CannotParseTheWishRecordURL);
        }
        var provider = match.Groups[1].Value.ToLowerInvariant();
        var uri = new Uri(match.Value);
        var values = HttpUtility.ParseQueryString(uri.Query);
        var u8Token = values["u8_token"] ?? values["token"] ?? "";
        if (string.IsNullOrWhiteSpace(u8Token))
        {
            throw new miHoYoApiException(-1, "Cannot find u8_token in Endfield gacha URL.");
        }
        var serverId = provider == "hypergryph" ? "1" : values["server_id"] ?? values["serverId"] ?? values["server"] ?? "";
        if (string.IsNullOrWhiteSpace(serverId))
        {
            throw new miHoYoApiException(-1, "Cannot find server_id in Endfield gacha URL.");
        }
        return new EndfieldQuery(provider, u8Token, serverId, FilterLanguage(lang) ?? values["lang"] ?? "zh-cn");
    }


    private static string NormalizeProvider(string provider)
    {
        return provider.Equals("gryphline", StringComparison.OrdinalIgnoreCase) ? "gryphline" : "hypergryph";
    }


    private static bool TryFindString(JsonElement element, string[] names, out string value)
    {
        value = "";
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String
                    && names.Any(x => string.Equals(x, property.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    value = property.Value.GetString() ?? "";
                    return !string.IsNullOrWhiteSpace(value);
                }
                if (TryFindString(property.Value, names, out value))
                {
                    return true;
                }
            }
        }
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryFindString(item, names, out value))
                {
                    return true;
                }
            }
        }
        return false;
    }


    private static GachaLogItem ToCharacterGachaLogItem(long uid, EndfieldQuery query, int gachaType, JsonElement item)
    {
        var seqId = GetString(item, "seqId") ?? "";
        var charId = GetString(item, "charId") ?? "";
        return new GachaLogItem
        {
            Uid = uid,
            Id = ToRecordId(seqId, gachaType, GetString(item, "gachaTs"), GetString(item, "charName")),
            Name = GetString(item, "charName") ?? "",
            Time = ParseEndfieldTime(GetString(item, "gachaTs")),
            ItemId = ToInt32Id(charId),
            ItemType = GetString(item, "poolName") ?? "角色",
            RankType = GetInt32(item, "rarity"),
            GachaType = gachaType,
            Count = 1,
            Lang = query.Language,
        };
    }


    private static GachaLogItem ToWeaponGachaLogItem(long uid, EndfieldQuery query, JsonElement item, EndfieldWeaponPool pool)
    {
        var seqId = GetString(item, "seqId") ?? "";
        var weaponId = GetString(item, "weaponId") ?? "";
        return new GachaLogItem
        {
            Uid = uid,
            Id = ToRecordId(seqId, EndfieldGachaType.Weapon, GetString(item, "gachaTs"), GetString(item, "weaponName")),
            Name = GetString(item, "weaponName") ?? "",
            Time = ParseEndfieldTime(GetString(item, "gachaTs")),
            ItemId = ToInt32Id(weaponId),
            ItemType = GetString(item, "poolName") ?? pool.PoolName ?? "武器",
            RankType = GetInt32(item, "rarity"),
            GachaType = EndfieldGachaType.Weapon,
            Count = 1,
            Lang = query.Language,
        };
    }


    private static string? FindGachaUrl(string text)
    {
        var matches = GachaUrlRegex.Matches(text);
        return matches.Count == 0 ? null : matches[^1].Value;
    }


    private static string ReadLogText(string path)
    {
        using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }


    private static IEnumerable<string> GetCandidateLogFiles(string provider)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var vendor = provider == "gryphline" ? "Gryphline" : "Hypergryph";
        yield return Path.Join(home, "AppData", "LocalLow", vendor, "Endfield", "sdklogs", "HGWebview.log");
    }


    private static JsonDocument ParseJsonResponse(string text, string name)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new miHoYoApiException(-1, $"{name} is empty.");
        }
        if (text.TrimStart().StartsWith('<'))
        {
            throw new miHoYoApiException(-1, $"{name} returned HTML instead of JSON.");
        }
        try
        {
            return JsonDocument.Parse(text);
        }
        catch (JsonException ex)
        {
            throw new miHoYoApiException(-1, $"{name} is not valid JSON. {ex.Message}");
        }
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


    private static int GetInt32(JsonElement element, string name, int defaultValue = 0)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return defaultValue;
        }
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
        {
            return intValue;
        }
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue) ? intValue : defaultValue;
    }


    private static bool GetBoolean(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return false;
        }
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => value.TryGetInt32(out var i) && i != 0,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var b) ? b : value.GetString() == "1",
            _ => false,
        };
    }


    private static DateTime ParseEndfieldTime(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new miHoYoApiException(-1, "The Endfield gacha record time is empty.");
        }
        text = text.Trim();
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix))
        {
            return (Math.Abs(unix) >= 10_000_000_000 ? DateTimeOffset.FromUnixTimeMilliseconds(unix) : DateTimeOffset.FromUnixTimeSeconds(unix)).LocalDateTime;
        }
        string[] formats =
        [
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy/M/d H:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
            "O",
        ];
        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var time)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out time)
            || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out time))
        {
            return time;
        }
        throw new miHoYoApiException(-1, $"Cannot parse Endfield gacha record time: {text}");
    }


    private static string? FilterLanguage(string? lang)
    {
        return lang?.ToLowerInvariant() switch
        {
            "zh-cn" or "zh-hans" => "zh-cn",
            "zh-tw" or "zh-hant" => "zh-tw",
            "ja" or "ja-jp" => "ja-jp",
            "ko" or "ko-kr" => "ko-kr",
            "en" or "en-us" => "en-us",
            _ => null,
        };
    }


    private static long ToRecordId(string seqId, int gachaType, string? time, string? name)
    {
        if (long.TryParse(seqId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return id;
        }
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{gachaType}:{seqId}:{time}:{name}"));
        return Math.Abs(BitConverter.ToInt64(bytes, 0));
    }


    private static int ToInt32Id(string id)
    {
        return int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }


    private sealed record EndfieldQuery(string Provider, string U8Token, string ServerId, string Language);

    private sealed record EndfieldRoleInfo(string Uid, string RoleId, string NickName, string ServerName);

    private sealed record EndfieldWeaponPool(string PoolId, string? PoolName);

    public sealed class EndfieldBindingAccount
    {
        public long Uid { get; set; }

        public string Provider { get; set; } = "hypergryph";

        public string ServerId { get; set; } = "";

        public string ServerName { get; set; } = "";

        public string RoleId { get; set; } = "";

        public string NickName { get; set; } = "";
    }
}
