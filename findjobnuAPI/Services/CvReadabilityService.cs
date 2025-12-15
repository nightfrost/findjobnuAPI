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
    private const int MaxExtractedCharacters = 2_500_000;
    private const int MaxResultCharacters = 2_000_000;
    private const int MaxArraySegmentCharacters = 200_000;
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
            if (!TryFillBuffer(stream, header))
                return false;
            if (header[0] != (byte)'%' || header[1] != (byte)'P' || header[2] != (byte)'D' || header[3] != (byte)'F' || header[4] != (byte)'-')
                return false;

            // EOF check (look in last 1KB)
            var tailSize = (int)Math.Min(1024, stream.Length);
            stream.Position = stream.Length - tailSize;
            var tailBuf = new byte[tailSize];
            if (!TryFillBuffer(stream, tailBuf))
                return false;
            var tailStr = Encoding.ASCII.GetString(tailBuf);
            if (!tailStr.Contains("%%EOF", StringComparison.Ordinal))
                return false;

            // Encryption hint
            stream.Position = 0;
            var headSize = (int)Math.Min(4096, stream.Length);
            var headBuf = new byte[headSize];
            if (!TryFillBuffer(stream, headBuf))
                return false;
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
                if (sb.Length > MaxExtractedCharacters) break; // safety cap
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
            var memoryStream = EnsureMemoryStream(pdfStream, out var ownsStream);
            try
            {
                var combinedContent = ExtractDecodedStreams(memoryStream);
                if (string.IsNullOrEmpty(combinedContent))
                {
                    combinedContent = ReadRawPdfContent(memoryStream);
                }

                return ExtractTextFromContentStreams(combinedContent);
            }
            finally
            {
                if (ownsStream)
                {
                    memoryStream.Dispose();
                }
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    private static MemoryStream EnsureMemoryStream(Stream source, out bool ownsStream)
    {
        if (source is MemoryStream memoryStream)
        {
            ownsStream = false;
            memoryStream.Position = 0;
            return memoryStream;
        }

        var copy = new MemoryStream();
        source.CopyTo(copy);
        copy.Position = 0;
        ownsStream = true;
        return copy;
    }

    private static string ExtractDecodedStreams(MemoryStream pdfStream)
    {
        var data = pdfStream.ToArray();
        var content = new StringBuilder();

        foreach (var streamContent in EnumeratePdfStreams(data))
        {
            var decoded = TryDecodeStream(streamContent.Raw, streamContent.IsFlate) ?? string.Empty;
            if (decoded.Length == 0) continue;

            content.AppendLine(decoded);
            if (content.Length > MaxExtractedCharacters) break;
        }

        return content.ToString();
    }

    private static string ReadRawPdfContent(Stream pdfStream)
    {
        pdfStream.Position = 0;
        using var fallbackReader = new StreamReader(pdfStream, Encoding.Latin1, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return fallbackReader.ReadToEnd();
    }

    private static string ExtractTextFromContentStreams(string combined)
    {
        if (string.IsNullOrWhiteSpace(combined))
        {
            return string.Empty;
        }

        var results = new List<string>();
        int currentLength = 0;

        ProcessTjOperators(combined, results, ref currentLength);

        if (!HasExceededResultLimit(currentLength))
        {
            ProcessTjArrayOperators(combined, results, ref currentLength);
        }

        if (!HasExceededResultLimit(currentLength))
        {
            ProcessQuoteOperators(combined, results, ref currentLength);
        }

        if (results.Count == 0) return string.Empty;

        return NormalizeWhitespace(string.Join("\n", results));
    }

    private static void ProcessTjOperators(string combined, List<string> results, ref int currentLength)
    {
        var matches = Regex.Matches(combined, "\\((?<s>(?:\\\\.|[^\\\\\\)])*)\\)\\s*T[jJ]");
        foreach (Match match in matches)
        {
            var text = UnescapePdfString(match.Groups["s"].Value);
            if (!TryAddResult(results, text, ref currentLength))
            {
                break;
            }
        }
    }

    private static void ProcessTjArrayOperators(string combined, List<string> results, ref int currentLength)
    {
        var arrayMatches = Regex.Matches(combined, "\\[(?<arr>[^\\]]*)\\]\\s*TJ");
        foreach (Match match in arrayMatches)
        {
            var arr = match.Groups["arr"].Value;
            var sb = new StringBuilder();
            foreach (Match part in Regex.Matches(arr, "\\((?<s>(?:\\\\.|[^\\\\\\)])*)\\)"))
            {
                sb.Append(UnescapePdfString(part.Groups["s"].Value));
                if (sb.Length > MaxArraySegmentCharacters) break;
            }

            var text = sb.ToString();
            if (!TryAddResult(results, text, ref currentLength))
            {
                break;
            }
        }
    }

    private static void ProcessQuoteOperators(string combined, List<string> results, ref int currentLength)
    {
        ProcessQuotePattern(combined, results, ref currentLength, "\\((?<s>(?:\\\\.|[^\\\\\\)])*)\\)\\s*'");
        if (HasExceededResultLimit(currentLength))
        {
            return;
        }

        ProcessQuotePattern(combined, results, ref currentLength, "\\((?<s>(?:\\\\.|[^\\\\\\)])*)\\)\\s*\"");
    }

    private static void ProcessQuotePattern(string combined, List<string> results, ref int currentLength, string pattern)
    {
        var matches = Regex.Matches(combined, pattern);
        foreach (Match match in matches)
        {
            var text = UnescapePdfString(match.Groups["s"].Value);
            if (!TryAddResult(results, text, ref currentLength))
            {
                break;
            }
        }
    }

    private static bool TryAddResult(List<string> results, string text, ref int currentLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return !HasExceededResultLimit(currentLength);
        }

        results.Add(text);
        currentLength += text.Length;
        return !HasExceededResultLimit(currentLength);
    }

    private static bool HasExceededResultLimit(int currentLength) => currentLength > MaxResultCharacters;

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
        // Escape sequences can span multiple characters, so we manually move the index.
        var sb = new StringBuilder();
        int index = 0;
        while (index < s.Length)
        {
            if (s[index] != '\\' || index + 1 >= s.Length)
            {
                sb.Append(s[index]);
                index++;
                continue;
            }

            if (TryAppendEscapedCharacter(s, index, sb, out var consumed))
            {
                index += consumed;
            }
            else
            {
                sb.Append(s[index + 1]);
                index += 2;
            }
        }

        return sb.ToString();
    }

    private static bool TryAppendEscapedCharacter(string source, int escapeStartIndex, StringBuilder target, out int consumed)
    {
        var nextChar = source[escapeStartIndex + 1];
        switch (nextChar)
        {
            case '\\':
            case '(':
            case ')':
                target.Append(nextChar);
                consumed = 2;
                return true;
            case 'n':
                target.Append('\n');
                consumed = 2;
                return true;
            case 'r':
                target.Append('\r');
                consumed = 2;
                return true;
            case 't':
                target.Append('\t');
                consumed = 2;
                return true;
            default:
                if (char.IsDigit(nextChar))
                {
                    int digits = 1;
                    while (escapeStartIndex + 1 + digits < source.Length && digits < 3 && char.IsDigit(source[escapeStartIndex + 1 + digits]))
                    {
                        digits++;
                    }

                    var octalSegment = source.Substring(escapeStartIndex + 1, digits);
                    if (TryParseOctalValue(octalSegment, out var value))
                    {
                        target.Append(value);
                        consumed = 1 + digits;
                        return true;
                    }
                }

                consumed = 0;
                return false;
        }
    }

    private static bool TryParseOctalValue(string octalSegment, out char value)
    {
        value = default;
        if (string.IsNullOrEmpty(octalSegment))
        {
            return false;
        }

        int total = 0;
        foreach (var ch in octalSegment)
        {
            if (ch < '0' || ch > '7')
            {
                return false;
            }

            total = (total << 3) + (ch - '0');
        }

        value = (char)total;
        return true;
    }

    private static bool TryFillBuffer(Stream stream, Span<byte> buffer)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = stream.Read(buffer.Slice(totalRead));
            if (read == 0)
            {
                return false;
            }
            totalRead += read;
        }

        return true;
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
