namespace pcheli_diplomenproekt.Services;

public interface IUploadService
{
    Task<string> SaveImageAsync(IFormFile file);
}

/// <summary>
/// Качване на снимки (JPG, PNG, WEBP до 4 МБ). Типът се определя по съдържанието на файла, а не по името му,
/// името се генерира на сървъра, а файловете се пазят в wwwroot/uploads.
/// </summary>
public class UploadService : IUploadService
{
    public const long MaxBytes = 4 * 1024 * 1024;
    private readonly IWebHostEnvironment _env;
    public UploadService(IWebHostEnvironment env) => _env = env;

    public static string? DetectExtension(ReadOnlySpan<byte> h)
    {
        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
        if (h.Length >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 && h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A) return ".png";
        if (h.Length >= 12 && h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' && h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P') return ".webp";
        return null;
    }

    public async Task<string> SaveImageAsync(IFormFile file)
    {
        if (file is null || file.Length == 0) throw new BusinessException("Не е избран файл.");
        if (file.Length > MaxBytes) throw new BusinessException("Файлът е твърде голям (максимум 4 МБ).", StatusCodes.Status413PayloadTooLarge);

        var header = new byte[12];
        await using (var s = file.OpenReadStream())
        {
            var read = 0;
            while (read < header.Length) { var n = await s.ReadAsync(header.AsMemory(read)); if (n == 0) break; read += n; }
        }
        var ext = DetectExtension(header) ?? throw new BusinessException("Позволени са само снимки във формат JPG, PNG или WEBP.");

        var dir = Path.Combine(_env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "uploads");
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{ext}";
        await using (var target = File.Create(Path.Combine(dir, name)))
            await file.CopyToAsync(target);
        return $"/uploads/{name}";
    }
}
