using System.Globalization;

namespace DanteConfigEditor.Services;

public static class DanteLatencyFormatter
{
    public static string FormatLatencyDisplay(string xmlValue)
    {
        if (!int.TryParse(xmlValue?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            return string.IsNullOrWhiteSpace(xmlValue) ? string.Empty : xmlValue;
        }

        return value switch
        {
            250 => "0,25 ms",
            1000 => "1 ms",
            2000 => "2 ms",
            5000 => "5 ms",
            _ => $"{(value / 1000.0).ToString("0.###", CultureInfo.GetCultureInfo("fr-FR"))} ms (valeur XML : {value})"
        };
    }

    public static string FormatLatencyWithXmlValue(string xmlValue)
    {
        if (string.IsNullOrWhiteSpace(xmlValue))
        {
            return "-";
        }

        return $"{FormatLatencyDisplay(xmlValue)} (XML : {xmlValue})";
    }
}
