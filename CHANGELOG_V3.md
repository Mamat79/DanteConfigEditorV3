# Changelog V3

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
