# Partage sur les réseaux : recherche et choix (V11)

Le but : qu'une partie de Bascule **se raconte facilement**, qu'elle donne une réaction à filmer (TikTok, Reels, Shorts) et un résultat dont on peut se vanter (un « flex »), et que ceux qui la voient aient envie d'essayer.

## 1. Ce que dit la recherche

Peu d'études sérieuses existent sur le sujet. Les sources trouvées sont surtout de la presse jeu vidéo et des blogs de marketing. Les constats qui reviennent partout :

1. **Les moments « clipables » se propagent le plus.**
   - Exemples : une chute spectaculaire, un sauvetage in extremis, une physique absurde.
   - Il vaut mieux concevoir ces moments dans le jeu que coller une tendance après coup.
   - Sources : [Gamigion](https://www.gamigion.com/?p=33212), [Ecomdy](https://ecomdymedia.com/blog/how-tiktok-became-major-growth-channel-gaming-industry).
2. **La réaction compte autant que le jeu.**
   - *Only Up!* et *Getting Over It* sont devenus viraux grâce aux streamers qui ragent : une seule erreur fait tout perdre, et la règle est simple et juste.
   - Un « presque » ne ressemble pas à un échec : il prouve que c'était possible, donc on rejoue et on en parle.
   - Sources : [Dexerto](https://dexerto.com/gaming/what-is-only-up-climbing-game-goes-viral-as-streamers-cant-stop-raging-at-it-2189869), [Kotaku](https://kotaku.com/streamers-reacting-to-speedruns-of-a-game-they-just-rag-1820415200), [Game Developer](https://gamedeveloper.com/design/designer-interview-the-aesthetics-of-frustration-in-i-getting-over-it-i-).
3. **Le partage de Wordle**, une grille d'émojis :
   - elle raconte la partie **sans la dévoiler** ;
   - elle se copie en un geste ;
   - elle passe partout (SMS, WhatsApp, X) ;
   - et **une partie identique pour tout le monde chaque jour** crée une conversation quotidienne.
   - Sources : [Smithsonian](https://www.smithsonianmag.com/smart-news/heres-why-the-word-game-wordle-went-viral-180979439/), [BuzzFeed News](https://www.buzzfeednews.com/article/stefficao/how-wordle-went-viral-strategy).
4. **Proposer le replay au bon moment**, sur l'écran de fin, juste après le moment fort. C'est ce que faisait Everyplay pour les jeux mobiles indépendants.
   - Sources : [GamesBeat](https://gamesbeat.com/applifiers-everyplay-shares-videos-of-your-best-mobile-game-replays/), [Pocket Gamer](https://www.pocketgamer.com/articles/044318/r/).
5. **Une vidéo filmée doit porter le nom du jeu.**
   - Sinon le spectateur ne peut pas le retrouver.
   - Un code à taper (« même partie que moi ») transforme le spectateur en joueur : on peut lui répondre par un duo ou un stitch.

Prudence : la viralité dépend aussi du moment et de la chance de l'algorithme. Aucun de ces leviers ne la garantit. Ce qui reste vrai : un jeu dont la partie se raconte en une phrase a beaucoup plus de chances d'être montré.

## 2. Ce qui rend Bascule « racontable »

Bascule a déjà les bons ingrédients :
- une **tension visible** : la planche penche, Basile transpire ;
- une **chute physique** : tout dégringole ;
- un **sauvetage de justesse** chiffré : « sauvé à 97 % » ;
- un **score** ;
- une **partie rejouable à l'identique** avec le code défi et le Défi du jour.

Ce qui manquait :
- le moment de la chute passait trop vite pour être filmé ;
- rien ne disait **ce qu'il s'était passé** pendant la partie ;
- la carte n'était pas au format des Stories et de TikTok ;
- une vidéo d'écran ne montrait ni le nom du jeu ni le code.

## 3. Ce que la V11 ajoute (et pourquoi)

| Constat | Dans le jeu |
|---|---|
| Moments clipables, réactions | **Chute au ralenti** pendant environ 1 s, avec les **derniers mots** de Basile (8 répliques, choisies au hasard). La partie est déjà perdue à ce moment-là : le ralenti ne change ni la physique ni le score. |
| Avoir quelque chose à dire | **Titre de la partie** : « Funambule de l'extrême », « Roi du combo », « Mains de beurre », « Chute express », « Légende de la bascule »… L'échec drôle est aussi partageable que l'exploit. |
| Le « presque » fait rejouer et parler | **« À 120 points du record ! »** sur l'écran de fin. Le bouton Partager s'anime aussi après un sauvetage à 95 % ou plus, ou un record frôlé, plus seulement après un record. |
| Raconter sans dévoiler (Wordle) | **Frise de tension** : 🟩 calme, 🟨 ça penche, 🟧 chaud, 🟥 à deux doigts, 💥 la chute. Une case par tranche de 8 s, 10 cases au plus, et on garde toujours le pic. On voit d'un coup d'œil une partie « 🟩🟨🟥🟩🟥💥 ». |
| Copier en un geste | Le **résumé** est copié dans le presse-papiers au moment du partage. TikTok et Instagram ignorent souvent le texte joint : on le colle en légende. |
| Format des réseaux | **Carte 9:16** (1080 × 1920) avec la **vraie photo de la chute**. C'est la caméra du jeu, sans l'interface, prise 0,3 s après la bascule et recadrée sur la tour. |
| Un défi à relever | Code défi sur la carte et dans le résumé. Pour le Défi du jour, le **numéro du jour** remplace le code : « Défi du jour n°12 », pareil pour tout le monde. |
| Le nom du jeu dans les vidéos | **Mode vidéo** (Réglages) : en bas de l'écran pendant la partie, « BASCULE · défi K7PM-3QXZ ». Les conseils sont masqués. On lance l'enregistrement d'écran du téléphone et c'est prêt. |

Exemple de résumé :

```
BASCULE ⚖️ Défi du jour n°12
4 820 points · Funambule de l'extrême
🟩🟨🟧🟥🟩🟨💥
🏆 Nouveau record !
😱 Sauvé à 97 % de la chute
🔥 Combo ×4 · 🎈 ×1
Même parcours pour tout le monde aujourd'hui. À toi !
orpheex.fr/bascule #DéfiBascule
```

Le code est dans `Core/Brag.cs`, testé par `BragTests` :
- titres ;
- frise ;
- record frôlé ;
- numéro du jour, qui commence au 1er novembre 2026 : à changer pour la date de sortie.

## 4. Formats de vidéos à encourager (compte TikTok d'Orphex)

1. **« Sauvé à 99 % »** : le ralenti du sauvetage, avec la réaction du joueur en incrustation.
2. **« Ses derniers mots »** : compilations de chutes avec les répliques de Basile.
3. **« Défi du jour n°X : qui fait mieux ? »** : le score du jour, avec un appel au duo ou au stitch. Tout le monde a la même partie.
4. **« Joue mon code »** : un code défi en légende. Les abonnés rejouent exactement la même partie et répondent avec leur score.
5. **« Mains de beurre »** : le titre le plus honteux de la semaine, envoyé par les joueurs.
6. **« Combien Basile peut porter ? »** : la tour la plus haute, en photo de chute.

Mots-dièse :
- `#DéfiBascule`, déjà dans le résumé ;
- `#Bascule`, `#jeumobile` ;
- `#rage`, ou `#satisfaisant` selon la vidéo.

Lancement conseillé :
- un **défi de la semaine** avec un code fixe, épinglé sur le compte ;
- reposter les meilleures vidéos de joueurs ;
- quelques petits créateurs francophones de jeux mobiles, avec le mode vidéo à leur montrer.

## 5. Pour plus tard (non fait dans cette version)

- **Clip vidéo intégré** : garder les 10 dernières secondes en mémoire et les exporter en MP4 à l'écran de fin. Il faut un module natif :
  - [InstantReplay](https://github.com/nowsprinting/InstantReplay), libre, pour iOS 13+ et Android ;
  - ou [VideoKit](https://www.github.com/natmlx/videokit), payant à l'abonnement. NatCorder est abandonné.

  À tester sur de vrais téléphones (mémoire, chauffe). En attendant, l'enregistrement d'écran du téléphone et le mode vidéo couvrent le besoin.
- **Lien qui ouvre le défi** (`orpheex.fr/bascule/defi/K7PM-3QXZ` qui lance la partie). Il faut configurer les liens d'application (Android App Links et iOS Universal Links) sur orpheex.fr.
- **Mesure** : l'évènement `share_score` (titre, record, mode) est envoyé. Après la sortie, comparer les partages par partie et les installations venues d'un code pour savoir quels titres et quels moments font le plus partager.

Le modèle de 1000 joueurs (`Tools/player_sim.py`) **ne simule pas** le bouche-à-oreille : aucune donnée fiable ne permet de le chiffrer avant la sortie. Ces choix s'appuient sur la recherche ci-dessus, pas sur une simulation.


## Message (V20)

Le texte partagé avec l'image (`Brag.Summary`) se lit comme un message entre amis :
1. le score, et le record s'il est battu (ou l'écart au record) ;
2. le titre de la partie, avec le moment fort (sauvetage, montgolfière, combo) ;
3. le défi : même partie avec le code, puis le lien.

La frise de carrés de couleur n'est plus dans le texte, elle reste sur l'image. Dans un message, elle ne disait rien à ceux qui n'avaient pas joué.
