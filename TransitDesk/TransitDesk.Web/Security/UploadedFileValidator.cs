namespace TransitDesk.Web.Security;

/// <summary>
/// Weryfikacja przesyłanych załączników: rozmiar, biała lista rozszerzeń oraz sygnatura
/// binarna („magic bytes”). Nagłówek Content-Type i rozszerzenie kontroluje klient,
/// więc same w sobie niczego nie dowodzą – faktyczną zawartość sprawdzamy po pierwszych bajtach.
/// </summary>
public static class UploadedFileValidator
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, byte[][]> AllowedSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".pdf"] = [[0x25, 0x50, 0x44, 0x46, 0x2D]] // "%PDF-"
    };

    public static IReadOnlyCollection<string> AllowedExtensions => AllowedSignatures.Keys;

    /// <summary>Zwraca komunikat błędu lub null, jeśli plik jest poprawny.</summary>
    public static async Task<string?> ValidateAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length > MaxFileSizeBytes)
        {
            return "Rozmiar załącznika nie może przekraczać 5 MB.";
        }

        // Path.GetExtension operuje tylko na końcówce nazwy – ścieżki typu "../../x.pdf" nie mają tu znaczenia,
        // bo nazwy klienta i tak nie używamy do zapisu.
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedSignatures.TryGetValue(extension, out var signatures))
        {
            return $"Niedozwolony typ pliku. Dozwolone: {string.Join(", ", AllowedExtensions)}.";
        }

        var maxSignatureLength = signatures.Max(s => s.Length);
        var header = new byte[maxSignatureLength];

        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, maxSignatureLength, throwOnEndOfStream: false, cancellationToken);

        var matches = signatures.Any(signature =>
            read >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature));

        return matches ? null : "Zawartość pliku nie odpowiada jego rozszerzeniu.";
    }

    public static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };
}
