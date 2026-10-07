using Nebula.Setup.Core.Github;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nebula.Setup.Core;

public class ReleaseClient
{
    public static Uri DefaultBaseAddress { get; set; } = new("https://api.github.com/repos/Anoth3rr/Nebula/");


    private readonly HttpClient _httpClient;

    public ReleaseClient(HttpClient? httpClient)
    {
        if (httpClient is null)
        {
            _httpClient = new HttpClient(new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                EnableMultipleHttp2Connections = true,
                EnableMultipleHttp3Connections = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            });
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"{Path.GetFileNameWithoutExtension(Environment.ProcessPath)}/*");
        }
        else
        {
            _httpClient = httpClient;
        }
        _httpClient.BaseAddress = DefaultBaseAddress;
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Nebula-Updater/1.0");
        }
    }


    public async Task<ReleaseInfo> GetLatestReleaseInfoAsync(bool isPrerelease, string currentVersion, CancellationToken cancellationToken = default)
    {
        var releases = new List<GithubRelease>();
        for (int page = 1; ; page++)
        {
            var batch = await GetGithubReleaseAsync(page, 100, cancellationToken);
            releases.AddRange(batch);
            if (batch.Count < 100) break;
        }
        var release = GithubReleaseSource.SelectLatest(releases, isPrerelease) ?? throw new ReleaseNotFoundException();
        return GithubReleaseSource.ToReleaseInfo(release);
    }


    public async Task<ReleaseInfoDetail> GetLatestReleaseInfoDetailAsync(bool isPrerelease, string currentVersion, Architecture arch, InstallType type, CancellationToken cancellationToken = default)
    {
        var info = await GetLatestReleaseInfoAsync(isPrerelease, currentVersion, cancellationToken);
        string key = $"{arch}-{type}".ToLower();
        if (info.Releases?.TryGetValue(key, out var value) ?? false)
        {
            return value;
        }
        else
        {
            throw new PlatformNotSupportedException($"Platform ({arch}, {type}) is not supported.");
        }
    }

    public async Task<ReleaseInfo> GetReleaseInfoAsync(string version, CancellationToken cancellationToken = default)
    {
        GithubRelease? release;
        try
        {
            release = await GetGithubReleaseAsync(version, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound && !version.StartsWith('v'))
        {
            release = await GetGithubReleaseAsync($"v{version}", cancellationToken);
        }
        return GithubReleaseSource.ToReleaseInfo(release ?? throw new ReleaseNotFoundException());
    }


    public async Task<ReleaseManifest> GetReleaseManifestAsync(string url, CancellationToken cancellationToken = default)
    {
        var manifest = await _httpClient.GetFromJsonAsync(url, ReleaseJsonContext.Default.ReleaseManifest, cancellationToken);
        return manifest ?? throw new NullReferenceException($"Cannot get json content from '{url}'.");
    }



    #region Github



    public async Task<GithubRelease?> GetGithubLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        const string url = "https://api.github.com/repos/Anoth3rr/Nebula/releases?page=1&per_page=1";
        var list = await _httpClient.GetFromJsonAsync(url, ReleaseJsonContext.Default.ListGithubRelease, cancellationToken);
        return list?.FirstOrDefault();
    }



    public async Task<List<GithubRelease>> GetGithubReleaseAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        string url = $"https://api.github.com/repos/Anoth3rr/Nebula/releases?page={page}&per_page={perPage}";
        var list = await _httpClient.GetFromJsonAsync(url, ReleaseJsonContext.Default.ListGithubRelease, cancellationToken);
        return list ?? new List<GithubRelease>();
    }



    public async Task<GithubRelease?> GetGithubReleaseAsync(string tag, CancellationToken cancellationToken = default)
    {
        string url = $"https://api.github.com/repos/Anoth3rr/Nebula/releases/tags/{Uri.EscapeDataString(tag)}";
        return await _httpClient.GetFromJsonAsync(url, ReleaseJsonContext.Default.GithubRelease, cancellationToken);
    }


    public async Task<string> RenderGithubMarkdownAsync(string markdown, CancellationToken cancellationToken = default)
    {
        const string url = "https://api.github.com/markdown";
        var request = new GithubMarkdownRequest
        {
            Text = markdown,
            Mode = "gfm",
            Context = "Anoth3rr/Nebula",
        };
        var content = new StringContent(JsonSerializer.Serialize(request, ReleaseJsonContext.Default.GithubMarkdownRequest), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }



    #endregion


}
