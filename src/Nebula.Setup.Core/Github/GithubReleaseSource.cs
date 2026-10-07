using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nebula.Setup.Core.Github;

public static class GithubReleaseSource
{
    public static NuGetVersion? ParseVersion(string? value)
    {
        return NuGetVersion.TryParse(value?.Trim().TrimStart('v', 'V'), out var version) ? version : null;
    }

    public static GithubRelease? SelectLatest(IEnumerable<GithubRelease> releases, bool includePreview)
    {
        return releases.Select(release => (Release: release, Version: ParseVersion(release.TagName)))
            .Where(item => !item.Release.Draft && item.Version is not null
                && (includePreview || (!item.Release.Prerelease && !item.Version.IsPrerelease)))
            .OrderByDescending(item => item.Version)
            .ThenByDescending(item => item.Release.PublishedAt)
            .Select(item => item.Release).FirstOrDefault();
    }

    public static ReleaseInfo ToReleaseInfo(GithubRelease release)
    {
        var version = ParseVersion(release.TagName) ?? throw new FormatException("Invalid release version.");
        var info = new ReleaseInfo { Version = version.ToNormalizedString(), Releases = new() };
        foreach (var arch in new[] { Architecture.X64, Architecture.Arm64, Architecture.X86 })
        {
            foreach (var type in new[] { InstallType.Setup, InstallType.Portable })
            {
                string prefix = $"Nebula_{type}_{info.Version}_{arch}";
                var asset = release.Assets?.FirstOrDefault(x => x.Name.Equals(prefix + (type == InstallType.Setup ? ".exe" : ".zip"), StringComparison.OrdinalIgnoreCase))
                    ?? (type == InstallType.Portable ? release.Assets?.FirstOrDefault(x => x.Name.Equals(prefix + ".7z", StringComparison.OrdinalIgnoreCase)) : null);
                if (asset is null || asset.State != "uploaded")
                {
                    continue;
                }
                string hash = asset.Digest?.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) == true ? asset.Digest[7..] : "";
                bool verified = hash.Length == 64 && hash.All(Uri.IsHexDigit) && asset.Size > 0;
                var downloadUri = new Uri(asset.BrowserDownloadUrl, UriKind.Absolute);
                if (downloadUri.Scheme != Uri.UriSchemeHttps || downloadUri.Host != "github.com"
                    || !downloadUri.AbsolutePath.StartsWith("/Anoth3rr/Nebula/releases/download/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException("Release asset must belong to Anoth3rr/Nebula on GitHub.");
                }
                var detail = new ReleaseInfoDetail
                {
                    Version = info.Version,
                    ReleaseUrl = release.HtmlUrl,
                    Architecture = arch,
                    InstallType = type,
                    BuildTime = release.PublishedAt,
                    PackageUrl = asset.BrowserDownloadUrl,
                    PackageSize = asset.Size,
                    PackageHash = hash,
                    ManifestUrl = "",
                    Diffs = new(),
                    DisableAutoUpdate = !verified || asset.Name.EndsWith(".7z", StringComparison.OrdinalIgnoreCase),
                };
                if (type == InstallType.Setup && verified)
                {
                    detail.Setup = new ReleaseSetup { FileName = asset.Name, Url = asset.BrowserDownloadUrl, Size = asset.Size, Hash = hash };
                }
                info.Releases.Add($"{arch}-{type}".ToLowerInvariant(), detail);
            }
        }
        return info;
    }
}
