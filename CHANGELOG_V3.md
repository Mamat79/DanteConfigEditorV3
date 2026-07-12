# Changelog V3

## 2026-07-12 - Version 3.08 Beta Windows

- Promotion de la V3.08 Beta dans la branche `main` en remplacement de la V3.07 pour le développement Windows courant ; le tag et la Release V3.07 restent conservés.
- Conservation du workflow macOS en lancement manuel uniquement tant que la V3.08 n'est pas déclarée Release officielle.
- Création de la branche `3.08-beta` depuis la V3.07 officielle, sans workflow ni compilation macOS.
- Nouvel AppId V3.08, dossier `Program Files`, groupe Menu Démarrer et stockage local distincts pour permettre la coexistence avec la V3.07.
- Ajout d'un moteur central de patch par sélection avec identités RX stables basées sur le device et le Dante Id.
- Appariement un-à-un pour des sélections TX/RX de même taille et diffusion d'un TX vers plusieurs RX.
- Blocage de plusieurs TX vers un RX, des tailles multiples incohérentes et des plages incomplètes.
- Prévisualisation des créations, remplacements et lignes inchangées avant préparation du lot.
- Suppression de l'ascenseur général d'Easy patch : les machines RX/TX restent visibles et les listes défilent dans leur propre zone.
- Prévisualisation compacte, masquée lorsqu'elle est vide, avec colonnes RX, source actuelle, nouvelle source et action toujours lisibles.
- Ajout de deux parcours : `Appliquer` directement une sélection ou une plage, ou `Prévisualiser` puis `Ajouter au lot` / `Appliquer ces changements`.
- Résolution explicite des conflits : annuler le lot, ignorer les RX déjà patchés ou remplacer leurs subscriptions.
- Déconnexion groupée de plusieurs RX, matrice unitaire conservée et suppression de la dépendance au glisser-déposer.
- Ajout d'un patch par plage avec premier TX, premier RX et quantité exacte.
- Ajout d'un véritable onglet principal `Easy patch` à côté de l'onglet classique `Patch`.
- Réorganisation de l'outil avec RX à gauche, TX à droite et navigation précédent/suivant entre machines.
- Ajout d'un sélecteur de machine en haut de `Détail machine`, avec protection des modifications non appliquées.
- Intégration de l'atelier dans `Détail machine`, avec RX verrouillé sur la machine ouverte et application avant les renommages.
- Application finale des patchs en un seul lot annulable et persistance contrôlée après sauvegarde/rechargement.
- Extension de la suite Windows à 88 tests ; build WPF sans warning.
- Génération automatique d'une somme SHA-256 fraîche après chaque construction de l'installateur.

## 2026-07-11 - Version 3.07 officielle

- Promotion de la V3.07 en version stable officielle du projet, sans prétendre à une publication officielle Audinate.
- Retrait du suffixe `Beta` dans les interfaces Windows/macOS, les métadonnées, les notices et l'installateur.
- Correction du mode d'affichage Expert de la page Patch : les colonnes TX détaillées apparaissent désormais réellement.
- Extension de la suite à 67 tests Core et maintien des 7 tests d'interface Mac headless.
- Conservation du même AppId : l'installateur V3.07 remplace la V3.07 Beta au lieu de créer une copie parallèle.
- Les nouvelles fonctions de patch par sélection et par série sont réservées à la branche `3.08-beta`.

## 2026-07-11 - Version 3.07 Beta

- Extraction d'un projet `DanteConfigEditor.Core` partagé par les interfaces Windows et macOS.
- Ajout d'une interface macOS Avalonia reprenant les workflows de configuration, patch, santé du fichier, sécurité, exports et notices.
- Ajout de deux tests Avalonia headless pour la structure du rail latéral et l'affichage réel des alertes.
- Placement des alertes importantes dans la colonne latérale sur macOS et Windows.
- Ajout de bundles `.app` et de DMG autonomes Apple Silicon / Intel, avec runtime .NET 8 et documentation FR/EN inclus.
- Ajout d'une signature ad hoc pour vérifier l'intégrité locale ; la notarisation Apple reste une limite connue de cette bêta.
- Promotion de l'unique version Windows/macOS en V3.07 Beta, sans branche de version parallèle.
- Ajout d'un atelier de patch visuel Windows/macOS avec filtres par device, sélection TX multiple, affectation séquentielle, glisser-déposer et matrice TX vers RX.
- Les changements du patch visuel sont préparés sans toucher au projet, puis appliqués en un lot unique et une seule étape d'annulation.
- Refus des affectations visuelles ambiguës quand plusieurs TX d'un même device portent le même nom.
- Réorganisation compacte de l'en-tête et des panneaux ; le reset global des canaux est déplacé sous la latence globale.
- Extension de la suite à 63 tests Core et 7 tests d'interface Mac headless.
- Ajout du pack de validation non destructif, de la matrice de compatibilité et des documents de validation, accessibilité, architecture et limites connues.

## 2026-07-10 - Version 3.06 Beta

- Remplacement de l'identité XML basée sur le nom par une association stable utilisant en priorité `instance_id/device_id`, puis les identifiants techniques de repli.
- Blocage par défaut des chemins XML inconnus et maintien du blocage des attributs techniques `danteId`, `mediaType` et identifiants de machine.
- Comparaison des balises par contenu et identité plutôt que par position, afin de tolérer leur simple réordonnancement.
- Prise en charge des XML utilisant un namespace par défaut, sans création de balises hors namespace pendant les modifications.
- `SaveAs` rendu atomique avec fichier temporaire unique, backup de la destination remplacée et injection d'erreur testée avant validation finale.
- La destination sauvegardée devient la nouvelle référence de la session et de la récupération automatique.
- Ciblage exclusif de l'interface IPv4 principale ; conservation du DNS, de la passerelle non demandée et des interfaces secondaires.
- Ajout d'une API de mutations groupées avec un seul `ReloadModel` pour les réglages machine et les actions globales.
- Récupération automatique déplacée hors du thread UI après une temporisation de 750 ms.
- Limitation de la pile d'annulation à 10 snapshots XML.
- Ajout d'un workflow GitHub Actions `windows-latest` et vérification stricte des codes retour dans `build.ps1`.
- Passage de l'application, des exports et de l'installateur en V3.06 Beta.
- Suppression de l'option d'installation parallèle : l'installateur V3.06 remplace la version existante avec le même AppId et le même dossier par défaut.
- Extension de la suite à 38 tests, incluant les garde-fous, l'atomicité, la récupération, les alias de subscription, les namespaces, les deux interfaces IPv4 et les presets 10/50/200 en 64x64.

## 2026-07-10 - Version 3.05 - mise à jour

- Passage du statut public et des métadonnées de `dev` à `beta`, sans changer le numéro V3.05.
- Ajout des notices rapide et complète en français et en anglais ; l'application ouvre automatiquement la langue active.
- Ajout d'alertes navigables qui filtrent les machines concernées depuis `Points à vérifier`.
- Ajout du filtre `Modifiées uniquement` et d'une comparaison détaillée `Avant / après` des machines, canaux et patchs.
- Traduction complète en français/anglais des alertes dynamiques et des lignes de comparaison avant/après.
- Ajout d'une récupération automatique des modifications non sauvegardées, avec détection d'un XML original modifié depuis la copie temporaire.
- Ajout de six profils rapides combinant sample rate, bits, latence, IP automatique et mode réseau optionnel.
- Les profils rapides utilisent la cible d'actions globales et respectent le verrouillage des machines.
- Ajout d'une suite de tests de non-régression sur des fixtures XML anonymisées et d'un contrôle facultatif des vrais exports locaux.
- Retrait des binaires V2 du dépôt et des anciennes Releases V3.03/V3.04 ; la V3.05 reste l'unique publication téléchargeable.
- Remplacement des boutons séparés de nom, mode réseau, latence et preferred master par un bouton unique `Appliquer les paramètres`.
- Conservation de la configuration IP automatique/fixe dans la fenêtre `Détail machine`.
- Regroupement sur une seule ligne des resets patch RX/TX, RX, TX et de la suppression de machine.
- Transformation du bandeau `Points à vérifier` en alerte compacte sur une ligne, avec accès au message complet par `Détails`.
- Ajout d'un bouton `Réduire les réglages` / `Afficher les réglages` pour donner plus de hauteur au tableau des machines.
- Suppression du résumé de fichier dupliqué au-dessus de la page Configuration.
- Vérification visuelle à 1920 x 1080 : plusieurs lignes du tableau restent visibles avec une alerte de sample rates mélangées.
- Vérification fonctionnelle du bouton unique et de l'annulation groupée des changements.

## 2026-07-09 - Version 3.05

- Première publication des textes visibles et métadonnées V3.05.
- Correction de l'import d'un second XML : les machines uniques sont importées même si d'autres machines du fichier sont en doublon.
- Ajout d'une fenêtre de résolution des doublons à l'import, avec choix entre import des uniques seulement, renommage automatique et renommage manuel.
- Mise à jour des subscriptions/patchs importés quand une machine importée est renommée.
- Ajout des réglages sample rate et bits par échantillon machine par machine.
- Ajout des actions globales sample rate et bits par échantillon.
- Ajout d'une fonction pour remettre les adresses IPv4 en automatique, par machine ou globalement quand le XML expose ces champs.
- Ajout des listes rapides Sample rates, Bits et IP fixes.
- Ajout d'alertes visibles si plusieurs sample rates ou plusieurs encodages coexistent dans le preset.
- Adaptation de la validation de sauvegarde aux imports et suppressions de machines.
- Amélioration visuelle de la page Configuration : zone de réglages défilable, table des machines toujours visible et recherche globale plus lisible quand aucun résultat n'est affiché.
- Ouverture de l'application en plein écran pour que la page Configuration soit lisible dès le départ.
- Ajout d'une fenêtre de détail machine au double-clic, avec édition du nom, formats audio, IP et canaux TX/RX.
- Preferred master cochable directement depuis la table des machines.
- Ajout de l'IP fixe manuelle par machine et d'une action globale pour fixer une plage IP en série.
- Ajout d'un reset patch RX/TX par machine : déconnexion des entrées RX et suppression des patchs qui utilisent ses TX.
- Retouche de la page Configuration : bloc machine plus compact, bouton détail machine visible, actions globales séparées en onglets.
- Retouche de la fenêtre détail machine : les champs IP fixes sont alignés avec le mode `Fixe` et désactivés en mode automatique.
- Correction du style des onglets internes en thème sombre, pour éviter le fond blanc dans `Actions globales`.
- Retrait de l'action globale `Preferred master sécurisé`, trop ambiguë dans l'interface ; le réglage reste disponible machine par machine.
- Ajout d'un filtre machines, d'une sélection multiple et d'une colonne `Lock` dans la table des machines.
- Ajout d'une cible pour les actions globales : toutes les machines non verrouillées, sélection non verrouillée ou filtre affiché non verrouillé.
- Ajout des boutons `Sélectionner visibles`, `Effacer sélection`, `Verrouiller sélection` et `Déverrouiller sélection`.
- Ajout des resets séparés `Reset patch RX` et `Reset patch TX`, en plus du reset RX/TX complet.
- Ajout de modèles de renommage en série : `{00}`, `{000}`, `{n}` et `{device}`.
- Ajout d'un rapport final avant import Dante et d'un affichage de l'historique des actions.
- Ajout d'une fenêtre de comparaison XML en tableau.
- Ajout d'une notice rapide PDF, de raccourcis d'ouverture des notices dans l'application et de propositions d'ouverture en fin d'installation.
- Ajout d'info-bulles sur les commandes principales.
- Ajout d'une détection de version déjà installée dans l'installateur, avec choix entre remplacement/mise à jour et installation supplémentaire dans un autre dossier.

## 2026-07-07 - Version 3.04

- Passage des textes visibles et métadonnées en V3.04 dev.
- Ajout du garde-fou de modifications XML avant sauvegarde.
- Ajout du rapport compatibilité Dante Controller.
- Correction des libellés utilisateur `Dante Id` avec espace, sans renommer l'attribut XML `danteId`.
- Amélioration de l'affichage des latences Dante en ms, avec conservation des valeurs XML brutes.
- Ajout du mode Lecture seule / Édition.
- Prévisualisation des actions globales existantes.
- Sécurisation du preferred master global.
- Amélioration de la page Patch : mode simple/expert, colonne Source complète et choix TX avec Dante Id.
- Amélioration du Patchbook TXT et ajout d'un export CSV lecture seule.
- Amélioration de la page Santé du fichier.
- Ajout d'une vue Topologie simple.
- Amélioration du script installateur pour détecter Inno Setup dans plusieurs emplacements.
- Ajout d'un script d'archive source propre.
- Correction des accents dans l'export PDF et ajout de la signature/version dans les exports TXT/PDF.
- Correction du mode lecture seule : les boutons d'action restent utilisables après ouverture du XML, avec sauvegarde obligatoire sous un autre nom.
- Ajout d'un sélecteur Français / Anglais dans l'interface, mémorisé entre deux lancements.
- Ajout de la suppression d'une machine, avec nettoyage des subscriptions qui pointent vers cette machine.
- Ajout de l'import d'un second XML dans le projet ouvert, avec refus des noms de machines déjà présents.

## 2026-07-07 - Version 3.03 - mise à jour

- Ajout de la page `Santé du fichier` avec synthèse du preset, statistiques TX/RX, patchs actifs/libres/locaux, preferred masters, modes réseau, IP fixes et tableau filtrable des points à vérifier.
- Ajout d'un service de compatibilité XML Dante Controller avant sauvegarde : contrôle de la racine `<preset>`, de la version, des devices, des canaux `txchannel` / `rxchannel`, des attributs XML `danteId` / `mediaType` et des balises techniques importantes.
- Gestion correcte de `subscribed_device="."` comme source locale : affichage distinct, pas de faux conflit, conservation du `.` à la sauvegarde quand le fichier source l'utilise.
- Utilisation du Dante Id comme identifiant métier principal des canaux, avec conservation des attributs XML existants.
- Amélioration de la page `Patch` : colonnes TX brut/résolu/affiché, type de patch, warnings et filtres par état.
- Ajout d'un export `Patchbook TXT` organisé par device RX.
- Amélioration de la comparaison XML pour les canaux et patchs en s'appuyant sur les Dante Id.

## 2026-07-05 - Version 3.03

- Correction de la lisibilité des listes et menus déroulants : fond blanc et texte noir, y compris en thème sombre.
- Correction de la réinitialisation des canaux TX : les patchs RX reconnus sont maintenant mis à jour vers les nouveaux noms `1`, `2`, `3`, etc.
- Ajout du choix canal début / canal fin dans le renommage en série, pour ne renommer qu'une plage de canaux.
- Renforcement de la mise à jour des patchs TX pendant les renommages en série : les abonnements reconnus sont mis à jour en une seule passe depuis les anciens noms.
- Ajout d'un bandeau `Points à vérifier` pour les réseaux mélangeant redondant/daisychain et pour les machines détectées en IP fixe.
- Ajout d'une exclusion explicite du dossier `tmp` dans le projet `.NET` pour éviter que les fichiers temporaires de test soient compilés.

## 2026-07-05 - Version 3.02

- Ajout de l'annulation de la dernière action.
- Ajout d'un résumé avant sauvegarde plus lisible, avec tableau des modifications.
- Ajout d'une recherche globale machines / canaux / patchs.
- Ajout de l'export de rapport en TXT et PDF.
- Mise en évidence visuelle des conflits et lignes modifiées dans la table Patch.
- Ajout du renommage de canaux en série.
- Ajout de la liste des fichiers récents.
- Ajout d'un mode comparaison avec un autre XML.
- Renforcement de la sécurité XML : sauvegarde via fichier temporaire relu avant remplacement.
- Renforcement de la compatibilité XML : les renommages écrivent dans le champ de nom d'origine quand il existe.

## 2026-07-05 - Version 3.01

- Réduction de la colonne `Projet` à un résumé compact.
- Suppression des boutons d'onglets dupliqués dans la colonne de gauche.
- Conservation de la navigation uniquement par les onglets du haut.
- Réorganisation horizontale de la page `Configuration`.
- Ajout du filtre émetteur TX effectif dans la page `Patch`.
- Ajout des choix `Tous les émetteurs` et `Tous les récepteurs`.
- Clarification de la zone d'application de patch : `Source TX à appliquer` et `Canal TX à appliquer`.
- Séparation des champs de renommage `RX sélectionné` et `TX source`.
- Passage de la livraison en version `3.01`.

## 2026-07-04

- Création du dossier `V3` sans modification de `V2`.
- Ajout d'un projet source WPF `.NET 8`.
- Reprise de l'icône existante.
- Ajout d'une interface modernisée :
  - navigation latérale ;
  - thème sombre ;
  - thème clair ;
  - barre de statut ;
  - journal d'actions ;
  - affichage du fichier ouvert ;
  - état modifié / non modifié.
- Ajout d'une couche métier séparée pour les fichiers Dante.
- Conservation des fonctions V2 :
  - ouverture XML ;
  - renommage des devices ;
  - modification redondance / daisychain ;
  - modification latence ;
  - reset des noms de canaux ;
  - preferred master ;
  - listes rapides ;
  - sauvegarde sous un autre nom.
- Ajout du renommage individuel des canaux TX/RX.
- Ajout de la mise à jour automatique des références RX quand un canal TX renommé est utilisé par un patch reconnu.
- Ajout d'une première vue Patch hors ligne.
- Ajout de la détection de conflits simples :
  - device TX introuvable ;
  - canal TX introuvable.
- Ajout de confirmations avant actions globales ou risquées.
- Ajout d'une validation avant sauvegarde.
- Ajout d'un résumé des modifications avant sauvegarde.
- Ajout d'un backup automatique du fichier original.
- Ajout de `ANALYSE_V2.md`.
- Ajout de `README_V3.md`.
- Ajout de `build.ps1` et `run.ps1`.
- Installation du SDK .NET 8 en mode utilisateur pour permettre la compilation locale.
- Ajout d'un installateur autonome `dist\DanteConfigEditorV3_Setup.exe`.
- Ajout d'une version portable `dist\portable\DanteConfigEditorV3.exe`.
- Ajout d'un script reproductible `installer\build_installer.ps1`.
- Correction du démarrage autonome : l'icône reste au niveau de l'exécutable et des raccourcis, sans chargement XAML fragile au lancement.
- Ajout d'une signature discrète `By Mamat` dans la barre de statut.
- Correction d'un crash après ouverture XML causé par des colonnes booléennes en lecture seule.
- Ajout d'une interception globale des erreurs UI avec journal dans `%LOCALAPPDATA%\DanteConfigEditorV3\Logs`.
- Ajout de l'ouverture directe d'un XML passé en argument de ligne de commande pour faciliter les tests et les raccourcis.
- Correction de la lisibilité des en-têtes de colonnes en thème sombre.
- Ajout du renommage TX/RX directement depuis la page Patch.
- Renforcement de la mise à jour des patchs quand un canal TX est renommé : tous les champs d'abonnement reconnus du fichier sont parcourus.
- Ajout d'une copie archive de la V2 dans `legacy/V2`.
- Ajout d'un installateur Windows professionnel `dist\DanteConfigEditorV3_Installer.exe` avec choix du dossier, raccourcis et désinstallation.
- Installation par défaut dans Program Files avec demande de droits administrateur.
- Ajout de la signature `By Mamat` et du lien GitHub public dans l'assistant d'installation.
- Ajout des release notes et de la notice PDF dans l'installateur, avec proposition d'ouverture en fin d'installation.
