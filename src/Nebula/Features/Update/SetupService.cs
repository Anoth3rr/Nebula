using Nebula.Setup.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Nebula.Features.Update;

internal class SetupService
{
    private readonly HttpClient _httpClient;
    private readonly ReleaseClient _releaseClient;

    public SetupService(HttpClient httpClient, ReleaseClient releaseClient)
    {
        _httpClient = httpClient;
        _releaseClient = releaseClient;
    }

    public long SetupTotalBytes { get; private set; }
    public long SetupDownloadBytes { get; private set; }

    public async Task<string?> DownloadSetupAsync(ReleaseInfoDetail? detail, CancellationToken cancellationToken = default)
    {
        detail ??= await _releaseClient.GetLatestReleaseInfoDetailAsync(AppConfig.EnablePreviewRelease, AppConfig.AppVersion,
            RuntimeInformation.ProcessArchitecture, AppConfig.InstallType, cancellationToken);
        if (detail.Setup is null || detail.DisableAutoUpdate) return null;

        string setupPath = Path.Combine(AppConfig.CacheFolder, "update", Path.GetFileName(detail.Setup.FileName));
        SetupTotalBytes = detail.Setup.Size;
        SetupDownloadBytes = 0;
        var progress = new Progress<long>(bytes => SetupDownloadBytes = bytes);
        await UpdatePackage.DownloadAsync(_httpClient, detail.Setup.Url, setupPath, detail.Setup.Size, detail.Setup.Hash, progress, cancellationToken);
        return setupPath;
    }

    public async Task UpdateAsync(ReleaseInfoDetail detail, CancellationToken cancellationToken = default)
    {
        string? setupPath = await DownloadSetupAsync(detail, cancellationToken);
        if (!File.Exists(setupPath)) throw new NotSupportedException("Update is not supported.");
        cancellationToken.ThrowIfCancellationRequested();
        Process.Start(new ProcessStartInfo
        {
            FileName = setupPath,
            UseShellExecute = true,
            Verb = "runas",
            Arguments = $"""
                update --InstallFolder "{AppContext.BaseDirectory.TrimEnd('\\')}" --OldVersion "{AppConfig.AppVersion}" --NewVersion "{detail.Version}" --Preview "{AppConfig.EnablePreviewRelease}" --Restart "{AppConfig.AutoRestartWhenUpdateFinished}" --pid {Environment.ProcessId}
                """,
        });
    }
}
