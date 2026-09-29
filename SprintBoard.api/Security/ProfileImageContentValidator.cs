namespace SprintBoard.api.Security;

/// <summary>
/// Validates uploaded profile images using their declared metadata,
/// file extension, size, and binary file signature.
/// </summary>
public static class ProfileImageContentValidator
{
    /// <summary>
    /// Maximum allowed profile image size in bytes.
    /// </summary>
    public const long MaxFileSizeBytes =
        5_000_000;

    /// <summary>
    /// Validates a profile image and returns trusted image data
    /// that can safely be persisted.
    /// </summary>
    public static async Task<ProfileImageValidationResult>
        ValidateAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken =
                default)
    {
        if (fileStream is null ||
            !fileStream.CanRead)
        {
            throw new ArgumentException(
                "File stream is invalid.",
                nameof(fileStream));
        }

        if (string.IsNullOrWhiteSpace(
                fileName))
        {
            throw new ArgumentException(
                "File name is required.",
                nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(
                contentType))
        {
            throw new ArgumentException(
                "File content type is required.",
                nameof(contentType));
        }

        var normalizedContentType =
            contentType
                .Trim()
                .ToLowerInvariant();

        var originalExtension =
            Path.GetExtension(fileName)
                .ToLowerInvariant();

        var canonicalExtension =
            GetCanonicalExtension(
                normalizedContentType,
                originalExtension);

        var content =
            await ReadContentAsync(
                fileStream,
                cancellationToken);

        if (!HasExpectedSignature(
                content,
                normalizedContentType))
        {
            throw new ArgumentException(
                "File content does not match " +
                "the declared image type.");
        }

        return new ProfileImageValidationResult(
            content,
            normalizedContentType,
            canonicalExtension);
    }

    /// <summary>
    /// Reads the uploaded content while enforcing the
    /// maximum allowed profile image size.
    /// </summary>
    private static async Task<byte[]>
        ReadContentAsync(
            Stream fileStream,
            CancellationToken cancellationToken)
    {
        const int bufferSize =
            81_920;

        byte[] buffer =
            new byte[bufferSize];

        await using var memoryStream =
            new MemoryStream();

        long totalBytes =
            0;

        int bytesRead;

        while ((bytesRead =
                   await fileStream.ReadAsync(
                       buffer.AsMemory(
                           0,
                           buffer.Length),
                       cancellationToken)) > 0)
        {
            totalBytes +=
                bytesRead;

            if (totalBytes >
                MaxFileSizeBytes)
            {
                throw new ArgumentException(
                    "Profile image cannot exceed " +
                    "5 MB.");
            }

            await memoryStream.WriteAsync(
                buffer.AsMemory(
                    0,
                    bytesRead),
                cancellationToken);
        }

        if (totalBytes == 0)
        {
            throw new ArgumentException(
                "Profile image cannot be empty.");
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Resolves the trusted extension for a supported
    /// image MIME type and verifies that the original
    /// extension is compatible with it.
    /// </summary>
    private static string GetCanonicalExtension(
        string contentType,
        string originalExtension)
    {
        return contentType switch
        {
            "image/jpeg"
                when originalExtension is
                    ".jpg" or ".jpeg"
                => ".jpg",

            "image/png"
                when originalExtension ==
                    ".png"
                => ".png",

            "image/webp"
                when originalExtension ==
                    ".webp"
                => ".webp",

            "image/jpeg" or
            "image/png" or
            "image/webp"
                => throw new ArgumentException(
                    "File extension does not match " +
                    "the declared image type."),

            _ =>
                throw new ArgumentException(
                    "Only JPG, PNG and WEBP images " +
                    "are allowed.")
        };
    }

    /// <summary>
    /// Determines whether the binary file signature
    /// matches the declared image type.
    /// </summary>
    private static bool HasExpectedSignature(
        ReadOnlySpan<byte> content,
        string contentType)
    {
        return contentType switch
        {
            "image/jpeg" =>
                HasJpegSignature(content),

            "image/png" =>
                HasPngSignature(content),

            "image/webp" =>
                HasWebpSignature(content),

            _ =>
                false
        };
    }

    /// <summary>
    /// Determines whether the content begins with a
    /// valid JPEG signature.
    /// </summary>
    private static bool HasJpegSignature(
        ReadOnlySpan<byte> content)
    {
        return content.Length >= 3 &&
            content[0] == 0xFF &&
            content[1] == 0xD8 &&
            content[2] == 0xFF;
    }

    /// <summary>
    /// Determines whether the content begins with the
    /// standard PNG signature.
    /// </summary>
    private static bool HasPngSignature(
        ReadOnlySpan<byte> content)
    {
        return content.Length >= 8 &&
            content[0] == 0x89 &&
            content[1] == 0x50 &&
            content[2] == 0x4E &&
            content[3] == 0x47 &&
            content[4] == 0x0D &&
            content[5] == 0x0A &&
            content[6] == 0x1A &&
            content[7] == 0x0A;
    }

    /// <summary>
    /// Determines whether the content contains the
    /// RIFF and WEBP signatures required by WebP.
    /// </summary>
    private static bool HasWebpSignature(
        ReadOnlySpan<byte> content)
    {
        return content.Length >= 12 &&
            content[0] == 0x52 &&
            content[1] == 0x49 &&
            content[2] == 0x46 &&
            content[3] == 0x46 &&
            content[8] == 0x57 &&
            content[9] == 0x45 &&
            content[10] == 0x42 &&
            content[11] == 0x50;
    }
}

/// <summary>
/// Represents trusted profile image data after
/// security validation has completed.
/// </summary>
public sealed record ProfileImageValidationResult(
    byte[] Content,
    string ContentType,
    string FileExtension);