# Dante Config Editor V3.06 Beta

## Statut

Version beta en developpement. Cette application n'est pas exempte de bugs.

Travaillez toujours sur une copie de vos fichiers XML Dante et validez le resultat dans les outils Dante officiels avant utilisation sur une installation reelle.

## Nouveautes principales

- Version 3.06 beta.
- Premiere version macOS, avec une interface Avalonia partageant le meme moteur XML que l'application Windows.
- DMG autonomes Apple Silicon et Intel avec .NET 8 et notices FR/EN inclus.
- Alertes importantes placees dans la colonne laterale sur macOS et Windows pour conserver la hauteur des tableaux.
- Les DMG de cette beta utilisent une signature ad hoc et ne sont pas encore notaries par Apple ; le premier lancement se fait par clic droit puis `Ouvrir`.
- Garde-fou XML durci : identité stable des machines, chemins inconnus bloqués par défaut et absence de faux positif lors d'un simple réordonnancement de balises.
- Prise en charge des XML avec namespace par défaut.
- Sauvegarde atomique avec conservation de l'ancienne destination en cas d'erreur et backup de la destination remplacée.
- La nouvelle destination devient la référence de la session et de la récupération automatique.
- Ciblage de l'interface IPv4 principale sans modification implicite du DNS ou de l'interface secondaire.
- API de modification groupée, récupération asynchrone temporisée et historique d'annulation limité à 10 états.
- 38 tests verts, presets synthétiques 10/50/200 machines en 64 TX / 64 RX, et CI Windows.
- Les alertes `Points a verifier` sont navigables : elles affichent et filtrent les machines concernees.
- Nouveau filtre `Modifiees uniquement` et comparaison detaillee `Avant / apres` des machines, canaux et patchs.
- Alertes dynamiques et comparaison avant/apres traduites en francais et en anglais.
- Recuperation automatique des modifications non sauvegardees apres une fermeture inattendue.
- Profils rapides 48/96 kHz, 24 bit, latence, IP automatique et mode reseau optionnel, appliques a la cible choisie sans toucher aux machines verrouillees.
- Suite de tests de non-regression sur des XML anonymises, completee par un controle local facultatif de vrais exports Dante.
- Reglages de la machine selectionnee simplifies : le nom, le mode reseau, la latence et le preferred master sont appliques ensemble avec un seul bouton `Appliquer les parametres`.
- Le reglage IP automatique ou fixe reste accessible dans `Detail machine`, avec les formats audio et les noms des canaux.
- Les commandes reset patch RX/TX, reset RX, reset TX et suppression sont regroupees sur une seule ligne.
- Le bandeau `Points a verifier` est maintenant compact, avec un bouton `Details` pour lire le message complet.
- Bouton `Reduire les reglages` pour masquer les panneaux superieurs et agrandir instantanement le tableau des machines.
- Le resume de fichier duplique a ete retire de la page Configuration afin de conserver davantage de lignes visibles, y compris lorsqu'une alerte est presente.
- Import d'un second XML corrige : les machines uniques sont importees meme si le fichier contient aussi des noms deja presents.
- Fenetre de resolution des doublons a l'import : import des uniques seulement, renommage automatique ou renommage manuel.
- Mise a jour des subscriptions/patchs importes quand une machine importee est renommee.
- Reglage sample rate et bits par echantillon machine par machine.
- Actions globales pour appliquer sample rate et bits par echantillon a toutes les machines.
- Remise des adresses IPv4 en automatique par machine ou globalement quand le XML expose ces champs.
- IP fixe manuelle par machine et IP fixes en serie pour toutes les machines ayant une interface IPv4 modifiable.
- Fenetre detail machine au double-clic : nom, formats audio, IP et canaux TX/RX.
- Fenetre detail machine : champs d'adresse IP fixe alignes avec le choix `Fixe`, et champs grises en mode automatique.
- Preferred master cochable directement dans la table des machines.
- Reset patch RX/TX d'une machine : deconnecte ses entrees RX et retire les patchs qui utilisent ses TX.
- Listes rapides Sample rates, Bits et IP fixes.
- Avertissements visibles si plusieurs sample rates ou plusieurs encodages coexistent dans le preset.
- Page Configuration plus lisible : reglages defilables, table des machines gardee visible et recherche globale avec message d'aide.
- Page Configuration compactee : actions rapides machine, bouton detail machine et actions globales rangees par onglet pour eviter les blocs coupes.
- Correction du fond blanc des onglets internes en theme sombre, notamment dans Actions globales.
- Retrait de l'action globale Preferred master securise, jugee confuse et peu utile.
- Ouverture de l'application en plein ecran pour une lecture plus confortable.
- Tableau machines avec selection multiple, filtre rapide et colonne Lock.
- Actions globales ciblables sur toutes les machines non verrouillees, la selection ou le filtre affiche.
- Machines verrouillees ignorees par les actions globales.
- Boutons pour selectionner les machines visibles, verrouiller/deverrouiller la selection et effacer la selection.
- Renommage en serie avance avec modeles `{00}`, `{000}`, `{n}` et `{device}`.
- Reset patch RX et reset patch TX separes, en plus du reset RX/TX complet de la machine.
- Rapport final avant import Dante et historique d'actions accessibles dans l'application.
- Comparaison XML en tableau, en plus du resume texte.
- Notices rapide et complete en francais/anglais, ouvertes automatiquement selon la langue active et integrees a l'installateur.
- Info-bulles ajoutees sur les principales commandes.
- L'installateur detecte une version deja installee et propose de la remplacer / mettre a jour, sans copie parallele.
- Garde-fou de modifications XML : la sauvegarde est bloquee si une zone technique Dante sensible change par accident.
- Rapport compatibilite Dante Controller.
- Mode Lecture seule par defaut apres ouverture du XML, avec bouton Activer l'edition.
- Libelles utilisateur corriges : Dante Id avec espace. L'attribut XML reste danteId.
- Latences affichees en ms, valeurs XML brutes conservees.
- Previsualisation des actions globales.
- Page Patch avec mode simple/expert et colonne Source complete.
- Patchbook TXT enrichi et export CSV lecture seule.
- Page Sante enrichie et vue Topologie simple.
- Script installateur plus portable.
- Correction des accents dans l'export PDF.
- Ajout de la mention `Fait avec le soft Dante Config Editor V3.06 - version 3.06-beta - By Mamat` dans les exports TXT/PDF.
- Correction des boutons d'action après ouverture d'un XML : l'interface reste modifiable, mais l'enregistrement impose un nouveau nom de fichier.
- Sélecteur Français / Anglais directement dans l'interface, modifiable à tout moment.
- Suppression d'une machine avec nettoyage des subscriptions/patchs qui pointent vers elle.
- Import d'un second XML dans le projet ouvert pour assembler plusieurs exports, avec refus des noms de machines en doublon.
- Page Sante du fichier avec synthese du preset, statistiques TX/RX, patchs actifs/libres/locaux, preferred masters, modes reseau, IP fixes et tableau filtrable des points a verifier.
- Controle de compatibilite XML Dante Controller avant sauvegarde : racine `preset`, version, devices, canaux TX/RX, attributs XML `danteId` / `mediaType` et balises techniques importantes.
- Gestion correcte des patchs locaux `subscribed_device="."` : affichage comme source locale, pas comme conflit, et conservation du `.` a la sauvegarde quand le fichier source l'utilise.
- Utilisation du Dante Id comme identifiant principal des canaux, sans renumerotation.
- Page Patch plus lisible : TX brut, TX resolu, TX affiche, type de patch, warnings et filtres par etat.
- Export Patchbook TXT organise par device RX.
- Comparaison XML plus lisible des canaux et patchs.
- Correction de la lisibilite des listes et menus : fond blanc et texte noir, y compris en theme sombre.
- Correction de la propagation des patchs quand les canaux TX sont reinitialises.
- Choix canal debut / canal fin pour limiter le renommage en serie a une plage.
- Alerte visible si le fichier melange redondant/daisychain ou contient des machines detectees en IP fixe.
- Annulation de la derniere action.
- Resume avant sauvegarde plus lisible.
- Recherche globale dans machines, canaux et patchs.
- Export du rapport en TXT ou PDF.
- Conflits de patch plus visibles dans la table.
- Renommage de canaux en serie, avec plage configurable.
- Liste des fichiers recents.
- Comparaison avec un autre XML.
- Sauvegarde securisee par fichier temporaire relu avant remplacement.
- Ergonomie revue suite au test video du 2026-07-05.
- Colonne Projet reduite a un resume compact.
- Onglets gardes uniquement en haut de l'application.
- Page Configuration reorganisee horizontalement pour limiter le defilement vertical.
- Page Patch avec filtres TX/RX explicites et choix `Tous les emetteurs` / `Tous les recepteurs`.
- Renommage RX et TX separes dans la page Patch.
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

Si une version de Dante Config Editor est deja installee, l'assistant le signale au demarrage et propose de remplacer / mettre a jour l'installation existante. Refuser ferme l'installateur sans creer de version parallele.

En fin d'installation, l'assistant peut ouvrir :

- les release notes ;
- la notice rapide PDF dans la langue de l'installateur ;
- la notice complete PDF dans la langue de l'installateur.

Les quatre PDF francais/anglais sont installes et restent accessibles depuis l'application et le menu Demarrer.

## Limites connues

- L'application ne pilote pas un reseau Dante en direct.
- Elle n'utilise pas de SDK ou API Audinate.
- Elle travaille uniquement sur des fichiers XML hors ligne.
- `subscribed_device="."` est interprete comme une source locale.
- Les devices TX absents du preset sont des avertissements, car certains presets peuvent etre partiels.
- L'application verifie la coherence du XML genere, mais la validation definitive doit etre faite par un import dans Dante Controller avant toute utilisation en production.
- Les Dante Id sont preserves. L'interface affiche Dante Id, l'attribut XML reste danteId.
- L'application demarre en lecture seule pour eviter les modifications accidentelles.
- Certains formats XML Dante peuvent ne pas etre totalement reconnus.
- La page Patch depend des champs d'abonnement presents dans le fichier.

## Depot GitHub

https://github.com/Mamat79/DanteConfigEditorV3

Seule la publication V3.06 courante est conservee dans les Releases. L'historique des versions reste disponible dans Git et dans le changelog.
