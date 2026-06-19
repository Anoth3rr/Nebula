using Dapper;
using Microsoft.Extensions.Logging;
using Nebula.Core;
using Nebula.Core.Gacha;
using Nebula.Core.Gacha.WutheringWaves;
using Nebula.Features.Database;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nebula.Features.Gacha;

internal class WutheringWavesGachaService : ThirdPartyGachaService
{
    protected override GameBiz CurrentGameBiz { get; } = GameBiz.wutheringwaves;

    protected override string GachaTableName { get; } = "WutheringWavesGachaItem";


    public WutheringWavesGachaService(ILogger<GachaLogService> logger, WutheringWavesGachaClient client) : base(logger, client)
    {

    }


    public override string? GetGachaLogUrlFromWebCache(GameBiz gameBiz, string path)
    {
        return WutheringWavesGachaClient.GetGachaUrlFromGameLogs(path);
    }


    public override List<GachaLogItemEx> GetGachaLogItemEx(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>("""
            SELECT item.*, COALESCE(infoById.Icon, infoByName.Icon) Icon
            FROM WutheringWavesGachaItem item
            LEFT JOIN WutheringWavesGachaInfo infoById ON item.ItemId = infoById.Id
            LEFT JOIN WutheringWavesGachaInfo infoByName ON item.Name = infoByName.Name
            WHERE Uid = @uid
            ORDER BY item.Time, item.Id;
            """, new { uid }).ToList();
        foreach (IGachaType type in QueryGachaTypes)
        {
            var l = GetGachaLogItemsByQueryType(list, type);
            int index = 0;
            int pity = 0;
            foreach (var item in l)
            {
                item.Index = ++index;
                item.Pity = ++pity;
                if (item.RankType == 5)
                {
                    pity = 0;
                }
            }
        }
        return list;
    }


    protected override int InsertGachaLogItems(List<GachaLogItem> items)
    {
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        dapper.Execute("""
            DELETE FROM WutheringWavesGachaItem
            WHERE Uid = @Uid AND GachaType = @GachaType AND Time = @Time AND Name = @Name;
            """, items, t);
        int affect = dapper.Execute("""
            INSERT OR REPLACE INTO WutheringWavesGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @Count, @Lang);
            """, items, t);
        t.Commit();
        UpdateGachaItemId();
        return affect;
    }


    public override (List<GachaTypeStats> GachaStats, List<GachaLogItemEx> ItemStats) GetGachaTypeStats(long uid)
    {
        var result = base.GetGachaTypeStats(uid);
        var allItems = GetGachaLogItemEx(uid);
        var groupStats = allItems.GroupBy(x => x.ItemId > 0 ? $"id:{x.ItemId}" : $"name:{x.Name}")
                                 .Select(x =>
                                 {
                                     var item = x.OrderByDescending(y => !string.IsNullOrWhiteSpace(y.Icon))
                                                 .ThenByDescending(y => y.Time)
                                                 .First();
                                     item.ItemCount = x.Count();
                                     return item;
                                 })
                                 .OrderByDescending(x => x.RankType)
                                 .ThenByDescending(x => x.ItemCount)
                                 .ThenByDescending(x => x.Time)
                                 .ToList();
        return (result.GachaStats, groupStats);
    }


    public override async Task<string> UpdateGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        var data = await _client.GetWutheringWavesGachaInfoAsync(cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        dapper.Execute("""
            INSERT OR REPLACE INTO WutheringWavesGachaInfo (Id, Name, Icon, CatalogueId, Rarity)
            VALUES (@Id, @Name, @Icon, @CatalogueId, @Rarity);
            """, data, t);
        t.Commit();
        UpdateGachaItemId();
        return lang;
    }


    public override async Task<(string Language, int Count)> ChangeGachaItemNameAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        await UpdateGachaInfoAsync(gameBiz, lang, cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
        int count = dapper.Execute("""
            INSERT OR REPLACE INTO WutheringWavesGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            SELECT item.Uid, item.Id, info.Name, Time, item.ItemId, ItemType, RankType, GachaType, Count, @Lang
            FROM WutheringWavesGachaItem item INNER JOIN WutheringWavesGachaInfo info ON item.ItemId = info.Id;
            """, new { Lang = lang });
        return (lang, count);
    }


    private static void UpdateGachaItemId()
    {
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO WutheringWavesGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            SELECT item.Uid, item.Id, item.Name, Time, info.Id, ItemType, RankType, GachaType, Count, Lang
            FROM WutheringWavesGachaItem item
            INNER JOIN WutheringWavesGachaInfo info ON item.Name = info.Name
            LEFT JOIN WutheringWavesGachaInfo existingInfo ON item.ItemId = existingInfo.Id
            WHERE item.ItemId = 0 OR existingInfo.Id IS NULL;
            """);
    }
}
