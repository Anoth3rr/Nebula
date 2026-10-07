using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Nebula.Setup.Core;

public static class UpdatePackage
{
    public static async Task DownloadAsync(HttpClient client, string url, string path, long size, string hash,
        IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        if (size <= 0 || hash.Length != 64 || !hash.All(Uri.IsHexDigit))
            throw new InvalidDataException("The update package has no valid SHA-256 checksum.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (file.Length > size) file.SetLength(0);
            if (file.Length < size)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                long offset = file.Length;
                if (offset > 0) request.Headers.Range = new RangeHeaderValue(offset, null);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                if (response.StatusCode == HttpStatusCode.PartialContent)
                {
                    if (response.Content.Headers.ContentRange?.From != offset)
                        throw new InvalidDataException("The update server returned an invalid download range.");
                }
                else
                {
                    file.SetLength(0);
                    offset = 0;
                }
                file.Position = offset;
                progress?.Report(offset);
                using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                byte[] buffer = new byte[64 * 1024];
                int count;
                while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    if (file.Position + count > size)
                    {
                        file.SetLength(0);
                        throw new InvalidDataException("The update package exceeds its advertised size.");
                    }
                    await file.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                    progress?.Report(file.Position);
                }
            }
            await file.FlushAsync(cancellationToken);
            file.Position = 0;
            if (file.Length == size && string.Equals(Convert.ToHexString(await SHA256.HashDataAsync(file, cancellationToken)), hash, StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report(size);
                return;
            }
            file.SetLength(0);
            progress?.Report(0);
        }
        throw new InvalidDataException("Update package checksum mismatch. Please retry the download.");
    }

    public static async Task InstallPortableAsync(string archivePath, string targetFolder, string version, CancellationToken cancellationToken = default)
    {
        string appFolder = $"app-{version}";
        string staging = Path.Combine(targetFolder, $".nebula-update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        try
        {
            using var archive = ZipFile.OpenRead(archivePath);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relative = entry.FullName.Replace('\\', '/');
                if (relative.EndsWith('/')) continue;
                if (relative != "Nebula.exe" && relative != "version.ini" && !relative.StartsWith(appFolder + "/", StringComparison.Ordinal))
                    throw new InvalidDataException($"Unexpected file in portable package: {relative}");
                string path = GetContainedPath(staging, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var input = entry.Open();
                using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await input.CopyToAsync(output, cancellationToken);
            }
            if (!File.Exists(Path.Combine(staging, "Nebula.exe")) || !File.Exists(Path.Combine(staging, appFolder, "Nebula.exe"))
                || (await File.ReadAllTextAsync(Path.Combine(staging, "version.ini"), cancellationToken)).Trim() != $"version={version}")
                throw new InvalidDataException("The portable package is incomplete or has a mismatched version.");
            ApplyDirectory(staging, targetFolder, cancellationToken);
        }
        finally
        {
            TryDeleteDirectory(staging);
        }
    }

    // Keep backups until every replacement succeeds. Unrelated user files are never removed.
    public static void ApplyDirectory(string sourceFolder, string targetFolder, CancellationToken cancellationToken = default)
    {
        string backupFolder = Path.Combine(targetFolder, $".nebula-backup-{Guid.NewGuid():N}");
        var changes = new List<(string Target, string Backup, bool Existed)>();
        bool canDeleteBackup = false;
        try
        {
            var files = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories)
                .OrderBy(path => Path.GetRelativePath(sourceFolder, path).Equals("version.ini", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase);
            foreach (string source in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relative = Path.GetRelativePath(sourceFolder, source);
                string target = GetContainedPath(targetFolder, relative);
                string backup = GetContainedPath(backupFolder, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                bool existed = File.Exists(target);
                if (existed)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                    File.Move(target, backup);
                }
                changes.Add((target, backup, existed));
                File.Move(source, target);
            }
            canDeleteBackup = true;
        }
        catch (Exception updateError)
        {
            var errors = new List<Exception> { updateError };
            foreach (var change in changes.AsEnumerable().Reverse())
            {
                try
                {
                    if (File.Exists(change.Target)) File.Delete(change.Target);
                    if (change.Existed) File.Move(change.Backup, change.Target);
                }
                catch (Exception rollbackError) { errors.Add(rollbackError); }
            }
            canDeleteBackup = errors.Count == 1;
            if (!canDeleteBackup)
                throw new AggregateException($"Update rollback was incomplete. Original files are retained in {backupFolder}.", errors);
            throw;
        }
        finally
        {
            if (canDeleteBackup) TryDeleteDirectory(backupFolder);
        }
    }

    public static string GetContainedPath(string root, string relative)
    {
        if (Path.IsPathRooted(relative) || relative.Contains(':')
            || relative.Replace('\\', '/').Split('/').Any(x => x is ".." or "." || x != x.TrimEnd(' ', '.')))
            throw new InvalidDataException($"Invalid package path: {relative}");
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(Path.Combine(fullRoot, relative));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Package path leaves the installation folder: {relative}");
        for (string? current = fullPath; current is not null; current = Path.GetDirectoryName(current))
        {
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Update path contains a symbolic link: {current}");
        }
        return fullPath;
    }

    public static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
