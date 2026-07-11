# Audit V3.06 - baseline V3.05 Beta

Date de mesure : 2026-07-10
Commit audite : `9dda89c6017793eedf0a5f36fd1cb2f5bfd2a16d`
Branche de travail : `agent/v3-06-hardening`
Environnement : Windows `10.0.26200`, x64, SDK .NET `8.0.422`, MSBuild `17.11.48`, VSTest `17.11.1`.

Ce document decrit l'etat initial avant toute correction du code de production. Les mesures ne constituent pas une garantie de compatibilite avec Dante Controller : elles servent de reference reproductible pour les travaux de durcissement V3.06 Beta.

## 1. Commandes de reference

Les quatre commandes demandees ont ete executees depuis la racine du depot, dans cet ordre.

| Commande | Code retour | Temps mesure | Resultat exact |
|---|---:|---:|---|
| `dotnet restore` | 0 | 1,470 s | Projet principal restaure en 92 ms. |
| `dotnet test .\tests\DanteConfigEditorV3.Tests\DanteConfigEditorV3.Tests.csproj -c Release` | 0 | 17,730 s | 10 reussis, 0 echec, 0 ignore, total 10 ; execution des tests 286 ms. |
| `dotnet build .\DanteConfigEditorV3.csproj -c Release` | 0 | 1,950 s | Generation reussie ; 0 avertissement, 0 erreur ; temps MSBuild 1,57 s. |
| `dotnet publish .\DanteConfigEditorV3.csproj -c Release -r win-x64` | 0 | 4,860 s | Publication reussie dans `bin\Release\net8.0-windows\win-x64\publish`. Aucun avertissement affiche. |

### Couverture initiale

Les 10 tests existants couvrent principalement le chargement d'un petit preset representatif, les alertes, le renommage d'un device et d'un TX, un profil rapide, la suppression, la fusion, la recuperation simple, les metadonnees de version et un corpus reel optionnel. Ils ne couvrent pas les changements techniques apres renommage, les chemins XML inconnus, l'atomicite de `SaveAs`, plusieurs interfaces IPv4, tous les alias de subscription, les namespaces XML ni les gros presets.

## 2. Architecture et taille

L'application est une application WPF .NET 8 sans couche de persistance separee. Le modele travaille directement sur un `XDocument`, puis reconstruit les objets de lecture apres les mutations.

| Fichier | Lignes | Responsabilites principales |
|---|---:|---|
| `MainWindow.xaml.cs` | 3 200 | Evenements UI, orchestration, sauvegarde, recuperation, traduction, filtres et exports. |
| `Models/DanteProject.cs` | 2 759 | Chargement XML, modele, mutations, patch, merge, validation, rapports, undo, comparaison et sauvegarde. |
| `MainWindow.xaml` | 1 178 | Styles et interface principale. |
| `Services/LocalizationService.cs` | 691 | Dictionnaires et traduction des textes de l'interface. |
| `Services/DanteXmlCompatibilityService.cs` | 369 | Profil et controle structurel de compatibilite. |
| `Services/DanteXmlChangeGuardService.cs` | 335 | Comparaison XML et liste blanche/noire des changements. |

`MainWindow.xaml.cs` et `DanteProject.cs` sont trop volumineux. Le risque principal n'est pas seulement la lisibilite : les responsabilites de securite XML, d'identite, de sauvegarde et d'interface sont couplees, ce qui rend les regressions difficiles a isoler. La V3.06 doit rester ciblee sur le durcissement ; une decomposition plus large restera une limite connue si elle n'est pas necessaire aux correctifs.

## 3. Risques de corruption XML

### Critiques

1. **Identite du garde-fou basee sur le nom.** `DanteXmlChangeGuardService` associe les balises `<device>` par `<name>`. Apres un renommage, le device original apparait supprime et le nouveau apparait ajoute. Le chemin `/preset/device` etant autorise, une modification simultanee de `instance_id/device_id`, `danteId`, `mediaType` ou d'une autre balise technique peut echapper au controle.
2. **Chemins inconnus non bloquants.** Une modification hors liste blanche et hors liste noire ne produit qu'un avertissement. Une balise technique inconnue peut donc etre ajoutee ou modifiee puis sauvegardee.
3. **Remplacement non atomique.** `SaveAs` supprime d'abord une destination existante, puis deplace le fichier temporaire. Une erreur entre ces deux operations laisse la destination absente. La destination existante n'est pas sauvegardee ; seul le fichier source initial recoit une copie de securite.

### Eleves

4. **Reference de session non mise a jour apres `SaveAs`.** `OriginalFilePath` reste immuable et la recuperation automatique continue a s'identifier avec l'ancien chemin. Les modifications faites apres un premier `Enregistrer sous` peuvent donc etre rattachees au mauvais fichier.
5. **Comparaison positionnelle des enfants XML.** En dehors de la liste des devices, les enfants sont compares par index. Un simple reordonnancement de balises techniques identiques peut produire des differences artificielles et masquer la nature exacte du changement.
6. **Interfaces IPv4 non ciblees.** Le passage en IP automatique parcourt tous les `ipv4_address`. Le passage en IP fixe prend le premier descendant, supprime de nombreux attributs, force la passerelle fournie et cree `dnsserver=0.0.0.0`. Une interface secondaire, un DNS ou une passerelle existante peuvent etre modifies implicitement.
7. **Namespace par defaut non pris en charge.** La plupart des lectures utilisent `Element("device")`, `Elements("txchannel")`, etc. Un XML semantiquement equivalent avec un namespace par defaut ne charge aucune machine.
8. **Alias de subscription non verifies exhaustivement.** Les listes d'alias sont presentes, mais aucun test ne prouve que chaque alias est lu, renomme et sauvegarde sans toucher aux autres balises.

## 4. Performances et memoire

`RegisterChange` appelle toujours `ReloadModel`. Les actions globales de l'interface appellent parfois une methode par machine, et la fenetre de detail peut appeler une methode par champ puis par canal. Une edition complete de 64 TX et 64 RX provoque donc jusqu'a 134 reconstructions completes du modele. Chaque snapshot d'annulation copie egalement tout le `XDocument`, sans limite de profondeur.

La recuperation automatique est synchrone et execute une ecriture XML, une relecture XML et deux remplacements de fichiers apres chaque action. Sur les gros presets, ce travail bloque le thread UI. `SaveAs` valide, serialise, recharge un projet complet, revalide et compare encore le document, ce qui multiplie les allocations.

### Baseline synthetique

Methodologie : presets sans namespace, 64 TX et 64 RX par machine, trois executions par taille, mediane affichee. Le scenario d'edition reproduit la fenetre de detail : renommage du device, cinq reglages, puis renommage des 64 TX et 64 RX. La memoire indique les octets manages alloues pendant l'operation, pas seulement la memoire encore vivante.

| Machines | XML | Chargement | Allocation chargement | Edition detail | Allocation edition | Garde-fou | Allocation garde-fou | SaveAs | Allocation SaveAs | Working set final |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 0,156 MiB | 41,207 ms | 4,981 MiB | 875,704 ms | 436,535 MiB | 9,476 ms | 6,719 MiB | 122,916 ms | 37,548 MiB | 123,086 MiB |
| 50 | 0,780 MiB | 126,398 ms | 24,622 MiB | 4 373,159 ms | 2 183,245 MiB | 27,978 ms | 35,978 MiB | 331,996 ms | 194,335 MiB | 146,406 MiB |
| 200 | 3,122 MiB | 369,615 ms | 98,353 MiB | 18 977,339 ms | 8 735,596 MiB | 101,924 ms | 145,718 MiB | 1 090,778 ms | 782,367 MiB | 224,852 MiB |

Le garde-fou initial retourne `HasErrors=false` dans les neuf executions, bien que le scenario commence par renommer le device. Ce resultat confirme le besoin de tests specifiques avant correction.

## 5. Traduction et accessibilite

1. La traduction repose en partie sur la recherche de litteraux francais dans l'arbre WPF. Les modeles et services contiennent encore des messages, statuts et rapports directement en francais ; le passage en anglais n'est donc pas complet pour tous les retours techniques.
2. Plusieurs textes XAML servent de valeur francaise de secours, ce qui rend les cles de traduction sensibles a une modification de libelle.
3. Aucun `AutomationProperties.Name` ou `AutomationProperties.HelpText` n'a ete trouve dans l'interface principale. Les lecteurs d'ecran dependent donc du texte visible et de l'inference WPF.
4. De nombreux controles ont un tooltip, mais pas tous les boutons de modification ni la case Preferred master du tableau. Les explications ne sont pas accessibles de facon homogene au clavier ou aux technologies d'assistance.
5. Aucun test automatise ne controle l'ordre de tabulation, les noms accessibles, la traduction complete, le contraste ou l'affichage a fort grossissement.
6. Les etats de patch utilisent des couleurs. Une colonne de statut existe, mais il faut conserver un libelle textuel pour ne jamais rendre l'information dependante de la couleur seule.

## 6. Priorites V3.06 Beta

1. Ecrire les tests de securite et de non-regression demandes et constater leurs echecs sur cette baseline.
2. Introduire une identite stable des devices et une comparaison semantique des balises techniques.
3. Bloquer par defaut toute modification XML non explicitement autorisee.
4. Rendre le remplacement de fichier atomique, avec sauvegarde de la destination existante et point d'injection testable en cas d'erreur.
5. Faire de la nouvelle destination la reference courante de sauvegarde et de recuperation.
6. Cibler explicitement l'interface IPv4 principale et conserver DNS, passerelle et interfaces secondaires hors des champs demandes.
7. Regrouper les mutations en une seule reconstruction du modele, temporiser la recuperation asynchrone et borner l'annulation.
8. Ajouter la CI Windows, verifier les codes de retour des scripts, rejouer les benchmarks, construire l'installateur V3.06 Beta et remplacer l'installation V3.05 existante.

## 7. Limites de l'audit initial

- Aucun import automatique dans Dante Controller n'est disponible dans ce depot ; la compatibilite finale reste a confirmer manuellement dans le logiciel Audinate sur des copies de fichiers.
- Les timings dependent de la machine et de l'activite systeme. Les memes presets et le meme protocole seront reutilises pour la comparaison apres correction.
- L'audit ne valide pas encore l'accessibilite avec un lecteur d'ecran reel.

## 8. Validation finale V3.06 Beta

La phase de correction a ete realisee apres l'ajout des tests de securite et de non-regression. La suite finale contient 38 tests et couvre notamment l'identite stable apres renommage, les chemins XML inconnus, le reordonnancement de balises, la sauvegarde atomique, la nouvelle reference apres `SaveAs`, les interfaces IPv4 multiples, tous les alias de subscription, les namespaces par defaut, les mutations groupees, la recuperation temporisee et la limite de la pile d'annulation.

| Commande finale | Code retour | Temps mesure | Resultat exact |
|---|---:|---:|---|
| `dotnet restore` | 0 | 0,951 s | Tous les projets etaient a jour. |
| `dotnet test .\tests\DanteConfigEditorV3.Tests\DanteConfigEditorV3.Tests.csproj -c Release --no-restore` | 0 | 7,863 s | 38 reussis, 0 echec, 0 ignore ; execution des tests 2 s. |
| `dotnet build .\DanteConfigEditorV3.csproj -c Release --no-restore` | 0 | 0,915 s | Generation reussie ; 0 avertissement, 0 erreur ; temps MSBuild 0,65 s. |
| `dotnet publish .\DanteConfigEditorV3.csproj -c Release -r win-x64` | 0 | 2,957 s | Publication autonome Windows reussie dans `bin\Release\net8.0-windows\win-x64\publish`. |

Le test d'integration local a egalement charge, sans les modifier, les neuf XML Dante trouves sous `Montpellier 2026`. Les neuf fichiers contiennent au moins un device et le garde-fou ne detecte aucune difference sur leur contenu charge intact.

### Corrections verifiees

1. Les devices sont associes par identifiants techniques stables et non plus par leur seul nom visible.
2. Une modification XML inconnue est bloquante par defaut, tandis qu'un simple reordonnancement conserve une comparaison semantique.
3. `SaveAs` ecrit et valide un fichier temporaire dans le repertoire cible, sauvegarde la destination existante, puis utilise un remplacement atomique. En cas d'erreur injectee apres creation du temporaire, l'ancienne destination reste intacte.
4. Apres `SaveAs`, la destination devient la reference de la session et de la recuperation automatique.
5. Les changements IP ciblent uniquement l'interface IPv4 principale ; DNS, passerelle et interface secondaire ne sont plus reecrits implicitement.
6. Les modifications groupees ne reconstruisent le modele qu'une fois. La recuperation est asynchrone, temporisee et annule l'ecriture devenue obsolete. La pile d'annulation est bornee a dix snapshots.
7. La CI Windows restaure, teste, compile et publie sur `windows-latest`. `build.ps1` arrete immediatement la construction lorsqu'une commande echoue.

## 9. Benchmarks apres correction

Le protocole est identique a la baseline : trois executions, mediane, 64 TX et 64 RX par machine. Les fichiers bruts et le programme reproductible sont conserves dans `benchmarks`.

| Machines | Chargement | Allocation chargement | Edition detail | Allocation edition | Garde-fou | Allocation garde-fou | SaveAs | Allocation SaveAs | Working set final |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 23,180 ms | 2,783 MiB | 34,042 ms | 20,075 MiB | 7,201 ms | 5,735 MiB | 94,898 ms | 29,802 MiB | 51,336 MiB |
| 50 | 76,186 ms | 13,735 MiB | 211,272 ms | 97,378 MiB | 31,021 ms | 27,734 MiB | 266,858 ms | 145,565 MiB | 86,977 MiB |
| 200 | 243,042 ms | 54,822 MiB | 279,990 ms | 387,250 MiB | 55,482 ms | 110,238 MiB | 683,049 ms | 579,725 MiB | 180,867 MiB |

### Evolution par rapport a V3.05 Beta

Une valeur positive indique une reduction du temps ou de la memoire allouee.

| Machines | Chargement | Edition | Allocation edition | Garde-fou | Allocation garde-fou | SaveAs | Allocation SaveAs | Working set |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 43,7 % | 96,1 % | 95,4 % | 24,0 % | 14,6 % | 22,8 % | 20,6 % | 58,3 % |
| 50 | 39,7 % | 95,2 % | 95,5 % | -10,9 % | 22,9 % | 19,6 % | 25,1 % | 40,6 % |
| 200 | 34,2 % | 98,5 % | 95,6 % | 45,6 % | 24,3 % | 37,4 % | 25,9 % | 19,6 % |

Le temps du garde-fou a 50 machines varie davantage entre les trois passages et sa mediane est 10,9 % plus lente que la baseline. Cette regression mesuree est conservee explicitement. A 10 et 200 machines, le garde-fou est plus rapide et ses allocations diminuent pour les trois tailles. Le gain principal vient de l'edition groupee : a 200 machines, le scenario complet passe de 18 977,339 ms a 279,990 ms.

## 10. Limites restantes

- La structure XML est protegee par comparaison et par tests, mais l'import final doit encore etre confirme manuellement dans une version officielle de Dante Controller. Le depot ne contient ni SDK Audinate ni automatisation de cet import.
- L'application reste un editeur de fichiers hors ligne ; elle ne pilote pas le reseau Dante en temps reel.
- `MainWindow.xaml.cs` et `DanteProject.cs` restent volumineux. Leur decomposition n'a pas ete incluse afin de ne pas melanger une refonte generale avec le durcissement V3.06.
- L'accessibilite n'a pas ete validee avec un lecteur d'ecran, un mode contraste eleve ou un fort grossissement.
- Les temps sont des mesures locales et non des seuils contractuels. La mesure du garde-fou a 50 machines montre la variabilite attendue d'un micro-benchmark execute sur un poste de travail.
