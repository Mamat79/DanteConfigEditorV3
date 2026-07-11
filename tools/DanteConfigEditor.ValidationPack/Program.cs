using DanteConfigEditor.Services;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: DanteConfigEditor.ValidationPack <source.xml> <dossier-sortie-vide>");
    return 2;
}

try
{
    ValidationPackResult result = ValidationPackService.Create(args[0], args[1]);
    Console.WriteLine($"Pack créé : {result.OutputDirectory}");
    Console.WriteLine($"SHA-256 source : {result.SourceSha256}");
    foreach (ValidationPackScenario scenario in result.Scenarios)
    {
        Console.WriteLine($"{scenario.Status,-7} {scenario.Label}: {scenario.Detail}");
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
