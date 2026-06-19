using Nebula.Core.GameRecord;

namespace Nebula.Features.GameRecord;

internal class GameRecordRoleChangedMessage
{

    public GameRecordRole? GameRole { get; set; }

    public GameRecordRoleChangedMessage(GameRecordRole? gameRole)
    {
        GameRole = gameRole;
    }

}
