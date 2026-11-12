# 1000 joueurs pendant 30 jours : combien Bascule rapporte, et comment les garder plus longtemps

Rapport de novembre 2026, version 1.3. Commande pour refaire les calculs :

```
python3 Tools/game_library.py 80
python3 Tools/player_sim.py 1000 30 rapport13
```

Résultats détaillés : `Tools/sim_out/rapport13.json`.

## En bref

- **1000 joueurs installent Bascule.** Sur 30 jours, la **1.2** (en ligne aujourd'hui) rapporte **environ 43 €**. La **1.3** rapporte **59 €** avec la régie actuelle, et **85 €** une fois les réseaux publicitaires et les pubs personnalisées branchés.
- Avec les **améliorations proposées** plus bas (sans ajouter de pub imposée), on monte à **environ 129 €**, soit **+52 %**. Les joueurs qui reviennent au 7e jour passent de 18 % à 26 %.
- **Ce qui manque le plus : une première session plus longue.** Aujourd'hui, la moitié des nouveaux joueurs ne fait que 2 parties (3,6 minutes) le premier jour. Puis **la lassitude** : rien de neuf d'une semaine à l'autre.
- Un joueur rapporte **8 à 13 centimes sur 30 jours**. Le revenu dépend donc surtout du **nombre d'installations** : référencement, partage, duels entre amis, parrainage. Pour gagner 500 € par mois, il faut environ 6 000 installations par mois avec la 1.3 complète, et 3 900 avec toutes les améliorations.

## Comment ces chiffres sont obtenus

Ce sont des **estimations, pas des mesures**. 1000 joueurs simulés jouent chacun 30 jours, et chacun a :
- son talent ;
- son envie de revenir, qui monte avec les récompenses et s'use avec le temps ;
- sa tolérance aux pubs ;
- son envie de payer.

Leurs parties ne sont pas inventées : elles viennent de 400 vraies parties simulées avec la physique et les réglages actuels du jeu (vent compris).

**Calage du modèle :**
- **Retours** : calés sur les moyennes des jeux mobiles « casual ». La 1.2 donne 37 % au jour 1, 15 % au jour 7 et 6 % au jour 30, soit un bon jeu casual.
- **Pubs** : calées sur **ton vrai premier jour** (11 novembre : 37 joueurs, 40 pubs, 0,16 $). Le modèle donne 1,25 pub par joueur le premier jour (réel : 1,08) et 0,0044 € de pub par joueur actif et par jour pour la 1.2 (réel : 0,0043 $).
- **Prix des pubs (eCPM)**, en € pour 1000 pubs vues :

  | Scénario | Pub imposée | Vidéo |
  |---|---|---|
  | S1, aujourd'hui (Unity seul, pubs non personnalisées) | 2,6 | 3,7 |
  | S2, 1.3 avec l'accord des joueurs (environ 42 % de pubs personnalisées) | 3,3 | 4,7 |
  | S3, S2 + 5 réseaux en enchères (AppLovin, Google, Meta, Mintegral, Liftoff) | 4,6 | 6,6 |

- **Achats** : environ 1 joueur sur 100 paie. Après TVA et commission du store, il reste 71 % du prix.

À remplacer par les vrais chiffres dès qu'il y en aura (voir « Mesurer pour de vrai »).

## 1. Les 1000 joueurs pendant 30 jours

| | 1.2 (en ligne) | 1.3 |
|---|---|---|
| Reviennent le jour 1 | 37,0 % | 37,7 % |
| Reviennent le jour 3 | 28,6 % | 30,8 % |
| Reviennent le jour 7 | 15,3 % | **17,9 %** |
| Reviennent le jour 14 | 7,6 % | 8,9 % |
| Reviennent le jour 30 | 6,4 % | 7,1 % |
| Jours joués au total (pour 1000 joueurs) | 4 486 | 4 831 |
| Temps de jeu par joueur sur 30 jours | 21 min | 26 min |
| Temps par jour joué | 4,8 min | 5,3 min |
| Parties par session | 2,2 | 2,3 |
| Pubs imposées par jour joué | 0,33 | 0,34 |
| Vidéos choisies par jour joué | 0,96 | **1,61** |
| Mondes vus (médiane) | 4 | **7** |
| Duels par joueur | – | 0,7 |
| Joueurs qui gagnent la Superstar | – | 1,6 % |

Ce que la 1.3 change :
- **+2,6 points de retour au 7e jour**, grâce aux duels entre amis, aux décors plus tôt et aux places gagnées au classement.
- **+68 % de vidéos choisies**, grâce au coffre doublé, au coffre gratuit et aux exclusivités, sans aucune pub imposée de plus.

## 2. Combien ça rapporte (1000 joueurs, 30 jours)

| | Pubs | Achats | **Total** | Par joueur |
|---|---|---|---|---|
| 1.2, S1 (aujourd'hui) | 20 € | 23 € | **43 €** | 4,3 centimes |
| 1.3, S1 (même régie) | 33 € | 26 € | **59 €** | 5,9 centimes |
| 1.3, S2 (avec accord) | 42 € | 26 € | **68 €** | 6,8 centimes |
| 1.3, S3 (avec 5 réseaux) | 59 € | 26 € | **85 €** | 8,5 centimes |
| 1.3, S3 + toutes les améliorations | 103 € | 26 € | **129 €** | 12,9 centimes |

**Quand l'argent arrive** (1.3, S3) :
- **20 % le premier jour** ;
- 45 % du jour 1 au jour 7 ;
- 35 % du jour 8 au jour 30.

Les premiers jours comptent le plus.

**Achats :** le pack de départ fait l'essentiel (6 achats pour 1000 joueurs), puis les pièces (2) et le pack Légendes (1). Le « Sans publicité » se vend peu, parce que les pubs imposées sont déjà rares.

**Pour 500 € par mois :**
- 1.2 aujourd'hui : environ 11 600 installations par mois ;
- 1.3 complète : environ 5 900 ;
- avec les améliorations : environ 3 900.

Payer pour des installations (publicité pour le jeu) coûte 0,50 à 1,50 € par installation en France, alors qu'un joueur rapporte moins de 0,15 €. **Ce n'est pas rentable** : il faut des installations gratuites (référencement, partage, amis).

## 3. Pourquoi ils partent

1. **Première session trop courte.** La moitié des joueurs fait 2 parties et 3,6 minutes le premier jour. Un joueur qui n'a rien gagné ni compris dans sa première session revient rarement. 63 % ne reviennent pas le lendemain.
2. **La lassitude.** C'est la cause principale des départs dans le modèle : au bout de quelques jours, chaque partie ressemble à la précédente. Il manque **un rendez-vous qui change chaque semaine**.
3. **Les trois questions avant de jouer.** Pseudo, année de naissance et pubs passent avant la première partie. Le modèle compte environ 3 % d'installations perdues là.
4. **Les pubs ne font pas fuir** : 0,34 pub imposée par jour joué, c'est très peu. Les doubler ne ferait perdre que 0,7 point au 7e jour. La marge est là si besoin, mais ce n'est pas la priorité.

## 4. Les améliorations, classées par effet

Mesurées une par une sur la 1.3, en scénario S3. Aucune n'ajoute de pub imposée.

| Amélioration | Jour 1 | Jour 7 | Jour 30 | Revenu sur 30 jours |
|---|---|---|---|---|
| 1.3 telle quelle | 37,7 % | 17,9 % | 7,1 % | 85 € |
| **Quête du débutant** | **44,9 %** | 20,2 % | 6,4 % | **+21 %** |
| **Événement de la semaine** | 39,1 % | **20,8 %** | **8,2 %** | **+13 %** |
| Premier coffre dès la 1re partie | 39,4 % | 17,9 % | 7,4 % | +6 % |
| Rappels acceptés par 70 % des joueurs (au lieu de 50 %) | 38,3 % | 18,6 % | 8,0 % | +5 % |
| Parrainage (2 fois plus de joueurs avec un ami) | 38,3 % | 19,2 % | 7,8 % | +4 % |
| Âge et pubs demandés après la 1re partie | 38,5 % | 18,2 % | 7,3 % | +1,5 % |
| **Tout ensemble** | **50,4 %** | **26,2 %** | **10,3 %** | **+52 %** |
| *Pour comparer : pub imposée toutes les 2 parties* | 37,5 % | 17,2 % | 7,2 % | +10 % |
| *Pour comparer : pub imposée toutes les 4 parties* | 37,7 % | 17,7 % | 7,4 % | −2 % |

### Quête du débutant (le plus important)

Trois étapes affichées dès la première partie, avec une vraie récompense à chacune :
1. « Joue ta 1re partie » : une tête.
2. « Fais 500 points » : des pièces et un mini-coffre.
3. « Joue 5 parties » : la planche Bambou.

Une petite barre « Quête du débutant 2/3 » en haut de l'accueil, qui disparaît une fois finie. Le joueur sait quoi faire et gagne quelque chose tout de suite : sa première session passe de 2 à 4 ou 5 parties.

### Événement de la semaine (contre la lassitude)

Chaque lundi, un thème et un objectif commun. Par exemple : « Semaine brûlante : cumule 50 000 points », ou « 30 fusions de 64 ».
- La récompense est cosmétique et limitée à la semaine : une tête, une planche, un fond de profil.
- Un petit classement de la semaine est possible : un nouveau classement `bascule_semaine`, remis à zéro chaque lundi.
- Le **défi du jour** existe déjà. L'événement donne une raison de revenir **plusieurs jours de suite**.

### Premier coffre dès la 1re partie

Le premier coffre s'ouvre après la 1re partie au lieu de la 3e. C'est l'animation la plus réussie du jeu : la montrer tout de suite.

### Rappels au bon moment

Aujourd'hui, la demande arrive après la 3e partie. Mieux :
- la faire **juste après le premier coffre** ou le premier record, avec une phrase claire : « Je te préviens quand ton cadeau est prêt ? » ;
- des rappels plus concrets : « Ton coffre du jour t'attend », « Plus que 2 vidéos pour la Superstar », « Ta série de 4 jours est en danger ».

### Parrainage et duels

- Après un **duel gagné**, proposer « Défie un autre ami » avec le lien.
- Si ton ami installe le jeu avec ton code, **vous gagnez tous les deux une tête « Duo »**. C'est aussi le meilleur moyen d'avoir des installations gratuites.

### Âge et pubs après la première partie

Garder le pseudo au premier lancement (c'est ce que tu as demandé). Poser l'année de naissance et la question des pubs **à la fin de la première partie**, avant la première pub : le joueur a déjà joué, il ne part pas avant d'avoir essayé.

### Pubs : ne pas toucher à la fréquence

- Pub imposée toutes les 2 parties : +10 % de revenu, mais −0,7 point au 7e jour.
- Avec si peu de joueurs, mieux vaut garder les joueurs.
- Le vrai gain sur les pubs vient du **prix** :
  - accord des joueurs : ×1,25, déjà dans la 1.3 ;
  - réseaux en enchères : ×1,4, à brancher, voir `docs/PUBS.md`.

## 5. Mesurer pour de vrai

Le jeu n'envoie aujourd'hui aucune statistique de partie. Pour vérifier ce rapport, il y a trois sources :

1. **LevelPlay → Reports → Cohorts.** Les joueurs regroupés par jour d'installation, avec le retour aux jours 1, 7 et 30 et ce que chaque groupe rapporte en pubs. C'est la meilleure source, gratuite et déjà là.
2. **App Store Connect → Analyses.** Installations, sessions, retours (seulement pour les joueurs qui partagent leurs données avec les développeurs).
3. **Plus tard, Unity Analytics** (avec l'accord des joueurs) :
   - le nombre de parties de la première session ;
   - où les joueurs quittent (questions, première partie, coffre) ;
   - les vidéos regardées par emplacement.

Dès qu'il y aura une ou deux semaines de vrais chiffres (retour au jour 1 et au jour 7, pubs par joueur, eCPM), remplace les hypothèses de `Tools/player_sim.py` (`HYP`, `ECPM`) et relance le rapport.

## 6. Plan d'action

| Ordre | Quoi | Qui | Effet estimé |
|---|---|---|---|
| 1 | Brancher les 5 réseaux publicitaires (`docs/PUBS.md`) | toi (comptes) + build | ×1,4 sur les pubs |
| 2 | Quête du débutant | code | +21 % |
| 3 | Premier coffre dès la 1re partie | code (1 réglage) | +6 % |
| 4 | Âge et pubs après la 1re partie, rappels après le premier coffre | code | +6 % |
| 5 | Événement de la semaine | code + un thème par semaine | +13 % |
| 6 | Parrainage « Duo » et défi après un duel gagné | code | +4 % et des installations gratuites |
| 7 | Suivre LevelPlay Cohorts chaque semaine, recaler le modèle | toi | des chiffres réels |

Les points 2 à 6 se codent sans toucher aux règles du jeu ni aux pubs imposées : rien ne change le score.
