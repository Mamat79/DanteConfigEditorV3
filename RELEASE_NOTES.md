# Dante Config Editor V3 - 0.3.1-dev

## Statut

Version en developpement. Cette application n'est pas exempte de bugs.

Travaillez toujours sur une copie de vos fichiers XML Dante et validez le resultat dans les outils Dante officiels avant utilisation sur une installation reelle.

## Nouveautes principales

- Interface WPF modernisee avec theme sombre et theme clair.
- Edition hors ligne des fichiers XML Dante compatibles.
- Vue Patch pour consulter et modifier les abonnements RX vers TX reconnus.
- Renommage des devices.
- Renommage des canaux TX/RX.
- Mise a jour des patchs RX quand un canal TX utilise est renomme.
- Sauvegarde avec backup automatique du fichier original.
- Installateur Windows professionnel avec choix du dossier, raccourcis et desinstallation.

## Installation

Utiliser le fichier :

```text
DanteConfigEditorV3_Installer.exe
```

L'installateur inclut le runtime .NET necessaire. Il n'est normalement pas necessaire d'installer .NET separement sur une machine Windows x64.

En fin d'installation, l'assistant peut ouvrir :

- les release notes ;
- la notice d'utilisation PDF du logiciel.

## Limites connues

- L'application ne pilote pas un reseau Dante en direct.
- Elle n'utilise pas de SDK ou API Audinate.
- Elle travaille uniquement sur des fichiers XML hors ligne.
- Certains formats XML Dante peuvent ne pas etre totalement reconnus.
- La page Patch depend des champs d'abonnement presents dans le fichier.

## Depot GitHub

https://github.com/Mamat79/DanteConfigEditorV3
