using System.Globalization;
using System.Text;

namespace SharedInfrastructure.Cities;

public static class SlugHelper
{
    public static string ToSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(value.Length);
        var lastAppendedHyphen = false;

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                lastAppendedHyphen = false;
            }
            else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_')
            {
                if (!lastAppendedHyphen && builder.Length > 0)
                {
                    builder.Append('-');
                    lastAppendedHyphen = true;
                }
            }
        }

        if (builder.Length == 0)
        {
            return string.Empty;
        }

        if (lastAppendedHyphen)
        {
            builder.Length -= 1;
        }

        return builder.ToString();
    }
}
