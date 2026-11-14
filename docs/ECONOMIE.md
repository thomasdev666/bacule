# Économie de Bascule (V18)

**Règle d'or** : les pièces n'achètent que du **style**. Rien ne change le score, la physique, la taille de la planche, les poids qui tombent ou la difficulté. Un joueur qui paie et un joueur qui ne paie pas ont exactement les mêmes chances au classement.

## 1. Ce que dit la recherche

- **Le cosmétique est la dépense idéale.** Il retire des pièces sans fausser la compétition. Il faut aussi des objets **chers et rares**, pour les joueurs les plus investis. Sources : [Unity, guide d'économie (partie 2)](https://unity.com/how-to/building-in-game-economy-guide-part-2), [Unity (partie 3)](https://activation.unity3d.com/how-to/design-balanced-game-economy-guide-part-3), [Pulse Geek](https://pulsegeek.com/articles/virtual-economy-basics-systems-sinks-and-stability/).
- **Équilibrer les gains et les dépenses.** Trop de gains et les pièces ne valent plus rien. Trop peu et les joueurs se sentent bloqués. Un joueur qui ne paie jamais doit pouvoir tout obtenir en jouant. Sources : [Adrian Crook](https://adriancrook.com/?p=8177), [Android Developers](https://android-developers.googleblog.com/2015/10/virtual-currency-sources-and-sinks.html?hl=es_419&m=1).
- **La « machine à surprises » de Crossy Road** (des pièces contre un personnage au hasard) est le modèle le plus cité pour un jeu d'arcade gratuit sans pression. Elle est aussi critiquée pour son côté jeu de hasard. Sources : [Mobile Dev Memo](https://mobiledevmemo.com/crossy-road-a-case-study-in-mobile-ad-monetization), [Pocket Gamer](https://www.pocketgamer.com/crossy-road/crossy-road-uses-a-free-to-play-model-to-make-over-1m-without-going-the-dungeon/).
- **Apple impose d'afficher les chances** de tout tirage au sort payant (règle 3.1.1, depuis 2017). Sources : [PC Gamer](https://www.pcgamer.com/apple-now-requires-app-store-games-to-disclose-loot-box-odds/), [Game Developer](https://gamedeveloper.com/business/guideline-changes-mean-app-store-devs-must-now-reveal-loot-crate-odds).

Choix de Bascule pour la machine :
- **pas de doublon** : on gagne toujours quelque chose de nouveau ;
- **chances affichées** avant chaque ouverture ;
- **tout ce qu'elle donne s'achète aussi directement** : le hasard reste un choix.

## 2. D'où viennent les pièces

| Source | Combien |
|---|---|
| Chaque partie | score ÷ 25 (×2 pendant une partie spéciale « pièces ») |
| Objectif de partie atteint | +25 |
| Missions du jour | 50 à 90 chacune, +150 si les trois sont faites |
| Cadeau du jour (V20) | jour 1 : 50 ; jour 2 : coffre ; jour 3 : 120 ; jour 4 : surprise gratuite ; jour 5 : 200 ; jour 6 : coffre ; jour 7 : planche + 300 (ou 600 pièces). Pièces ×2 en vidéo |
| Coffre (toutes les 3 parties, cadeau du jour, roue, vidéo, événement) | 1.5 : rareté tirée d'abord, chances écrites sous le coffre : **Commun 60 %** (60 à 110 pièces), **Rare 25 %** (150 à 230), **Épique 9 %** (280 à 400), **Légendaire 6 %** (une tête de la boutique + 100 à 150 pièces ; s'il n'en reste plus : épique). Le tout premier coffre est au moins rare. Moyenne 137 pièces (avant : 130) ; une tête dans 6 % des coffres (avant : 8 %). `Core/Engagement.cs` (`Chest`) |
| Passage de niveau | 50 + 10 × niveau |
| Pièces gratuites en vidéo | 60, 5 fois par jour |
| Paquets payants | 600 (0,99 €), 2 200 (2,99 €), 6 000 (6,99 €) |

Un joueur régulier (une dizaine de parties par jour) gagne environ 800 à 1 200 pièces par jour.

## 3. Où vont les pièces

| Dépense | Prix | Pourquoi |
|---|---|---|
| **Têtes de la boutique** (16) | 600 à 8 000 | La collection, visible sur Basile et dans le classement |
| **Machine à surprises** (V20) | 1 000, ou un ticket du cadeau du jour | Un lot au hasard : tête de la boutique (55 %), planche (15 %) ou pièces (30 % : 400, 1 200, jackpot 3 000). Sans doublon : têtes et planches déjà gagnées ne sortent plus. Dans chaque famille, les objets chers sortent plus rarement (1/prix). Toutes les chances sont affichées avant l'ouverture. La valeur moyenne en pièces reste bien sous le prix. |
| **Planches** (6) | 500 à 5 000 | Bambou, Skate, Surf, Marbre, Sucre d'orge, **Or massif** (l'objet de prestige, 5 000). Seule l'apparence change : même taille, même poids, mêmes rebords. |
| **Protège-série** | 300 (2 au plus en réserve) | Garde la série du cadeau du jour si l'on oublie un jour. Il s'utilise tout seul. |

Total pour tout avoir : environ 56 000 pièces de têtes et 10 900 de planches. Un joueur gratuit régulier y arrive en plusieurs mois. Un joueur qui achète un paquet y arrive plus vite, sans jamais jouer mieux.

## 3 bis. Rareté des têtes (V18)

Chaque tête a une rareté, affichée dans la collection avec un halo et une étiquette de couleur :

| Rareté | Couleur | Exemples |
|---|---|---|
| Commune | gris | Basile, paliers sous 3 000 points, niveaux 3 à 12, boutique sous 2 000 pièces |
| Rare | bleu | paliers de 3 000 à 10 000, niveaux 16 à 25, boutique jusqu'à 3 400, série de 5 jours, 128 envolé, combo ×5 |
| Épique | violet | paliers de 10 000 à 25 000, niveaux 30 à 40, boutique jusqu'à 5 800, sauvetage à 97 %, série de 14 jours |
| Légendaire | or | paliers à partir de 25 000 (jusqu'à 75 000), niveaux 45 à 60, boutique de 6 500 à 8 000, top 100 mondial, têtes premium |

Les têtes étaient trop faciles à obtenir : le joueur médian en avait 21 au bout de 60 jours. Quatre réglages ont été comparés sur le modèle de 1 000 joueurs :

| Réglage | Encore là après 7 jours | Après 30 jours | Minutes par joueur | Têtes après 60 jours (médiane) |
|---|---|---|---|---|
| V17 (avant) | 38,2 % | 9,6 % | 41 | 21 |
| Tout rare | 29,6 % | 9,3 % | 33 | 11 |
| **Début doux, puis rare (retenu)** | 31,8 % | 9,4 % | 34 | 13 |
| Début très doux, puis rare | 33,2 % | 9,2 % | 36 | 15 |

Plus les têtes sont rares, plus le modèle perd de joueurs dans la première semaine. Après 30 jours, l'écart est faible. Le réglage retenu garde quatre têtes faciles au tout début (250, 500, 1 000 et 1 500 points) pour accrocher les nouveaux joueurs. Ensuite, chaque tête demande un vrai effort. Si les premiers retours montrent trop de départs en première semaine, il suffit de baisser les premiers paliers dans `Core/HeadCatalog.cs`.

## 4. Ce qui est volontairement exclu

- **Revivre avec des pièces** : ce serait acheter du score.
- **Voir plus de poids à l'avance, planche plus longue, ralenti à volonté** : ce serait acheter de la facilité.
- **Rejouer le Défi du jour** pour améliorer son score du jour : une seule partie compte pour tout le monde.

Revivre reste possible avec une vidéo, une fois par partie, pour tout le monde de la même façon.

## 5. Effet attendu (modèle de 1 000 joueurs)

Commande : `python3 Tools/player_sim.py 1000 60 economie`. C'est un modèle de joueurs simulés : il donne une tendance, pas une promesse.

Résultats de la V17 (avant le passage aux têtes plus rares) :

| | V10 | V17 (nouvelle économie) |
|---|---|---|
| Encore là après 7 jours | 36,3 % | 38,2 % |
| Encore là après 30 jours | 8,3 % | 9,6 % |
| Minutes jouées par joueur | 37 | 41 |
| Séances sans aucun progrès | 18 % | 16 % |
| Paquets de pièces achetés (pour 1 000 joueurs) | 4,0 | 5,0 |
| Revenu par joueur (60 jours) | 0,143 € | 0,151 € |

Les nouvelles dépenses donnent toujours un objectif proche : une planche, une surprise ou une tête. Elles ajoutent aussi un objectif lointain qui donne envie de compléter ses pièces : l'**Or massif**.

## 6. Où c'est dans le code

- `Core/Cosmetics.cs` : planches, machine à surprises, protège-série. Testés par `CosmeticsTests`.
- `Core/SceneArt.cs` : `SceneArtist.Plank(…, PlankSkin)` dessine chaque planche.
- `Runtime/UI/MetaScreens.cs` (`ShopScreen`) : cartes Surprise, Planches et Protège-série.
- `Runtime/GameApp.cs` : `OpenSurprise`, `ConfirmSurprise`, `TapPlank`, `BuyShield`. Les chances sont affichées avant l'achat, tête par tête (`SurpriseScreen`, dans `Runtime/UI/MetaScreens.cs`), puis un carrousel montre la tête gagnée.
- `Tools/player_sim.py` : `DESIGNS["v17"]`, `PLANK_PRICES`, `SURPRISE_PRICE`, `SHIELD_PRICE`. Ces valeurs sont à garder synchronisées avec `Core/Cosmetics.cs`.
