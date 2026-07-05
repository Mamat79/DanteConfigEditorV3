# Changelog V3

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
