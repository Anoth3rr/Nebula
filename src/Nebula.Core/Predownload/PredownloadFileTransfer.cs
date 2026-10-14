using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Nebula.Core.Predownload;

/// <summary>可恢复的缓存下载；只有长度和 MD5 同时通过才发布最终文件。</summary>
public sealed class PredownloadFileTransfer(HttpClient client)
{
    public async Task DownloadAsync(GamePredownloadFile file, string path,
        Action<long, long>? progress = null,
        Func<int, CancellationToken, Task>? throttle = null,
        CancellationToken cancellationToken = default)
    {
        long accounted = 0;
        void Report(long bytes, long networkBytes = 0)
        {
            accounted += bytes;
            progress?.Invoke(bytes, networkBytes);
        }
        string temporary = path + "_tmp";
        try
        {
            if (await MatchesAsync(path, file, cancellationToken))
            {
                Report(file.Size);
                return;
            }
            // 已有缓存校验失败时先释放它，避免修复时额外占用一个完整文件的空间。
            if (File.Exists(path))
                File.Delete(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using (var output = new FileStream(temporary, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.Read, 81920, FileOptions.Asynchronous))
            {
                if (output.Length > file.Size)
                    output.SetLength(0);
                long offset = output.Length;
                if (offset < file.Size)
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, file.Url);
                    if (offset > 0)
                        request.Headers.Range = new RangeHeaderValue(offset, null);
                    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    if (response.StatusCode == HttpStatusCode.PartialContent)
                    {
                        var range = response.Content.Headers.ContentRange;
                        if (range?.Unit != "bytes" || range.From != offset || range.Length != file.Size
                            || range.To != file.Size - 1)
                            throw new InvalidDataException("Unexpected predownload response range.");
                    }
                    else if (response.StatusCode == HttpStatusCode.OK)
                    {
                        // CDN 忽略 Range 时重写临时文件，不能把整包追加到已有部分后面。
                        offset = 0;
                        output.SetLength(0);
                    }
                    else
                    {
                        throw new InvalidDataException("Unexpected predownload response status.");
                    }
                    output.Position = offset;
                    Report(offset);
                    await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                    byte[] buffer = new byte[81920];
                    int read;
                    while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        if (output.Position + read > file.Size)
                            throw new InvalidDataException("Predownload response exceeds the expected size.");
                        if (throttle is not null)
                            await throttle(read, cancellationToken);
                        await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                        Report(read, read);
                    }
                    if (output.Length != file.Size)
                        throw new EndOfStreamException("Incomplete predownload response.");
                }
                else
                {
                    Report(offset);
                }
            }
            if (!await MatchesAsync(temporary, file, cancellationToken))
            {
                File.Delete(temporary);
                throw new InvalidDataException("Predownload MD5 does not match.");
            }
            File.Move(temporary, path, true);
        }
        catch
        {
            // 重试时重新计入已缓存字节，避免失败/暂停后进度重复累加。
            progress?.Invoke(-accounted, 0);
            throw;
        }
    }

    private static async Task<bool> MatchesAsync(string path, GamePredownloadFile file, CancellationToken token)
    {
        if (!File.Exists(path) || new FileInfo(path).Length != file.Size)
            return false;
        await using var stream = File.OpenRead(path);
        string hash = Convert.ToHexStringLower(await MD5.HashDataAsync(stream, token));
        return string.Equals(hash, file.MD5, StringComparison.OrdinalIgnoreCase);
    }
}
