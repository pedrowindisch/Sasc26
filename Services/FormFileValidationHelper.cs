namespace Sasc26.Services;

public static class FormFileValidationHelper
{
    public const long DefaultMaxBytes = 10 * 1024 * 1024;

    public static string GetAcceptAttribute(string? fileAccept) => fileAccept switch
    {
        "pdf" => ".pdf,application/pdf",
        "image" => "image/*",
        "pdf_image" => ".pdf,application/pdf,image/*",
        _ => "*/*"
    };

    public static string[] GetAllowedContentTypes(string? fileAccept) => fileAccept switch
    {
        "pdf" => ["application/pdf"],
        "image" => ["image/jpeg", "image/png", "image/webp", "image/heic", "image/heif", "image/gif", "image/bmp", "image/svg+xml"],
        "pdf_image" => ["application/pdf", "image/jpeg", "image/png", "image/webp", "image/heic", "image/heif", "image/gif", "image/bmp", "image/svg+xml"],
        _ => [] // any
    };

    public static bool IsContentTypeAllowed(string contentType, string? fileAccept)
    {
        var allowed = GetAllowedContentTypes(fileAccept);
        if (allowed.Length == 0) return true;
        contentType = contentType.ToLowerInvariant();
        if (allowed.Contains(contentType)) return true;
        if (fileAccept == "image" || fileAccept == "pdf_image")
        {
            if (contentType.StartsWith("image/")) return true;
        }
        return false;
    }

    public static long GetMaxBytes(int? fileMaxSizeMb)
    {
        var mb = fileMaxSizeMb ?? 10;
        if (mb < 1) mb = 1;
        if (mb > 10) mb = 10;
        return mb * 1024L * 1024L;
    }
}
