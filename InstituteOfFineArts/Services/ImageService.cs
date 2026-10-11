using SkiaSharp;

namespace InstituteOfFineArts.Services;

public record SavedImage(string Path, string ThumbPath, long Size, int Width, int Height);

public interface IImageService
{
    /// <summary>Validates that the upload really is a JPEG, stores it and creates a thumbnail.</summary>
    Task<(SavedImage? Image, string? Error)> SaveJpegAsync(IFormFile file);

    /// <summary>Stores a bitmap that was generated in code (used by the demo-data seeder).</summary>
    SavedImage SaveBitmap(SKBitmap bitmap);

    void Delete(string? path, string? thumbPath);
}

public class ImageService : IImageService
{
    public const long MaxBytes = 10 * 1024 * 1024;
    private const int MinSide = 300;
    private const int ThumbWidth = 720;

    private readonly string _paintings;
    private readonly string _thumbs;
    private readonly string _webRoot;

    public ImageService(IWebHostEnvironment env)
    {
        _webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _paintings = Path.Combine(_webRoot, "uploads", "paintings");
        _thumbs = Path.Combine(_webRoot, "uploads", "thumbs");
        Directory.CreateDirectory(_paintings);
        Directory.CreateDirectory(_thumbs);
    }

    public async Task<(SavedImage? Image, string? Error)> SaveJpegAsync(IFormFile file)
    {
        if (file.Length == 0) return (null, "The selected file is empty.");
        if (file.Length > MaxBytes) return (null, "The painting must be 10 MB or smaller.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg")) return (null, "Only JPEG (.jpg / .jpeg) files are allowed.");

        byte[] bytes;
        await using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        // Real content check – the extension alone proves nothing.
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.EncodedFormat != SKEncodedImageFormat.Jpeg)
            return (null, "That file is not a valid JPEG image.");

        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null) return (null, "The image could not be read. Please try another file.");
        if (bitmap.Width < MinSide || bitmap.Height < MinSide)
            return (null, $"The painting is too small – at least {MinSide}×{MinSide} pixels are required.");

        var name = Guid.NewGuid().ToString("N") + ".jpg";
        await File.WriteAllBytesAsync(Path.Combine(_paintings, name), bytes);
        WriteThumb(bitmap, name);

        return (new SavedImage($"/uploads/paintings/{name}", $"/uploads/thumbs/{name}", bytes.Length, bitmap.Width, bitmap.Height), null);
    }

    public SavedImage SaveBitmap(SKBitmap bitmap)
    {
        var name = Guid.NewGuid().ToString("N") + ".jpg";
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 88);
        var full = Path.Combine(_paintings, name);
        using (var fs = File.Create(full)) encoded.SaveTo(fs);
        WriteThumb(bitmap, name);
        return new SavedImage($"/uploads/paintings/{name}", $"/uploads/thumbs/{name}", new FileInfo(full).Length, bitmap.Width, bitmap.Height);
    }

    public void Delete(string? path, string? thumbPath)
    {
        foreach (var rel in new[] { path, thumbPath })
        {
            if (string.IsNullOrWhiteSpace(rel)) continue;
            var full = Path.GetFullPath(Path.Combine(_webRoot, rel.TrimStart('/', '\\')));
            // never delete anything outside the uploads folder
            if (!full.StartsWith(Path.Combine(_webRoot, "uploads"), StringComparison.OrdinalIgnoreCase)) continue;
            if (File.Exists(full)) File.Delete(full);
        }
    }

    private void WriteThumb(SKBitmap source, string name)
    {
        var width = Math.Min(ThumbWidth, source.Width);
        var height = (int)Math.Round(source.Height * (width / (double)source.Width));
        using var resized = source.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium) ?? source.Copy();
        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 82);
        using var fs = File.Create(Path.Combine(_thumbs, name));
        encoded.SaveTo(fs);
    }
}
