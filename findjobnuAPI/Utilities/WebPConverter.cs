using System.IO;

namespace FindjobnuService.Utilities
{
    public static class WebPConverter
    {
        // Placeholder encoder using System.Drawing for JPEG/PNG passthrough; real WebP needs a library.
        // Keeps API stable so we can swap in SkiaSharp or ImageSharp.WebP later.
        public static (byte[] bytes, string format, string mimeType) ConvertToWebP(byte[]? input)
        {
            if (input == null || input.Length == 0)
                return (input ?? [], null!, null!);

            // Without external deps, we cannot encode WebP. Return original.
            // Frontend can still honor mime type if we detect JPEG/PNG by simple header check.
            var mime = DetectMimeType(input);
            return (input, "original", mime);
        }

        private static string DetectMimeType(byte[] data)
        {
            // JPEG FF D8 FF, PNG 89 50 4E 47 0D 0A 1A 0A
            if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return "image/jpeg";
            if (data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A) return "image/png";
            if (data.Length >= 12 && data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F' && data[8] == 'W' && data[9] == 'E' && data[10] == 'B' && data[11] == 'P') return "image/webp";
            return "application/octet-stream";
        }
    }
}
