using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using DanteConfigEditor.Models;

namespace DanteConfigEditor.Services;

public static class ConsoleChannelFileService
{
    public static bool IsConsoleCsv(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        SourceText source = ReadSource(path);
        return ContainsSection(source.Text, "[Channels]") || ContainsSection(source.Text, "[InName]");
    }

    public static ConsoleChannelFileReadResult Read(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".csv" => ReadCsv(path),
            ".zip" => ReadYamahaZip(path),
            _ => throw new InvalidDataException("Modèle console non pris en charge. Utilisez un CSV A&H/Yamaha ou un ZIP Yamaha CL/QL.")
        };
    }

    public static void WriteCopy(string templatePath, string outputPath, ChannelLabelSet labels, bool adaptLabels)
    {
        ArgumentNullException.ThrowIfNull(labels);
        string sourceFullPath = Path.GetFullPath(templatePath);
        string outputFullPath = Path.GetFullPath(outputPath);
        if (string.Equals(sourceFullPath, outputFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Le fichier de sortie doit être différent du modèle console original.");
        }

        Dictionary<int, string> names = PrepareNames(labels, adaptLabels);
        Directory.CreateDirectory(Path.GetDirectoryName(outputFullPath) ?? Environment.CurrentDirectory);
        File.Copy(sourceFullPath, outputFullPath, true);
        try
        {
            string extension = Path.GetExtension(sourceFullPath).ToLowerInvariant();
            if (extension == ".zip")
            {
                UpdateYamahaZip(outputFullPath, names);
            }
            else if (extension == ".csv")
            {
                SourceText source = ReadSource(sourceFullPath);
                string updated = ContainsSection(source.Text, "[Channels]")
                    ? UpdateAllenHeathCsv(source.Text, names)
                    : ContainsSection(source.Text, "[InName]")
                        ? UpdateYamahaInputCsv(source.Text, names)
                        : throw new InvalidDataException("Le CSV choisi n'est ni un fichier A&H [Channels], ni un fichier Yamaha [InName].");
                WriteSource(outputFullPath, updated, source);
            }
            else
            {
                throw new InvalidDataException("Le modèle console doit être un CSV ou un ZIP.");
            }
        }
        catch
        {
            File.Delete(outputFullPath);
            throw;
        }
    }

    private static ConsoleChannelFileReadResult ReadCsv(string path)
    {
        SourceText source = ReadSource(path);
        if (ContainsSection(source.Text, "[Channels]"))
        {
            return ReadAllenHeathCsv(path, source.Text);
        }
        if (ContainsSection(source.Text, "[InName]"))
        {
            return ReadYamahaInputCsv(path, source.Text, fromZip: false);
        }
        throw new InvalidDataException("Le CSV ne contient pas de section [Channels] A&H ni [InName] Yamaha.");
    }

    private static ConsoleChannelFileReadResult ReadAllenHeathCsv(string path, string text)
    {
        string[][] rows = ReadRows(text);
        List<ChannelLabelEntry> channels = rows
            .Where(row => row.Length >= 3 && string.Equals(row[0].Trim(), "Input", StringComparison.OrdinalIgnoreCase))
            .Select(row => new ChannelLabelEntry(ParseChannel(row[1]), row[2].Trim(), null))
            .Where(channel => !string.IsNullOrWhiteSpace(channel.Label))
            .ToList();
        if (channels.Count == 0)
        {
            throw new InvalidDataException("Le CSV A&H ne contient aucun nom de canal Input.");
        }

        bool isDLive = rows.Any(row => row.Any(value => string.Equals(value.Trim(), "MixRack", StringComparison.OrdinalIgnoreCase)));
        bool isAvantis = rows.Any(row => row.Any(value => string.Equals(value.Trim(), "SLink", StringComparison.OrdinalIgnoreCase)));
        string model = isDLive ? "dLive" : isAvantis ? "Avantis" : "Allen & Heath";
        string version = rows.FirstOrDefault(row => row.Length > 1 && string.Equals(row[0].Trim(), "[Version]", StringComparison.OrdinalIgnoreCase))?[1].Trim() ?? string.Empty;
        string sourceVersion = string.IsNullOrWhiteSpace(version) ? model : $"{model} {version}";
        ChannelLabelDocument document = ChannelLabelExchangeService.BuildConsoleDocument(
            Path.GetFileNameWithoutExtension(path),
            channels,
            $"Allen & Heath {model}",
            sourceVersion);
        return new ConsoleChannelFileReadResult($"Allen & Heath {model}", sourceVersion, document);
    }

    private static ConsoleChannelFileReadResult ReadYamahaInputCsv(string path, string text, bool fromZip)
    {
        string[][] rows = ReadRows(text);
        int information = Array.FindIndex(rows, row => FirstValue(row).Equals("[Information]", StringComparison.OrdinalIgnoreCase));
        string model = information >= 0 && information + 1 < rows.Length ? FirstValue(rows[information + 1]) : "Yamaha CL/QL";
        string version = information >= 0 && information + 2 < rows.Length ? FirstValue(rows[information + 2]) : string.Empty;
        int section = Array.FindIndex(rows, row => FirstValue(row).Equals("[InName]", StringComparison.OrdinalIgnoreCase));
        if (section < 0)
        {
            throw new InvalidDataException("Le fichier Yamaha ne contient pas la section [InName].");
        }

        List<ChannelLabelEntry> channels = [];
        foreach (string[] row in rows.Skip(section + 1))
        {
            string first = FirstValue(row);
            if (first.StartsWith("[", StringComparison.Ordinal))
            {
                break;
            }
            if (row.Length >= 2 && TryParseYamahaChannel(first, out int channel) && !string.IsNullOrWhiteSpace(row[1]))
            {
                channels.Add(new ChannelLabelEntry(channel, row[1].Trim(), null));
            }
        }
        if (channels.Count == 0)
        {
            throw new InvalidDataException("Le fichier Yamaha [InName] ne contient aucun nom de canal.");
        }

        string sourceVersion = string.Join(" ", new[] { model, version, fromZip ? "ZIP" : "CSV" }.Where(value => !string.IsNullOrWhiteSpace(value)));
        ChannelLabelDocument document = ChannelLabelExchangeService.BuildConsoleDocument(
            Path.GetFileNameWithoutExtension(path),
            channels,
            $"Yamaha {model}",
            sourceVersion);
        return new ConsoleChannelFileReadResult($"Yamaha {model}", sourceVersion, document);
    }

    private static ConsoleChannelFileReadResult ReadYamahaZip(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);
        ZipArchiveEntry entry = FindYamahaInputEntry(archive);
        SourceText source = ReadSource(entry);
        return ReadYamahaInputCsv(path, source.Text, fromZip: true);
    }

    private static Dictionary<int, string> PrepareNames(ChannelLabelSet labels, bool adaptLabels)
    {
        Dictionary<int, string> names = [];
        foreach (ChannelLabelEntry channel in labels.Channels)
        {
            if (channel.ChannelNumber <= 0)
            {
                throw new InvalidOperationException("Un numéro de canal console est invalide.");
            }

            string label = channel.Label.Trim();
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            DmtLabelCompatibility compatibility = DmtChannelWorkbookService.CheckCompatibility(label);
            if (!compatibility.IsCompatible && !adaptLabels)
            {
                throw new InvalidOperationException(
                    $"Le label '{label}' n'est pas compatible avec le format console : {string.Join("; ", compatibility.Warnings)}. Activez l'adaptation ASCII/8 caractères.");
            }
            if (!names.TryAdd(channel.ChannelNumber, adaptLabels ? compatibility.AdaptedLabel : label))
            {
                throw new InvalidOperationException($"Le canal console {channel.ChannelNumber} est présent plusieurs fois.");
            }
        }
        return names.Count > 0 ? names : throw new InvalidOperationException("Aucun label non vide à exporter vers la console.");
    }

    private static string UpdateAllenHeathCsv(string text, IReadOnlyDictionary<int, string> names)
    {
        return UpdateLines(text, (row, activeSection) =>
        {
            if (activeSection && row.Length >= 3 && string.Equals(row[0].Trim(), "Input", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(row[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int channel)
                && names.TryGetValue(channel, out string? label))
            {
                row[2] = label;
                return channel;
            }
            return null;
        }, "[Channels]", names);
    }

    private static string UpdateYamahaInputCsv(string text, IReadOnlyDictionary<int, string> names)
    {
        return UpdateLines(text, (row, activeSection) =>
        {
            if (activeSection && row.Length >= 2 && TryParseYamahaChannel(FirstValue(row), out int channel)
                && names.TryGetValue(channel, out string? label))
            {
                row[1] = label;
                return channel;
            }
            return null;
        }, "[InName]", names);
    }

    private static string UpdateLines(
        string text,
        Func<string[], bool, int?> update,
        string sectionName,
        IReadOnlyDictionary<int, string> names)
    {
        string newline = DetectNewLine(text);
        bool endsWithNewline = text.EndsWith("\r\n", StringComparison.Ordinal) || text.EndsWith('\n') || text.EndsWith('\r');
        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        List<string> output = new(lines.Length);
        HashSet<int> written = [];
        bool active = false;
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            if (line.Length == 0 && lineIndex == lines.Length - 1)
            {
                continue;
            }
            string[] row = ParseCsvLine(line);
            string first = FirstValue(row);
            if (first.StartsWith("[", StringComparison.Ordinal))
            {
                active = string.Equals(first, sectionName, StringComparison.OrdinalIgnoreCase);
            }
            int? changed = update(row, active);
            if (changed.HasValue)
            {
                written.Add(changed.Value);
                output.Add(SerializeCsvRow(row));
            }
            else
            {
                output.Add(line);
            }
        }

        int[] missing = names.Keys.Where(channel => !written.Contains(channel)).OrderBy(channel => channel).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"Le modèle console ne contient pas les canaux suivants : {string.Join(", ", missing)}.");
        }
        string result = string.Join(newline, output);
        return endsWithNewline ? result + newline : result;
    }

    private static void UpdateYamahaZip(string path, IReadOnlyDictionary<int, string> names)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using ZipArchive archive = new(stream, ZipArchiveMode.Update);
        ZipArchiveEntry entry = FindYamahaInputEntry(archive);
        SourceText source = ReadSource(entry);
        string updated = UpdateYamahaInputCsv(source.Text, names);
        using Stream entryStream = entry.Open();
        entryStream.SetLength(0);
        WriteSource(entryStream, updated, source);
    }

    private static ZipArchiveEntry FindYamahaInputEntry(ZipArchive archive)
    {
        return archive.Entries.FirstOrDefault(entry =>
                   string.Equals(Path.GetFileName(entry.FullName), "InName.csv", StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidDataException("Le ZIP Yamaha ne contient pas InName.csv.");
    }

    private static SourceText ReadSource(string path)
    {
        return ReadSource(File.ReadAllBytes(path));
    }

    private static SourceText ReadSource(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        using MemoryStream memory = new();
        stream.CopyTo(memory);
        return ReadSource(memory.ToArray());
    }

    private static SourceText ReadSource(byte[] bytes)
    {
        Encoding encoding;
        int preambleLength;
        if (bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()))
        {
            encoding = new UTF8Encoding(true);
            preambleLength = Encoding.UTF8.GetPreamble().Length;
        }
        else if (bytes.AsSpan().StartsWith(Encoding.Unicode.GetPreamble()))
        {
            encoding = Encoding.Unicode;
            preambleLength = Encoding.Unicode.GetPreamble().Length;
        }
        else
        {
            // Les exports A&H et Yamaha fournis sont ANSI. Latin-1 préserve chaque octet non modifié.
            encoding = Encoding.Latin1;
            preambleLength = 0;
        }
        return new SourceText(encoding.GetString(bytes, preambleLength, bytes.Length - preambleLength), encoding, preambleLength > 0);
    }

    private static void WriteSource(string path, string text, SourceText source)
    {
        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteSource(stream, text, source);
    }

    private static void WriteSource(Stream stream, string text, SourceText source)
    {
        if (source.HasPreamble)
        {
            byte[] preamble = source.Encoding.GetPreamble();
            stream.Write(preamble);
        }
        byte[] content = source.Encoding.GetBytes(text);
        stream.Write(content);
    }

    private static bool ContainsSection(string text, string section)
    {
        return text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Any(line => string.Equals(FirstValue(ParseCsvLine(line)), section, StringComparison.OrdinalIgnoreCase));
    }

    private static string[][] ReadRows(string text)
    {
        return text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(ParseCsvLine).ToArray();
    }

    private static string[] ParseCsvLine(string line)
    {
        List<string> values = [];
        StringBuilder field = new();
        bool quoted = false;
        for (int index = 0; index < line.Length; index++)
        {
            char current = line[index];
            if (quoted)
            {
                if (current == '"' && index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (current == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(current);
                }
            }
            else if (current == '"')
            {
                quoted = true;
            }
            else if (current == ',')
            {
                values.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(current);
            }
        }
        if (quoted)
        {
            throw new InvalidDataException("Le CSV console contient un champ entre guillemets non terminé.");
        }
        values.Add(field.ToString());
        return values.ToArray();
    }

    private static string SerializeCsvRow(IEnumerable<string> values)
    {
        return string.Join(",", values.Select(value => value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value));
    }

    private static int ParseChannel(string value)
    {
        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int channel) || channel <= 0)
        {
            throw new InvalidDataException($"Numéro de canal console invalide : '{value}'.");
        }
        return channel;
    }

    private static bool TryParseYamahaChannel(string value, out int channel)
    {
        return int.TryParse(value.Trim().TrimStart('_'), NumberStyles.Integer, CultureInfo.InvariantCulture, out channel) && channel > 0;
    }

    private static string FirstValue(IReadOnlyList<string> row) => row.Count == 0 ? string.Empty : row[0].Trim().TrimStart('\uFEFF');

    private static string DetectNewLine(string text) => text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : text.Contains('\r') ? "\r" : "\n";

    private sealed record SourceText(string Text, Encoding Encoding, bool HasPreamble);
}

public sealed record ConsoleChannelFileReadResult(
    string TemplateName,
    string TemplateVersion,
    ChannelLabelDocument Document);
