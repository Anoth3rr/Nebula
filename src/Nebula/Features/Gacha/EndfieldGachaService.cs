using Dapper;
using Microsoft.Extensions.Logging;
using Nebula.Core;
using Nebula.Core.Gacha;
using Nebula.Core.Gacha.Endfield;
using Nebula.Features.Database;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Nebula.Features.Gacha;

internal class EndfieldGachaService : ThirdPartyGachaService
{
    private const string AccountUrlScheme = "nebula-endfield-account://";

    private readonly EndfieldGachaClient _endfieldClient;

    protected override GameBiz CurrentGameBiz { get; } = GameBiz.endfield;

    protected override string GachaTableName { get; } = "EndfieldGachaItem";


    public EndfieldGachaService(ILogger<GachaLogService> logger, EndfieldGachaClient client) : base(logger, client)
    {
        _endfieldClient = client;
    }


    public override string? GetGachaLogUrlFromWebCache(GameBiz gameBiz, string path)
    {
        return EndfieldGachaClient.GetGachaUrlFromGameLogs(gameBiz);
    }


    public override List<long> GetUids()
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<long>($"""
            SELECT DISTINCT Uid FROM {GachaTableName}
            UNION
            SELECT DISTINCT Uid FROM GachaLogUrl WHERE GameBiz = @GameBiz
            ORDER BY Uid;
            """, new { GameBiz = CurrentGameBiz }).ToList();
    }


    public override List<GachaLogItemEx> GetGachaLogItemEx(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Time, Id;", new { uid }).ToList();
        foreach (IGachaType type in QueryGachaTypes)
        {
            var typedItems = GetGachaLogItemsByQueryType(list, type);
            int index = 0;
            int pity = 0;
            foreach (var item in typedItems)
            {
                item.Index = ++index;
                item.Pity = ++pity;
                if (item.RankType == 6)
                {
                    pity = 0;
                }
            }
        }
        return list;
    }


    public override (List<GachaTypeStats> GachaStats, List<GachaLogItemEx> ItemStats) GetGachaTypeStats(long uid)
    {
        var statsList = new List<GachaTypeStats>();
        var groupStats = new List<GachaLogItemEx>();
        var allItems = GetGachaLogItemEx(uid);
        if (allItems.Count > 0)
        {
            foreach (IGachaType type in QueryGachaTypes)
            {
                var list = GetGachaLogItemsByQueryType(allItems, type);
                if (list.Count == 0)
                {
                    continue;
                }
                var stats = new GachaTypeStats
                {
                    GachaType = type.Value,
                    GachaTypeText = type.ToLocalization(),
                    TopRankType = 6,
                    Count = list.Count,
                    Count_6 = list.Count(x => x.RankType == 6),
                    Count_5 = list.Count(x => x.RankType == 5),
                    Count_4 = list.Count(x => x.RankType == 4),
                    Count_3 = list.Count(x => x.RankType == 3),
                    StartTime = list.First().Time,
                    EndTime = list.Last().Time,
                };
                stats.Ratio_5 = (double)stats.Count_5 / stats.Count;
                stats.Ratio_4 = (double)stats.Count_4 / stats.Count;
                stats.Ratio_3 = (double)stats.Count_3 / stats.Count;
                stats.List_All = list.AsEnumerable().Reverse().ToList();
                stats.List_5 = list.Where(x => x.RankType == 6).Reverse().ToList();
                stats.List_4 = list.Where(x => x.RankType == 5).Reverse().ToList();
                stats.Pity_5 = list.Last().Pity;
                if (list.Last().RankType == 6)
                {
                    stats.Pity_5 = 0;
                }
                stats.Average_5 = stats.Count_6 == 0 ? 0 : (double)(stats.Count - stats.Pity_5) / stats.Count_6;
                stats.Pity_4 = list.Count - 1 - list.FindLastIndex(x => x.RankType == 5);

                int pity5 = 0;
                foreach (var item in list)
                {
                    pity5++;
                    if (item.RankType == 5)
                    {
                        item.Pity = pity5;
                        pity5 = 0;
                    }
                }

                stats.List_5.Insert(0, new GachaLogItemEx
                {
                    GachaType = type.Value,
                    Name = Lang.GachaStatsCard_Pity,
                    Pity = stats.Pity_5,
                    Time = list.Last().Time,
                });
                stats.List_4.Insert(0, new GachaLogItemEx
                {
                    GachaType = type.Value,
                    Name = Lang.GachaStatsCard_Pity,
                    Pity = stats.Pity_4,
                    Time = list.Last().Time,
                });
                statsList.Add(stats);
            }
            groupStats = allItems.GroupBy(x => x.ItemId)
                                 .Select(x => { var item = x.First(); item.ItemCount = x.Count(); return item; })
                                 .OrderByDescending(x => x.RankType)
                                 .ThenByDescending(x => x.ItemCount)
                                 .ThenByDescending(x => x.Time)
                                 .ToList();
        }
        return (statsList, groupStats);
    }


    public async Task<List<EndfieldGachaClient.EndfieldBindingAccount>> GetAccountsByTokenAsync(string loginToken, string provider, CancellationToken cancellationToken = default)
    {
        return await _endfieldClient.GetBindingsByTokenAsync(loginToken, provider, cancellationToken);
    }


    public string SaveAccount(EndfieldGachaClient.EndfieldBindingAccount account, string loginToken)
    {
        if (account.Uid <= 0)
        {
            throw new ArgumentException("Endfield account uid is empty.", nameof(account));
        }
        var url = BuildAccountUrl(account, loginToken);
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("INSERT OR REPLACE INTO GachaLogUrl (GameBiz, Uid, Url, Time) VALUES (@GameBiz, @Uid, @Url, @Time);", new GachaLogUrl(CurrentGameBiz, account.Uid, url));
        return url;
    }


    public bool HasSavedAccount(long uid)
    {
        var url = GetGachaLogUrlByUid(uid);
        return IsAccountUrl(url);
    }


    public override async Task<long> GetUidFromGachaLogUrl(string url)
    {
        if (TryParseAccountUrl(url, out var account))
        {
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("INSERT OR REPLACE INTO GachaLogUrl (GameBiz, Uid, Url, Time) VALUES (@GameBiz, @Uid, @Url, @Time);", new GachaLogUrl(CurrentGameBiz, account.Uid, url));
            return account.Uid;
        }
        return await base.GetUidFromGachaLogUrl(url);
    }


    public override async Task<long> GetGachaLogAsync(string url, bool all, string? lang = null, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!TryParseAccountUrl(url, out var account))
        {
            return await base.GetGachaLogAsync(url, all, lang, progress, cancellationToken);
        }

        using var dapper = DatabaseService.CreateConnection();
        progress?.Report(Lang.GachaLogService_GettingUid);
        var u8Token = await _endfieldClient.GetU8TokenByUidAsync(account.Uid, account.LoginToken, account.Provider, cancellationToken);
        var gachaUrl = EndfieldGachaClient.BuildGachaUrl(account.Provider, u8Token, account.ServerId, lang);

        long endId = 0;
        if (!all)
        {
            endId = dapper.QueryFirstOrDefault<long>($"SELECT Id FROM {GachaTableName} WHERE Uid = @Uid ORDER BY Id DESC LIMIT 1;", new { Uid = account.Uid });
            _logger.LogInformation("Last Endfield gacha log id of uid {uid} is {endId}", account.Uid, endId);
        }

        var internalProgress = new Progress<(IGachaType GachaType, int Page)>((x) => progress?.Report(string.Format(Lang.GachaLogService_GetGachaProgressText, x.GachaType.ToLocalization(), x.Page)));
        var list = (await _client.GetGachaLogAsync(gachaUrl, endId, lang, internalProgress, cancellationToken)).ToList();
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }
        var oldCount = dapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {GachaTableName} WHERE Uid = @Uid;", new { Uid = account.Uid });
        InsertGachaLogItems(list);
        var newCount = dapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {GachaTableName} WHERE Uid = @Uid;", new { Uid = account.Uid });
        progress?.Report(string.Format(Lang.GachaLogService_GetGachaResult, list.Count, newCount - oldCount));
        return account.Uid;
    }


    private static string BuildAccountUrl(EndfieldGachaClient.EndfieldBindingAccount account, string loginToken)
    {
        var values = HttpUtility.ParseQueryString("");
        values["provider"] = account.Provider;
        values["uid"] = account.Uid.ToString(CultureInfo.InvariantCulture);
        values["token"] = EndfieldGachaClient.ExtractAccountToken(loginToken);
        values["server_id"] = account.Provider == "hypergryph" ? "1" : account.ServerId;
        values["role_id"] = account.RoleId;
        values["nickname"] = account.NickName;
        values["server_name"] = account.ServerName;
        return $"{AccountUrlScheme}?{values}";
    }


    private static bool IsAccountUrl(string? url)
    {
        return url?.StartsWith(AccountUrlScheme, StringComparison.OrdinalIgnoreCase) is true;
    }


    private static bool TryParseAccountUrl(string? url, out EndfieldSavedAccount account)
    {
        account = default;
        if (!IsAccountUrl(url))
        {
            return false;
        }
        var queryIndex = url!.IndexOf('?');
        if (queryIndex < 0 || queryIndex == url.Length - 1)
        {
            return false;
        }
        var values = HttpUtility.ParseQueryString(url[(queryIndex + 1)..]);
        if (!long.TryParse(values["uid"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var uid) || uid <= 0)
        {
            return false;
        }
        var provider = string.Equals(values["provider"], "gryphline", StringComparison.OrdinalIgnoreCase) ? "gryphline" : "hypergryph";
        var token = values["token"] ?? "";
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }
        var serverId = provider == "hypergryph" ? "1" : values["server_id"] ?? "";
        if (string.IsNullOrWhiteSpace(serverId))
        {
            return false;
        }
        account = new EndfieldSavedAccount(uid, provider, token, serverId);
        return true;
    }


    private readonly record struct EndfieldSavedAccount(long Uid, string Provider, string LoginToken, string ServerId);
}
