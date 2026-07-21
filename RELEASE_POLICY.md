# Politique de publication GitHub

Chaque version publique de Dante Config Editor possède son propre tag Git immuable et sa propre Release GitHub. Le format historique du projet est `v3.08`, `v3.09`, puis `v3.1` pour la version suivante demandée.

Les métadonnées éditoriales réglées sur GitHub sont également conservées : nom,
description du dépôt, titre et description de chaque Release publiée. Le dépôt
local contient un instantané de référence dans `GITHUB_METADATA.md`. Il sert à
préparer les prochaines mises à jour sans remplacer silencieusement les textes
modifiés en ligne.

## Règles

- Ne jamais déplacer un tag de version déjà publié.
- Ne jamais supprimer une ancienne Release pour publier la suivante.
- Ne jamais utiliser `gh release upload --clobber` sur une Release publiée.
- Ne jamais modifier automatiquement le titre ou la description d'une Release existante.
- Ne jamais remplacer la description « About » du dépôt lors d'un push Git.
- Construire les binaires depuis le commit désigné par le tag, et non depuis la branche courante.
- Conserver les sommes SHA-256 avec chaque installateur ou DMG.
- Une ancienne version recréée doit utiliser `make_latest=false`.
- La version stable la plus récente reste marquée `Latest`.

## Workflow automatique

`.github/workflows/versioned-release.yml` se déclenche pour un nouveau tag de version ou manuellement pour un tag existant sans Release.

Le workflow :

1. vérifie le format et l'existence du tag ;
2. refuse de continuer si une Release utilise déjà ce tag ;
3. teste et construit Windows et macOS depuis ce tag exact ;
4. valide les sommes SHA-256 ;
5. crée une nouvelle Release sans suppression ni écrasement ;
6. attribue `Latest` uniquement à une version plus récente, ou sur demande explicite lors d'un lancement manuel.

Après une publication, synchroniser le dépôt local avec :

```powershell
git fetch --prune --tags origin
git status --short --branch
```
