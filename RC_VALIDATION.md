# Validation de livraison V3.07 Beta

## Statut

Ce document suit la préparation de la **V3.07 Beta**. Son nom est conservé pour le pack documentaire demandé, mais il ne signifie pas que l'application est une Release Candidate.

Un résultat automatisé réussi ne prouve pas la compatibilité avec Dante Controller. La bêta ne pourra être qualifiée pour un usage terrain qu'après les imports manuels décrits dans `MANUAL_DANTE_CONTROLLER_TESTS.md`.

## Référence de travail

- dépôt : `Mamat79/DanteConfigEditorV3` ;
- branche : `main` ;
- version : `3.07-beta` ;
- tag de sécurité avant travaux : `safety-v3.07-before-validation-20260711` ;
- système de validation locale : Windows x64, .NET 8 ;
- commit final validé : à renseigner après la validation finale ;
- date de validation finale : à renseigner après la validation finale.

## Contrôles automatisés

| Contrôle | État | Preuve attendue |
|---|---|---|
| Restore application et projets de tests | À rejouer en validation finale | code retour 0 |
| Tests Core en Release | À rejouer en validation finale | total exact, 0 échec |
| Tests UI Mac headless en Release | À rejouer en validation finale | total exact, 0 échec |
| Build Windows en Release | À rejouer en validation finale | 0 warning, 0 erreur |
| Build interface Mac en Release | À rejouer en validation finale | 0 warning, 0 erreur |
| Publish Windows autonome `win-x64` | À rejouer en validation finale | exécutable produit |
| Publish Mac autonome `osx-arm64` et `osx-x64` | À rejouer en validation finale | publications produites |
| Construction installateur Inno Setup | À rejouer en validation finale | installateur et SHA-256 |
| Installation de remplacement sur ce PC | À effectuer en validation finale | version, chemin, raccourcis, démarrage |
| GitHub Actions Windows et macOS | En attente du push final | runs distants réussis |
| Scan NuGet vulnérable | À rejouer en validation finale | aucun package vulnérable signalé |
| État Git final | À contrôler après publication | branche `main` propre |

## Contrôles fonctionnels locaux déjà couverts

- garde-fou XML lié à une identité technique stable ;
- blocage des chemins XML inconnus ;
- sauvegarde atomique et protection d'une destination existante ;
- récupération liée à la nouvelle destination après `Enregistrer sous` ;
- namespace XML par défaut ;
- aliases de subscription reconnus et patch local `.` ;
- interface IPv4 principale sans modification implicite de l'interface secondaire ;
- fusion, suppression de machine, formats audio et actions groupées ;
- presets synthétiques de 10, 50 et 200 machines en 64 TX / 64 RX ;
- atelier de patch visuel : sélection multiple, affectation séquentielle, matrice et changements en attente ;
- comportement compact des interfaces Windows et Mac.

Les nombres exacts de tests et les temps mesurés sont consignés dans `TESTING.md`.

## Validation Dante Controller

**État : non testée dans ce cycle.**

Aucune case d'import ne doit être marquée comme réussie sans preuve obtenue dans une version identifiée de Dante Controller. Utiliser `tools/New-ValidationPack.ps1`, puis suivre `MANUAL_DANTE_CONTROLLER_TESTS.md` sur une copie de travail.

## Décision de livraison

- [ ] tous les tests locaux sont verts ;
- [ ] les builds et publications sont sans warning ;
- [ ] l'installateur de remplacement est construit et vérifié ;
- [ ] l'application installée démarre depuis Program Files ;
- [ ] les notices et raccourcis installés sont présents ;
- [ ] le push `main` et le tag de sécurité sont publiés ;
- [ ] les workflows GitHub Actions distants sont verts ;
- [ ] les limites restantes sont cohérentes avec `KNOWN_LIMITATIONS.md` ;
- [ ] aucune donnée XML de production n'est suivie dans Git.

La livraison peut rester une **V3.07 Beta** même si tous les contrôles techniques ci-dessus réussissent. L'import Dante Controller demeure une validation manuelle séparée.
