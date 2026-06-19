using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Nebula.Core;
using Nebula.Core.HoYoPlay;
using Nebula.Features.GameLauncher;
using Nebula.Features.PlayTime;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Vanara.PInvoke;

namespace Nebula.Features.UrlProtocol;

internal class UrlProtocolService
{



    public static void RegisterProtocol()
    {
        UnregisterProtocol();
        string exe;
        if (AppConfig.IsPortable)
        {
            exe = AppConfig.NebulaPortableLauncherExecutePath ?? Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('\\', '/')), "Nebula.exe");
        }
        else
        {
            exe = AppConfig.NebulaExecutePath;
        }
        string command = $"""
            "{exe}" "%1"
            """;
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Nebula", "", "URL:Nebula Protocol");
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Nebula", "URL Protocol", "");
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Nebula\DefaultIcon", "", "Nebula.exe,1");
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Nebula\Shell\Open\Command", "", command);
    }



    public static void UnregisterProtocol()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Nebula", false);
    }



    public static async Task<bool> HandleUrlProtocolAsync(string url)
    {
        var log = AppConfig.GetLogger<UrlProtocolService>();
        try
        {
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                if (uri.Host is "test")
                {
                    return false;
                }
                if (string.IsNullOrWhiteSpace(AppConfig.UserDataFolder))
                {
                    log.LogWarning("UserDataFolder is null");
                    return false;
                }
                if (uri.Host is "startgame")
                {
                    if (GameBiz.TryParse(uri.AbsolutePath.Trim('/'), out GameBiz biz) && GameId.FromGameBiz(biz) is GameId gameId)
                    {
                        var kvs = HttpUtility.ParseQueryString(uri.Query);
                        string? installPath = kvs["install_path"];
                        await AppConfig.GetService<GameLauncherService>().StartGameAsync(gameId, installPath);
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot parse the game_biz \"{uri.AbsolutePath.Trim('/')}\".");
                    }
                    return true;
                }
                if (uri.Host is "playtime")
                {
                    if (GameBiz.TryParse(uri.AbsolutePath.Trim('/'), out GameBiz biz) && GameId.FromGameBiz(biz) is GameId gameId)
                    {
                        var kvs = HttpUtility.ParseQueryString(uri.Query);
                        if (int.TryParse(kvs["pid"], out int pid))
                        {
                            await AppConfig.GetService<PlayTimeService>().StartProcessToLogAsync(gameId, pid);
                        }
                        else
                        {
                            await AppConfig.GetService<PlayTimeService>().StartProcessToLogAsync(gameId);
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot parse the game_biz \"{uri.AbsolutePath.Trim('/')}\".");
                    }
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Handle url protocol");
            User32.MessageBox(HWND.NULL, ex.Message, "Nebula");
            return true;
        }
        return false;
    }





}
