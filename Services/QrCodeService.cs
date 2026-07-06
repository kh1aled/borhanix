using DepiLms.Models;
using QRCoder;

namespace DepiLms.Services;

public class QrCodeService : IQrCodeService
{
    public string BuildStudentPayload(StudentProfile profile)
        => $"DEPI-LMS|STUDENT|{profile.StudentCode}|{profile.QrToken}";

    public string? ExtractStudentToken(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        var trimmed = payload.Trim();
        var parts = trimmed.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 4 &&
            parts[0].Equals("DEPI-LMS", StringComparison.OrdinalIgnoreCase) &&
            parts[1].Equals("STUDENT", StringComparison.OrdinalIgnoreCase))
        {
            return parts[3];
        }

        return trimmed.Length is >= 24 and <= 128 ? trimmed : null;
    }

    public string CreateSvg(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qr = new SvgQRCode(data);
        return qr.GetGraphic(6, "#111827", "#ffffff", true);
    }

    public string CreatePngDataUri(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(12, [17, 24, 39], [255, 255, 255], true);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
