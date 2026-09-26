# Kingdom Access

Un mod qui rend **Kingdom Two Crowns** jouable par les personnes aveugles et malvoyantes.
Il parle à travers votre lecteur d'écran (NVDA, JAWS et d'autres), lit les menus, décrit le
monde autour de vous, permet de parcourir l'île et de marcher vers n'importe quel élément, et
vous prévient par des sons quand le danger approche.

*[English version](README.md)*

> État : **bêta (0.7.0)**. Développé et testé par un joueur aveugle avec NVDA sous Windows, sur la
> version Steam du jeu, surtout dans la campagne des Terres du Nord. Retours et signalements de
> bugs bienvenus.

---

## Ce que fait le mod

**Parole**
- Parle à travers votre lecteur d'écran grâce à Tolk (NVDA, JAWS, SuperNova, ZoomText...), avec
  la voix Windows en secours. Le braille passe par votre lecteur d'écran.
- Suit automatiquement la langue du jeu, y compris quand vous la changez dans les options.
  Français et anglais inclus ; les autres langues retombent sur l'anglais (voir [Langues](#langues)).
- Historique des 50 derniers messages (répéter, précédent, suivant).

**Menus et écrans**
- Menus standard : élément sélectionné, son type (case à cocher, curseur), son état, sa position
  (« 2 sur 5 ») et le texte des fenêtres qui s'ouvrent. Les boutons sans texte ont un nom.
- Écran de nouvelle partie (monde, difficulté, monarque), carte et chronologie, éditeur de blason
  (description de chaque fond et emblème, noms des couleurs), résumé de fin d'île (lu en entier,
  puis parcourable ligne par ligne).
- Tutoriel : les indices du fantôme sont annoncés (action attendue et position du fantôme).
- Textes à l'écran (notifications, bulles d'information) : lus à leur apparition, et regroupables
  dans une liste parcourable.

**Autour de vous**
- L'objet sélectionné par le jeu (l'endroit où l'on paie) est annoncé avec son prix, l'action, ou
  la raison de son verrouillage.
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
- Retour d'une capacité (objet de pouvoir, monarque, monture).

**Contenu des DLC**
- Énigmes des Terres du Nord : Heimdall (moment de la journée de chaque pilier, monture requise,
  cor), Thor (symbole actuel de chaque pilier, s'il est juste, combien sont justes), Hel (ce
  qu'attend chaque support), Loki (marche à suivre et risques). Les énigmes résolues sont annoncées.
- Description des reliques nordiques (marteau de Thor, trophée de Hel, cor de Heimdall, bâton de
  Loki), de certains artefacts de l'Olympe et de nombreuses montures.
- Expédition de la bombe vers la grotte des Greed : chaque étape est annoncée (escorte, entrée,
  traversée, gardien, détonation, sortie), et tant que vous êtes au-delà du portail de la falaise,
  le scanner et le radar ne montrent que cette zone.

## Ce que le mod ne fait pas (encore)

- **Clavier uniquement.** Les raccourcis du mod sont au clavier, pas à la manette.
- **Un seul joueur.** Seul le joueur 1 est suivi. La coopération locale et en ligne n'est pas
  prise en charge, et la fenêtre d'invitation de Steam ne peut être lue par aucun mod.
- **Écrans pilotés à la souris** qui ne sont pas des menus standard : certains peuvent rester
  muets. Signalez-les.
- **L'Olympe (Call of Olympus)** n'est que partiellement couvert : ses énigmes et quêtes n'ont pas
  encore été testées, et plusieurs descriptions d'artefacts et de montures manquent volontairement
  (pas de source fiable).
- **Les autres langues du jeu** ont les textes du jeu, mais ceux du mod en anglais tant qu'une
  traduction n'est pas ajoutée.
- **MelonLoader** n'est pas encore pris en charge (le cœur du mod est indépendant du chargeur,
  seul l'adaptateur BepInEx existe). La version Mono du jeu n'a pas été testée.

## Touches

Touches par défaut, toutes réglables. Elles évitent celles du jeu : le joueur 1 utilise WASD,
les flèches et Maj ; le joueur 2 utilise G, H, J, K, L, I et Maj droite (G et J lancent aussi
l'écran partagé). **F1** en jeu donne la liste de vos raccourcis actuels.

| Touche | Action |
|---|---|
| F1 | Aide : liste des raccourcis (Page haut / Page bas pour parcourir) |
| Maj+F1 | Répéter l'indice du tutoriel en cours |
| F11 / Maj+F11 / Ctrl+F11 | Répéter le dernier message / précédent / suivant dans l'historique |
| F2 | Relire l'écran (fenêtre et élément sélectionné ; résumé du blason) |
| F3 | Mettre tous les textes à l'écran dans une liste |
| O | Or et gemmes |
| T | Jour, saison, moment de la journée, temps avant la nuit ou l'aube |
| M | Monture et fatigue |
| R | Relique et capacités, avec ce qu'elles font |
| C | Boussole : direction, moment de la journée, danger, mur le plus proche |
| P | Population : recensement des troupes (Page haut / Page bas pour parcourir) |
| V | Radar (Page haut / Page bas pour parcourir) |
| Maj+V | Détail de l'objet devant vous |
| Origine / Maj+Origine | Scanner : catégorie suivante / précédente |
| Page haut / Page bas | Élément précédent / suivant de la dernière liste (scanner, radar, recensement, aide, résumé...) |
| Ctrl+Origine | Relire l'élément choisi avec sa distance à jour |
| Fin / Maj+Fin | Marcher / courir vers l'élément choisi (nouvel appui pour arrêter) |
| B | Courir à la base (château ou feu de camp) |
| Maj+C | Courir vers votre couronne perdue |
| Ctrl+Gauche / Ctrl+Droite | Courir juste derrière (à l'intérieur) le mur le plus éloigné de ce côté |
| Maj+F3 / Ctrl+Maj+F3 | Développement : écrire l'objet devant vous / toute l'île dans le journal |

## Installation

1. **BepInEx 6 (IL2CPP).** Le jeu a besoin de BepInEx 6 « bleeding edge » pour IL2CPP, avec les
   correctifs propres à ce jeu. Suivez l'installation de
   [abevol/KingdomMod](https://github.com/abevol/KingdomMod#install) (version de BepInEx et
   correctifs Cpp2IL / Il2CppInterop). Lancez le jeu une fois pour que BepInEx crée ses fichiers.
2. **Le mod.** Décompressez l'archive de la version dans `Kingdom Two Crowns\BepInEx\plugins`. Vous
   devez obtenir `BepInEx\plugins\KingdomAccess\` avec `KingdomAccess.BepInEx.dll`,
   `KingdomAccess.Core.dll`, `Tolk.dll`, `nvdaControllerClient64.dll` et les dossiers `Lang` et `Sounds`.
3. **Lancez le jeu** avec votre lecteur d'écran. Après quelques secondes, vous devez entendre
   « Kingdom Access version ... chargée ».

## Configuration

`BepInEx\config\kingdom.access.cfg` est créé au premier lancement. Chaque réglage et chaque
raccourci y est documenté (en anglais). Sections :

1. **General** : activation, langue forcée, voix Windows en secours, taille de l'historique.
2. **Announcements** : objet sélectionné par le jeu, zones du royaume et des camps, textes à
   l'écran et tutoriel, capacités prêtes.
3. **Radar and scanner** : portées, limitation à la zone explorée et sa marge, portails détruits.
4. **Alerts and sounds** : alerte ennemis et sa distance, alerte couronne, moments de la journée, sons.
5. **Menus** : lecture des menus, journal des menus (développement).
6. **Keys** : chaque raccourci, par exemple `Wallet = O`, `Radar = V`, `TargetDetails = Shift+V`.
   Les noms de touches sont ceux d'Unity (`F5`, `PageDown`, `LeftArrow`...) ; les modificateurs
   sont `Ctrl`, `Shift`, `Alt`. Une valeur vide désactive le raccourci.

Relancez le jeu après avoir modifié le fichier. Les sons sont des fichiers WAV dans
`plugins\KingdomAccess\Sounds` : remplacez-en un par votre propre fichier du même nom.

## Langues

Les textes du mod sont dans [`Localization/`](Localization/README.md), un fichier JSON par
langue. Pour ajouter une langue : copier `en.json`, traduire les valeurs, vérifier avec
`python tools/check_localization.py`. Voir le [guide de traduction](Localization/README.md).

## Encore à tester

Ces parties fonctionnent en principe mais n'ont pas été confirmées en jeu, ou seulement en partie :

- **Course** automatique (Maj+Fin, B, Maj+C, Ctrl+flèches) : le mod demande le galop au jeu ;
  vérifier que le monarque court vraiment, avec chaque monture.
- Énigme de Heimdall : la monture requise (cheval du jour et de la nuit) est déduite des fichiers du jeu.
- Énigme de Thor : on ne sait pas ce qui rend les piliers actifs ; le mod les dit « inactifs ».
- Énigmes de Hel et de Loki : annonces écrites d'après le code du jeu, pas encore jouées de bout en bout.
- Call of Olympus : énigmes (Cerbère, char), quêtes, bâton d'Hermès, montures.
- Grotte des Greed : les « portails temporaires » sont supposés être les nids de Greed ; les
  distances vers l'entrée et le point de détonation peuvent être fausses.
- Alerte de couronne perdue : vérifier que la couronne portée ne déclenche jamais de fausse alerte.
- Lecture des textes à l'écran : peut être trop bavarde par endroits (désactivable).

Quand quelque chose ne va pas, `Ctrl+Maj+F3` à côté écrit toute l'île dans
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
