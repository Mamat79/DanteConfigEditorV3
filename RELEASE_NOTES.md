# Dante Config Editor V3.08 Beta

## Statut

Version bêta Windows en cours de développement. Il s'agit d'un outil tiers non officiel Audinate et cette application n'est pas exempte de bugs.

La V3.08 Beta est désormais la version Windows courante de la branche `main` et remplace la V3.07 dans le développement actif. La V3.07 reste disponible dans l'historique et demeure la version Mac distribuée. Travaillez toujours sur une copie de vos fichiers XML Dante et validez le résultat dans les outils Dante officiels avant toute utilisation réelle.

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
- Après une prévisualisation, `Ajouter au lot` permet d'accumuler plusieurs opérations et `Appliquer ces changements` les valide immédiatement.
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

## Installation parallèle avec la V3.07

- Nouvel AppId réservé à la famille V3.08.
- Dossier par défaut : `C:\Program Files\Dante Config Editor V3.08\`.
- Groupe Menu Démarrer et raccourci `Dante Config Editor V3.08 Beta` distincts.
- Données locales V3.08 séparées : langue, fichiers récents, récupération et journaux.
- L'installation ou la mise à niveau V3.08 ne remplace pas la V3.07.
- Une nouvelle V3.08 Beta met à niveau uniquement une précédente V3.08.
- L'installateur inclut le runtime .NET 8 et les notices françaises/anglaises.
- Une somme SHA-256 est régénérée après chaque construction de l'installateur.

## Plateformes

- Cette bêta V3.08 est uniquement construite et testée sur Windows x64.
- Aucun build automatique ni paquet macOS V3.08 n'est publié pour le moment ; le workflow existant reste manuel.
- La version Mac reste la V3.07 stable.
- Le workflow et les DMG Mac seront ajoutés lors de la future Release officielle V3.08.

## Validation automatisée

- 88 tests Core et contrats Windows réussissent localement.
- Le build WPF Release réussit sans warning.
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

## Dépôt public

https://github.com/Mamat79/DanteConfigEditorV3

By Mamat
