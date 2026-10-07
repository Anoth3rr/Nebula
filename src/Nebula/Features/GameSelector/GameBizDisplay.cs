using Nebula.Core.HoYoPlay;
using System.Collections.Generic;


namespace Nebula.Features.GameSelector;

public class GameBizDisplay
{

    public GameInfo GameInfo { get; set; }

    public bool ShowGameTitle => GameInfo.Hypergryph is not null;


    public List<GameBizIcon> Servers { get; set; } = new();

}
