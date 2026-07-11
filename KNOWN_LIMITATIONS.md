# Limites connues - V3.07 Beta

## Compatibilité Dante

- L'application édite uniquement des fichiers XML hors ligne. Elle ne découvre, ne surveille et ne pilote aucun appareil Dante en temps réel.
- Aucun SDK ou service Audinate n'est utilisé. Dante Config Editor ne remplace pas Dante Controller ni Dante Domain Manager.
- La compatibilité dépend de la structure exacte du preset. Seul un import réel dans une version identifiée de Dante Controller peut valider le résultat final.
- Aucun fabricant ni modèle matériel n'est déclaré compatible sans preuve enregistrée dans `COMPATIBILITY_MATRIX.md`.
- Un preset partiel peut référencer un device TX absent. Cette situation reste un avertissement et non une erreur systématiquement bloquante.
- Les chemins XML inconnus sont bloqués par défaut à la sauvegarde. Un nouveau format Dante peut donc nécessiter une mise à jour de l'application avant d'être éditable.
- L'application ne vérifie pas les capacités matérielles réelles : nombre de flows, sample rates ou encodages supportés par un appareil.

## Subscriptions et patch visuel

- Seuls les aliases de subscription documentés et testés sont modifiés. Une structure différente reste préservée ou bloque la sauvegarde selon son emplacement.
- `subscribed_device="."` est traité comme une source locale au device RX.
- Des noms TX dupliqués sur un même device rendent une subscription textuelle ambiguë. Le patch visuel refuse cette affectation ; renommer d'abord les TX concernés.
- La matrice affiche un couple de devices à la fois pour rester exploitable sur les gros presets.
- Sur macOS, la matrice est limitée aux 128 premiers TX et 128 premiers RX du couple choisi. Les listes conservent tous les canaux et permettent l'affectation séquentielle au-delà de cette vue.
- Les changements du patch visuel restent en attente jusqu'à `Appliquer au projet`. Fermer sans appliquer les abandonne.

## Fichiers et récupération

- Le fichier ouvert doit être sauvegardé sous un nouveau nom avant toute écriture. Le nouveau chemin devient ensuite la référence de session.
- Une sauvegarde atomique protège la destination contre les échecs testés, mais ne remplace pas une sauvegarde externe du projet.
- La récupération automatique est temporisée. Une coupure immédiate après une modification peut survenir avant l'écriture de la copie de récupération.
- La pile d'annulation conserve au maximum 10 états XML pour limiter l'usage mémoire.
- Les gros presets restent traités en mémoire ; les mesures synthétiques actuelles sont publiées dans `TESTING.md`.

## Réseau

- Les changements IPv4 ciblent l'interface principale reconnue. Une structure réseau non reconnue peut être refusée.
- DNS, passerelle et interface secondaire ne sont pas modifiés implicitement.
- La remise en IP automatique ne configure pas physiquement un appareil ; elle modifie uniquement le preset XML.

## Plateformes et distribution

- L'installateur Windows est autonome pour Windows x64 et inclut .NET 8. Les architectures Windows ARM64 et x86 ne sont pas distribuées.
- Les paquets Mac Apple Silicon et Intel sont autonomes, mais la bêta n'est pas notariée par Apple. Un premier lancement par clic droit puis `Ouvrir` peut être nécessaire.
- Les tests Mac automatisés sont headless. VoiceOver et le rendu sur matériel macOS réel restent à contrôler.
- Le contraste élevé, les lecteurs d'écran et les échelles Windows 125 %, 150 % et 200 % nécessitent encore la validation manuelle décrite dans `ACCESSIBILITY.md`.

## Statut de la bêta

La V3.07 est une version bêta en développement, non exempte de bugs. Toujours travailler sur une copie, lire le rapport avant/après et valider le fichier généré dans l'outil Dante officiel avant une utilisation terrain.
