# Analyse de Dante Config Editor V2

## Contenu du dossier V2

Le dossier `V2` ne contient pas les sources originales du projet. Il contient uniquement une application .NET déjà compilée :

- `DanteConfigEditor.exe`
- `DanteConfigEditor.dll`
- fichiers `.deps.json`, `.runtimeconfig.json`, `.pdb`
- `DanteEdit.ico`

La configuration runtime indique une application WPF ciblant `.NET 8.0` avec `Microsoft.WindowsDesktop.App`.

## Structure technique observée

La V2 est une application WPF très compacte :

- `App` lance `MainWindow.xaml`.
- `MainWindow` contient toute la logique dans le code-behind.
- Il n'y a pas de couche métier dédiée.
- Il n'y a pas de séparation nette entre interface, parsing XML et modification des données.

## Fonctionnement V2

La V2 charge un fichier XML, le parse avec `XElement.Load`, puis travaille sur les éléments enfants `<device>` de la racine.

Fonctions présentes :

- ouvrir un fichier XML ;
- lister les devices à partir de `<device><name>...` ;
- renommer un device et son `friendly_name` ;
- mettre à jour les références `<subscribed_device>` quand un device est renommé ;
- modifier le mode réseau via `<redundancy value="true|false">` ;
- appliquer le mode réseau à tous les devices ;
- modifier la latence via `<unicast_latency>` ;
- appliquer la latence à tous les devices ;
- réinitialiser les noms des canaux :
  - TX : `<txchannel><label>` devient `1`, `2`, `3`, etc. ;
  - RX : `<rxchannel><name>` devient `1`, `2`, `3`, etc. ;
- modifier `preferred_master value="true|false"` ;
- lister devices redondants, daisychain, latences et preferred masters ;
- sauvegarder le XML avec un dialogue "Enregistrer sous".

## Limites V2

- Aucun code source n'était fourni dans le dossier V2.
- Toute la logique est concentrée dans la fenêtre principale.
- Pas de modèle `DanteProject`, `DanteDevice`, `DanteChannel` ou `DanteSubscription`.
- Pas de vraie vue patch ou matrice de routage.
- Pas de journal d'actions.
- Pas d'état clair "modifié / non modifié".
- Pas de sauvegarde automatique du fichier original avant écriture.
- Pas de validation approfondie avant sauvegarde.
- Pas de confirmation avant plusieurs actions globales risquées.
- Messages d'erreur très techniques.
- Interface fonctionnelle mais brute, en deux colonnes, sans navigation métier.

## Hypothèse de format XML

La V2 montre que le format manipulé est un XML hors ligne contenant des éléments :

- `<device>`
- `<name>`
- `<friendly_name>`
- `<redundancy value="...">`
- `<preferred_master value="...">`
- `<unicast_latency>`
- `<txchannel>`
- `<rxchannel>`
- `<subscribed_device>`

La V3 reste strictement dans cette logique hors ligne. Elle ne tente pas de contrôler un réseau Dante live.
