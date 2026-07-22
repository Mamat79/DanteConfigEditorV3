using System.Globalization;
using System.Text;
using DanteConfigEditor.Models;

namespace DanteConfigEditor.Services;

public static class ChannelLabelTransformService
{
    private static readonly HashSet<char> AllowedConsoleCharacters = new(
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 !\"#%&'()*+,-./<=>?@[\\]_{|}~");

    public static string Transform(string label, ChannelLabelTransformOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        string transformed = (label ?? string.Empty).Trim();
        if (options.AsciiOnly)
        {
            transformed = ToConsoleAscii(transformed);
        }

        transformed = options.CaseMode switch
        {
            ChannelLabelCaseMode.Lowercase => transformed.ToLowerInvariant(),
            ChannelLabelCaseMode.Uppercase => transformed.ToUpperInvariant(),
            ChannelLabelCaseMode.FirstLetterUppercase => SentenceCase(transformed),
            _ => transformed
        };

        int startOffset = Math.Max(0, options.StartPosition - 1);
        int maximumLength = Math.Max(0, options.MaximumLength);
        if (transformed.Length == 0 || startOffset >= transformed.Length)
        {
            return string.Empty;
        }

        if (options.FromEnd)
        {
            int endExclusive = transformed.Length - startOffset;
            if (endExclusive <= 0)
            {
                return string.Empty;
            }
            int start = maximumLength == 0 ? 0 : Math.Max(0, endExclusive - maximumLength);
            return transformed[start..endExclusive];
        }

        string remaining = transformed[startOffset..];
        return maximumLength > 0 && remaining.Length > maximumLength
            ? remaining[..maximumLength]
            : remaining;
    }

    public static ChannelLabelTransformResult Transform(ChannelLabelSet labels, ChannelLabelTransformOptions options)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ChannelLabelEntry[] transformed = labels.Channels
            .Select(channel => channel with { Label = Transform(channel.Label, options) })
            .ToArray();
        ChannelLabelCollision[] collisions = transformed
            .Where(channel => !string.IsNullOrWhiteSpace(channel.Label))
            .GroupBy(channel => channel.Label, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => new ChannelLabelCollision(group.Key, group.Select(channel => channel.ChannelNumber).ToArray()))
            .ToArray();
        return new ChannelLabelTransformResult(
            new ChannelLabelSet(labels.DeviceName, labels.Direction, transformed),
            collisions);
    }

    public static ChannelLabelDocument Transform(ChannelLabelDocument document, ChannelLabelTransformOptions options)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new ChannelLabelDocument(
            document.Format,
            document.SchemaVersion,
            document.SourceApplication,
            document.SourceVersion,
            document.Sets.Select(set => Transform(set, options).Labels).ToArray());
    }

    private static string SentenceCase(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }
        string lower = value.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }

    private static string ToConsoleAscii(string value)
    {
        string expanded = value
            .Replace("œ", "oe", StringComparison.Ordinal)
            .Replace("Œ", "OE", StringComparison.Ordinal)
            .Replace("æ", "ae", StringComparison.Ordinal)
            .Replace("Æ", "AE", StringComparison.Ordinal)
            .Replace("ß", "ss", StringComparison.Ordinal)
            .Replace("ø", "o", StringComparison.Ordinal)
            .Replace("Ø", "O", StringComparison.Ordinal)
            .Replace("ł", "l", StringComparison.Ordinal)
            .Replace("Ł", "L", StringComparison.Ordinal);
        string decomposed = expanded.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new();
        foreach (Rune rune in decomposed.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            builder.Append(rune.IsAscii && AllowedConsoleCharacters.Contains((char)rune.Value)
                ? (char)rune.Value
                : '_');
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
