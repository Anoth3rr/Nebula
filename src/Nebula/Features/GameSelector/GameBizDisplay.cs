using Nebula.Core.HoYoPlay;
using System.Collections.Generic;


namespace Nebula.Features.GameSelector;

public class GameBizDisplay
{

    public GameInfo GameInfo { get; set; }


    public List<GameBizIcon> Servers { get; set; } = new();

}
