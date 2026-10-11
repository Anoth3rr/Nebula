using Microsoft.Extensions.Logging;
using Nebula.Core.HoYoPlay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nebula.Features.Background;

/// <summary>米哈游和鹰角目录共用的版本海报缓存。</summary>
public sealed class GamePosterCache(HttpClient httpClient, ILogger<GamePosterCache> logger)
{
    public async Task DownloadAsync(IEnumerable<GameInfo> games)
    {
        try
        {
            if (AppConfig.CacheFolder is not string cacheFolder)
                return;
            string folder = Path.Combine(cacheFolder, "bg");
            Directory.CreateDirectory(folder);
            var posters = games.Where(x => Uri.TryCreate(x.Display.Background?.Url, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps).GroupBy(x => x.Display.Background!.Url);
            await Parallel.ForEachAsync(posters, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (group, token) =>
            {
                string? temporaryFile = null;
                try
                {
                    string name = Path.GetFileName(new Uri(group.Key).AbsolutePath);
                    if (string.IsNullOrWhiteSpace(name))
                        return;
                    string path = Path.Combine(folder, name);
                    if (!File.Exists(path))
                    {
                        byte[] bytes = await httpClient.GetByteArrayAsync(group.Key, token);
                        temporaryFile = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                        await File.WriteAllBytesAsync(temporaryFile, bytes, token);
                        File.Move(temporaryFile, path, overwrite: true);
                    }
                    foreach (var game in group)
                        AppConfig.SetVersionPoster(game.GameBiz, name);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Cache game poster: {Url}", group.Key);
                }
                finally
                {
                    if (temporaryFile is not null && File.Exists(temporaryFile))
                        File.Delete(temporaryFile);
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache game posters");
        }
    }
}
