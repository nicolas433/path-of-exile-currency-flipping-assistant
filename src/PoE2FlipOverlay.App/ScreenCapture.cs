using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace PoE2FlipOverlay.App;

/// <summary>
/// Grabs a rectangle of the screen, enhances it for OCR (upscale + grayscale +
/// contrast stretch — the same recipe the HTML prototype used), and saves a PNG.
/// The app is per-monitor DPI aware, so coordinates match what calibration stored.
/// Saving the processed image also makes it easy to eyeball what OCR receives.
/// </summary>
internal static class ScreenCapture
{
    private const int Scale = 4; // enlarge small in-game text before OCR

    public static string CaptureToPng(CaptureRegion region, string name)
    {
        using var raw = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(raw))
        {
            g.CopyFromScreen(region.X, region.Y, 0, 0,
                new Size(region.Width, region.Height), CopyPixelOperation.SourceCopy);
        }

        using var processed = Enhance(raw);

        var dir = Path.Combine(Path.GetTempPath(), "poe2flip");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{name}.png");
        processed.Save(path, ImageFormat.Png);
        return path;
    }

    private static Bitmap Enhance(Bitmap src)
    {
        var dst = new Bitmap(src.Width * Scale, src.Height * Scale, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(dst))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(src, new Rectangle(0, 0, dst.Width, dst.Height));
        }

        var rect = new Rectangle(0, 0, dst.Width, dst.Height);
        var data = dst.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            int bytes = Math.Abs(data.Stride) * dst.Height;
            var buffer = new byte[bytes];
            Marshal.Copy(data.Scan0, buffer, 0, bytes);

            for (int i = 0; i < bytes; i += 4)
            {
                // BGRA order. Luminance, then stretch mid-tones to separate the
                // cream game text from the dark parchment.
                double gray = 0.11 * buffer[i] + 0.59 * buffer[i + 1] + 0.30 * buffer[i + 2];
                byte v = (byte)Math.Clamp((gray - 60.0) * 255.0 / 140.0, 0.0, 255.0);
                buffer[i] = buffer[i + 1] = buffer[i + 2] = v;
                buffer[i + 3] = 255;
            }

            Marshal.Copy(buffer, 0, data.Scan0, bytes);
        }
        finally
        {
            dst.UnlockBits(data);
        }

        return dst;
    }
}
