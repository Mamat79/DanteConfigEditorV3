# Dante Config Editor V3.08 Beta

Branche bêta Windows pour éditer hors ligne des fichiers XML de configuration Dante.

> **Statut : V3.08 Beta Windows. Outil tiers non officiel Audinate.**
> Cette branche est en développement et peut contenir des bugs. La V3.07 reste la Release stable recommandée pour Windows et macOS. Travaillez toujours sur une copie et validez le XML généré dans les outils Dante officiels.

## Ce que fait l'application

- Ouvre des fichiers XML de configuration Dante hors ligne.
- Affiche les devices, canaux TX/RX, latences, mode réseau et preferred master.
- Renomme les devices.
- Supprime un device et nettoie les subscriptions/patchs qui pointent vers lui.
- Ajoute les devices d'un second XML dans le projet ouvert, avec import des machines uniques même en présence de doublons.
- Propose de renommer automatiquement ou manuellement les machines en doublon pendant l'import XML.
- Renomme les canaux TX/RX.
- Renomme des plages de canaux en série.
- Réinitialise les noms de canaux.
- Modifie les paramètres réseau et audio exposés par les fichiers XML reconnus.
- Affiche une page Patch pour visualiser et modifier les abonnements RX vers TX lorsque le format XML le permet.
- Ajoute l'onglet Windows `Easy patch` avec RX à gauche, TX à droite, navigation rapide entre machines, sélection multiple, prévisualisation, plages strictes, résolution explicite des conflits et matrice interactive.
- Met à jour les patchs RX quand un canal TX utilisé est renommé.
- Crée une sauvegarde du fichier source et de toute destination existante avant sauvegarde.
- Sauvegarde via un fichier temporaire relu puis un remplacement atomique de la destination.
- Vérifie la compatibilité XML de base avant sauvegarde.
- Bloque les modifications interdites des zones sensibles du XML Dante grâce au garde-fou de changements XML.
- Permet de modifier l'interface après ouverture d'un XML, mais impose une sauvegarde sous un autre nom pour protéger le fichier original.
- Propose un choix de langue Français / Anglais dans l'application.
- Affiche les latences Dante en ms tout en conservant les valeurs XML brutes.
- Affiche et modifie les sample rates et les bits par échantillon machine par machine ou globalement.
- Signale les projets qui mélangent plusieurs sample rates ou plusieurs encodages.
- Peut remettre les adresses IPv4 des machines en automatique quand le XML contient ces informations.
- Peut fixer une IP manuelle sur une machine ou générer une plage d'IP fixes en série.
- Ouvre une fiche machine au double-clic pour modifier les formats, l'IP, les canaux TX/RX et les patchs de ses entrées RX.
- Filtre, sélectionne et verrouille des machines pour contrôler précisément les actions globales.
- Réinitialise les patchs RX/TX d'une machine, ou seulement ses RX / seulement ses TX.
- Ouvre une notice rapide PDF et une notice complète PDF dans la langue active de l'application.
- Affiche des info-bulles sur les principales fonctions.
- Permet de cliquer sur un point important pour filtrer immédiatement les machines concernées.
- Filtre les machines modifiées et affiche une comparaison détaillée avant / après.
- Enregistre automatiquement une copie de récupération asynchrone après un court délai suivant les modifications.
- Applique des profils rapides 48/96 kHz, 24 bit, latence, mode réseau et IP automatique à la cible choisie.
- Affiche une page `Santé du fichier` avec les points à vérifier.
- Annule la dernière action.
- Recherche globalement dans les machines, canaux et patchs.
- Exporte un rapport TXT ou PDF.
- Exporte un patchbook TXT organisé par device RX.
- Exporte un patchbook CSV en lecture seule.
- Affiche un rapport compatibilité Dante Controller et une topologie simple TX vers RX.
- Compare le fichier ouvert avec un autre XML.
- Renomme des canaux en série.
- Signale en gros les mélanges redondant/daisychain, les machines détectées en IP fixe et les formats audio mélangés.
- Affiche les fichiers récents.
- Propose un thème sombre et un thème clair.

## Limites importantes

- L'application ne pilote pas un réseau Dante en direct.
- Elle n'utilise pas de SDK ou d'API Audinate.
- Elle travaille uniquement sur des fichiers XML hors ligne.
- Elle ne contourne aucune protection Audinate et ne réimplémente pas de protocole propriétaire.
- La compatibilité dépend de la structure réelle du XML fourni.
- Certains champs de patch peuvent ne pas être détectés si le fichier utilise une structure différente de celles actuellement reconnues.
- `subscribed_device="."` est interprété comme une source locale, c'est-à-dire le device RX lui-même.
- Un device TX absent du preset est un avertissement, pas forcément une erreur bloquante, car un preset peut être partiel.
- Les Dante Id sont préservés. L'interface écrit `Dante Id`, mais l'attribut XML reste exactement `danteId`.

## Télécharger / installer

Le fichier recommandé est fourni dans le dossier `dist` local et dans les Releases GitHub :

- `dist/DanteConfigEditorV3_08_Beta_Installer.exe` : installateur Windows bêta autonome, avec installation par défaut dans Program Files, choix du dossier, raccourcis et désinstallation propre.

La version autonome inclut le runtime .NET nécessaire. Sur une machine Windows x64, il ne devrait pas être nécessaire d'installer .NET séparément pour utiliser l'application.

### macOS

La V3.08 Beta n'est pas compilée ni distribuée pour macOS. Les utilisateurs Mac doivent rester sur la Release stable V3.07, qui fournit deux DMG autonomes :

- `DanteConfigEditorV3_macOS_AppleSilicon.dmg` pour les Mac M1, M2, M3, M4 et suivants ;
- `DanteConfigEditorV3_macOS_Intel.dmg` pour les Mac Intel 64 bits.

Ouvrir le DMG, puis glisser `Dante Config Editor` dans `Applications`. Le runtime .NET 8 et les notices FR/EN sont inclus.

La distribution Mac n'est pas encore notariée avec un compte Apple Developer. Au premier lancement, faire un clic droit sur l'application dans `Applications`, choisir `Ouvrir`, puis confirmer l'ouverture. Les détails de compilation, signature et notarisation sont documentés dans `MACOS_BUILD.md`.

L'installateur contient uniquement l'application autonome et la documentation utilisateur. Les sources du projet ne sont pas installees sur la machine de l'utilisateur.

En fin d'installation, il peut proposer d'ouvrir les release notes, le quick start PDF et la notice complète PDF dans la langue choisie dans l'installateur. Les quatre PDF français/anglais restent installés et accessibles depuis le menu Démarrer.

Notices fournies :

- `QuickStart_DanteConfigEditorV3_FR.pdf` et `Notice_DanteConfigEditorV3_FR.pdf` ;
- `QuickStart_DanteConfigEditorV3_EN.pdf` et `Notice_DanteConfigEditorV3_EN.pdf`.

Dans l'application, les boutons d'aide ouvrent automatiquement les fichiers FR ou EN selon la langue active.

La V3.08 Beta utilise un AppId, un dossier Program Files, un menu Démarrer et un stockage local distincts. Elle peut donc cohabiter avec la V3.07. Une nouvelle V3.08 Beta remplace uniquement une précédente installation V3.08.

## Version distribuée

- La V3.07 est la version stable actuellement recommandée et porte le tag `v3.07`.
- La branche `3.08-beta` contient la bêta Windows en cours de validation.
- Aucun paquet Mac V3.08 ne sera produit avant la future Release officielle.
- Les anciens tags et Releases restent de l'historique ; ils ne doivent pas être utilisés à la place de la publication courante.
- L'historique fonctionnel reste consultable dans Git et dans `CHANGELOG_V3.md`.

## Utilisation rapide

1. Lancer l'application.
2. Cliquer sur `Ouvrir XML`.
3. Sélectionner une copie du fichier de configuration Dante.
4. Vérifier les devices et paramètres détectés.
5. Modifier les champs souhaités.
6. Dans l'onglet `Easy patch`, choisir les machines RX et TX, sélectionner les canaux, puis prévisualiser la sélection ou une plage.
7. Choisir explicitement d'annuler, d'ignorer ou de remplacer les patchs existants, préparer le lot, puis cliquer sur `Appliquer à la configuration`.
8. Si besoin, utiliser `Ajouter XML au projet` pour importer les devices d'un autre export XML.
9. Sauvegarder sous un nouveau nom.
10. Valider le fichier généré dans l'outil Dante officiel approprié avant usage terrain.

## Nouveautés V3.08 Beta

- Sélection multiple indépendante des canaux TX et RX avec `Ctrl` ou `Maj`, plus commandes `Tout sélectionner`.
- Appariement un-à-un quand les quantités sont égales et diffusion autorisée d'un seul TX vers plusieurs RX.
- Blocage de plusieurs TX vers un seul RX et de toute sélection multiple aux quantités incohérentes.
- Patch par plage avec premier TX, premier RX et nombre exact ; aucune application partielle si la plage dépasse les canaux disponibles.
- Prévisualisation avant préparation : création, remplacement ou ligne inchangée.
- Traitement explicite des RX déjà patchés : annuler le lot, ignorer les conflits ou remplacer les subscriptions.
- Déconnexion groupée de plusieurs RX et matrice unitaire TX vers RX conservée.
- Changements préparés en mémoire puis appliqués au XML en un seul lot et une seule étape d'annulation.
- Onglets principaux distincts : `Patch` conserve l'éditeur classique et `Easy patch` accueille le nouveau système.
- Disposition `Easy patch` avec machines et canaux RX à gauche, machines et canaux TX à droite.
- Menus et flèches précédent/suivant pour parcourir rapidement les machines RX et TX.
- Sélecteur de machine dans `Détail machine`, avec confirmation appliquer/abandonner/annuler si des changements sont en attente.
- Onglet `Patch RX` dans `Détail machine`, limité aux entrées de la machine ouverte.
- Identité d'installation, dossier Program Files, raccourcis et données locales séparés de la V3.07.
- V3.08 Beta limitée à Windows ; le workflow et les paquets Mac seront ajoutés lors de la future Release officielle.
- 86 tests Core et contrats Windows validant notamment sélection, plages, conflits, rollback, persistance, onglet Easy patch et navigation du détail machine.

## Nouveautés V3.07

- Garde-fou XML associé aux machines par identité technique stable et non plus par leur nom visible.
- Blocage par défaut des chemins et balises XML inconnus.
- Comparaison XML sémantique qui tolère le réordonnancement de balises inchangées.
- Lecture, modification et sauvegarde des presets utilisant un namespace XML par défaut.
- `Enregistrer sous` atomique : l'ancienne destination reste intacte en cas d'échec et reçoit une copie de sécurité lors d'un remplacement.
- Le fichier créé par `Enregistrer sous` devient la référence de la session et de la récupération automatique.
- Les changements IPv4 ciblent uniquement l'interface principale et ne réécrivent plus implicitement le DNS ou une interface secondaire.
- Mutations groupées avec une seule reconstruction du modèle pour les réglages de détail et les actions globales.
- Récupération automatique asynchrone et temporisée ; historique d'annulation limité à 10 états.
- 67 tests de sécurité et de non-régression, dont les presets synthétiques de 10, 50 et 200 machines en 64 TX / 64 RX.
- Workflow GitHub Actions sur Windows et script `build.ps1` qui vérifie chaque code de retour.
- Installateur de mise à niveau unique : la V3.07 remplace la version déjà installée sans proposer de copie parallèle.
- Nouvelle interface macOS via Avalonia, avec les alertes placées dans la colonne latérale pour préserver la hauteur des tableaux.
- Sept tests d'interface sans écran vérifient la disposition, la navigation clavier, les tailles compactes et l'atelier de patch sur Mac.
- DMG autonomes distincts Apple Silicon et Intel, incluant .NET 8 et les notices françaises/anglaises.
- Les alertes importantes de la version Windows sont également déplacées dans la colonne latérale.
- Atelier de patch visuel partagé : sélection de plusieurs TX, affectation aux RX suivants, glisser-déposer, matrice et application en un seul lot annulable.
- Interface principale plus compacte ; le reset global des canaux est placé sous la latence globale pour réduire la hauteur du panneau.

## Validation et maintenance

- `TESTING.md` : commandes, résultats et mesures synthétiques ;
- `COMPATIBILITY_MATRIX.md` : niveau de preuve par structure XML ;
- `MANUAL_DANTE_CONTROLLER_TESTS.md` : checklist d'import réel ;
- `RC_VALIDATION.md` : preuves historiques de validation de la publication stable V3.07 ;
- `ACCESSIBILITY.md` : contrôles effectués et tests manuels restants ;
- `KNOWN_LIMITATIONS.md` : limites techniques et de distribution ;
- `ARCHITECTURE_REFACTORING.md` : extractions réalisées et suite prudente.

## Nouveautés V3.05

- Les alertes `Points à vérifier` ouvrent la liste des machines concernées et permettent de les filtrer dans le tableau principal.
- Le filtre `Modifiées uniquement` et le bouton `Avant / après` permettent de contrôler précisément les changements depuis l'ouverture du XML.
- Les messages dynamiques de ces nouvelles vues suivent le choix Français / Anglais de l'interface.
- Une récupération automatique protège les modifications non sauvegardées après une fermeture inattendue ; l'utilisateur choisit de restaurer ou d'abandonner la copie temporaire à la prochaine ouverture du même fichier.
- Des profils rapides 48/96 kHz et 24 bit combinent latence, IP automatique et, selon le profil, mode redondant ou daisychain.
- Les profils respectent la cible des actions globales et ignorent les machines verrouillées.
- Une suite de tests vérifie le chargement XML, les renommages et leurs patchs, les imports, la suppression, les profils, la récupération et la sauvegarde sécurisée.
- Un seul bouton `Appliquer les paramètres` valide ensemble le nom, le mode réseau, la latence et le preferred master de la machine sélectionnée.
- Les IP automatique/fixe, sample rate, bits et noms de canaux restent modifiables dans `Détail machine`.
- L'alerte `Points à vérifier` reste compacte et le bouton `Détails` affiche son texte complet.
- Le bouton `Réduire les réglages` agrandit le tableau des machines quand les panneaux supérieurs ne sont pas nécessaires.
- Les resets patch RX/TX, RX, TX et la suppression de machine sont regroupés sur une seule ligne.
- Import d'un second XML corrigé : les machines uniques sont importées même si d'autres machines du fichier sont en doublon.
- Gestion des doublons à l'import : choix entre import des uniques seulement, renommage automatique ou renommage manuel.
- Les patchs/subscriptions du XML importé suivent les nouveaux noms quand des machines importées sont renommées.
- Réglages sample rate et bits par échantillon par machine.
- Actions globales pour appliquer une sample rate ou des bits par échantillon à toutes les machines.
- Fonction pour remettre les adresses IPv4 en automatique, par machine ou globalement, quand le XML expose ces champs.
- Cible d'actions globales : toutes non verrouillées, sélection non verrouillée ou filtre affiché non verrouillé.
- Verrouillage de machines pour éviter qu'une action globale ne les modifie.
- Reset patch RX et reset patch TX séparés.
- Notices rapide et complète en français/anglais, sélectionnées automatiquement selon la langue de l'interface, et info-bulles intégrées.
- Comparaison XML en tableau.
- Installateur avec détection d'une version déjà installée.
- Listes rapides Sample rates, Bits et IP fixes.
- Avertissements renforcés si plusieurs sample rates ou plusieurs encodages coexistent dans le projet.
- Compatibilité de sauvegarde adaptée aux fonctions d'ajout/suppression de machines.
- Page Configuration plus lisible : alerte compacte, panneaux masquables, table des machines gardée visible et recherche globale avec message d'aide.
- Ouverture en plein écran, fiche machine au double-clic, preferred master cochable dans la table, IP fixe en série et reset patch RX/TX par machine.

## Nouveautés V3.04

- Passage en V3.04 dev.
- Interface Français / Anglais, modifiable à tout moment.
- Suppression d'un device avec nettoyage des subscriptions associées.
- Import d'un second XML dans le projet ouvert.
- Ajout d'un garde-fou de changements XML : seules les zones métier autorisées peuvent changer, les zones techniques Dante sensibles bloquent la sauvegarde.
- Ajout d'un rapport `Compatibilité Dante Controller` exportable/copier depuis l'écran Sécurité.
- Mode `Lecture seule` par défaut après ouverture d'un XML, avec bouton `Activer l'édition`.
- Correction des libellés utilisateur : `Dante Id` avec espace dans l'interface, les rapports et la documentation.
- Affichage des latences en ms : `250` devient `0,25 ms`, `1000` devient `1 ms`, tout en gardant la valeur XML brute.
- Prévisualisation des actions globales existantes avant application.
- Preferred master global rendu plus sûr : définir un seul device comme preferred master, ou retirer tous les preferred masters.
- Page Patch : mode simple/expert, colonne `Source complète`, choix TX avec Dante Id, protection des patchs vers devices absents.
- Patchbook enrichi et export CSV lecture seule.
- Page Santé enrichie avec mode Lecture seule/Édition, latences en ms, samplerates et encodages.
- Ajout d'une topologie simple des sources les plus utilisées, receivers les plus patchés et relations TX vers RX.
- Script installateur plus portable pour trouver Inno Setup.
- Option `installer/build_source_zip.ps1` pour créer une archive source propre.

## Nouveautés V3.03

- Page `Santé du fichier` : synthèse du preset, statistiques TX/RX, patchs actifs/libres/locaux, preferred masters, modes réseau, IP fixes et tableau filtrable des points à vérifier.
- Contrôles de compatibilité XML Dante Controller avant sauvegarde : racine `<preset>`, version, devices, canaux `txchannel` / `rxchannel`, attributs XML `danteId` / `mediaType` et balises techniques importantes.
- Gestion correcte des patchs locaux `subscribed_device="."` : affichage comme source locale, pas comme conflit, et conservation du `.` à la sauvegarde quand le fichier source l'utilise.
- Utilisation du Dante Id comme identifiant métier principal des canaux, sans renumérotation.
- Page `Patch` plus lisible : TX brut/résolu/affiché, type de patch, warnings et filtres par état.
- Export `Patchbook TXT` organisé par device RX.
- Comparaison XML plus lisible des canaux et patchs.
- Correction de la lisibilité des listes et menus : fond blanc et texte noir, y compris en thème sombre.
- Correction de la propagation des patchs lors de la réinitialisation des canaux TX.
- Ajout du choix canal début / canal fin pour limiter le renommage en série à une plage.

## Nouveautés V3.02

- Annulation de la dernière action.
- Résumé avant sauvegarde plus lisible.
- Recherche globale.
- Export du rapport en TXT ou PDF.
- Détection visuelle plus forte des conflits de patch.
- Renommage en série des canaux.
- Liste des fichiers récents.
- Comparaison avec un autre XML.
- Sauvegarde sécurisée par fichier temporaire relu avant remplacement.

## Nouveautés V3.01

- Colonne `Projet` plus compacte.
- Navigation par onglets uniquement en haut.
- Page `Configuration` réorganisée pour limiter le défilement vertical.
- Page `Patch` avec filtres TX/RX explicites.
- Ajout des choix `Tous les émetteurs` et `Tous les récepteurs`.
- Renommage RX et TX séparés dans la page `Patch`.

## Renommage des canaux et patchs

Quand un canal TX est renommé, y compris lors d'un renommage en série sur une plage, l'application parcourt les abonnements RX reconnus et remplace l'ancien nom du canal par le nouveau partout où il est utilisé avec le même device TX.

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

Pour lancer les tests automatiques :

```powershell
.\tests\run-tests.ps1
```

## Sécurité des fichiers

- Toujours travailler sur une copie.
- Ne jamais écraser un fichier de production sans test.
- Conserver les backups générés automatiquement.
- Tester le fichier final dans les outils Dante officiels avant exploitation.
- L'application vérifie la cohérence du XML généré, mais la validation définitive doit être faite par un import dans Dante Controller avant toute utilisation en production.

## Crédit

By Mamat
