using Nebula.Core.Gacha.Genshin;
using Nebula.Core.Gacha.StarRail;
using Nebula.Core.Gacha.WutheringWaves;
using Nebula.Core.Gacha.ZZZ;
using System.Text.Json.Serialization;

namespace Nebula.Core.Gacha;


[JsonSerializable(typeof(miHoYoApiWrapper<GachaLogResult<GachaLogItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GachaLogResult<StarRailGachaItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GachaLogResult<GenshinGachaItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GachaLogResult<ZZZGachaItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GenshinBeyondGachaResult>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GenshinGachaWiki>))]
[JsonSerializable(typeof(miHoYoApiWrapper<StarRailGachaWiki>))]
[JsonSerializable(typeof(miHoYoApiWrapper<StarRailGachaInfoWrapper>))]
[JsonSerializable(typeof(miHoYoApiWrapper<ZZZGachaWiki>))]
[JsonSerializable(typeof(WutheringWavesCataloguePage))]
[JsonSerializable(typeof(List<GenshinBeyondGachaInfo>))]
internal partial class GachaLogJsonContext : JsonSerializerContext
{

}
