using System;
using System.Collections.Generic;

namespace Nebula.Features.Gacha;

public class GachaTypeStats
{

    public int GachaType { get; set; }

    public string GachaTypeText { get; set; }

    public int Count { get; set; }

    public int TopRankType { get; set; } = 5;

    public int Pity_5 { get; set; }

    public int Pity_4 { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int Count_6 { get; set; }

    public int Count_5 { get; set; }

    public int Count_5_Up { get; set; }

    public int Count_4 { get; set; }

    public int Count_3 { get; set; }

    public double Ratio_5 { get; set; }

    public double Ratio_4 { get; set; }

    public double Ratio_3 { get; set; }

    public double Average_5 { get; set; }

    public double Average_5_Up { get; set; }

    public List<GachaLogItemEx> List_5 { get; set; }

    public List<GachaLogItemEx> List_4 { get; set; }

    public List<GachaLogItemEx> List_All { get; set; }

    public int Count_Low => Count - Count_6 - Count_5 - Count_4;

    public int SecondaryRankType => TopRankType - 1;

    public int ThirdRankType => TopRankType - 2;

    public string TopRankText => $"{TopRankType}\u2605";

    public string SecondaryRankText => $"{SecondaryRankType}\u2605";

    public string ThirdRankText => $"{ThirdRankType}\u2605";

    public int Count_Top => GetCountByRank(TopRankType);

    public int Count_Secondary => GetCountByRank(SecondaryRankType);

    public int Count_Third => GetCountByRank(ThirdRankType);

    public double Ratio_Top => Count == 0 ? 0 : (double)Count_Top / Count;

    public double Ratio_Secondary => Count == 0 ? 0 : (double)Count_Secondary / Count;

    public double Ratio_Third => Count == 0 ? 0 : (double)Count_Third / Count;

    public string Avarage_5_Desc_Text => Count_5_Up == 0 ? "" : $" / UP";

    public string Avarage_5_Up_Text => Count_5_Up == 0 ? "" : $" / {Average_5_Up:F2} ({Count_5_Up})";


    private int GetCountByRank(int rank)
    {
        return rank switch
        {
            6 => Count_6,
            5 => Count_5,
            4 => Count_4,
            3 => Count_3,
            _ => Count_Low,
        };
    }

}
