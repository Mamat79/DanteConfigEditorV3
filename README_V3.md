# Dante Config Editor V3.03

Application Windows WPF pour éditer hors ligne des fichiers XML de configuration Dante compatibles avec la structure utilisée par la V2.

## Lancer l'application

Si l'application a déjà été compilée :

```powershell
.\run.ps1
```

Ou lancer directement :

```powershell
.\bin\Release\net8.0-windows\DanteConfigEditorV3.exe
```

Ce mode sert surtout au développement local. Pour transmettre l'application à quelqu'un, utiliser l'installateur.

## Installer sur une autre machine

Le fichier autonome à transmettre est :

```text
dist\DanteConfigEditorV3_Installer.exe
```

La personne le lance, choisit le dossier d'installation si besoin, puis l'application est installée par défaut dans Program Files avec un raccourci dans le Menu Démarrer. Un raccourci Bureau est proposé en option.

Cette version est publiée en mode `self-contained win-x64` : le runtime .NET nécessaire est inclus dans l'exécutable de l'application. La machine destinataire n'a pas besoin d'installer le SDK .NET ni le runtime .NET pour utiliser Dante Config Editor V3.03.

L'installateur ne déploie pas les sources du projet. Il installe uniquement l'application autonome et la documentation utilisateur.

## Compiler

Installer le SDK .NET 8 si nécessaire, puis depuis le dossier `V3` :

```powershell
.\build.ps1
```

Ou manuellement :

```powershell
dotnet build .\DanteConfigEditorV3.csproj -c Release
dotnet publish .\DanteConfigEditorV3.csproj -c Release -r win-x64 --self-contained false -o .\publish
```

Pour reconstruire l'installateur autonome :

```powershell
.\installer\build_installer.ps1
```

## Nouveautés V3.03

- Correction de la lisibilité des listes et menus en thème sombre.
- Correction de la propagation des patchs lors de la réinitialisation des canaux TX.

## Nouveautés V3.02

- Annulation de la dernière action.
- Résumé avant sauvegarde plus lisible.
- Recherche globale dans machines, canaux et patchs.
- Export du rapport en TXT ou PDF.
- Conflits de patch plus visibles.
- Renommage de canaux en série.
- Liste des fichiers récents.
- Comparaison avec un autre XML.
- Sauvegarde sécurisée par fichier temporaire relu avant remplacement.

## Nouveautés V3.01

- Colonne `Projet` plus compacte.
- Navigation par onglets uniquement en haut.
- Page `Configuration` réorganisée horizontalement.
- Page `Patch` avec filtre émetteur TX réellement actif.
- Choix `Tous les émetteurs` et `Tous les récepteurs` pour revenir à l'affichage complet.
- Renommage RX et TX séparés dans la page `Patch`.

## Nouveautés V3

- Nouveau projet source séparé de `V2`.
- Interface WPF modernisée avec navigation latérale.
- Thème sombre et thème clair.
- Barre de statut en bas.
- Journal des actions.
- Affichage du fichier ouvert et de l'état modifié / non modifié.
- Vue Configuration plus lisible.
- Couche métier séparée :
  - `DanteProject`
  - `DanteDevice`
  - `DanteChannel`
  - `DanteSubscription`
  - `DantePatchMatrix`
- Renommage de devices avec mise à jour des références `subscribed_device`.
- Renommage individuel des canaux TX et RX.
- Reset des noms de canaux par device ou globalement.
- Si un canal TX renommé est référencé par des RX, les références de patch reconnues sont mises à jour partout dans le fichier.
- Première vue Patch hors ligne :
  - liste des devices émetteurs ;
  - liste des devices récepteurs ;
  - table des canaux RX ;
  - source TX device / canal ;
  - recherche ;
  - filtre par récepteur ;
  - affichage des conflits ;
  - ajout, modification et suppression de patchs quand les champs XML reconnus existent ou peuvent être créés.
  - renommage direct des canaux TX/RX depuis la page Patch.
- Validation avant sauvegarde.
- Résumé des changements avant sauvegarde.
- Backup automatique du fichier original avant écriture.
- Sauvegarde sous un nouveau nom par défaut.

## Précautions avec les fichiers Dante

- Travailler sur une copie du fichier original quand c'est possible.
- La V3 crée automatiquement un backup dans `DanteConfigEditor_Backups` à côté du fichier original avant une sauvegarde.
- La sauvegarde par défaut propose un nom avec suffixe `_V3`.
- Relire le résumé avant sauvegarde.
- Tester le fichier généré dans l'outil Dante officiel approprié avant utilisation en production.
- Ne pas utiliser un fichier modifié directement sur une installation critique sans validation.

## Limites connues

- La V3 ne se connecte pas au réseau Dante.
- La V3 n'utilise pas d'API ou SDK Audinate.
- La vue Patch travaille uniquement sur le XML hors ligne.
- Les champs de patch reconnus sont notamment :
  - `subscribed_device`
  - `subscribed_channel`
  - `subscribed_channel_name`
  - `subscribed_channel_label`
  - `subscribed_tx_channel`
  - `subscribed_tx_channel_name`
  - `subscribed_label`
  - `source_channel`
  - `source_channel_name`
- Si un fichier Dante utilise une structure propriétaire différente, la V3 peut afficher des données partielles ou signaler des conflits.
- Aucun contournement de protection Audinate ni reverse engineering protocolaire n'est implémenté.

## Prochaines améliorations possibles

- Ajouter une vraie matrice graphique TX vers RX avec cases à cocher.
- Ajouter une comparaison visuelle original / modifié.
- Ajouter import/export CSV des patchs.
- Ajouter tests unitaires sur plusieurs vrais exemples XML Dante.
- Ajouter une détection de schéma plus large si d'autres formats hors ligne doivent être pris en charge.
