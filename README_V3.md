# Dante Config Editor V3.05

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

Cette version est publiée en mode `self-contained win-x64` : le runtime .NET nécessaire est inclus dans l'exécutable de l'application. La machine destinataire n'a pas besoin d'installer le SDK .NET ni le runtime .NET pour utiliser Dante Config Editor V3.05.

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

## Nouveautés V3.05

- Import d'un second XML corrigé : les machines uniques sont ajoutées même si le fichier contient aussi des noms déjà présents.
- Fenêtre de résolution des doublons à l'import : import des uniques seulement, renommage automatique ou renommage manuel.
- Mise à jour des subscriptions/patchs importés quand une machine importée est renommée.
- Modification de la sample rate et des bits par échantillon machine par machine.
- Actions globales pour appliquer sample rate et bits par échantillon à toutes les machines.
- Remise des adresses IPv4 en automatique par machine ou globalement quand le XML expose ces champs.
- Listes rapides Sample rates, Bits et IP fixes.
- Avertissement visible si plusieurs sample rates ou plusieurs encodages sont présents dans le preset.
- Validation de sauvegarde adaptée aux imports et suppressions de machines.
- Page Configuration plus lisible : zone de réglages défilable, table des machines gardée visible et recherche globale avec message d'aide.

## Nouveautés V3.04

- Garde-fou de changements XML avant sauvegarde : les zones techniques Dante sensibles sont bloquées si elles changent par accident.
- Rapport `Compatibilité Dante Controller` avec état de la racine, version, devices, TX/RX, Dante Id, mediaType, balises techniques et warnings non bloquants.
- Interface modifiable après ouverture du XML, avec sauvegarde obligatoire sous un autre nom pour protéger le fichier source.
- Sélecteur Français / Anglais directement dans l'application, modifiable à tout moment.
- Suppression d'un device avec nettoyage des subscriptions/patchs qui pointent vers lui.
- Import d'un second XML dans le projet ouvert, avec refus des noms de devices en doublon.
- Libellés utilisateur harmonisés : `Dante Id` dans l'interface, les exports et les rapports. L'attribut XML reste exactement `danteId`.
- Latences affichées en ms : `250` -> `0,25 ms`, `1000` -> `1 ms`, `2000` -> `2 ms`, `5000` -> `5 ms`, sans changer les valeurs XML.
- Prévisualisation des actions globales avant application.
- Preferred master global sécurisé : définir le device sélectionné comme seul preferred master ou retirer tous les preferred masters.
- Page Patch avec mode simple/expert, colonne `Source complète`, choix TX avec Dante Id et avertissement pour les devices absents du preset.
- Patchbook TXT enrichi et export CSV lecture seule.
- Page Santé enrichie : mode lecture/édition, samplerates, encodages, latences en ms et compteurs de contrôle.
- Vue Topologie simple : sources les plus utilisées, receivers les plus patchés et relations TX vers RX.
- Script installateur plus portable pour trouver Inno Setup et option d'archive source propre.

## Nouveautés V3.03

- Ajout d'une page `Santé du fichier` avec synthèse du preset, statistiques TX/RX, patchs actifs/libres/locaux, preferred masters, modes réseau, IP fixes et tableau filtrable des points à vérifier.
- Renforcement de la compatibilité XML Dante Controller avant sauvegarde : contrôle de la racine `<preset>`, de la version, des devices, des canaux `txchannel` / `rxchannel`, des attributs XML `danteId` / `mediaType` et des balises techniques importantes.
- Meilleure gestion des patchs locaux `subscribed_device="."` : affichage comme source locale, pas comme conflit, et conservation du `.` à la sauvegarde quand le fichier source l'utilise.
- Utilisation du Dante Id comme identifiant métier principal des canaux, sans renumérotation ni réécriture des attributs existants.
- Page `Patch` enrichie : colonnes TX brut/résolu/affiché, type de patch, warnings, filtres par état, patchs locaux, devices TX absents et canaux TX introuvables.
- Ajout d'un export `Patchbook TXT` organisé par device RX, avec options tous les RX, patchs actifs ou warnings/conflits.
- Comparaison XML plus lisible pour les canaux et patchs, en s'appuyant sur les Dante Id quand ils sont présents.
- Correction de la lisibilité des listes et menus : fond blanc et texte noir, y compris en thème sombre.
- Correction de la propagation des patchs lors de la réinitialisation des canaux TX.
- Ajout du choix canal début / canal fin pour limiter le renommage en série à une plage.

## Nouveautés V3.02

- Annulation de la dernière action.
- Résumé avant sauvegarde plus lisible.
- Recherche globale dans machines, canaux et patchs.
- Export du rapport en TXT ou PDF.
- Conflits de patch plus visibles.
- Renommage de canaux en série, avec plage de canaux configurable.
- Alerte visible pour les fichiers qui mélangent redondant/daisychain ou contiennent des machines détectées en IP fixe.
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
- Le renommage en série peut être limité à une plage avec un canal de début et un canal de fin.
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
- L'application vérifie la cohérence du XML généré, mais la validation définitive doit être faite par un import dans Dante Controller avant toute utilisation en production.

## Limites connues

- La V3 ne se connecte pas au réseau Dante.
- La V3 n'utilise pas d'API ou SDK Audinate.
- La vue Patch travaille uniquement sur le XML hors ligne.
- `subscribed_device="."` est interprété comme une source locale, c'est-à-dire le device RX lui-même.
- Un device TX absent du preset est signalé en avertissement, pas forcément en erreur bloquante, car certains presets peuvent être partiels.
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
