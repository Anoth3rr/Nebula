using Nebula.Core;
using System;
using System.Text.RegularExpressions;

namespace Nebula.Features.GameLauncher;

internal static partial class GameLaunchArguments
{
    public static string? Prepare(GameBiz gameBiz, string? arguments, bool thirdPartyTool)
    {
        arguments = arguments?.Trim();
        if (thirdPartyTool || gameBiz.Game is not GameBiz.wutheringwaves)
        {
            return arguments;
        }

        foreach (Match match in ArgumentRegex().Matches(arguments ?? ""))
        {
            string argument = match.Value.Replace("\"", "");
            if (argument.Equals("-krqlv", StringComparison.OrdinalIgnoreCase)
                || argument.StartsWith("-krqlv=", StringComparison.OrdinalIgnoreCase))
            {
                return arguments;
            }
        }

        // Wuthering Waves 3.7 HD requires the quality argument supplied by its official launcher.
        // Without it, direct launches fail with "kuro: Use launcher to start game!".
        return string.IsNullOrEmpty(arguments) ? "-krqlv=hd" : $"{arguments} -krqlv=hd";
    }

    [GeneratedRegex("""(?:[^\s"]+|"[^"]*")+""")]
    private static partial Regex ArgumentRegex();
}
