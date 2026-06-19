using System.Text.Json.Serialization;

namespace Nebula.Core.GameRecord.ZZZ.InterKnotReport;

public class InterKnotReportRoleInfo
{
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; }
}


