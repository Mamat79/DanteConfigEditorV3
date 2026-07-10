using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using DanteConfigEditor.Models;
using DanteConfigEditorV3.TestSupport;

const int RunsPerSize = 3;
int[] deviceCounts = [10, 50, 200];
string phase = ReadArgument("--phase") ?? "after";
string output = Path.GetFullPath(ReadArgument("--output") ?? Path.Combine("benchmarks", "results", $"{phase}.json"));
string commit = ReadArgument("--commit") ?? "unknown";
string temporaryRoot = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3.Benchmarks", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporaryRoot);

try
{
    WarmUp(temporaryRoot);
    List<BenchmarkRun> runs = [];
    foreach (int deviceCount in deviceCounts)
    {
        string template = Path.Combine(temporaryRoot, $"synthetic-{deviceCount}.xml");
        SyntheticPresetFactory.Create(template, deviceCount);
        for (int run = 1; run <= RunsPerSize; run++)
        {
            runs.Add(RunScenario(template, temporaryRoot, deviceCount, run));
        }
    }

    BenchmarkResult result = BuildResult(phase, commit, runs);
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine($"Benchmark {phase} écrit dans {output}");
    foreach (BenchmarkSummary row in result.Results)
    {
        Console.WriteLine(
            $"{row.Devices,3} devices | load {row.LoadMedianMs,8:0.000} ms | edit {row.EditMedianMs,8:0.000} ms | " +
            $"guard {row.GuardMedianMs,8:0.000} ms | save {row.SaveMedianMs,8:0.000} ms | edit alloc {row.EditMedianAllocatedMiB,8:0.000} MiB");
    }
}
finally
{
    if (Directory.Exists(temporaryRoot))
    {
        Directory.Delete(temporaryRoot, recursive: true);
    }
}

string? ReadArgument(string name)
{
    int index = Array.FindIndex(args, argument => string.Equals(argument, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

void WarmUp(string directory)
{
    string path = Path.Combine(directory, "warmup.xml");
    SyntheticPresetFactory.Create(path, 1, txPerDevice: 1, rxPerDevice: 1);
    DanteProject project = DanteProject.Load(path);
    project.SetLatency("DEVICE-001", "2000");
    _ = project.ValidateXmlChangeGuard();
}

BenchmarkRun RunScenario(string template, string directory, int devices, int run)
{
    string source = Path.Combine(directory, $"synthetic-{devices}-run-{run}.xml");
    string destination = Path.Combine(directory, $"synthetic-{devices}-run-{run}-saved.xml");
    File.Copy(template, source, overwrite: true);

    DanteProject? project = null;
    Measurement load = Measure(() =>
    {
        project = DanteProject.Load(source);
        return project.Devices.Count;
    });

    // Ce scénario reproduit la validation complète de la fenêtre Détail machine.
    // La V3.06 l'exécute dans un lot afin de ne reconstruire le modèle qu'une fois.
    Measurement edit = Measure(() =>
    {
        project!.ApplyBatch(batch =>
        {
            batch.RenameDevice("DEVICE-001", "DEVICE-001-EDITED");
            batch.SetNetworkMode("DEVICE-001-EDITED", true);
            batch.SetLatency("DEVICE-001-EDITED", "2000");
            batch.SetSamplerate("DEVICE-001-EDITED", "96000");
            batch.SetEncoding("DEVICE-001-EDITED", "32");
            batch.SetPreferredMaster("DEVICE-001-EDITED", false);
            for (int channel = 1; channel <= 64; channel++)
            {
                batch.RenameChannel("DEVICE-001-EDITED", DanteChannelKind.Tx, channel, $"EDIT-TX-{channel:D2}");
                batch.RenameChannel("DEVICE-001-EDITED", DanteChannelKind.Rx, channel, $"EDIT-RX-{channel:D2}");
            }
        });
        return project.Changes.Count;
    });
    Measurement guard = Measure(() => project!.ValidateXmlChangeGuard().HasErrors);
    Measurement save = Measure(() => project!.SaveAs(destination));

    return new BenchmarkRun(
        devices,
        run,
        Math.Round(new FileInfo(template).Length / 1024d / 1024d, 3),
        load.Milliseconds,
        load.AllocatedMiB,
        edit.Milliseconds,
        edit.AllocatedMiB,
        guard.Milliseconds,
        guard.AllocatedMiB,
        (bool)guard.Value,
        save.Milliseconds,
        save.AllocatedMiB,
        save.WorkingSetMiB);
}

Measurement Measure(Func<object> operation)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
    Stopwatch stopwatch = Stopwatch.StartNew();
    object value = operation();
    stopwatch.Stop();
    using Process process = Process.GetCurrentProcess();
    process.Refresh();
    return new Measurement(
        value,
        Math.Round(stopwatch.Elapsed.TotalMilliseconds, 3),
        Math.Round((GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore) / 1024d / 1024d, 3),
        Math.Round(process.WorkingSet64 / 1024d / 1024d, 3));
}

BenchmarkResult BuildResult(string measuredPhase, string measuredCommit, IReadOnlyList<BenchmarkRun> runs)
{
    BenchmarkSummary[] summaries = runs
        .GroupBy(run => run.Devices)
        .OrderBy(group => group.Key)
        .Select(group => new BenchmarkSummary(
            group.Key,
            Median(group.Select(run => run.XmlMiB)),
            Median(group.Select(run => run.LoadMs)),
            Median(group.Select(run => run.LoadAllocatedMiB)),
            Median(group.Select(run => run.EditMs)),
            Median(group.Select(run => run.EditAllocatedMiB)),
            Median(group.Select(run => run.GuardMs)),
            Median(group.Select(run => run.GuardAllocatedMiB)),
            Median(group.Select(run => run.SaveMs)),
            Median(group.Select(run => run.SaveAllocatedMiB)),
            Median(group.Select(run => run.FinalWorkingSetMiB)),
            group.Any(run => run.GuardHasErrors)))
        .ToArray();

    string version = typeof(DanteProject).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? string.Empty;
    return new BenchmarkResult(
        measuredPhase,
        measuredCommit,
        DateTime.UtcNow.ToString("yyyy-MM-dd"),
        new BenchmarkEnvironment(Environment.OSVersion.VersionString, Environment.Version.ToString(), version, "Release"),
        new BenchmarkScenario(RunsPerSize, "median", 64, 64, "grouped device details edit"),
        summaries,
        runs);
}

double Median(IEnumerable<double> values)
{
    double[] sorted = values.Order().ToArray();
    return sorted[sorted.Length / 2];
}

internal sealed record Measurement(object Value, double Milliseconds, double AllocatedMiB, double WorkingSetMiB);
internal sealed record BenchmarkEnvironment(string Os, string Runtime, string ApplicationVersion, string Configuration);
internal sealed record BenchmarkScenario(int RunsPerSize, string Aggregation, int TxPerDevice, int RxPerDevice, string Edit);
internal sealed record BenchmarkRun(
    int Devices,
    int Run,
    double XmlMiB,
    double LoadMs,
    double LoadAllocatedMiB,
    double EditMs,
    double EditAllocatedMiB,
    double GuardMs,
    double GuardAllocatedMiB,
    bool GuardHasErrors,
    double SaveMs,
    double SaveAllocatedMiB,
    double FinalWorkingSetMiB);
internal sealed record BenchmarkSummary(
    int Devices,
    double XmlMiB,
    double LoadMedianMs,
    double LoadMedianAllocatedMiB,
    double EditMedianMs,
    double EditMedianAllocatedMiB,
    double GuardMedianMs,
    double GuardMedianAllocatedMiB,
    double SaveMedianMs,
    double SaveMedianAllocatedMiB,
    double FinalWorkingSetMedianMiB,
    bool GuardHasErrors);
internal sealed record BenchmarkResult(
    string Phase,
    string Commit,
    string Date,
    BenchmarkEnvironment Environment,
    BenchmarkScenario Scenario,
    IReadOnlyList<BenchmarkSummary> Results,
    IReadOnlyList<BenchmarkRun> RawRuns);
