# Benchmarks de durcissement

Le benchmark mesure des presets synthétiques de 10, 50 et 200 machines, avec 64 TX et 64 RX par machine. Trois exécutions sont réalisées pour chaque taille et la médiane est conservée.

Le scénario d'édition reproduit une validation complète de `Détail machine` : renommage de la machine, cinq réglages, puis renommage de 64 TX et 64 RX. Les mesures comprennent le chargement, l'édition, le garde-fou XML, `SaveAs`, les allocations mémoire gérées et le working set final.

## Exécution

Depuis la racine du dépôt :

```powershell
dotnet run --project .\benchmarks\DanteConfigEditorV3.Benchmarks\DanteConfigEditorV3.Benchmarks.csproj -c Release -- --phase after --commit <commit> --output .\tmp\benchmarks\after-<commit>.json
```

Les timings dépendent de la machine et de son activité. Ils servent à comparer les deux implémentations sur le même poste ; ils ne constituent pas un seuil de réussite pour la CI.

## Sorties

Les fichiers JSON sont des sorties générées, dépendantes de la machine et non versionnées. Les mesures historiques avant/après du durcissement V3.06 restent synthétisées dans `AUDIT_V3_06.md`.
