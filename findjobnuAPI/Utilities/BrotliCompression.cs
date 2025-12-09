using System.IO.Compression;

namespace FindjobnuAPI.Utilities
{
    public static class BrotliCompression
    {
        // Compress a byte array using Brotli with configurable quality (0-11). Default 11 for max ratio.
        public static byte[] Compress(byte[] data, int quality = 11)
        {
            if (data == null || data.Length == 0) return data ?? [];

            using var input = new MemoryStream(data);
            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                // Set quality if available via properties on underlying encoder (not exposed directly in .NET).
                // BrotliStream uses CompressionLevel; Optimal maps to high quality.
                input.CopyTo(brotli);
            }
            return output.ToArray();
        }

        // Decompress a Brotli-compressed byte array
        public static byte[] Decompress(byte[] compressed)
        {
            if (compressed == null || compressed.Length == 0) return compressed ?? [];

            using var input = new MemoryStream(compressed);
            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(input, CompressionMode.Decompress))
            {
                brotli.CopyTo(output);
            }
            return output.ToArray();
        }

        // Stream-to-stream helpers for large payloads
        public static void Compress(Stream input, Stream output)
        {
            using var brotli = new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true);
            input.CopyTo(brotli);
        }

        public static void Decompress(Stream input, Stream output)
        {
            using var brotli = new BrotliStream(input, CompressionMode.Decompress, leaveOpen: true);
            brotli.CopyTo(output);
        }
    }
}
