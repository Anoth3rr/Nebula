using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nebula.Core.Predownload;

/// <summary>隔离保存预下载资源及完成记录，不写入游戏的版本配置。</summary>
public static class GamePredownloadCache
{
    public static string GetDirectory(GamePredownloadPlan plan, string installPath) =>
        SafePath(installPath, $"Nebula_Predownload/{plan.GameBiz}/{plan.Version}");

    public static string GetFilePath(GamePredownloadPlan plan, string installPath, GamePredownloadFile file)
    {
        string directory = GetDirectory(plan, installPath);
        _ = SafePath(directory, "files/" + file.RelativePath + "_tmp");
        return SafePath(directory, "files/" + file.RelativePath);
    }

    public static long GetRemainingBytes(GamePredownloadPlan plan, string installPath)
    {
        long remaining = 0;
        foreach (var file in plan.Files)
        {
            string path = GetFilePath(plan, installPath, file);
            string partial = SafePath(GetDirectory(plan, installPath), "files/" + file.RelativePath + "_tmp");
            long cached = File.Exists(path) && new FileInfo(path).Length == file.Size ? file.Size : 0;
            if (File.Exists(partial))
                cached = Math.Max(cached, Math.Min(new FileInfo(partial).Length, file.Size));
            remaining = checked(remaining + file.Size - cached);
        }
        return remaining;
    }

    public static async Task<bool> IsFinishedAsync(GamePredownloadPlan plan, string installPath,
        CancellationToken token = default)
    {
        try
        {
            string marker = SafePath(GetDirectory(plan, installPath), "completed.json");
            if (!File.Exists(marker))
                return false;
            await using var stream = File.OpenRead(marker);
            var receipt = await JsonSerializer.DeserializeAsync(stream, PredownloadJsonContext.Default.PredownloadReceipt, token);
            if (receipt?.Fingerprint != plan.Fingerprint || receipt.LastWriteTimes.Length != plan.Files.Count)
                return false;
            for (int i = 0; i < plan.Files.Count; i++)
            {
                var info = new FileInfo(GetFilePath(plan, installPath, plan.Files[i]));
                if (!info.Exists || info.Length != plan.Files[i].Size || info.LastWriteTimeUtc.Ticks != receipt.LastWriteTimes[i])
                    return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    /// <summary>调用方必须先下载并校验所有文件，记录用于后续快速判断是否完成。</summary>
    public static async Task MarkFinishedAsync(GamePredownloadPlan plan, string installPath, CancellationToken token = default)
    {
        var receipt = new PredownloadReceipt { Fingerprint = plan.Fingerprint };
        receipt.LastWriteTimes = plan.Files.Select(file =>
        {
            var info = new FileInfo(GetFilePath(plan, installPath, file));
            if (!info.Exists || info.Length != file.Size)
                throw new InvalidDataException("Predownload files are incomplete.");
            return info.LastWriteTimeUtc.Ticks;
        }).ToArray();
        string directory = GetDirectory(plan, installPath);
        Directory.CreateDirectory(directory);
        string temporary = SafePath(directory, "completed.json.tmp");
        await using (var stream = File.Create(temporary))
            await JsonSerializer.SerializeAsync(stream, receipt, PredownloadJsonContext.Default.PredownloadReceipt, token);
        File.Move(temporary, SafePath(directory, "completed.json"), true);
    }

    private static string SafePath(string root, string relative)
    {
        if (!Path.IsPathFullyQualified(root))
            throw new InvalidDataException("An absolute install path is required.");
        string current = root;
        // 拒绝缓存内部的 junction/symlink，防止提权下载写到缓存以外的位置。
        foreach (string part in GamePredownloadPlan.ValidateRelativePath(relative).Split('/'))
        {
            current = Path.Combine(current, part);
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Predownload cache must not contain symbolic links.");
        }
        return current;
    }
}

internal sealed class PredownloadReceipt
{
    public string Fingerprint { get; set; } = "";
    public long[] LastWriteTimes { get; set; } = [];
}

[JsonSerializable(typeof(PredownloadReceipt))]
internal partial class PredownloadJsonContext : JsonSerializerContext;
