# Dante Config Editor V3

Outil Windows pour éditer hors ligne des fichiers XML de configuration Dante.

> **Statut du projet : version en développement.**  
> Cette application est fournie comme outil de travail en cours. Elle n'est pas exempte de bugs, peut encore mal interpréter certains fichiers XML Dante, et ne doit pas être utilisée directement sur des fichiers critiques sans sauvegarde et validation dans les outils Dante officiels.

## Ce que fait l'application

- Ouvre des fichiers XML de configuration Dante hors ligne.
- Affiche les devices, canaux TX/RX, latences, mode réseau et preferred master.
- Renomme les devices.
- Renomme les canaux TX/RX.
- Réinitialise les noms de canaux.
- Modifie les paramètres déjà couverts par la V2.
- Affiche une page Patch pour visualiser et modifier les abonnements RX vers TX lorsque le format XML le permet.
- Met à jour les patchs RX quand un canal TX utilisé est renommé.
- Crée une sauvegarde du fichier original avant sauvegarde.
- Propose un thème sombre et un thème clair.

## Limites importantes

- L'application ne pilote pas un réseau Dante en direct.
- Elle n'utilise pas de SDK ou d'API Audinate.
- Elle travaille uniquement sur des fichiers XML hors ligne.
- Elle ne contourne aucune protection Audinate et ne réimplémente pas de protocole propriétaire.
- La compatibilité dépend de la structure réelle du XML fourni.
- Certains champs de patch peuvent ne pas être détectés si le fichier utilise une structure différente de celles actuellement reconnues.

## Télécharger / installer

Deux options sont fournies dans le dossier `dist` :

- `dist/DanteConfigEditorV3_Installer.exe` : installateur Windows recommandé, avec installation par défaut dans Program Files, choix du dossier d'installation, raccourcis Menu Démarrer/Bureau et désinstallation propre.
- `dist/DanteConfigEditorV3_Setup.exe` : ancien installateur autonome simple.
- `dist/portable/DanteConfigEditorV3.exe` : version portable, lançable sans installation.

La version autonome inclut le runtime .NET nécessaire. Sur une machine Windows x64, il ne devrait pas être nécessaire d'installer .NET séparément pour utiliser l'application.

L'installateur contient uniquement l'application autonome et la documentation utilisateur. Les sources du projet ne sont pas installees sur la machine de l'utilisateur.

## Versions incluses

- La V3 est la version active du projet, avec les sources dans ce dépôt.
- Une copie de la V2 historique est disponible dans `legacy/V2`.
- La V2 est conservée comme archive de référence : elle contient les binaires compilés d'origine, pas les sources.
- L'analyse de la V2 est documentée dans `ANALYSE_V2.md`.

## Utilisation rapide

1. Lancer l'application.
2. Cliquer sur `Ouvrir XML`.
3. Sélectionner une copie du fichier de configuration Dante.
4. Vérifier les devices et paramètres détectés.
5. Modifier les champs souhaités.
6. Utiliser la page `Patch` pour consulter ou modifier les abonnements reconnus.
7. Sauvegarder sous un nouveau nom.
8. Valider le fichier généré dans l'outil Dante officiel approprié avant usage terrain.

## Renommage des canaux et patchs

Quand un canal TX est renommé, l'application parcourt les abonnements RX reconnus et remplace l'ancien nom du canal par le nouveau partout où il est utilisé avec le même device TX.

Les champs de patch actuellement reconnus incluent notamment :

- `subscribed_device`
- `subscription_device`
- `tx_device`
- `source_device`
- `subscribed_channel`
- `subscribed_channel_name`
- `subscribed_channel_label`
- `subscribed_tx_channel`
- `subscribed_tx_channel_name`
- `subscribed_label`
- `source_channel`
- `source_channel_name`

## Compiler depuis les sources

Prérequis :

- Windows
- SDK .NET 8

Depuis le dossier du projet :

```powershell
.\build.ps1
```

Pour reconstruire l'installateur autonome :

```powershell
.\installer\build_installer.ps1
```

## Sécurité des fichiers

- Toujours travailler sur une copie.
- Ne jamais écraser un fichier de production sans test.
- Conserver les backups générés automatiquement.
- Tester le fichier final dans les outils Dante officiels avant exploitation.

## Crédit

By Mamat
