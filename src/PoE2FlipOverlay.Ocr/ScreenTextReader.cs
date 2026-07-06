using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PoE2FlipOverlay.Ocr;

/// <summary>A recognised word and its normalised centre (0..1) within the image.</summary>
public readonly record struct OcrWord(string Text, double CenterX, double CenterY);

/// <summary>Full OCR output: the joined line text plus every word with its position.</summary>
public sealed record OcrReadout(string Text, IReadOnlyList<OcrWord> Words);

/// <summary>
/// Reads text off an image using the OCR engine built into Windows 10/11
/// (<c>Windows.Media.Ocr</c>) — no external dependencies. Optionally upscales
/// the image first, which noticeably helps with small in-game text.
/// </summary>
public sealed class ScreenTextReader
{
    private readonly OcrEngine _engine;

    /// <summary>The language the OCR engine was created for (e.g. "en-US").</summary>
    public string RecognizerLanguage { get; }

    public ScreenTextReader()
    {
        var engine = OcrEngine.TryCreateFromLanguage(new Language("en"))
                     ?? OcrEngine.TryCreateFromUserProfileLanguages();

        _engine = engine ?? throw new InvalidOperationException(
            "No OCR engine is available. Install an OCR language pack via " +
            "Windows Settings > Time & Language > Language & region.");

        RecognizerLanguage = _engine.RecognizerLanguage.LanguageTag;
    }

    /// <summary>Runs OCR on an image file and returns the recognised text.</summary>
    public async Task<string> ReadFileAsync(string imagePath, int upscaleFactor = 2)
    {
        using var bitmap = await DecodeAsync(imagePath, upscaleFactor);
        return await ReadAsync(bitmap);
    }

    /// <summary>Runs OCR on an image file and returns text plus word positions.</summary>
    public async Task<OcrReadout> ReadDetailedFileAsync(string imagePath, int upscaleFactor = 2)
    {
        using var bitmap = await DecodeAsync(imagePath, upscaleFactor);
        var result = await _engine.RecognizeAsync(bitmap);

        double w = bitmap.PixelWidth;
        double h = bitmap.PixelHeight;
        var words = new List<OcrWord>();
        foreach (var line in result.Lines)
        {
            foreach (var word in line.Words)
            {
                var r = word.BoundingRect;
                words.Add(new OcrWord(word.Text, (r.X + r.Width / 2) / w, (r.Y + r.Height / 2) / h));
            }
        }

        var text = string.Join("\n", result.Lines.Select(l => l.Text));
        return new OcrReadout(text, words);
    }

    /// <summary>Runs OCR on an already-decoded bitmap.</summary>
    public async Task<string> ReadAsync(SoftwareBitmap bitmap)
    {
        var result = await _engine.RecognizeAsync(bitmap);
        return string.Join("\n", result.Lines.Select(line => line.Text));
    }

    private static async Task<SoftwareBitmap> DecodeAsync(string imagePath, int upscaleFactor)
    {
        if (upscaleFactor < 1) upscaleFactor = 1;

        var file = await StorageFile.GetFileFromPathAsync(imagePath);
        using var stream = await file.OpenAsync(FileAccessMode.Read);
        var decoder = await BitmapDecoder.CreateAsync(stream);

        var transform = new BitmapTransform
        {
            ScaledWidth = (uint)(decoder.PixelWidth * upscaleFactor),
            ScaledHeight = (uint)(decoder.PixelHeight * upscaleFactor),
            InterpolationMode = BitmapInterpolationMode.Cubic
        };

        return await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            transform,
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);
    }
}
