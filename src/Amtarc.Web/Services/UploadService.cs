using Amtarc.Web.Options;
using Microsoft.Extensions.Options;

namespace Amtarc.Web.Services;

public sealed record UploadResult(bool Succeeded, string? Url, string? Error)
{
    public static UploadResult Ok(string url) => new(true, url, null);

    public static UploadResult Failed(string error) => new(false, null, error);
}

public interface IUploadService
{
    /// <summary>Validates the bytes, writes the file, and returns the URL to store on the item.</summary>
    Task<UploadResult> SaveAsync(
        Stream content, long length, CancellationToken cancellationToken = default);

    /// <summary>The directory uploads live in, as an absolute path.</summary>
    string ResolveDirectory();
}

public sealed class UploadService(
    IOptions<UploadOptions> options,
    IHostEnvironment environment,
    ILogger<UploadService> logger) : IUploadService
{
    private readonly UploadOptions _options = options.Value;

    /// <summary>
    /// The four formats the site accepts, keyed by what their first bytes actually are. The
    /// browser's declared content type and the client's filename are both attacker-controlled;
    /// the magic bytes are the only part of an upload that says what it really is.
    /// </summary>
    private static readonly (byte[] Signature, int Offset, string Extension)[] Signatures =
    [
        ([0xFF, 0xD8, 0xFF], 0, ".jpg"),
        ([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], 0, ".png"),
        ("GIF87a"u8.ToArray(), 0, ".gif"),
        ("GIF89a"u8.ToArray(), 0, ".gif"),
        // WEBP is a RIFF container: "RIFF" then 4 length bytes then "WEBP".
        ("WEBP"u8.ToArray(), 8, ".webp"),
    ];

    private const int HeaderBytes = 12;

    public async Task<UploadResult> SaveAsync(
        Stream content, long length, CancellationToken cancellationToken = default)
    {
        if (length <= 0)
        {
            return UploadResult.Failed("Le fichier est vide.");
        }

        if (length > _options.MaxBytes)
        {
            var limitMib = _options.MaxBytes / (1024 * 1024);
            return UploadResult.Failed($"L'image dépasse la taille maximale de {limitMib} Mo.");
        }

        var header = new byte[HeaderBytes];
        var read = await content.ReadAtLeastAsync(header, HeaderBytes, throwOnEndOfStream: false, cancellationToken);

        var extension = MatchExtension(header.AsSpan(0, read));
        if (extension is null)
        {
            return UploadResult.Failed("Format non accepté. Utilisez un JPEG, PNG, WebP ou GIF.");
        }

        var directory = ResolveDirectory();
        System.IO.Directory.CreateDirectory(directory);

        // The name comes from the sniffed type and a fresh GUID, never from the client: a client
        // filename can carry a path, an extension the bytes contradict, or a name that collides
        // with a file already there.
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(directory, fileName);

        await using (var file = File.Create(path))
        {
            await file.WriteAsync(header.AsMemory(0, read), cancellationToken);
            await content.CopyToAsync(file, cancellationToken);
        }

        logger.LogInformation("Stored upload {FileName} ({Bytes} bytes).", fileName, length);

        return UploadResult.Ok($"{_options.PublicBaseUrl.TrimEnd('/')}/uploads/{fileName}");
    }

    /// <summary>The directory uploads live in, as an absolute path.</summary>
    public string ResolveDirectory() =>
        Path.IsPathRooted(_options.Directory)
            ? _options.Directory
            : Path.Combine(environment.ContentRootPath, _options.Directory);

    private static string? MatchExtension(ReadOnlySpan<byte> header)
    {
        foreach (var (signature, offset, extension) in Signatures)
        {
            if (header.Length >= offset + signature.Length
                && header.Slice(offset, signature.Length).SequenceEqual(signature))
            {
                return extension;
            }
        }

        return null;
    }
}
