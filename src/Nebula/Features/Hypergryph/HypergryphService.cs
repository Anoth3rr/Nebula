using Microsoft.Extensions.Logging;
using Nebula.Core;
using Nebula.Core.HoYoPlay;
using Nebula.Core.Hypergryph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nebula.Features.Hypergryph;

public sealed class HypergryphService(HypergryphClient client, ILogger<HypergryphService> logger)
{
    private readonly SemaphoreSlim _refreshLock = new(1);
    private List<GameInfo>? _games;

    public List<GameInfo> GetCachedGames()
    {
        if (_games is not null)
            return new(_games);
        try
        {
            _games = string.IsNullOrWhiteSpace(AppConfig.CachedHypergryphGameInfo) ? []
                : JsonSerializer.Deserialize(AppConfig.CachedHypergryphGameInfo, HypergryphJsonContext.Default.ListGameInfo) ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Read cached Hypergryph catalog");
            _games = [];
        }
        return new(_games);
    }

    public GameInfo? GetGameInfo(GameBiz gameBiz) => GetCachedGames().FirstOrDefault(x => x.GameBiz == gameBiz);

    public async Task<List<GameInfo>> UpdateGameInfoListAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var games = await client.GetGamesAsync(GetCachedGames(), cancellationToken);
            AppConfig.CachedHypergryphGameInfo = JsonSerializer.Serialize(games, HypergryphJsonContext.Default.ListGameInfo);
            _games = games;
            return new(games);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Refresh Hypergryph catalog; using cached games");
            return GetCachedGames();
        }
        finally { _refreshLock.Release(); }
    }
}
