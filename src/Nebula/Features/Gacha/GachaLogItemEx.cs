
using CommunityToolkit.Mvvm.ComponentModel;
using Nebula.Core.Gacha;
using Nebula.Core.Gacha.Genshin;
using Nebula.Core.Gacha.StarRail;
using Nebula.Core.Gacha.ZZZ;

namespace Nebula.Features.Gacha;

[INotifyPropertyChanged]
public partial class GachaLogItemEx : GachaLogItem
{

    /// <summary>
    /// 不要删除，导出 Excel 时有用
    /// </summary>
    public string IdText => Id.ToString();

    /// <summary>
    /// 相同保底卡池中的顺序
    /// </summary>
    public int Index { get; set; }

    public int Pity { get; set; }

    public string Icon { get; set; }

    public bool HasIcon => !string.IsNullOrWhiteSpace(Icon);

    public double Progress => (double)Pity / ((GachaType is GenshinGachaType.WeaponEventWish or StarRailGachaType.LightConeEventWarp or StarRailGachaType.LightConeCollaborationWarp or ZZZGachaType.WEngineChannel or ZZZGachaType.WEngineReverberation or ZZZGachaType.BangbooChannel) ? 80 : 90) * 100;


    public bool IsPointerIn { get; set => SetProperty(ref field, value); }

    public int ItemCount { get; set; }


    public bool HasUpItem { get; set; }

    public bool IsUp { get; set; }

    public double UpTextOpacity => IsUp ? 1 : 0;

}
