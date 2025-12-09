using FindjobnuService.DTOs;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace FindjobnuService.Services;

public class CvReadabilityService : ICvReadabilityService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private readonly ILogger<CvReadabilityService> _logger;

    public CvReadabilityService(ILogger<CvReadabilityService> logger)
    {
        _logger = logger;
    }

    public async Task<CvReadabilityResult> AnalyzeAsync(IFormFile pdfFile, CancellationToken cancellationToken = default)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            throw new ArgumentException("PDF file is required.");
        }

        if (!pdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Unsupported file extension. Only .pdf is allowed.");
        }

        if (pdfFile.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException($"File too large. Max allowed size is {MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        // Content-Type can be spoofed; we still check magic header below
        if (!string.Equals(pdfFile.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(pdfFile.ContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid Content-Type. Expecting application/pdf.");
        }

        string text;
        using (var ms = new MemoryStream((int)pdfFile.Length))
        {
            await pdfFile.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;

            // Basic PDF signature checks: header and EOF marker; simple encryption hint
            if (!LooksLikePdf(ms))
            {
                throw new ArgumentException("The uploaded file does not appear to be a valid PDF.");
            }

            ms.Position = 0;
            text = ExtractTextWithIText(ms);
            if (string.IsNullOrWhiteSpace(text))
            {
                // Fallback to internal extractor (handles some edge encodings)
                ms.Position = 0;
                text = ExtractTextFromPdf(ms);
            }
        }

        var summary = BuildSummary(text);
        var score = ComputeReadabilityScore(text);

        return new CvReadabilityResult(text, score, summary);
    }

    private static bool LooksLikePdf(Stream stream)
    {
        // Read first bytes for %PDF- and search for %%EOF near the end. Also block encrypted PDFs heuristically ("/Encrypt").
        long originalPos = stream.CanSeek ? stream.Position : 0;
        try
        {
            if (!stream.CanSeek) return false;

            // Header check
            stream.Position = 0;
            Span<byte> header = stackalloc byte[5];
            int read = stream.Read(header);
            if (read < 5 || header[0] != (byte)'%' || header[1] != (byte)'P' || header[2] != (byte)'D' || header[3] != (byte)'F' || header[4] != (byte)'-')
                return false;

            // EOF check (look in last 1KB)
            var tailSize = (int)Math.Min(1024, stream.Length);
            stream.Position = stream.Length - tailSize;
            var tailBuf = new byte[tailSize];
            stream.Read(tailBuf, 0, tailSize);
            var tailStr = Encoding.ASCII.GetString(tailBuf);
            if (!tailStr.Contains("%%EOF", StringComparison.Ordinal))
                return false;

            // Encryption hint
            stream.Position = 0;
            var headSize = (int)Math.Min(4096, stream.Length);
            var headBuf = new byte[headSize];
            stream.Read(headBuf, 0, headSize);
            var headStr = Encoding.ASCII.GetString(headBuf);
            if (headStr.Contains("/Encrypt", StringComparison.Ordinal))
                throw new ArgumentException("Encrypted/password-protected PDFs are not supported.");

            return true;
        }
        finally
        {
            if (stream.CanSeek) stream.Position = originalPos;
        }
    }

    private static string ExtractTextWithIText(Stream pdfStream)
    {
        try
        {
            var sb = new StringBuilder();
            using var reader = new PdfReader(pdfStream);
            using var pdf = new PdfDocument(reader);
            int pages = pdf.GetNumberOfPages();
            for (int i = 1; i <= pages; i++)
            {
                var strategy = new LocationTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(pdf.GetPage(i), strategy);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
                if (sb.Length > 2_500_000) break; // safety cap
            }

            var result = sb.ToString();
            // normalize
            result = Regex.Replace(result, "-\r?\n", string.Empty);
            result = Regex.Replace(result, "\r?\n", "\n");
            result = Regex.Replace(result, "[\t\u00A0]", " ");
            result = Regex.Replace(result, "[ ]{2,}", " ");
            result = Regex.Replace(result, "\n{3,}", "\n\n");
            return result.Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    // Improved extractor: parse streams, attempt to decompress Flate (zlib) content, then scan for text-show operators.
    private static string ExtractTextFromPdf(Stream pdfStream)
    {
        try
        {
            if (pdfStream is MemoryStream ms)
            {
                var data = ms.ToArray();
                var content = new StringBuilder();

                foreach (var streamContent in EnumeratePdfStreams(data))
                {
                    // Try to decompress if Flate/zlib; if not, use as-is
                    string decoded = TryDecodeStream(streamContent.Raw, streamContent.IsFlate) ?? string.Empty;
                    if (decoded.Length == 0) continue;
                    content.AppendLine(decoded);
                    if (content.Length > 2_500_000) break; // safety cap
                }

                var combined = content.ToString();
                if (string.IsNullOrEmpty(combined))
                {
                    // Fallback: raw ASCII scan (very naive)
                    pdfStream.Position = 0;
                    using var fallbackReader = new StreamReader(pdfStream, Encoding.Latin1, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                    combined = fallbackReader.ReadToEnd();
                }

                return ExtractTextFromContentStreams(combined);
            }
            else
            {
                using var tmp = new MemoryStream();
                pdfStream.CopyTo(tmp);
                tmp.Position = 0;
                return ExtractTextFromPdf(tmp);
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ExtractTextFromContentStreams(string combined)
    {
        var results = new List<string>();

        // ( ... ) Tj
        var tjMatches = Regex.Matches(combined, "\\((?<s>(?:\\\\.|[^\\\\\\)])*)\\)\\s*T[jJ]");
        foreach (Match m in tjMatches)
        {
            var s = UnescapePdfString(m.Groups["s"].Value);
            if (!string.IsNullOrWhiteSpace(s)) results.Add(s);
            if (TotalLength(results) > 2_000_000) break;
        }

        if (TotalLength(results) <= 2_000_000)
        {
            // [ (..).. ] TJ
            var tjArrayMatches = Regex.Matches(combined, "\\[(?<arr>[^\\]]*)\\]\\s*TJ");
            foreach (Match m in tjArrayMatches)
            {
                var arr = m.Groups["arr"].Value;
                var parts = Regex.Matches(arr, "\\((?<s>(?:\\\\.|[^\\\\\\)])*)\\)");
                var sb = new StringBuilder();
                foreach (Match p in parts)
                {
                    sb.Append(UnescapePdfString(p.Groups["s"].Value));
                    if (sb.Length > 200_000) break;
                }
                var s = sb.ToString();
                if (!string.IsNullOrWhiteSpace(s)) results.Add(s);
                if (TotalLength(results) > 2_000_000) break;
            }
        }

        if (TotalLength(results) <= 2_000_000)
        {
            // ( ... ) ' and ( ... ) " operators
            var singleQuoteMatches = Regex.Matches(combined, "\\((?<s>(?:\\\\.|[^\\\\\\)])*)\\)\\s*'");
            foreach (Match m in singleQuoteMatches)
            {
                var s = UnescapePdfString(m.Groups["s"].Value);
                if (!string.IsNullOrWhiteSpace(s)) results.Add(s);
                if (TotalLength(results) > 2_000_000) break;
            }

            var doubleQuoteMatches = Regex.Matches(combined, "\\((?<s>(?:\\\\.|[^\\\\\\)])*)\\)\\s*\"");
            foreach (Match m in doubleQuoteMatches)
            {
                var s = UnescapePdfString(m.Groups["s"].Value);
                if (!string.IsNullOrWhiteSpace(s)) results.Add(s);
                if (TotalLength(results) > 2_000_000) break;
            }
        }

        if (results.Count == 0) return string.Empty;

        return NormalizeWhitespace(string.Join("\n", results));
    }

    private readonly struct PdfStreamChunk
    {
        public PdfStreamChunk(byte[] raw, bool isFlate)
        {
            Raw = raw;
            IsFlate = isFlate;
        }
        public byte[] Raw { get; }
        public bool IsFlate { get; }
    }

    private static IEnumerable<PdfStreamChunk> EnumeratePdfStreams(byte[] data)
    {
        var ascii = Encoding.ASCII;
        var text = ascii.GetString(data);
        int index = 0;
        while (index < text.Length)
        {
            int streamPos = text.IndexOf("stream", index, StringComparison.Ordinal);
            if (streamPos == -1) yield break;

            // Find preceding dictionary to detect filter
            int dictEnd = streamPos;
            int dictStart = text.LastIndexOf("<<", dictEnd, dictEnd);
            string dict = dictStart >= 0 ? text.Substring(dictStart, dictEnd - dictStart) : string.Empty;
            bool isFlate = dict.Contains("/FlateDecode", StringComparison.Ordinal) || dict.Contains("/Fl", StringComparison.Ordinal);

            // Move to data start (after newline)
            int dataStart = streamPos + "stream".Length;
            if (dataStart < text.Length && (text[dataStart] == '\r' || text[dataStart] == '\n'))
            {
                if (text[dataStart] == '\r' && dataStart + 1 < text.Length && text[dataStart + 1] == '\n') dataStart += 2; else dataStart += 1;
            }

            int endStreamPos = text.IndexOf("endstream", dataStart, StringComparison.Ordinal);
            if (endStreamPos == -1) yield break;

            // Map char indices to byte offsets assuming ASCII (PDF keywords are ASCII; stream bytes are binary but we slice using same offsets)
            int byteStart = ascii.GetByteCount(text.Substring(0, dataStart));
            int byteEnd = ascii.GetByteCount(text.Substring(0, endStreamPos));
            if (byteEnd > data.Length || byteStart > data.Length || byteEnd <= byteStart)
            {
                index = endStreamPos + 9; // skip past endstream
                continue;
            }

            var length = byteEnd - byteStart;
            var raw = new byte[length];
            Buffer.BlockCopy(data, byteStart, raw, 0, length);
            yield return new PdfStreamChunk(raw, isFlate);

            index = endStreamPos + 9;
        }
    }

    private static string? TryDecodeStream(byte[] raw, bool isFlate)
    {
        // Most content streams are zlib (Flate). Try zlib first, then raw deflate, then ASCII fallback.
        if (isFlate)
        {
            try
            {
                using var input = new MemoryStream(raw);
                using var z = new ZLibStream(input, CompressionMode.Decompress, leaveOpen: false);
                using var reader = new StreamReader(z, Encoding.Latin1);
                return reader.ReadToEnd();
            }
            catch
            {
                try
                {
                    using var input = new MemoryStream(raw);
                    using var def = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: false);
                    using var reader = new StreamReader(def, Encoding.Latin1);
                    return reader.ReadToEnd();
                }
                catch
                {
                    // fall through
                }
            }
        }

        // Non-compressed or failed decompression: try ASCII
        try
        {
            return Encoding.Latin1.GetString(raw);
        }
        catch
        {
            return null;
        }
    }

    private static int TotalLength(List<string> list)
    {
        int len = 0;
        foreach (var s in list) len += s?.Length ?? 0;
        return len;
    }

    private static string UnescapePdfString(string s)
    {
        // Handle common escapes: \\ \( \) and octal \ddd
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '\\' && i + 1 < s.Length)
            {
                var n = s[i + 1];
                switch (n)
                {
                    case '\\': sb.Append('\\'); i++; break;
                    case '(': sb.Append('('); i++; break;
                    case ')': sb.Append(')'); i++; break;
                    case 'n': sb.Append('\n'); i++; break;
                    case 'r': sb.Append('\r'); i++; break;
                    case 't': sb.Append('\t'); i++; break;
                    default:
                        // octal: up to 3 digits
                        if (char.IsDigit(n))
                        {
                            int len = 1;
                            while (i + 1 + len < s.Length && len < 3 && char.IsDigit(s[i + 1 + len])) len++;
                            var oct = s.Substring(i + 1, len);
                            try
                            {
                                var val = Convert.ToInt32(oct, 8);
                                sb.Append((char)val);
                                i += len;
                            }
                            catch { sb.Append(n); i++; }
                        }
                        else
                        {
                            sb.Append(n);
                            i++;
                        }
                        break;
                }
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static string NormalizeWhitespace(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        input = Regex.Replace(input, "-\r?\n", string.Empty); // remove hyphenated line breaks that split words
        var normalized = Regex.Replace(input, "\r?\n", "\n"); // normalize newlines
        normalized = Regex.Replace(normalized, "[\t\u00A0]", " "); // tabs & non-breaking space
        normalized = Regex.Replace(normalized, "[ ]{2,}", " "); // collapse spaces
        normalized = Regex.Replace(normalized, "\n{3,}", "\n\n"); // collapse blank lines
        return normalized.Trim();
    }

    private static readonly string[] DefaultSectionKeywords = new[]
    {
        "experience", "education", "skills", "projects", "summary", "profile",
        "erfaring", "uddannelse", "færdigheder", "projekter", "om", "om mig", "bio", "profil", "kontakt"
    };

    private static CvReadabilitySummary BuildSummary(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new CvReadabilitySummary(
                TotalChars: 0,
                TotalWords: 0,
                TotalLines: 0,
                HasEmail: false,
                HasPhone: false,
                BulletCount: 0,
                MatchedSections: 0,
                TotalSectionKeywords: DefaultSectionKeywords.Length,
                Note: "Ingen tekst kunne udtrækkes. PDF'en kan være billedbaseret eller beskyttet."
            );
        }

        var totalChars = text.Length;
        var totalWords = Regex.Matches(text, @"\b[\r\n\p{L}\p{Nd}\-_.]+\b").Count;
        var totalLines = text.Split('\n').Length;

        // Heuristics: sections, bullet points, contact info
        var hasEmail = Regex.IsMatch(text, @"[A-Z0-9._%+-]+\s*@\s*[A-Z0-9.-]+\s*\.\s*[A-Z]{2,}", RegexOptions.IgnoreCase);
        var hasPhone = Regex.IsMatch(text, @"(\n|\s)(\+?\d[\d\s().-]{6,}\d)");
        var bulletCount = Regex.Matches(text, @"(^|\n)[\u2022\-*] \s?").Count;
        var matchedSections = DefaultSectionKeywords.Count(k => Regex.IsMatch(text, $@"(^|\n)\s*{Regex.Escape(k)}\b", RegexOptions.IgnoreCase));

        return new CvReadabilitySummary(
            TotalChars: totalChars,
            TotalWords: totalWords,
            TotalLines: totalLines,
            HasEmail: hasEmail,
            HasPhone: hasPhone,
            BulletCount: bulletCount,
            MatchedSections: matchedSections,
            TotalSectionKeywords: DefaultSectionKeywords.Length,
            Note: null
        );
    }

    // Simple heuristic readability score (0-100). Higher is more machine-readable.
    private static double ComputeReadabilityScore(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0.0;

        double score = 50.0; // base

        // Penalize if too short
        var words = Regex.Matches(text, @"\b[\r\n\p{L}\p{Nd}\-_.]+\b").Count;
        if (words < 100) score -= 10;
        else if (words > 1500) score -= 10; // too long
        else score += 5;

        // Reward presence of contact info (allow minor spacing around @ and .)
        if (Regex.IsMatch(text, @"[A-Z0-9._%+-]+\s*@\s*[A-Z0-9.-]+\s*\.\s*[A-Z]{2,}", RegexOptions.IgnoreCase)) score += 10;
        if (Regex.IsMatch(text, @"(\n|\s)(\+?\d[\d\s().-]{6,}\d)")) score += 5;

        // Reward bullets
        var bulletCount = Regex.Matches(text, @"(^|\n)[\u2022\-*] \s?").Count;
        score += Math.Min(10, bulletCount);

        // Reward recognized sections
        var sectionKeywords = new[] { "experience", "education", "skills", "projects", "summary", "profile", "erfaring", "uddannelse", "færdigheder", "projekter", "om", "om mig", "bio", "profil", "resumé", "resume" };
        var sections = sectionKeywords.Count(k => Regex.IsMatch(text, $@"(^|\n)\s*{Regex.Escape(k)}\b", RegexOptions.IgnoreCase));
        score += sections * 5;

        // Penalize if many non-letter symbols (suggests parsing noise)
        var nonLetterRatio = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && c != '-' && c != '.') / (double)text.Length;
        if (nonLetterRatio > 0.2) score -= 10;

        // Clamp 0-100
        return Math.Max(0, Math.Min(100, score));
    }
}
