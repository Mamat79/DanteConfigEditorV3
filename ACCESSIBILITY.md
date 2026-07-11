# Accessibilité et affichage

## Contrôles réellement effectués

### Windows

- ouverture et navigation de l'application WPF avec les contrôles d'automatisation Windows ;
- fenêtre compacte observée à environ `1266 x 813` : repli automatique des réglages et tableau machines accessible ;
- fenêtre maximisée observée à environ `1920 x 1032` : panneaux et tableau accessibles sans recouvrement incohérent ;
- atelier de patch ouvert, sélections et commandes actionnées via l'arbre d'accessibilité ;
- libellés d'automatisation et info-bulles ajoutés aux commandes du patch visuel.
- exécutable final installé contrôlé : démarrage, bouton `Ouvrir XML`, ouverture d'une fixture anonymisée et résumé du projet lus par l'arbre d'accessibilité.

La capture pixel directe des surfaces WPF a renvoyé une image vide dans l'outil utilisé. Les interactions et états ont été vérifiés par l'arbre d'accessibilité, mais ce cycle ne constitue donc pas une validation visuelle pixel par pixel complète de Windows.

### macOS / Avalonia headless

- test à `1366 x 768` : barre de patch accessible et contenu principal conservé ;
- test à `1920 x 1080` : disposition large sans perte des commandes testées ;
- atelier de patch testé à sa taille minimale `960 x 640` ;
- ordre de focus vérifié de `Ouvrir XML` vers `Ajouter XML au projet` avec Tab ;
- alertes placées dans le rail latéral et visibles sur un preset aux formats mélangés ;
- matrice de patch et changements en attente testés sans écran.

Ces tests Avalonia headless vérifient la structure et le comportement. Ils ne remplacent pas un contrôle VoiceOver sur un Mac réel.

## Contrôles restant manuels

Les points suivants **n'ont pas été validés matériellement dans ce cycle** :

- lecteur d'écran Windows Narrator ou NVDA ;
- VoiceOver sur macOS réel ;
- mode contraste élevé Windows ;
- échelle système exacte à 125 %, 150 % et 200 % ;
- écran physique `1366 x 768` et `1920 x 1080` ;
- grossissement système supérieur à 200 % ;
- navigation clavier exhaustive de toutes les boîtes de dialogue.

## Checklist manuelle

### Clavier et focus

- [ ] Toutes les commandes principales sont atteignables avec Tab et Maj+Tab.
- [ ] Le focus visible suit un ordre logique.
- [ ] Entrée et Espace activent les boutons, cases et cellules attendus.
- [ ] Échap ferme les dialogues sans appliquer les changements en attente.
- [ ] Le glisser-déposer du patch visuel possède une alternative clavier par le bouton d'affectation.
- [ ] La sélection multiple TX fonctionne au clavier avec Ctrl/Maj.

### Thèmes et contraste

- [ ] Le thème sombre garde un texte lisible dans les listes, tableaux et onglets.
- [ ] Le thème clair garde un contraste suffisant pour les textes secondaires.
- [ ] Le contraste élevé Windows conserve les contours, sélections et focus.
- [ ] Les états modifié, avertissement et erreur ne reposent pas uniquement sur la couleur.

### Taille et mise à l'échelle

- [ ] `1366 x 768` à 100 % : aucune commande critique inaccessible.
- [ ] `1920 x 1080` à 100 % : tableau et panneaux utilisent correctement l'espace.
- [ ] 125 %, 150 % et 200 % : aucun texte ou bouton tronqué.
- [ ] Les noms longs sont ellipsés ou défilables sans recouvrir une autre commande.
- [ ] La matrice et les tableaux conservent leurs ascenseurs internes.

### Lecteurs d'écran

- [ ] Le titre et le rôle de chaque fenêtre sont annoncés.
- [ ] Les boutons icônes et commandes de patch ont un nom accessible.
- [ ] Les cellules actives de la matrice indiquent clairement l'affectation.
- [ ] Les alertes sont annoncées sans déplacement brutal du focus.
- [ ] Les messages d'erreur identifient le champ ou l'action concernée.

Tout défaut observé doit préciser plateforme, résolution, échelle, thème, langue et étapes de reproduction.
