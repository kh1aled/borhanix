namespace DepiLms.Services;

public class VideoDurationService(IWebHostEnvironment environment) : IVideoDurationService
{
    public int? TryGetDurationSeconds(string? webRootPath, string? videoUrl, int fallbackDurationMinutes, int? clientReportedSeconds = null)
    {
        if (clientReportedSeconds is > 0)
        {
            return clientReportedSeconds;
        }

        if (!string.IsNullOrWhiteSpace(videoUrl) && videoUrl.StartsWith('/'))
        {
            var root = webRootPath ?? environment.WebRootPath;
            var fullPath = Path.Combine(root, videoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            var parsed = TryParseMediaDuration(fullPath);
            if (parsed is > 0)
            {
                return parsed;
            }
        }

        return fallbackDurationMinutes > 0 ? fallbackDurationMinutes * 60 : null;
    }

    public async Task<int?> TryGetUploadDurationSecondsAsync(IFormFile file, string savedRelativePath, int fallbackDurationMinutes)
    {
        var fullPath = Path.Combine(environment.WebRootPath, savedRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        await using (var stream = file.OpenReadStream())
        {
            var fromStream = TryParseMediaDuration(stream, Path.GetExtension(file.FileName));
            if (fromStream is > 0)
            {
                return fromStream;
            }
        }

        var fromFile = TryParseMediaDuration(fullPath);
        if (fromFile is > 0)
        {
            return fromFile;
        }

        return fallbackDurationMinutes > 0 ? fallbackDurationMinutes * 60 : null;
    }

    internal static int? TryParseMediaDuration(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        using var stream = File.OpenRead(filePath);
        return TryParseMediaDuration(stream, Path.GetExtension(filePath));
    }

    internal static int? TryParseMediaDuration(Stream stream, string extension)
    {
        extension = extension.ToLowerInvariant();
        return extension switch
        {
            ".mp4" or ".mov" or ".m4v" => TryParseMp4Duration(stream),
            _ => null
        };
    }

    private static int? TryParseMp4Duration(Stream stream)
    {
        if (!stream.CanSeek)
        {
            return null;
        }

        var buffer = new byte[Math.Min(stream.Length, 1024 * 1024)];
        stream.ReadExactly(buffer);

        for (var i = 0; i < buffer.Length - 8; i++)
        {
            if (buffer[i + 4] == (byte)'m' && buffer[i + 5] == (byte)'v' && buffer[i + 6] == (byte)'h' && buffer[i + 7] == (byte)'d')
            {
                var version = buffer[i + 8];
                if (version == 0 && i + 28 <= buffer.Length)
                {
                    var timescale = ReadUInt32BigEndian(buffer, i + 20);
                    var duration = ReadUInt32BigEndian(buffer, i + 24);
                    return timescale > 0 ? (int)Math.Round((double)duration / timescale) : null;
                }

                if (version == 1 && i + 40 <= buffer.Length)
                {
                    var timescale = ReadUInt32BigEndian(buffer, i + 28);
                    var durationHigh = ReadUInt32BigEndian(buffer, i + 32);
                    var durationLow = ReadUInt32BigEndian(buffer, i + 36);
                    var duration = ((long)durationHigh << 32) | durationLow;
                    return timescale > 0 ? (int)Math.Round((double)duration / timescale) : null;
                }
            }
        }

        return null;
    }

    private static uint ReadUInt32BigEndian(byte[] buffer, int offset)
        => (uint)((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3]);
}
