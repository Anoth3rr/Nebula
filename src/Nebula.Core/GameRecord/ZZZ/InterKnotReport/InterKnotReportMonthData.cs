using System.Text.Json.Serialization;

namespace Nebula.Core.GameRecord.ZZZ.InterKnotReport;

public class InterKnotReportMonthData
{
    [JsonPropertyName("list")]
    public List<InterKnotReportSummaryAward> List { get; set; }

    [JsonPropertyName("income_components")]
    public List<InterKnotReportIncomeComponent> IncomeComponents { get; set; }
}


