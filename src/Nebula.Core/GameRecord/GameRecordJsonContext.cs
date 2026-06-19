using Nebula.Core.GameRecord.BH3.DailyNote;
using Nebula.Core.GameRecord.Genshin.DailyNote;
using Nebula.Core.GameRecord.Genshin.ImaginariumTheater;
using Nebula.Core.GameRecord.Genshin.SpiralAbyss;
using Nebula.Core.GameRecord.Genshin.StygianOnslaught;
using Nebula.Core.GameRecord.Genshin.TravelersDiary;
using Nebula.Core.GameRecord.StarRail.ApocalypticShadow;
using Nebula.Core.GameRecord.StarRail.ChallengePeak;
using Nebula.Core.GameRecord.StarRail.DailyNote;
using Nebula.Core.GameRecord.StarRail.ForgottenHall;
using Nebula.Core.GameRecord.StarRail.PureFiction;
using Nebula.Core.GameRecord.StarRail.SimulatedUniverse;
using Nebula.Core.GameRecord.StarRail.TrailblazeCalendar;
using Nebula.Core.GameRecord.ZZZ.DailyNote;
using Nebula.Core.GameRecord.ZZZ.DeadlyAssault;
using Nebula.Core.GameRecord.ZZZ.GachaRecord;
using Nebula.Core.GameRecord.ZZZ.InterKnotReport;
using Nebula.Core.GameRecord.ZZZ.ShiyuDefense;
using Nebula.Core.GameRecord.ZZZ.ThresholdSimulation;
using Nebula.Core.GameRecord.ZZZ.UpgradeGuide;
using Nebula.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Nebula.Core.GameRecord;


[JsonSerializable(typeof(miHoYoApiWrapper<GameRecordUserWrapper>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GameRecordRoleWrapper>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GameRecordIndex>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SpiralAbyssInfo>))]
[JsonSerializable(typeof(miHoYoApiWrapper<StygianOnslaughtWrapper>))]
[JsonSerializable(typeof(miHoYoApiWrapper<TravelersDiarySummary>))]
[JsonSerializable(typeof(miHoYoApiWrapper<TravelersDiaryDetail>))]
[JsonSerializable(typeof(miHoYoApiWrapper<TrailblazeCalendarSummary>))]
[JsonSerializable(typeof(miHoYoApiWrapper<TrailblazeCalendarDetail>))]
[JsonSerializable(typeof(miHoYoApiWrapper<ForgottenHallInfo>))]
[JsonSerializable(typeof(miHoYoApiWrapper<PureFictionInfo>))]
[JsonSerializable(typeof(miHoYoApiWrapper<ApocalypticShadowInfo>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SimulatedUniverseInfo>))]
[JsonSerializable(typeof(miHoYoApiWrapper<ChallengePeakData>))]
[JsonSerializable(typeof(miHoYoApiWrapper<DeviceFpResult>))]
[JsonSerializable(typeof(miHoYoApiWrapper<ImaginariumTheaterWarpper>))]
[JsonSerializable(typeof(miHoYoApiWrapper<InterKnotReportSummary>))]
[JsonSerializable(typeof(miHoYoApiWrapper<InterKnotReportDetail>))]
[JsonSerializable(typeof(miHoYoApiWrapper<UpgradeGuideItemList>))]
[JsonSerializable(typeof(miHoYoApiWrapper<UpgradeGuidIconInfo>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GenshinDailyNote>))]
[JsonSerializable(typeof(miHoYoApiWrapper<StarRailDailyNote>))]
[JsonSerializable(typeof(miHoYoApiWrapper<ShiyuDefenseWrapper>))]
[JsonSerializable(typeof(miHoYoApiWrapper<DeadlyAssaultInfo>))]
[JsonSerializable(typeof(miHoYoApiWrapper<ZZZDailyNote>))]
[JsonSerializable(typeof(miHoYoApiWrapper<ZZZGachaRecordData>))]
[JsonSerializable(typeof(miHoYoApiWrapper<BH3DailyNote>))]
[JsonSerializable(typeof(miHoYoApiWrapper<ThresholdSimulationAbstractInfo>))]
[JsonSerializable(typeof(miHoYoApiWrapper<ThresholdSimulationDetailInfo>))]
[JsonSerializable(typeof(DateTimeObjectJsonConverter.DateTimeObject))]
internal partial class GameRecordJsonContext : JsonSerializerContext
{

}
