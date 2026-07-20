# Dante Config Editor V3.09

**Français** | [English](https://github.com/Mamat79/DanteConfigEditorV3/blob/main/RELEASE_NOTES_EN.md)

## Statut

Version officielle V3.09 pour Windows x64, macOS Apple Silicon et macOS Intel. Il s'agit d'un outil tiers non officiel Audinate et cette application n'est pas exempte de bugs.

La V3.09 remplace la V3.08 comme version courante de la branche `main`. Les anciennes versions ne sont plus proposées au téléchargement, mais leur historique fonctionnel reste consultable dans les commits et le changelog. Travaillez toujours sur une copie de vos fichiers XML Dante et validez le résultat dans les outils Dante officiels avant toute utilisation réelle.

## Pourquoi j'ai créé cet outil

J'ai créé Dante Config Editor pour pallier ce qui me manquait dans Dante Controller : vérifier rapidement une configuration complète sans devoir ouvrir successivement toutes les pages consacrées aux machines, aux latences, aux fréquences d'échantillonnage, aux modes réseau, aux adresses IP ou à l'horloge. Je voulais pouvoir tout survoler depuis une vue d'ensemble et corriger les valeurs du preset lorsque cela était nécessaire.

Je rencontrais aussi régulièrement le même problème sur des réseaux déjà patchés : renommer une machine ou des canaux TX pouvait imposer de reprendre les subscriptions et une partie du patch. L'éditeur met donc à jour les références XML reconnues lors de ces renommages afin de préserver le patch autant que la structure du preset le permet.

La préparation hors ligne est enfin un besoin central du projet. L'application permet de contrôler, modifier, fusionner et préparer un preset sans connexion au réseau Dante, avant de valider le fichier final dans Dante Controller.

## Atomic Bomb pour la formation

- Merci à **Charles Bouticourt** pour l'idée de cette fonction de formation.
- Ajout d'un gros bouton rouge dans `Sécurité et journal` pour générer un preset d'exercice volontairement en désordre.
- Trois confirmations successives sont obligatoires avant l'opération.
- Mélange contrôlé des noms, patchs, modes réseau, Preferred Master, latences, sample rates, encodages et IP principales.
- Noms de machines uniques et variés issus d'un catalogue mythologique, audio et humoristique, sans préfixe uniforme.
- Conservation des identifiants techniques, namespaces, DNS, passerelles et interfaces secondaires.
- Le fichier original reste intact ; le résultat doit être créé avec `Enregistrer sous`.
- Le scénario complet s'annule en une seule action et son numéro de graine est affiché.

## Nouvel onglet Easy patch

- L'onglet classique `Patch` reste disponible sans changement de principe.
- `Easy patch` est intégré directement comme nouvel onglet principal sous Windows.
- Les machines et canaux RX sont à gauche ; les machines et canaux TX sont à droite.
- Chaque côté possède un menu et des flèches précédent/suivant pour changer rapidement de machine.
- Sélection multiple indépendante des canaux TX et RX avec `Ctrl` ou `Maj`.
- Boutons `Tout sélectionner` pour les listes TX et RX.
- Appariement un-à-un lorsque le nombre de TX et de RX est identique.
- Un seul TX peut alimenter plusieurs RX sélectionnés.
- Plusieurs TX vers un seul RX sont refusés.
- Deux sélections multiples de tailles différentes sont refusées.
- Patch par plage avec premier TX, premier RX et quantité exacte.
- Une plage trop longue est entièrement bloquée ; aucune partie du lot n'est préparée.
- Les noms des machines RX/TX restent visibles : Easy patch n'utilise plus un ascenseur pour toute la page.
- Prévisualisation compacte de chaque ligne : RX cible, source actuelle, nouvelle source et action.
- `Appliquer` exécute directement la sélection ou la plage sans imposer la prévisualisation.
- Chaque prévisualisation rejoint automatiquement le lot cumulatif sans modifier le XML ; `Appliquer tout le lot` valide ensuite toutes les opérations en une seule action.
- La grille utilise des cellules compactes et affiche le nom TX complet au survol.
- Un glissement horizontal prépare une série TX/RX, un glissement vertical diffuse un TX vers plusieurs RX et une diagonale prépare une série un-à-un.
- Pour un RX déjà patché : annuler le lot, ignorer les conflits ou remplacer les subscriptions.
- Déconnexion groupée des RX sélectionnés.
- Grille TX/RX conservée pour affecter ou retirer un patch unitaire.
- Les changements ajoutés au lot restent en mémoire jusqu'à `Appliquer tout le lot`.
- L'application finale utilise un seul lot et une seule étape d'annulation.

## Détail machine

- Nouvel onglet `Patch RX` dans la fenêtre ouverte au double-clic sur une machine.
- Un menu en haut permet de passer à une autre machine sans revenir à la fenêtre principale.
- Si des réglages sont en attente, le choix appliquer/abandonner/annuler évite toute perte silencieuse.
- La machine RX reste verrouillée sur celle dont le détail est affiché.
- Toutes les machines TX compatibles du preset restent disponibles comme sources.
- Les changements de patch sont validés avec le nom, les formats, l'IP et les noms de canaux.
- Les patchs sont appliqués avant les renommages afin que toutes les références suivent les nouveaux noms.
- Une réouverture de l'atelier permet de modifier ou d'annuler les patchs encore en attente.

## Installation et remplacement des anciennes versions

- Nouvel AppId réservé à la famille V3.09.
- Dossier par défaut : `C:\Program Files\Dante Config Editor V3.09\`.
- Raccourcis `Dante Config Editor V3.09` dans le menu Démarrer et sur le Bureau.
- Données locales V3.09 séparées : langue, fichiers récents, récupération et journaux.
- L'installateur détecte et remplace les V3.07/V3.08 après confirmation.
- L'installateur V3.09 met à niveau une précédente V3.09.
- L'installateur inclut le runtime .NET 8 et les notices françaises/anglaises.
- Une somme SHA-256 est régénérée après chaque construction de l'installateur.

## Plateformes

- Windows x64 : installateur autonome contenant le runtime .NET 8 et les notices FR/EN.
- macOS Apple Silicon : DMG autonome pour les Mac M1 et suivants.
- macOS Intel : DMG autonome pour les Mac Intel 64 bits.
- Le moteur XML et ses garde-fous sont partagés entre Windows et macOS.
- L'interface Mac conserve l'atelier visuel Avalonia avec sélection multiple, glisser-déposer et matrice. Le nouvel onglet Windows `Easy patch` n'est pas reproduit à l'identique sur Mac dans cette version.
- Les DMG sont signés ad hoc mais ne sont pas notariés par Apple ; le premier lancement peut nécessiter un clic droit sur l'application puis `Ouvrir`.

## Documentation et présentation

- Notice complète illustrée en français et en anglais pour l'ensemble du logiciel.
- Démarrage rapide PDF dans les deux langues.
- Présentation générale V3.09 en français et en anglais, avec sous-titres lisibles intégrés et fichiers SRT séparés.
- Captures et vidéos réalisées uniquement avec un preset synthétique anonymisé.

## Validation automatisée

- 100 tests Core et contrats Windows, plus 9 tests d'interface Mac sans écran, réussissent en Release.
- Les builds WPF et Avalonia Release réussissent sans warning.
- Les tests couvrent la sélection, les plages, les conflits, le remplacement, l'annulation, le rollback, la persistance, la matrice et l'intégration au détail machine.
- Le garde-fou XML, les sauvegardes atomiques, les namespaces, les interfaces réseau et les presets synthétiques restent couverts.

## Limites importantes

- L'application ne pilote pas un réseau Dante en direct.
- Elle n'utilise aucun SDK ou API Audinate.
- Elle travaille uniquement sur des fichiers XML hors ligne.
- La compatibilité dépend de la structure réelle du preset.
- Les noms TX dupliqués sur un même device restent ambigus et doivent être corrigés avant une nouvelle affectation.
- L'application vérifie la cohérence du XML généré, mais seul un import réussi dans Dante Controller valide définitivement le fichier.
- L'installateur Windows n'est pas signé avec un certificat Authenticode public ; vérifiez la somme SHA-256 publiée.
- Les DMG macOS ne sont pas notariés avec un compte Apple Developer ; vérifiez également leurs sommes SHA-256 publiées.

## Dépôt public

https://github.com/Mamat79/DanteConfigEditorV3

## Origine et crédit

Le projet a commencé comme un petit éditeur XML personnel écrit manuellement par Mamat. Les agents de développement actuels ont ensuite permis une évolution importante de l'interface, des garde-fous, des tests, de la documentation et du packaging, sous la direction fonctionnelle de Mamat.

**By Mamat et ses agents**

Remerciement à **Charles Bouticourt** pour l'idée de la fonction `Atomic Bomb`.
