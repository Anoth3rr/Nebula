using CommunityToolkit.Mvvm.ComponentModel;
using Nebula.Core;
using Nebula.Core.HoYoPlay;
using System;


namespace Nebula.Features.GameSelector;

public partial class GameBizIcon : ObservableObject, IEquatable<GameBizIcon>
{


    private const double GB = 1 << 30;


    public GameId GameId { get; set; }

    public GameBiz GameBiz { get; set; }


    public string GameIcon { get; set => SetProperty(ref field, value); }

    public string GameName { get; set => SetProperty(ref field, value); }

    public string ServerName { get; set => SetProperty(ref field, value); }

    public double MaskOpacity { get; set => SetProperty(ref field, value); } = 1.0;

    public bool IsPinned { get; set => SetProperty(ref field, value); }

    public string? InstallPath { get; set => SetProperty(ref field, value); }

    public bool IsBilibili => GameBiz.IsBilibili();

    public long TotalSize { get; set { field = value; OnPropertyChanged(nameof(TotalSizeText)); } }

    public string? TotalSizeText => TotalSize == 0 ? null : $"{TotalSize / GB:F2}GB";


    public bool IsSelected
    {
        get;
        set
        {
            field = value;
            MaskOpacity = value ? 0 : 1;
        }
    }



    public GameBizIcon(GameBiz gameBiz)
    {
        GameBiz = gameBiz;
        GameId = GameId.FromGameBiz(gameBiz)!;
        GameIcon = GameBizToIcon(gameBiz);
        GameName = gameBiz.ToGameName();
        ServerName = gameBiz.ToGameServerName();
    }



    public GameBizIcon(GameInfo gameInfo)
    {
        GameId = gameInfo;
        GameBiz = gameInfo.GameBiz;
        GameIcon = gameInfo.Display.Icon.Url;
        GameName = gameInfo.Display.Name;
        ServerName = gameInfo.GameBiz.ToGameServerName();
    }



    public void UpdateInfo()
    {
        GameIcon = GameBizToIcon(GameBiz);
        GameName = GameBiz.ToGameName();
        ServerName = GameBiz.ToGameServerName();
    }


    public void UpdateInfo(GameInfo gameInfo)
    {
        GameIcon = gameInfo.Display.Icon.Url;
        GameName = gameInfo.Display.Name;
        ServerName = gameInfo.GameBiz.ToGameServerName();
    }



    internal static string GameBizToIcon(GameBiz gameBiz)
    {
        return gameBiz.Game switch
        {
            GameBiz.bh3 => "ms-appx:///Assets/Image/icon_bh3.jpg",
            GameBiz.hk4e => "ms-appx:///Assets/Image/icon_ys.jpg",
            GameBiz.hkrpg => "ms-appx:///Assets/Image/icon_sr.jpg",
            GameBiz.nap => "ms-appx:///Assets/Image/icon_zzz.jpg",
            GameBiz.arknights => "ms-appx:///Assets/Image/icon_arknights.ico",
            GameBiz.endfield => "ms-appx:///Assets/Image/icon_endfield.ico",
            GameBiz.wutheringwaves => "ms-appx:///Assets/Image/icon_wutheringwaves.ico",
            _ => "ms-appx:///Assets/Image/Transparent.png",
        };
    }

    public bool Equals(GameBizIcon? other)
    {
        return ReferenceEquals(this, other) || GameBiz == other?.GameBiz;
    }

}
