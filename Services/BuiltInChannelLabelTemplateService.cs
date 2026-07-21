using System.IO;
using System.Reflection;
using DanteConfigEditor.Models;

namespace DanteConfigEditor.Services;

public enum ChannelLabelTemplateKind
{
    DmtWorkbook,
    DmtOpenDocument,
    ConsoleFile
}

public sealed record BuiltInChannelLabelTemplateInfo(
    string Format,
    string DisplayName,
    string OutputExtension,
    string FileSuffix,
    string ResourceFileName,
    ChannelLabelTemplateKind Kind);

public static class BuiltInChannelLabelTemplateService
{
    private const string ResourcePrefix = "DanteConfigEditor.ChannelLabelTemplates.";

    private static readonly IReadOnlyDictionary<string, BuiltInChannelLabelTemplateInfo> Templates =
        new[]
        {
            new BuiltInChannelLabelTemplateInfo("dmt-dlive", "DMT dLive", ".xlsx", "DMT-dLive", "DmtDLive.xlsx", ChannelLabelTemplateKind.DmtWorkbook),
            new BuiltInChannelLabelTemplateInfo("dmt-avantis", "DMT Avantis", ".xlsx", "DMT-Avantis", "DmtAvantis.xlsx", ChannelLabelTemplateKind.DmtWorkbook),
            new BuiltInChannelLabelTemplateInfo("dmt-ods-dlive", "DMT ODS dLive", ".ods", "DMT-ODS-dLive", "DmtDLive.ods", ChannelLabelTemplateKind.DmtOpenDocument),
            new BuiltInChannelLabelTemplateInfo("dmt-ods-avantis", "DMT ODS Avantis", ".ods", "DMT-ODS-Avantis", "DmtAvantis.ods", ChannelLabelTemplateKind.DmtOpenDocument),
            new BuiltInChannelLabelTemplateInfo("ah-dlive", "Allen & Heath dLive", ".csv", "dLive", "AllenHeathDLive.csv", ChannelLabelTemplateKind.ConsoleFile),
            new BuiltInChannelLabelTemplateInfo("ah-avantis", "Allen & Heath Avantis", ".csv", "Avantis", "AllenHeathAvantis.csv", ChannelLabelTemplateKind.ConsoleFile),
            new BuiltInChannelLabelTemplateInfo("yamaha-cl", "Yamaha CL", ".zip", "Yamaha-CL", "YamahaCL.zip", ChannelLabelTemplateKind.ConsoleFile),
            new BuiltInChannelLabelTemplateInfo("yamaha-ql", "Yamaha QL", ".zip", "Yamaha-QL", "YamahaQL.zip", ChannelLabelTemplateKind.ConsoleFile)
        }.ToDictionary(template => template.Format, StringComparer.OrdinalIgnoreCase);

    public static bool IsBuiltInFormat(string format) => Templates.ContainsKey(format ?? string.Empty);

    public static BuiltInChannelLabelTemplateInfo Get(string format)
    {
        return Templates.TryGetValue(format ?? string.Empty, out BuiltInChannelLabelTemplateInfo? template)
            ? template
            : throw new InvalidOperationException($"Modèle de labels interne inconnu : '{format}'.");
    }

    public static void Write(string format, string outputPath, ChannelLabelSet labels, bool adaptLabels)
    {
        BuiltInChannelLabelTemplateInfo template = Get(format);
        using Stream source = OpenTemplate(template);
        if (template.Kind == ChannelLabelTemplateKind.DmtWorkbook)
        {
            // Les lignes DMT absentes du projet sont désactivées afin de ne pas exporter
            // les noms de démonstration présents dans le classeur officiel.
            DmtChannelWorkbookService.WriteFromTemplate(source, outputPath, labels, adaptLabels, replaceChannelSet: true);
            return;
        }
        if (template.Kind == ChannelLabelTemplateKind.DmtOpenDocument)
        {
            DmtOpenDocumentService.WriteFromTemplate(source, outputPath, labels, adaptLabels, replaceChannelSet: true);
            return;
        }

        ConsoleChannelFileService.WriteFromTemplate(source, outputPath, labels, adaptLabels);
    }

    public static Stream OpenTemplate(string format) => OpenTemplate(Get(format));

    private static Stream OpenTemplate(BuiltInChannelLabelTemplateInfo template)
    {
        Assembly assembly = typeof(BuiltInChannelLabelTemplateService).Assembly;
        return assembly.GetManifestResourceStream(ResourcePrefix + template.ResourceFileName)
               ?? throw new InvalidDataException($"Le modèle interne '{template.DisplayName}' est absent de l'application.");
    }
}
