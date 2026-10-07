# Kingdom Access

Un mod qui rend **Kingdom Two Crowns** jouable par les personnes aveugles et malvoyantes.
Il parle à travers votre lecteur d'écran (NVDA, JAWS et d'autres), lit les menus, décrit le
monde autour de vous, permet de parcourir l'île et de marcher vers n'importe quel élément, et
vous prévient par des sons quand le danger approche.

*[English version](README.md)*

> État : **bêta (0.11.0)**. Développé et testé par un joueur aveugle avec NVDA sous Windows, sur la
> version Steam du jeu, surtout dans la campagne des Terres du Nord. Retours et signalements de
> bugs bienvenus.

---

## Ce que fait le mod

**Parole**
- Parle à travers votre lecteur d'écran grâce à Tolk (NVDA, JAWS, SuperNova, ZoomText...), avec
  la voix Windows en secours. Le braille passe par votre lecteur d'écran.
- Suit automatiquement la langue du jeu, y compris quand vous la changez dans les options.
  Les 11 langues du jeu sont incluses : anglais, français, allemand, espagnol, italien, portugais,
  russe, japonais, coréen, chinois simplifié et traditionnel (voir [Langues](#langues)).
- Historique des 50 derniers messages (répéter, précédent, suivant).
- Clavier et manette : chaque raccourci existe sur les deux, et tous sont réglables.

**Menus et écrans**
- Menus standard : élément sélectionné, son type (case à cocher, curseur), son état, sa position
  (« 2 sur 5 ») et le texte des fenêtres qui s'ouvrent. Les boutons sans texte ont un nom.
- Écran de nouvelle partie (monde, difficulté, monarque), carte et chronologie (y compris la carte du
  monde de Call of Olympus : oracle, temples, îles de quête, mont Olympe, et ce qui les débloque), éditeur de blason
  (description de chaque fond et emblème, noms des couleurs), résumé de fin d'île (lu en entier,
  puis parcourable ligne par ligne).
- Tutoriel : les indices du fantôme sont annoncés (action attendue et position du fantôme).
- Textes à l'écran (notifications, bulles d'information) : lus à leur apparition, et regroupables
  dans une liste parcourable.

**Autour de vous**
- L'objet sélectionné par le jeu (l'endroit où l'on paie) est annoncé avec son niveau (murs, tours,
  château), son prix, l'action (avec le niveau visé pour une amélioration) et ce qui manque : une
  condition (technologie de la pierre ou du fer, niveau du château, ermite, moment de la
  journée...) ou les pièces qui vous manquent. Passer sur un autre objet coupe l'annonce précédente.
- Au galop (le jeu ne sélectionne alors rien), chaque objet utile que vous croisez est annoncé :
  château, magasins, marchand, montures, statues, énigmes, portails, coffres, arbres de lisière...
- Entrée et sortie du royaume et des camps de vagabonds ; direction du camp de base à l'arrivée.
- Rapports à la demande : pièces et gemmes, jour, saison, heure et temps avant la nuit, monture,
  relique et capacités (avec ce qu'elles font), boussole et danger, recensement des troupes
  (avec les pièces portées).

**Trouver les choses**
- **Scanner** par catégories : constructible, améliorable, murs, tours, arbres, camps, troupes,
  magasins, bâtiments, montures, statues, personnages, trésors (coffres compris), voyage (bateau,
  quai), énigmes et reliques, grotte et bombe, ennemis, autres. Éléments triés par distance, avec
  leur côté et leur distance.
- **Radar** : l'objet intéressant le plus proche de chaque type, de chaque côté.
- **Marche et course automatiques** vers l'élément choisi, vers le château, vers votre couronne
  perdue, ou juste derrière le mur le plus éloigné d'un côté. Appuyer dans l'autre sens reprend
  la main.
- Par défaut, seul ce que vous avez déjà exploré est listé (réglable).

**Alertes**
- Ennemis qui approchent : trois paliers (20, 10 et 5 par défaut), chacun avec son son, joué dans
  l'oreille d'où vient l'ennemi, plus le nombre d'ennemis et la distance.
- Couronne perdue : alerte avec sa position, répétée jusqu'à ce que vous la récupériez ; une
  touche vous y fait courir.
- Aube, jour, soir et nuit : un son et une annonce.
- Un petit carillon quand l'objet devant vous peut être payé tout de suite.

**Sons des objets**
- Chaque type d'objet a son propre son bref, joué quand le jeu le sélectionne : château, mur, tour,
  ferme, marchand, arbre, camp, personnage, statue, trésor, danger (portail, ennemi), bateau, énigme,
  bombe, autre bâtiment. Chaque magasin sonne comme ce qu'il vend (corde d'arc, marteau, lame, pique
  dégainée, bouclier, enclume, coup d'art martial, catapulte). Les montures jouent leur propre cri,
  pris dans le jeu. Le même son plus grave veut dire « à construire », plus aigu « à améliorer » :
  un seul son, sans suffixe.
- **Radar sonore** : joue le son de chaque objet autour de vous, du plus proche au plus lointain,
  l'un après l'autre. Plus l'objet est loin, plus le son est faible, et chaque son ne joue que dans
  l'oreille de son côté. Portée de 30, 50 ou 100, changée en jeu.
- **Légende des sons** : écoutez chaque son avec son nom.
- Les sons des objets viennent des ensembles CC0 de [Kenney](https://kenney.nl) (Impact Sounds, RPG
  Audio, Interface Sounds, Casino Audio). Chacun est un fichier de `BepInEx\plugins\KingdomAccess\Sounds\Earcons`
  (`castle.wav`, `wall.wav`...) : remplacez-le par n'importe quel fichier WAV 16 bits pour changer le son.
- Retour d'une capacité (objet de pouvoir, monarque, monture).

**Contenu des DLC**
- Énigmes des Terres du Nord : Heimdall (moment de la journée de chaque pilier, monture requise,
  cor), Thor (symbole actuel de chaque pilier, s'il est juste, combien sont justes), Hel (ce
  qu'attend chaque support), Loki (marche à suivre et risques). Les énigmes résolues sont annoncées.
- Ce que fait chaque relique et artefact (marteau de Thor, trophée de Hel, cor de Heimdall, bâton de
  Loki, bouclier d'Athéna, bâton d'Hermès, marteau d'Héphaïstos, arc d'Artémis), le pouvoir des
  monarques des Terres mortes, et la capacité spéciale de chaque monture (avec la touche qui la
  déclenche).
- Objets de Call of Olympus : oracle, chantier naval, bornes de frontière, montures à acheter ; noms
  distincts pour les parties du bateau (épave, construction, départ en mer, bateau).
- Expédition de la bombe vers la grotte des Greed : chaque étape est annoncée (escorte, entrée,
  traversée, gardien, détonation, sortie), et tant que vous êtes au-delà du portail de la falaise,
  le scanner et le radar ne montrent que cette zone. Dans la grotte, un battement de cœur vous guide
  vers la bombe, là où vous devez agir : plus rapide et plus fort en approchant, du côté où elle se
  trouve. La description de la bombe donne sa position actuelle, puis celle du portail de la falaise.

## Ce que le mod ne fait pas (encore)

- **Un seul joueur.** Seul le joueur 1 est suivi. La coopération locale et en ligne n'est pas
  prise en charge, et la fenêtre d'invitation de Steam ne peut être lue par aucun mod.
- **Écrans pilotés à la souris** qui ne sont pas des menus standard : certains peuvent rester
  muets. Signalez-les.
- **L'Olympe (Call of Olympus)** n'est que partiellement couvert : ses énigmes et quêtes n'ont pas
  encore été testées, et plusieurs descriptions d'artefacts et de montures manquent volontairement
  (pas de source fiable).

> **Remarque :** toutes les fonctions du mod devraient marcher en multijoueur, sauf peut-être les
> interactions avec l'autre joueur (sa position, l'achat d'une nouvelle couronne). Ce n'est pas encore testé.

## Touches

Touches par défaut, toutes réglables (clavier et manette). Au clavier, elles évitent celles du
jeu : le joueur 1 utilise WASD, les flèches et Maj (Maj gauche déclenche aussi la capacité de la
monture, donc aucun raccourci du mod n'utilise Maj) ; le joueur 2 utilise G, H, J, K, L, I et Maj
droite (G et J lancent aussi l'écran partagé). **F1** en jeu donne la liste de vos raccourcis actuels.

### Clavier

| Touche | Action |
|---|---|
| F1 | Aide : liste des raccourcis, avec leurs boutons de manette |
| F4 | Répéter l'indice du tutoriel en cours |
| F11 / F9 / F10 | Répéter le dernier message / précédent / suivant dans l'historique |
| F2 | Relire l'écran (fenêtre et élément sélectionné ; résumé du blason) |
| F3 | Mettre tous les textes à l'écran dans une liste |
| O | Or et gemmes |
| T | Jour, saison, moment de la journée, temps avant la nuit ou l'aube |
| M | Monture : fatigue, capacité et ce qu'elle fait |
| R | Relique et capacités, avec ce qu'elles font |
| C | Boussole : direction, moment de la journée, danger, mur le plus proche |
| P | Population : recensement des troupes |
| V | Radar |
| X | Détail de l'objet devant vous |
| N | Radar sonore : le son de chaque objet autour de vous, de son côté |
| Ctrl+N | Portée du radar sonore : 30, 50 ou 100 |
| Alt+N | Légende des sons (Page haut / Page bas pour parcourir) |
| Origine / Ctrl+Origine | Scanner : catégorie suivante / précédente |
| Page haut / Page bas | Élément précédent / suivant de la dernière liste (scanner, radar, recensement, aide, résumé...) |
| E | Relire l'élément choisi avec sa distance à jour |
| Fin / Ctrl+Fin | Marcher / courir vers l'élément choisi (nouvel appui pour arrêter) |
| B | Courir à la base (château ou feu de camp) |
| Ctrl+C | Courir vers votre couronne perdue |
| Ctrl+Gauche / Ctrl+Droite | Courir juste derrière (à l'intérieur) le mur le plus éloigné de ce côté |
| Ctrl+F3 / Alt+F3 | Développement : écrire l'objet devant vous / toute l'île dans le journal |

### Manette

Disposition Xbox (PlayStation : A = Croix, B = Rond, X = Carré, Y = Triangle). Toute manette que
Windows ou Steam Input présente comme une manette Xbox fonctionne. Le mod utilise deux **couches** :
maintenez **LB** pour la navigation ou **RB** pour les rapports, puis appuyez sur un bouton. Tant
que LB ou RB est maintenu, le jeu ignore la manette : un raccourci du mod ne lâche jamais de pièce
et ne fait jamais bouger le monarque.

| LB maintenu + | Action | RB maintenu + | Action |
|---|---|---|---|
| Croix haut / bas | Catégorie précédente / suivante | A | Or et gemmes |
| Croix gauche / droite | Élément précédent / suivant | B | Monture |
| A | Marcher vers l'élément | X | Heure |
| RT | Courir vers l'élément | Y | Relique et capacités |
| LT | Courir à la base | Croix haut | Boussole |
| X | Relire l'élément | Croix bas | Recensement |
| Y | Radar | Croix gauche / droite | Derrière le mur de gauche / droite |
| B | Objet devant vous | Clic du stick droit | Courir vers la couronne perdue |
| Affichage | Répéter le dernier message | Affichage | Aide |
| Menu | Radar sonore | Menu | Portée du radar sonore |
| Clic du stick droit | Relire l'écran | Clic du stick gauche | Indice du tutoriel |
| Clic du stick gauche | Liste des textes à l'écran | LT / RT | Message précédent / suivant |

## Installation

1. **BepInEx 6 (IL2CPP) et ses correctifs.** Le jeu a besoin de BepInEx 6 « bleeding edge » pour
   IL2CPP, plus deux correctifs faits pour ce jeu par [abevol/KingdomMod](https://github.com/abevol/KingdomMod#install)
   (sans eux, les mises à jour récentes du jeu empêchent BepInEx de fonctionner) :
   - [BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.753](https://builds.bepinex.dev/projects/bepinex_be/753/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.753%2B0d275a4.zip) : décompressez-le dans le dossier du jeu, de
     sorte que le dossier `BepInEx` et `winhttp.dll` soient à côté de `KingdomTwoCrowns.exe`.
   - [Cpp2IL.Patch](https://github.com/abevol/KingdomMod/releases/download/2.4.0/Cpp2IL.Patch.zip) et [Il2CppInterop.Patch](https://github.com/abevol/KingdomMod/releases/download/2.4.3/Il2CppInterop.Patch.zip) : décompressez-les aussi dans le dossier
     du jeu, en remplaçant les fichiers du même nom.

   Lancez le jeu une fois pour que BepInEx crée ses fichiers (le premier lancement est plus long).
2. **Le mod.** Décompressez l'archive `KingdomAccess-<version>.zip` dans le dossier du jeu (celui qui
   contient `KingdomTwoCrowns.exe`). Vous devez obtenir `BepInEx\plugins\KingdomAccess\` avec `KingdomAccess.BepInEx.dll`,
   `KingdomAccess.Core.dll`, `Tolk.dll`, `nvdaControllerClient64.dll` et les dossiers `Lang` et `Sounds`.
3. **Utilisateurs de NVDA : installez le module complémentaire NVDA** `kingdomAccessKeys` (joint à
   chaque version ; ouvrez le fichier `.nvda-addon` avec NVDA lancé). NVDA coupe normalement la
   parole à chaque touche pressée, ce qui interrompt sans cesse les annonces du mod dans un jeu.
   Tant que Kingdom Two Crowns a le focus, le module empêche les touches de couper la parole ; le
   mod coupe lui-même la parole quand il a quelque chose de nouveau à dire. N'utilisez pas le mode
   veille de NVDA dans le jeu : il fait aussi taire le mod.

   **Sans le module**, les réglages de NVDA aident en partie : dans les paramètres de NVDA, catégorie
   Clavier, décochez « Interrompre la parole pour les caractères tapés » et « Interrompre la parole
   pour la touche Entrée ». Faites-le dans un profil de configuration propre au jeu, pour que ça ne
   s'applique que là : le jeu ayant le focus, appuyez sur NVDA+Ctrl+P, choisissez « Nouveau »,
   sélectionnez le déclencheur « Application actuelle », puis changez les deux réglages pendant que
   ce profil est actif. Les flèches peuvent encore couper la parole ; le module reste la solution la
   plus complète.

   **Utilisateurs de JAWS** : si les flèches semblent sans effet dans le jeu, le curseur virtuel de
   JAWS les capture peut-être ; désactivez-le pendant que vous jouez.
4. **Lancez le jeu** avec votre lecteur d'écran. Après quelques secondes, vous devez entendre
   « Kingdom Access version ... chargée ».

## Configuration

`BepInEx\config\kingdom.access.cfg` est créé au premier lancement. Chaque réglage et chaque
raccourci y est documenté (en anglais). Sections :

1. **General** : activation, langue forcée, voix Windows en secours, taille de l'historique.
2. **Announcements** : objet sélectionné par le jeu, objets croisés au galop, zones du royaume et
   des camps, textes à l'écran et tutoriel, capacités prêtes.
3. **Radar and scanner** : portées, limitation à la zone explorée et sa marge, portails détruits.
4. **Alerts and sounds** : alerte ennemis et sa distance, alerte couronne, moments de la journée, sons,
   sons des objets au survol (`HoverSounds`), portée du radar sonore (`SoundRadarRange`) et délai entre
   ses sons (`SoundRadarDelay`), carillon de paiement, battement de cœur de la grotte.
5. **Menus** : lecture des menus, journal des menus (développement).
6. **Keys** : chaque raccourci, par exemple `Wallet = O`, `Radar = V`, `TargetDetails = X`.
   Les noms de touches sont ceux d'Unity (`F5`, `PageDown`, `LeftArrow`...) ; les modificateurs
   sont `Ctrl`, `Shift`, `Alt`. Une valeur vide désactive le raccourci. Les anciennes touches par
   défaut qui utilisaient Maj sont mises à jour automatiquement.
7. **Gamepad** : activation de la manette, blocage du jeu pendant qu'un bouton de couche est
   maintenu, boutons de couche, et chaque raccourci manette (par exemple `PadWallet = RB+A`).

Relancez le jeu après avoir modifié le fichier. Les sons sont des fichiers WAV dans
`plugins\KingdomAccess\Sounds` : remplacez-en un par votre propre fichier du même nom.

## Langues

Le français et l'anglais ont été écrits à la main ; les autres langues ont été traduites avec l'aide
d'une IA et n'ont pas encore été relues par des locuteurs natifs : toute correction est la bienvenue.
Les textes du mod sont dans [`Localization/`](Localization/README.md), un fichier JSON par
langue. Pour ajouter une langue : copier `en.json`, traduire les valeurs, vérifier avec
`python tools/check_localization.py`. Voir le [guide de traduction](Localization/README.md).

## Encore à tester

Ces parties fonctionnent en principe mais n'ont pas été confirmées en jeu, ou seulement en partie :

- Manette : tester chaque raccourci des deux couches avec une vraie manette, et vérifier que le jeu
  reprend la manette quand LB ou RB est relâché. LB et RB sont supposés inutilisés par le jeu.
- Annonces au galop : peuvent demander un réglage (trop ou pas assez).
- Lecture des textes à l'écran : peut être trop bavarde par endroits (désactivable).
- Fonctions multijoueur.
- Battement de cœur de la grotte : première version, à régler (désactivable avec `CaveBeacon = false`).
- Sons des objets et radar sonore : les sons sont-ils assez distincts et clairs ? (à écouter avec Alt+N).

Quand quelque chose ne va pas, `Alt+F3` à côté écrit toute l'île dans
`BepInEx\LogOutput.log` : joignez ce fichier à votre signalement.

## Compiler

Voir la section [Building from source](README.md#building-from-source) de la version anglaise
(SDK .NET 6 ou plus, BepInEx 6 IL2CPP installé et lancé une fois, `lib/native`, puis
`dotnet build KingdomAccess.slnx -c Release`).

## Remerciements

Ce mod s'inspire des fonctions d'accessibilité du mod KingdomEnhanced (raccourcis vocaux et
radar), sans en reprendre le code. Voir la section [Credits](README.md#credits) de la version anglaise. Ce projet n'est pas affilié
aux développeurs ni à l'éditeur du jeu et ne distribue aucun code ni élément du jeu.

## Licence

[MIT](LICENSE). Vous pouvez utiliser, modifier et redistribuer ce code, y compris dans vos
propres mods, à condition de conserver la mention de copyright, qui cite l'auteur :
Alexis (kaminosekai54). Les bibliothèques tierces (Tolk, client NVDA) gardent leurs propres licences.
