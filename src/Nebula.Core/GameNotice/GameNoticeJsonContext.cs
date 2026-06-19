using System.Text.Json.Serialization;

namespace Nebula.Core.GameNotice;


[JsonSerializable(typeof(miHoYoApiWrapper<AlertAnn>))]
internal partial class GameNoticeJsonContext : JsonSerializerContext
{

}
