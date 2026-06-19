using Nebula.RPC.GameInstall;

namespace Nebula.Features.GameInstall;

class GameInstallTaskStartedMessage
{
    public GameInstallContext InstallTask { get; init; }

    public GameInstallTaskStartedMessage(GameInstallContext installTask)
    {
        InstallTask = installTask;
    }
}
