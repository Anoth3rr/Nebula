using Dapper;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Nebula.Core;
using Nebula.Core.Gacha;
using Nebula.Features.Database;
using Nebula.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nebula.Features.Gacha;

internal abstract class ThirdPartyGachaService : GachaLogService
{
    protected ThirdPartyGachaService(ILogger<GachaLogService> logger, GachaLogClient client) : base(logger, client)
    {

    }


    protected override List<GachaLogItemEx> GetGachaLogItemsByQueryType(IEnumerable<GachaLogItemEx> items, IGachaType type)
    {
        return type.Value == 0 ? items.ToList() : items.Where(x => x.GachaType == type.Value).ToList();
    }


    public override List<GachaLogItemEx> GetGachaLogItemEx(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Time, Id;", new { uid }).ToList();
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
        var affect = dapper.Execute($"""
            INSERT OR REPLACE INTO {GachaTableName} (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @Count, @Lang);
            """, items, t);
        t.Commit();
        return affect;
    }


    public override async Task ExportGachaLogAsync(long uid, string file, string format)
    {
        if (format is "excel")
        {
            var list = GetGachaLogItemEx(uid).Select(item => new
            {
                item.Uid,
                Id = item.IdText,
                item.Time,
                item.Name,
                item.ItemType,
                item.RankType,
                GachaType = QueryGachaTypes.FirstOrDefault(x => x.Value == item.GachaType)?.ToLocalization() ?? item.GachaType.ToString(),
                item.Index,
                item.Pity,
            });
            await MiniExcel.SaveAsAsync(file, list, overwriteFile: true);
        }
        else
        {
            using var dapper = DatabaseService.CreateConnection();
            var list = dapper.Query<GachaLogItem>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
            using var fs = File.Create(file);
            await JsonSerializer.SerializeAsync(fs, new ThirdPartyGachaExportFile
            {
                GameBiz = CurrentGameBiz.Value,
                Uid = uid,
                ExportTime = DateTime.Now,
                List = list,
            }, AppConfig.JsonSerializerOptions);
        }
    }


    public override long ImportGachaLog(string file)
    {
        var text = File.ReadAllText(file);
        var obj = JsonSerializer.Deserialize<ThirdPartyGachaExportFile>(text, AppConfig.JsonSerializerOptions);
        if (obj is null || obj.Uid <= 0 || obj.List.Count == 0)
        {
            return 0;
        }
        foreach (var item in obj.List)
        {
            if (item.Uid == 0)
            {
                item.Uid = obj.Uid;
            }
        }
        InsertGachaLogItems(obj.List);
        InAppToast.MainWindow?.Success($"Uid {obj.Uid}", $"Imported {obj.List.Count} gacha records.", 5000);
        return obj.Uid;
    }


    public override Task<string> UpdateGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(lang);
    }


    public override Task<(string Language, int Count)> ChangeGachaItemNameAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((lang, 0));
    }


    private sealed class ThirdPartyGachaExportFile
    {
        public string GameBiz { get; set; } = "";

        public long Uid { get; set; }

        public DateTime ExportTime { get; set; }

        public List<GachaLogItem> List { get; set; } = [];
    }
}
