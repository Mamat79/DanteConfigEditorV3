# Matrice de compatibilité Dante XML

## Règles de lecture

Cette matrice sépare strictement trois niveaux de preuve :

- **Réussi - automatisé** : le moteur a ouvert, modifié ou sauvegardé le fichier dans un test reproductible ;
- **Réussi - import prouvé** : le fichier généré a réellement été importé dans une version identifiée de Dante Controller et le résultat a été contrôlé ;
- **Non testé / à vérifier** : aucune conclusion de compatibilité terrain ne peut être tirée.

Une réussite automatisée ne vaut jamais preuve d'import Dante Controller. Les colonnes `Résultat d'import`, `Version Dante Controller`, `Date` et `Testeur` doivent être complétées avec une preuve manuelle réelle.

## Matrice actuelle

| Cas / fichier | Version Dante Controller | Structure XML | Fabricant | Modèle | Devices | TX / RX | Namespace | Interfaces réseau | Structure des subscriptions | Complet / partiel | Ouverture | Sauvegarde sans modification | Modifications testées | Résultat d'import | Anomalies | Date / testeur |
|---|---|---|---|---|---:|---:|---|---|---|---|---|---|---|---|---|---|
| `representative-preset.xml` | Inconnue - fixture synthétique | `<preset version="3.0.0">`, devices directs | Test Manufacturer | Test TX / RX / IO | 3 | 3 / 4 | Aucun | `network=0`, IPv4 dynamique et fixe | `subscribed_device`, `subscribed_channel`, source locale `.` et source externe | Fixture représentative | Réussi - automatisé | À compléter par test dédié | renommage device/TX, patchs, profils, IP, suppression, récupération, garde-fou | **Non testé** | mélange 48/96 kHz, 24/32 bit, modes réseau, IP fixe volontaire | 2026-07-11 / tests automatisés Codex |
| `merge-preset.xml` | Inconnue - fixture synthétique | `<preset version="3.0.0">`, devices directs | Test | Duplicate / Imported | 2 | 1 / 1 | Aucun | IPv4 dynamique `network=0` | abonnement externe simple | Fixture partielle d'import | Réussi via scénario de fusion | Non testé isolément | fusion, doublon, renommage importé, conservation des patchs | **Non testé** | DEVICE-A est volontairement en doublon avec la fixture principale | 2026-07-11 / tests automatisés Codex |
| preset synthétique 10 devices | Sans objet | généré en mémoire, 64 TX + 64 RX par device | Synthetic | Synthetic | 10 | 640 / 640 | Aucun | interface principale synthétique | alias principal de subscription | Complet pour charge | Réussi - automatisé | Réussi - automatisé | édition groupée, garde-fou, SaveAs, rechargement | **Non testé** | aucun matériel réel | 2026-07-11 / tests automatisés Codex |
| preset synthétique 50 devices | Sans objet | généré en mémoire, 64 TX + 64 RX par device | Synthetic | Synthetic | 50 | 3 200 / 3 200 | Aucun | interface principale synthétique | alias principal de subscription | Complet pour charge | Réussi - automatisé | Réussi - automatisé | édition groupée, garde-fou, SaveAs, rechargement | **Non testé** | aucun matériel réel | 2026-07-11 / tests automatisés Codex |
| preset synthétique 200 devices | Sans objet | généré en mémoire, 64 TX + 64 RX par device | Synthetic | Synthetic | 200 | 12 800 / 12 800 | Aucun | interface principale synthétique | alias principal de subscription | Complet pour charge | Réussi - automatisé | Réussi - automatisé | édition groupée, garde-fou, SaveAs, rechargement | **Non testé** | aucun matériel réel | 2026-07-11 / tests automatisés Codex |
| preset avec namespace par défaut | Sans objet | `<preset xmlns="urn:test:dante:preset">` | Synthetic | Synthetic | 1 minimum | variable | `urn:test:dante:preset` | principale synthétique | éléments enfants dans le même namespace | Fixture ciblée | Réussi - automatisé | Réussi - automatisé | renommage, sauvegarde et rechargement sans élément hors namespace | **Non testé** | aucun matériel réel | 2026-07-11 / tests automatisés Codex |
| corpus local non versionné | À relever fichier par fichier | exports réels hors dépôt | À relever | À relever | variable | variable | variable | variable | variable | variable | 9 XML chargés en lecture seule lors de l'audit V3.06 | Non prouvé pour chaque fichier | aucun résultat matériel généralisable | **Non testé** | détails volontairement absents du dépôt public | 2026-07-10 / contrôle local automatisé |

## Ligne à dupliquer pour chaque validation réelle

| Cas / fichier anonymisé | Version Dante Controller | Structure XML | Fabricant | Modèle | Devices | TX / RX | Namespace | Interfaces réseau | Structure des subscriptions | Complet / partiel | Ouverture | Sauvegarde sans modification | Modifications testées | Résultat d'import | Anomalies | Date / testeur |
|---|---|---|---|---|---:|---:|---|---|---|---|---|---|---|---|---|---|
| `ID_INTERNE_SANS_DONNEE_SENSIBLE` | `x.y.z` | à décrire | à renseigner | à renseigner | 0 | 0 / 0 | aucun / URI | primaire, secondaire, modes IP | noms exacts des balises/attributs | complet / partiel | réussi / échec | réussi / échec | liste précise | réussi / échec / non testé | observations et logs | AAAA-MM-JJ / nom |

## Preuves à conserver hors du dépôt public

Pour chaque import manuel :

1. conserver le XML original et le XML généré dans un espace de test non public ;
2. noter la version exacte de Dante Controller et le système d'exploitation ;
3. conserver une capture ou un journal de l'import ;
4. contrôler les devices, Dante Id, mediaType, patchs, formats audio, preferred masters et interfaces ;
5. reporter uniquement des informations anonymisées dans cette matrice ;
6. ne jamais committer le XML de production.
