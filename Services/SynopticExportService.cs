using System.Globalization;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DanteConfigEditor.Models;

namespace DanteConfigEditor.Services;

public static class SynopticExportService
{
    private const double LocationWidth = 300;
    private const double LocationGap = 280;
    private const double NodeWidth = 240;
    private const double NodeHeight = 82;
    private const double NodeGap = 34;
    private const double BaseTopMargin = 88;
    private const double OverheadLaneSpacing = 10;
    private const double LegendSingleWidth = 420;
    private const double LegendDoubleWidth = 760;

    private static readonly string[] Palette =
    [
        "#1677D2", "#0F8B6D", "#C2415A", "#7A5AF8",
        "#B76E00", "#25748A", "#9A4D8C", "#4E7A31"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static SynopticLayoutDocument LoadOrCreate(DanteProject project, string? storageDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        string path = ResolveLayoutPath(project, storageDirectory);
        SynopticLayoutDocument layout = new();

        if (File.Exists(path))
        {
            try
            {
                layout = JsonSerializer.Deserialize<SynopticLayoutDocument>(File.ReadAllText(path, Encoding.UTF8), JsonOptions)
                    ?? new SynopticLayoutDocument();
            }
            catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
            {
                layout = new SynopticLayoutDocument();
            }
        }

        return Synchronize(project, layout);
    }

    public static SynopticLayoutDocument Synchronize(DanteProject project, SynopticLayoutDocument? layout)
    {
        ArgumentNullException.ThrowIfNull(project);
        layout ??= new SynopticLayoutDocument();

        Dictionary<string, SynopticDevicePlacement> existing = layout.Devices
            .Where(item => !string.IsNullOrWhiteSpace(item.DeviceIdentity))
            .GroupBy(item => item.DeviceIdentity, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        List<SynopticDevicePlacement> synchronized = [];
        for (int index = 0; index < project.Devices.Count; index++)
        {
            DanteDevice device = project.Devices[index];
            if (existing.TryGetValue(device.StableIdentity, out SynopticDevicePlacement? placement))
            {
                placement.DeviceName = device.Name;
                placement.Order = placement.Order < 0 ? index : placement.Order;
                synchronized.Add(placement);
            }
            else
            {
                synchronized.Add(new SynopticDevicePlacement
                {
                    DeviceIdentity = device.StableIdentity,
                    DeviceName = device.Name,
                    IsVisible = true,
                    Order = index
                });
            }
        }

        layout.SchemaVersion = SynopticLayoutDocument.CurrentSchemaVersion;
        layout.Devices = synchronized;
        return layout;
    }

    public static string SaveLayout(DanteProject project, SynopticLayoutDocument layout, string? storageDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(layout);
        layout = Synchronize(project, layout);
        string path = ResolveLayoutPath(project, storageDirectory);
        string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Dossier de synoptique introuvable.");
        Directory.CreateDirectory(directory);

        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(layout, JsonOptions) + Environment.NewLine, new UTF8Encoding(false));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return path;
    }

    public static SynopticDiagram BuildDiagram(DanteProject project, SynopticLayoutDocument layout, bool english = false)
    {
        ArgumentNullException.ThrowIfNull(project);
        layout = Synchronize(project, layout);

        Dictionary<string, DanteDevice> devicesByName = project.Devices
            .Where(device => !string.IsNullOrWhiteSpace(device.Name))
            .GroupBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        SynopticDevicePlacement[] visiblePlacements = layout.Devices
            .Where(placement => placement.IsVisible && devicesByName.ContainsKey(placement.DeviceName))
            .OrderBy(placement => placement.Order)
            .ThenBy(placement => placement.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string unspecified = english ? "Unspecified location" : "Emplacement non renseigné";
        IGrouping<string, SynopticDevicePlacement>[] locationGroups = visiblePlacements
            .GroupBy(placement => string.IsNullOrWhiteSpace(placement.Location) ? unspecified : placement.Location.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key == unspecified ? 1 : 0)
            .ThenBy(group => group.Min(item => item.Order))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Dictionary<string, int> locationIndexByDevice = [];
        for (int locationIndex = 0; locationIndex < locationGroups.Length; locationIndex++)
        {
            foreach (SynopticDevicePlacement placement in locationGroups[locationIndex])
            {
                locationIndexByDevice[placement.DeviceName] = locationIndex;
            }
        }

        DanteSubscription[] activeSubscriptions = project.PatchMatrix.Subscriptions
            .Where(subscription => subscription.IsActive)
            .ToArray();
        IGrouping<(string Source, string Target), DanteSubscription>[] subscriptionGroups = activeSubscriptions
            .Where(subscription => locationIndexByDevice.ContainsKey(subscription.ResolvedTxDeviceName)
                && locationIndexByDevice.ContainsKey(subscription.RxDevice))
            .GroupBy(subscription => (Source: subscription.ResolvedTxDeviceName, Target: subscription.RxDevice), DevicePairComparer.Instance)
            .OrderBy(group => group.Key.Source, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.Target, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] overheadBundleKeys = subscriptionGroups
            .Where(group => Math.Abs(locationIndexByDevice[group.Key.Source] - locationIndexByDevice[group.Key.Target]) > 1)
            .Select(group => BundleKey(
                locationIndexByDevice[group.Key.Source],
                locationIndexByDevice[group.Key.Target],
                group.Key.Source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        double topMargin = BaseTopMargin + (overheadBundleKeys.Length == 0 ? 0 : 28 + overheadBundleKeys.Length * OverheadLaneSpacing);

        List<SynopticLocationArea> locations = [];
        List<SynopticDeviceNode> nodes = [];
        for (int locationIndex = 0; locationIndex < locationGroups.Length; locationIndex++)
        {
            IGrouping<string, SynopticDevicePlacement> group = locationGroups[locationIndex];
            SynopticDevicePlacement[] placements = group.OrderBy(item => item.Order).ThenBy(item => item.DeviceName, StringComparer.OrdinalIgnoreCase).ToArray();
            double x = 34 + locationIndex * (LocationWidth + LocationGap);
            double locationHeight = Math.Max(190, 70 + placements.Length * (NodeHeight + NodeGap));
            string color = Palette[locationIndex % Palette.Length];
            locations.Add(new SynopticLocationArea(group.Key, color, x, topMargin, LocationWidth, locationHeight));

            for (int deviceIndex = 0; deviceIndex < placements.Length; deviceIndex++)
            {
                SynopticDevicePlacement placement = placements[deviceIndex];
                DanteDevice device = devicesByName[placement.DeviceName];
                nodes.Add(new SynopticDeviceNode(
                    placement.DeviceIdentity,
                    device.Name,
                    device.FriendlyName,
                    group.Key,
                    color,
                    device.TxCount,
                    device.RxCount,
                    x + (LocationWidth - NodeWidth) / 2,
                    topMargin + 52 + deviceIndex * (NodeHeight + NodeGap),
                    NodeWidth,
                    NodeHeight));
            }
        }

        Dictionary<string, SynopticDeviceNode> nodesByName = nodes
            .GroupBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        int skippedPatchCount = activeSubscriptions.Count(subscription =>
            !nodesByName.ContainsKey(subscription.ResolvedTxDeviceName)
            || !nodesByName.ContainsKey(subscription.RxDevice));

        CableDraft[] drafts = subscriptionGroups
            .Select(group => new CableDraft(
                nodesByName[group.Key.Source],
                nodesByName[group.Key.Target],
                locationIndexByDevice[group.Key.Source],
                locationIndexByDevice[group.Key.Target],
                BuildCableLabels(devicesByName[group.Key.Source], group, english)))
            .ToArray();

        Dictionary<int, string[]> corridorBundles = drafts
            .Where(draft => Math.Abs(draft.SourceLocationIndex - draft.TargetLocationIndex) <= 1)
            .GroupBy(CorridorIndex)
            .ToDictionary(
                group => group.Key,
                group => group.Select(BundleKey).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray());
        Dictionary<string, int> bundleColorIndices = drafts
            .Select(BundleKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .Select((key, index) => (key, index))
            .ToDictionary(item => item.key, item => item.index, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> overheadLaneIndices = overheadBundleKeys
            .Select((key, index) => (key, index))
            .ToDictionary(item => item.key, item => item.index, StringComparer.OrdinalIgnoreCase);

        List<SynopticCable> cables = [];
        for (int cableIndex = 0; cableIndex < drafts.Length; cableIndex++)
        {
            CableDraft draft = drafts[cableIndex];
            string bundleKey = BundleKey(draft);
            bool loopback = string.Equals(draft.Source.Name, draft.Target.Name, StringComparison.OrdinalIgnoreCase);
            double sourceOffset = PortOffset(drafts, draft, draft.Source.Name, source: true);
            double targetOffset = PortOffset(drafts, draft, draft.Target.Name, source: false);
            IReadOnlyList<SynopticRoutePoint> route;
            if (Math.Abs(draft.SourceLocationIndex - draft.TargetLocationIndex) <= 1)
            {
                int corridor = CorridorIndex(draft);
                string[] bundles = corridorBundles[corridor];
                int laneIndex = Array.FindIndex(bundles, key => string.Equals(key, bundleKey, StringComparison.OrdinalIgnoreCase));
                int laneCount = bundles.Length;
                double corridorLeft = locations[corridor].X + locations[corridor].Width;
                double laneX = corridorLeft + 24 + (laneIndex + 1) * (LocationGap - 48) / (laneCount + 1);
                route = BuildCorridorRoute(draft, laneX, sourceOffset, targetOffset, loopback);
            }
            else
            {
                int laneIndex = overheadLaneIndices[bundleKey];
                double laneY = BaseTopMargin + 18 + laneIndex * OverheadLaneSpacing;
                route = BuildOverheadRoute(draft, laneY, sourceOffset, targetOffset, laneIndex);
            }

            SynopticRoutePoint start = route[0];
            SynopticRoutePoint end = route[^1];
            SynopticRoutePoint label = PreferredLabelPoint(route);
            cables.Add(new SynopticCable(
                draft.Source.Name,
                draft.Target.Name,
                Palette[bundleColorIndices[bundleKey] % Palette.Length],
                draft.Labels,
                start.X,
                start.Y,
                end.X,
                end.Y,
                label.X,
                label.Y,
                route,
                loopback));
        }

        double locationsRight = locations.Count == 0 ? 34 : locations.Max(location => location.X + location.Width);
        double locationsBottom = locations.Count == 0 ? topMargin : locations.Max(location => location.Y + location.Height);
        int legendColumns = cables.Count > 18 ? 2 : 1;
        double legendWidth = legendColumns == 1 ? LegendSingleWidth : LegendDoubleWidth;
        double legendHeight = 52 + LegendRowsHeight(cables, legendColumns);
        double legendX = locationsRight + LocationGap;
        double width = Math.Max(900, legendX + legendWidth + 34);
        double height = Math.Max(560, Math.Max(locationsBottom, topMargin + legendHeight) + 76);
        string title = string.IsNullOrWhiteSpace(project.PresetName)
            ? (english ? "Dante project synoptic" : "Synoptique du projet Dante")
            : project.PresetName;

        return new SynopticDiagram(
            title,
            width,
            height,
            legendX,
            legendWidth,
            legendColumns,
            locations,
            nodes,
            cables,
            layout.Devices.Count(placement => !placement.IsVisible),
            skippedPatchCount);
    }

    public static void ExportSvg(string path, SynopticDiagram diagram, bool english = false)
    {
        ArgumentNullException.ThrowIfNull(diagram);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, BuildSvg(diagram, english), new UTF8Encoding(false));
    }

    public static string BuildSvg(SynopticDiagram diagram, bool english = false)
    {
        ArgumentNullException.ThrowIfNull(diagram);
        StringBuilder svg = new();
        string width = Number(diagram.Width);
        string height = Number(diagram.Height);
        svg.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        svg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\">");
        svg.AppendLine($"  <title>{Escape(diagram.Title)}</title>");
        svg.AppendLine("  <rect width=\"100%\" height=\"100%\" fill=\"#F7F9FC\"/>");
        svg.AppendLine($"  <text x=\"34\" y=\"38\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"24\" font-weight=\"700\" fill=\"#172033\">{Escape(diagram.Title)}</text>");
        svg.AppendLine($"  <text x=\"34\" y=\"62\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"12\" fill=\"#526070\">{Escape(english ? "Grouped Dante subscriptions - offline export" : "Abonnements Dante regroupés - export hors ligne")}</text>");

        for (int index = 0; index < diagram.Cables.Count; index++)
        {
            SynopticCable cable = diagram.Cables[index];
            svg.AppendLine($"  <defs><marker id=\"arrow-{index}\" markerWidth=\"10\" markerHeight=\"8\" refX=\"9\" refY=\"4\" orient=\"auto\"><path d=\"M0,0 L10,4 L0,8 z\" fill=\"{cable.Color}\"/></marker></defs>");
        }

        foreach (SynopticLocationArea location in diagram.Locations)
        {
            svg.AppendLine($"  <rect x=\"{Number(location.X)}\" y=\"{Number(location.Y)}\" width=\"{Number(location.Width)}\" height=\"{Number(location.Height)}\" rx=\"8\" fill=\"#FFFFFF\" stroke=\"{location.Color}\" stroke-width=\"2\"/>");
            svg.AppendLine($"  <rect x=\"{Number(location.X)}\" y=\"{Number(location.Y)}\" width=\"{Number(location.Width)}\" height=\"36\" rx=\"7\" fill=\"{location.Color}\"/>");
            svg.AppendLine($"  <text x=\"{Number(location.X + 14)}\" y=\"{Number(location.Y + 24)}\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"14\" font-weight=\"700\" fill=\"#FFFFFF\">{Escape(location.Name)}</text>");
        }

        for (int index = 0; index < diagram.Cables.Count; index++)
        {
            SynopticCable cable = diagram.Cables[index];
            string path = BuildCablePath(cable);
            svg.AppendLine($"  <path d=\"{path}\" fill=\"none\" stroke=\"#FFFFFF\" stroke-width=\"8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"0.96\"/>");
            svg.AppendLine($"  <path d=\"{path}\" fill=\"none\" stroke=\"{cable.Color}\" stroke-width=\"3.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"0.92\" marker-end=\"url(#arrow-{index})\"/>");
        }

        foreach (SynopticDeviceNode node in diagram.Devices)
        {
            svg.AppendLine($"  <rect x=\"{Number(node.X)}\" y=\"{Number(node.Y)}\" width=\"{Number(node.Width)}\" height=\"{Number(node.Height)}\" rx=\"6\" fill=\"#FFFFFF\" stroke=\"{node.Color}\" stroke-width=\"2\"/>");
            svg.AppendLine($"  <rect x=\"{Number(node.X)}\" y=\"{Number(node.Y)}\" width=\"8\" height=\"{Number(node.Height)}\" rx=\"4\" fill=\"{node.Color}\"/>");
            svg.AppendLine($"  <text x=\"{Number(node.X + 20)}\" y=\"{Number(node.Y + 29)}\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"16\" font-weight=\"700\" fill=\"#172033\">{Escape(node.Name)}</text>");
            if (!string.IsNullOrWhiteSpace(node.FriendlyName) && !string.Equals(node.FriendlyName, node.Name, StringComparison.OrdinalIgnoreCase))
            {
                svg.AppendLine($"  <text x=\"{Number(node.X + 20)}\" y=\"{Number(node.Y + 49)}\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"11\" fill=\"#526070\">{Escape(Trim(node.FriendlyName, 32))}</text>");
            }
            svg.AppendLine($"  <text x=\"{Number(node.X + 20)}\" y=\"{Number(node.Y + 68)}\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"11\" fill=\"#526070\">TX {node.TxCount}   RX {node.RxCount}</text>");
        }

        if (diagram.Cables.Count <= 18)
        {
            for (int index = 0; index < diagram.Cables.Count; index++)
            {
                SynopticCable cable = diagram.Cables[index];
                svg.AppendLine($"  <circle cx=\"{Number(cable.LabelX)}\" cy=\"{Number(cable.LabelY)}\" r=\"9\" fill=\"#FFFFFF\" stroke=\"{cable.Color}\" stroke-width=\"2\"/>");
                svg.AppendLine($"  <text x=\"{Number(cable.LabelX)}\" y=\"{Number(cable.LabelY + 3)}\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"8\" font-weight=\"700\" fill=\"#172033\">{index + 1}</text>");
            }
        }

        AppendLegend(svg, diagram, english);

        string summary = english
            ? $"{diagram.Devices.Count} devices shown - {diagram.Cables.Count} grouped cables - {diagram.HiddenDeviceCount} hidden - {diagram.SkippedPatchCount} subscriptions outside selection"
            : $"{diagram.Devices.Count} machines affichées - {diagram.Cables.Count} câbles regroupés - {diagram.HiddenDeviceCount} masquée(s) - {diagram.SkippedPatchCount} patch(s) hors sélection";
        svg.AppendLine($"  <text x=\"34\" y=\"{Number(diagram.Height - 30)}\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"11\" fill=\"#526070\">{Escape(summary)}</text>");
        svg.AppendLine($"  <text x=\"{Number(diagram.Width - 34)}\" y=\"{Number(diagram.Height - 30)}\" text-anchor=\"end\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"10\" fill=\"#718096\">Dante Config Editor V3.2 - By Mamat et ses agents</text>");
        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    public static string ResolveLayoutPath(DanteProject project, string? storageDirectory = null)
    {
        string directory = string.IsNullOrWhiteSpace(storageDirectory)
            ? ApplicationStoragePaths.Resolve("Synoptics")
            : Path.GetFullPath(storageDirectory);
        string fingerprintSource = string.Join("\n", project.Devices.Select(device => device.StableIdentity).OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(fingerprintSource))
        {
            fingerprintSource = Path.GetFullPath(project.OriginalFilePath).ToUpperInvariant();
        }

        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource))).ToLowerInvariant()[..20];
        return Path.Combine(directory, $"{hash}.synoptic.json");
    }

    private static IReadOnlyList<string> BuildCableLabels(DanteDevice sourceDevice, IEnumerable<DanteSubscription> subscriptions, bool english)
    {
        Dictionary<string, int> txIdsByName = sourceDevice.TxChannels
            .Where(channel => !string.IsNullOrWhiteSpace(channel.DisplayName))
            .GroupBy(channel => channel.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().DanteId, StringComparer.OrdinalIgnoreCase);

        CablePoint[] points = subscriptions
            .Select(subscription => new CablePoint(
                txIdsByName.TryGetValue(subscription.TxChannelName, out int txId) ? txId : null,
                subscription.TxChannelName,
                subscription.RxDanteId))
            .OrderBy(point => point.RxId)
            .ToArray();

        List<CableRange> ranges = [];
        foreach (CablePoint point in points)
        {
            CableRange? current = ranges.LastOrDefault();
            bool consecutive = current is not null
                && point.RxId == current.RxEnd + 1
                && point.TxId.HasValue
                && current.TxEnd.HasValue
                && point.TxId.Value == current.TxEnd.Value + 1;
            if (consecutive)
            {
                current!.RxEnd = point.RxId;
                current.TxEnd = point.TxId;
                current.Count++;
            }
            else
            {
                ranges.Add(new CableRange(point.TxId, point.TxId, point.TxName, point.RxId, point.RxId, 1));
            }
        }

        return ranges.Select(range => FormatRange(range, english)).ToArray();
    }

    private static string FormatRange(CableRange range, bool english)
    {
        string tx = range.TxStart.HasValue
            ? FormatNumberRange("TX", range.TxStart.Value, range.TxEnd ?? range.TxStart.Value)
            : $"TX {Trim(range.TxName, 18)}";
        string rx = FormatNumberRange("RX", range.RxStart, range.RxEnd);
        string count = english ? $"{range.Count} ch" : $"{range.Count} can.";
        return $"{tx} → {rx}  ({count})";
    }

    private static string FormatNumberRange(string prefix, int start, int end)
    {
        return start == end ? $"{prefix} {start}" : $"{prefix} {start}-{end}";
    }

    private static int CorridorIndex(CableDraft draft)
    {
        return draft.SourceLocationIndex == draft.TargetLocationIndex
            ? draft.SourceLocationIndex
            : Math.Min(draft.SourceLocationIndex, draft.TargetLocationIndex);
    }

    private static string BundleKey(CableDraft draft)
    {
        return BundleKey(draft.SourceLocationIndex, draft.TargetLocationIndex, draft.Source.Name);
    }

    private static string BundleKey(int sourceLocationIndex, int targetLocationIndex, string sourceDevice)
    {
        return $"{sourceLocationIndex}>{targetLocationIndex}:{sourceDevice}";
    }

    private static double PortOffset(CableDraft[] drafts, CableDraft current, string deviceName, bool source)
    {
        CableDraft[] related = drafts
            .Where(draft => string.Equals(
                source ? draft.Source.Name : draft.Target.Name,
                deviceName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (related.Length <= 1)
        {
            return 0;
        }

        int index = Array.IndexOf(related, current);
        double spread = Math.Min(44, (related.Length - 1) * 6);
        return -spread / 2 + index * spread / (related.Length - 1);
    }

    private static IReadOnlyList<SynopticRoutePoint> BuildCorridorRoute(
        CableDraft draft,
        double laneX,
        double sourceOffset,
        double targetOffset,
        bool loopback)
    {
        SynopticDeviceNode source = draft.Source;
        SynopticDeviceNode target = draft.Target;
        if (loopback)
        {
            double loopStartY = source.Y + source.Height / 2 - 14;
            double loopEndY = source.Y + source.Height / 2 + 14;
            return
            [
                new SynopticRoutePoint(source.X + source.Width, loopStartY),
                new SynopticRoutePoint(laneX, loopStartY),
                new SynopticRoutePoint(laneX, loopEndY),
                new SynopticRoutePoint(source.X + source.Width, loopEndY)
            ];
        }

        bool sameLocation = draft.SourceLocationIndex == draft.TargetLocationIndex;
        bool forward = draft.SourceLocationIndex < draft.TargetLocationIndex;
        double startX = sameLocation || forward ? source.X + source.Width : source.X;
        double endX = sameLocation || !forward ? target.X + target.Width : target.X;
        double startY = source.Y + source.Height / 2 + sourceOffset;
        double endY = target.Y + target.Height / 2 + targetOffset;
        return
        [
            new SynopticRoutePoint(startX, startY),
            new SynopticRoutePoint(laneX, startY),
            new SynopticRoutePoint(laneX, endY),
            new SynopticRoutePoint(endX, endY)
        ];
    }

    private static IReadOnlyList<SynopticRoutePoint> BuildOverheadRoute(
        CableDraft draft,
        double laneY,
        double sourceOffset,
        double targetOffset,
        int laneIndex)
    {
        bool forward = draft.SourceLocationIndex < draft.TargetLocationIndex;
        double direction = forward ? 1 : -1;
        double startX = forward ? draft.Source.X + draft.Source.Width : draft.Source.X;
        double endX = forward ? draft.Target.X : draft.Target.X + draft.Target.Width;
        double startY = draft.Source.Y + draft.Source.Height / 2 + sourceOffset;
        double endY = draft.Target.Y + draft.Target.Height / 2 + targetOffset;
        double sourceGutterX = startX + direction * (20 + (laneIndex % 4) * 4);
        double targetGutterX = endX - direction * (20 + ((laneIndex + 2) % 4) * 4);
        return
        [
            new SynopticRoutePoint(startX, startY),
            new SynopticRoutePoint(sourceGutterX, startY),
            new SynopticRoutePoint(sourceGutterX, laneY),
            new SynopticRoutePoint(targetGutterX, laneY),
            new SynopticRoutePoint(targetGutterX, endY),
            new SynopticRoutePoint(endX, endY)
        ];
    }

    private static SynopticRoutePoint PreferredLabelPoint(IReadOnlyList<SynopticRoutePoint> route)
    {
        if (route.Count == 4)
        {
            return Midpoint(route[2], route[3]);
        }
        if (route.Count >= 6)
        {
            return Midpoint(route[2], route[3]);
        }

        double longest = double.MinValue;
        SynopticRoutePoint midpoint = route[0];
        for (int index = 1; index < route.Count; index++)
        {
            SynopticRoutePoint first = route[index - 1];
            SynopticRoutePoint second = route[index];
            double length = Math.Abs(second.X - first.X) + Math.Abs(second.Y - first.Y);
            if (length > longest)
            {
                longest = length;
                midpoint = new SynopticRoutePoint((first.X + second.X) / 2, (first.Y + second.Y) / 2);
            }
        }
        return midpoint;
    }

    private static SynopticRoutePoint Midpoint(SynopticRoutePoint first, SynopticRoutePoint second)
    {
        return new SynopticRoutePoint((first.X + second.X) / 2, (first.Y + second.Y) / 2);
    }

    private static string BuildCablePath(SynopticCable cable)
    {
        if (cable.RoutePoints.Count == 0)
        {
            return $"M {Number(cable.StartX)} {Number(cable.StartY)} L {Number(cable.EndX)} {Number(cable.EndY)}";
        }

        return "M " + string.Join(" L ", cable.RoutePoints.Select(point => $"{Number(point.X)} {Number(point.Y)}"));
    }

    private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;

    private static void AppendLegend(StringBuilder svg, SynopticDiagram diagram, bool english)
    {
        double topMargin = diagram.Locations.Count == 0 ? BaseTopMargin : diagram.Locations.Min(location => location.Y);
        double legendX = diagram.LegendX;
        double legendHeight = diagram.Height - topMargin - 76;
        int columns = Math.Max(1, diagram.LegendColumns);
        double gap = 10;
        double itemWidth = (diagram.LegendWidth - 24 - (columns - 1) * gap) / columns;
        svg.AppendLine($"  <rect x=\"{Number(legendX)}\" y=\"{Number(topMargin)}\" width=\"{Number(diagram.LegendWidth)}\" height=\"{Number(legendHeight)}\" rx=\"8\" fill=\"#FFFFFF\" stroke=\"#CBD5E1\" stroke-width=\"1.5\"/>");
        svg.AppendLine($"  <text x=\"{Number(legendX + 18)}\" y=\"{Number(topMargin + 29)}\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"15\" font-weight=\"700\" fill=\"#172033\">{Escape(english ? "Grouped subscriptions" : "Liaisons regroupées")}</text>");

        double y = topMargin + 48;
        for (int rowStart = 0; rowStart < diagram.Cables.Count; rowStart += columns)
        {
            SynopticCable[] row = diagram.Cables.Skip(rowStart).Take(columns).ToArray();
            double rowHeight = row.Max(LegendItemHeight);
            for (int column = 0; column < row.Length; column++)
            {
                int index = rowStart + column;
                SynopticCable cable = row[column];
                double itemX = legendX + 12 + column * (itemWidth + gap);
                svg.AppendLine($"  <rect x=\"{Number(itemX)}\" y=\"{Number(y)}\" width=\"{Number(itemWidth)}\" height=\"{Number(rowHeight - 6)}\" rx=\"5\" fill=\"#F8FAFC\" stroke=\"#E2E8F0\"/>");
                svg.AppendLine($"  <circle cx=\"{Number(itemX + 19)}\" cy=\"{Number(y + 20)}\" r=\"11\" fill=\"{cable.Color}\"/>");
                svg.AppendLine($"  <text x=\"{Number(itemX + 19)}\" y=\"{Number(y + 24)}\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"10\" font-weight=\"700\" fill=\"#FFFFFF\">{index + 1}</text>");
                svg.AppendLine($"  <text x=\"{Number(itemX + 38)}\" y=\"{Number(y + 23)}\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"12\" font-weight=\"700\" fill=\"#172033\">{Escape(Trim($"{cable.SourceDevice} → {cable.TargetDevice}", columns == 1 ? 52 : 38))}</text>");
                for (int labelIndex = 0; labelIndex < cable.Labels.Count; labelIndex++)
                {
                    svg.AppendLine($"  <text x=\"{Number(itemX + 38)}\" y=\"{Number(y + 42 + labelIndex * 16)}\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"10.5\" fill=\"#526070\">{Escape(Trim(cable.Labels[labelIndex], columns == 1 ? 58 : 42))}</text>");
                }
            }
            y += rowHeight;
        }
    }

    private static double LegendRowsHeight(IReadOnlyList<SynopticCable> cables, int columns)
    {
        double height = 0;
        int safeColumns = Math.Max(1, columns);
        for (int rowStart = 0; rowStart < cables.Count; rowStart += safeColumns)
        {
            height += cables.Skip(rowStart).Take(safeColumns).Max(LegendItemHeight);
        }
        return height;
    }

    private static double LegendItemHeight(SynopticCable cable)
    {
        return Math.Max(62, 50 + cable.Labels.Count * 16);
    }

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Trim(string value, int maximumLength)
    {
        string clean = string.IsNullOrWhiteSpace(value) ? "?" : value.Trim();
        return clean.Length <= maximumLength ? clean : clean[..Math.Max(1, maximumLength - 1)] + "…";
    }

    private sealed record CablePoint(int? TxId, string TxName, int RxId);

    private sealed record CableDraft(
        SynopticDeviceNode Source,
        SynopticDeviceNode Target,
        int SourceLocationIndex,
        int TargetLocationIndex,
        IReadOnlyList<string> Labels);

    private sealed class CableRange(int? txStart, int? txEnd, string txName, int rxStart, int rxEnd, int count)
    {
        public int? TxStart { get; } = txStart;
        public int? TxEnd { get; set; } = txEnd;
        public string TxName { get; } = txName;
        public int RxStart { get; } = rxStart;
        public int RxEnd { get; set; } = rxEnd;
        public int Count { get; set; } = count;
    }

    private sealed class DevicePairComparer : IEqualityComparer<(string Source, string Target)>
    {
        public static DevicePairComparer Instance { get; } = new();

        public bool Equals((string Source, string Target) left, (string Source, string Target) right)
        {
            return string.Equals(left.Source, right.Source, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Target, right.Target, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string Source, string Target) value)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.Source),
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.Target));
        }
    }
}
