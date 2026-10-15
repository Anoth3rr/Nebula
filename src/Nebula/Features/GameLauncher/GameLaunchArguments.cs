using Nebula.Core;
using System;
using System.Text.RegularExpressions;

namespace Nebula.Features.GameLauncher;

internal static partial class GameLaunchArguments
{
    public static string? Prepare(GameBiz gameBiz, string? arguments, bool thirdPartyTool)
    {
        arguments = arguments?.Trim();
        if (thirdPartyTool)
        {
            return arguments;
        }

        if (gameBiz.Value is GameBiz.arknights_cn)
        {
            // The Hypergryph launcher passes these values to the official CN client.
            // Arknights uses the launcher sub-channel to distinguish an official start
            // from a direct executable launch.
            return AppendMissingArguments(
                arguments,
                ("-launcher_lang", "-launcher_lang=zh-cn"),
                ("-launcher_sub_channel", "-launcher_sub_channel=837"));
        }

        if (gameBiz.Game is not GameBiz.wutheringwaves)
        {
            return arguments;
        }

        if (HasArgument(arguments, "-krqlv"))
        {
            return arguments;
        }

        // Wuthering Waves 3.7 HD requires the quality argument supplied by its official launcher.
        // Without it, direct launches fail with "kuro: Use launcher to start game!".
        return string.IsNullOrEmpty(arguments) ? "-krqlv=hd" : $"{arguments} -krqlv=hd";
    }

    private static string AppendMissingArguments(
        string? arguments,
        params (string Name, string Value)[] defaults)
    {
        string result = arguments ?? "";
        foreach ((string name, string value) in defaults)
        {
            if (!HasArgument(result, name))
            {
                result = string.IsNullOrEmpty(result) ? value : $"{result} {value}";
            }
        }
        return result;
    }

    private static bool HasArgument(string? arguments, string name)
    {
        foreach (Match match in ArgumentRegex().Matches(arguments ?? ""))
        {
            string argument = match.Value.Replace("\"", "");
            if (argument.Equals(name, StringComparison.OrdinalIgnoreCase)
                || argument.StartsWith($"{name}=", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    [GeneratedRegex("""(?:[^\s"]+|"[^"]*")+""")]
    private static partial Regex ArgumentRegex();
}
