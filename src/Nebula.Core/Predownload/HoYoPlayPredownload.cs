using Nebula.Core.HoYoPlay;

namespace Nebula.Core.Predownload;

public static class HoYoPlayPredownload
{
    public static bool IsAvailable(GameBranch? branch, Version localVersion) =>
        branch?.PreDownload is { } pre
        && !string.IsNullOrWhiteSpace(pre.PackageId)
        && !string.IsNullOrWhiteSpace(pre.Branch)
        && IsFutureVersion(pre.Tag, branch.Main?.Tag, localVersion);

    public static bool IsAvailable(GamePackage? package, Version localVersion)
    {
        if (package?.PreDownload?.Major is not { } pre
            || !IsFutureVersion(pre.Version, package.Main?.Major?.Version, localVersion))
            return false;
        var resource = package.PreDownload.Patches?.FirstOrDefault(x => x.Version == localVersion.ToString()) ?? pre;
        return resource.GamePackages is { Count: > 0 }
            && resource.GamePackages.All(file => file.Size > 0 && GamePredownloadPlan.IsHttpsUrl(file.Url)
                && file.MD5?.Length == 32 && file.MD5.All(Uri.IsHexDigit));
    }

    private static bool IsFutureVersion(string version, string? currentVersion, Version localVersion) =>
        Version.TryParse(version, out var target) && target > localVersion
        && Version.TryParse(currentVersion, out var current) && target > current;
}
