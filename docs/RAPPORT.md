# Rapport : rétention, achats, publicités (simulation de 1000 joueurs)

> **Mise à jour après le test sur iPhone** : les cœurs ont été retirés à ta demande. Un poids tombé coûte seulement des points et casse le combo. La tension passe par la musique et le battement de cœur. Les passages ci-dessous sur les cœurs décrivent la version précédente.

## En bref

- **J'ai trouvé un défaut grave dans ma correction d'hier et je l'ai réparé.** Un cœur perdu par poids tombé faisait durer les parties 45 secondes en moyenne, au lieu de 85. Maintenant, seuls les poids de **8 et plus** coûtent un cœur. Les petits coûtent seulement des points. On regagne un cœur tous les 1 000 points et à chaque 128.
- **Le plus gros problème pour garder les joueurs était la collection.** Avec les anciens paliers (jusqu'à 60 000 points), **22 têtes sur 33 étaient au-delà de ce que réussissent 99 % des parties**. Résultat : **61 % des sessions se terminaient sans aucun progrès.** C'est la première raison pour laquelle on arrête un jeu.
- J'ai ajouté une **progression qui avance à chaque partie**, même ratée :
  - de l'XP et des niveaux, avec 13 têtes à gagner par niveau ;
  - des pièces, avec 16 têtes à acheter en pièces ;
  - 3 missions par jour ;
  - un cadeau du jour sur 7 jours ;
  - un tutoriel pour la première partie ;
  - des rappels (notifications) ;
  - une demande de note sur le store.
- **L'argent :**
  - Une **boutique** avec une offre de départ, l'option sans publicité, 3 paquets de pièces et un pack de 5 têtes « Légendes ».
  - Des **pubs récompensées toujours au choix** du joueur.
  - Des **pubs imposées très encadrées** : entre deux parties seulement, pas avant la 6e partie, au plus une toutes les 3 parties et 3 minutes.
  - Jamais d'avantage en partie contre de l'argent (cahier des charges, section 9).
- **Résultats du modèle :**

| | Avant | Après |
|---|---|---|
| Joueurs encore là au 7e jour | 16 % | 29 % |
| Joueurs encore là au 30e jour | 3,6 % | 7,4 % |
| Temps de jeu par joueur | 16 min | 30 min |
| Revenu par installation | 0,015 € | 0,12 € |

  Ce sont des estimations de modèle, pas des mesures : voir « Méthode » juste en dessous.

## Méthode : ce que « tester avec 1000 joueurs » veut dire ici

Je n'ai pas de vrais joueurs. J'ai fait deux choses, l'une solide, l'autre indicative.

1. **Des parties réellement jouées, c'est le solide** (`Tools/game_library.py`).
   - 400 parties complètes ont été jouées par des joueurs automatiques de 5 niveaux d'adresse.
   - Elles utilisent la même physique (approchée) et **le même fichier de réglages** que le jeu.
   - On en tire des chiffres fiables sur le jeu lui-même : durée des parties, scores, causes de défaite, rareté du 128, part des têtes atteignables.
2. **Un modèle de 1000 joueurs sur 60 jours, c'est l'indicatif** (`Tools/player_sim.py`).
   - Chaque joueur simulé a :
     - un talent ;
     - une envie de jouer qui monte avec les récompenses (records, têtes, niveaux, missions) et baisse avec l'ennui et les pubs imposées ;
     - une tolérance aux pubs ;
     - parfois, une envie d'acheter.
   - Chaque partie qu'il joue est **tirée des 400 vraies parties**, puis ajustée à son talent et à son apprentissage.
   - Le modèle est réglé pour retrouver les moyennes publiques du jeu mobile « casual » : environ 35 % de retour le lendemain, 15 % au 7e jour, 3 % au 30e jour.
   - Ses chiffres absolus (rétention, euros) **dépendent de ces hypothèses**. Elles sont toutes écrites dans `HYP`, en tête du fichier. Il sert surtout à **comparer** deux versions du jeu et à **repérer les défauts de structure**.
   - Test de prudence : si les récompenses motivaient **deux fois moins** que prévu, la nouvelle version ferait encore mieux que l'ancienne :

     | | Avant | Après |
     |---|---|---|
     | 7e jour | 13 % | 19 % |
     | 30e jour | 2,9 % | 5,5 % |

Pour relancer :

```bash
python3 Tools/game_library.py 80
```

```bash
python3 Tools/player_sim.py 1000 60
```

## Ce que la simulation a montré, et ce que j'ai corrigé

### 1. Les cœurs, tels que je les avais faits hier, étaient trop punitifs

Dans les parties simulées, un joueur perd 15 à 20 poids par partie. Ce sont surtout des 1, des 2 et des 4 qui glissent quand la planche penche. Avec « un cœur par poids tombé » et 3 cœurs, la partie s'arrêtait au 30e poids, au bout de 45 s, avec environ 450 points. Avant les cœurs, une partie faisait 1 400 points.

**Correction.** Tout poids tombé coûte des points (4 × sa valeur). Seuls ceux de 8 et plus coûtent aussi un cœur. On regagne un cœur tous les 1 000 points et à chaque 128 envolé.

**Résultat :**
- Une partie dure 65 à 125 s (85 s en médiane). C'est la bonne longueur pour un jeu mobile.
- Moins de 1 % des parties durent moins de 40 s.
- Environ 6 parties sur 10 se terminent faute de cœurs, les autres par la chute de la planche.

Tout se règle dans `bascule_tuning.json`, section `score` (`lifeMinValue`, `lifeEveryPoints`).

Un message de tutoriel explique la règle la première fois qu'un poids tombe, y compris aux joueurs qui connaissaient déjà le jeu.

### 2. La collection était hors de portée

- Les scores simulés sont de 1 350 en médiane, 2 600 pour les 10 % meilleures parties et 6 300 pour les 1 % meilleures.
- Les anciens paliers montaient jusqu'à 60 000 : 22 têtes sur 33 étaient pratiquement impossibles à obtenir.
- Après ses 5 ou 6 premières têtes, un joueur n'avait **plus aucun objectif atteignable**. C'est la cause principale de la chute du modèle « avant » entre le 7e et le 30e jour.

**Correction :** 62 têtes en tout, rangées par sections dans la collection.
- **20 têtes de record**, de 250 à 50 000 points, serrées au début. Avec le tutoriel, on gagne sa 1re tête dès la 1re partie. Les plus hautes restent des défis pour les meilleurs.
- **13 têtes de niveau**, aux niveaux 3 à 45. Le niveau monte à chaque partie, même ratée : le niveau 10 demande environ 2 semaines de jeu régulier, et le niveau 45 plusieurs mois.
- **16 têtes de boutique**, de 400 à 4 000 pièces. Elles portent 8 accessoires nouveaux dessinés pour l'occasion :
  - toque de chef, fleur, oreilles de chat, chapeau de fête ;
  - haut-de-forme, pirate, viking, magicien.
- **6 têtes premium** :
  - le pack Légendes : astronaute, robot, licorne, diablotin, ange ;
  - Basile doré, qui fait partie de l'offre de départ.
- Les 6 têtes d'exploits ne changent pas.

Les têtes déjà gagnées sont conservées : les identifiants n'ont pas changé, et un test le vérifie.

Aperçu : `docs/apercu/tetes.png`.

### 3. Trop de sessions sans progrès

Avant, **61 %** des sessions ne rapportaient rien. Maintenant, chaque partie rapporte quelque chose :
- de l'XP (barre animée en fin de partie) ;
- des pièces (score / 25), avec l'option de les **doubler en vidéo**.

Chaque jour apporte aussi :
- **3 missions** adaptées au niveau du joueur, comme « Fais 3 parties », « Fabrique un 64 » ou « Fais 2 000 points sans perdre de cœur ». On peut en **changer une par jour** en regardant une vidéo, et les 3 missions réussies rapportent un bonus de 150 pièces.
- un **cadeau** qui grossit sur 7 jours (50 → 400 pièces), que l'on peut doubler en vidéo. Un jour manqué fait repartir au jour 1 : c'est le meilleur moteur de retour quotidien connu.

En fin de partie, l'écran montre **l'objectif le plus proche**, par exemple « Encore 214 pièces pour Pirate », « Niveau 10 : nouvelle tête » ou « Encore 380 pts ». C'est ce qui donne envie de relancer une partie.

Dans le modèle, la part des sessions sans progrès passe de **61 % à 31 %**.

### 4. La première partie

- **Tutoriel guidé :**
  - une main animée montre le geste (glisser, lâcher) ;
  - ensuite : « deux poids pareils fusionnent » ;
  - « Bravo » à la première fusion ;
  - « ça penche, pose de l'autre côté » au premier vrai danger.
- **Pas de fenêtre avant de jouer.** Le cadeau du jour n'apparaît qu'après la 1re partie.
- **Paliers de départ.** Les premiers paliers (250, 500) sont pensés pour qu'on gagne une tête dès la 1re partie.

### 5. L'argent

Avant, il n'y avait qu'une pub (revivre) et un achat (retirer les pubs, qui ne servait à rien puisqu'il n'y avait pas de pub imposée). Dans le modèle, cela rapportait **0,003 € par joueur actif et par jour**, soit quasiment rien.

**Pubs récompensées** (au choix, plafond de 20 par jour) :
- revivre ;
- pièces ×2 en fin de partie ;
- cadeau ×2 ;
- 60 pièces gratuites (5 fois par jour) ;
- changer une mission.

**Pubs imposées.** J'ai comparé 5 règles dans le modèle :

| Règle | 7e jour | 30e jour | Minutes / joueur | Revenu / installation |
|---|---|---|---|---|
| Aucune | 30 % | 8,0 % | 31 | 0,123 € |
| Toutes les 5 parties, 5 min | 30 % | 8,6 % | 30 | 0,131 € |
| **Toutes les 3 parties, 3 min (choisie)** | **29 %** | **8,1 %** | **29** | **0,143 €** |
| Toutes les 2 parties, 1 min 30 | 27 % | 7,9 % | 28 | 0,138 € |
| Après chaque partie | 27 % | 8,9 % | 26 | 0,160 € |

Les écarts d'un point au 30e jour sont dans le bruit de la simulation.

« Après chaque partie » rapporte un peu plus dans le modèle. Mais il coûte 15 % de temps de jeu, et le modèle ne compte ni les mauvaises notes sur les stores ni les désinstallations rageuses. J'ai donc retenu **toutes les 3 parties et 3 minutes**, avec en plus ces règles :
- pas avant la 6e partie ;
- pas juste après une vidéo choisie ;
- pas après un record ou une nouvelle tête (on laisse savourer) ;
- pas après une partie de moins de 25 s ;
- 12 au maximum par jour ;
- jamais pendant une partie.

Tout se règle dans `bascule_tuning.json`, section `ads`.

**Achats** (uniquement du confort ou du cosmétique) :

| Produit | Prix | Contenu |
|---|---|---|
| Offre de départ | 3,99 € | sans pub + 2 000 pièces + Basile doré. Proposée une seule fois, pendant 48 h, après la 8e partie |
| Sans publicité | 2,99 € | plus de pub imposée |
| Pièces | 0,99 €, 2,99 €, 6,99 € | 600, 2 200 (+20 %) ou 6 000 (+40 %) pièces |
| Pack Légendes | 4,99 € | 5 têtes exclusives |

Après la 4e pub imposée, une fenêtre propose une fois l'achat « sans publicité ».

Dans le modèle :
- environ **1,4 % des joueurs** achètent quelque chose ;
- l'**offre de départ** fait la grande majorité des ventes ;
- le revenu par installation passe de **0,015 € à environ 0,12–0,14 €** sur 60 jours ;
- selon le prix réel des pubs, cela va de 0,10 à 0,20 €.

### 6. Faire connaître le jeu

- Après un **record**, le bouton Partager s'anime : c'est le moment où l'on a envie de défier un ami.
- La **demande de note** n'arrive qu'une seule fois, juste après un record, et seulement chez un joueur qui est revenu au moins un 2e jour.
- **Rappels** (avec le paquet Mobile Notifications) :
  - la permission est demandée après la 3e partie, avec d'abord une explication ;
  - au plus un rappel par jour, vers 18 h 30 : cadeau prêt, série du défi en danger, ou « Basile s'ennuie » après 3 jours d'absence.

## V10 : pourquoi les joueurs mécontents partent, et ce que j'ai changé

Commande : `python3 Tools/player_sim.py 1000 60 departs`. Pour chaque joueur qui arrête avant 60 jours, le modèle regarde **ce qui a fait baisser son envie** pendant ses trois derniers jours de jeu. C'est un modèle, pas un sondage : il montre où la structure du jeu fait perdre des joueurs, pas des chiffres exacts.

Avec la V9, 625 joueurs sur 1000 partaient. Leurs raisons :

| Raison principale du départ | Part (V9) | Part (V10) | Réponse dans la V10 |
|---|---|---|---|
| « Plus rien à gagner en jouant » | 42 % | 21 % | **Coffre** toutes les 3 parties : 60 à 200 pièces, parfois une tête de la boutique (le 2e coffre en donne toujours une). |
| « Toujours la même chose » | 40 % | 54 % | **Partie spéciale** une partie sur 4 : pièces ×2, XP ×2 ou coffre +2, annoncée sur le bouton Jouer. Seules les récompenses changent, jamais le score : le classement mondial reste juste. |
| « Trop de parties ratées » | 14 % | 21 % | **Objectif de partie** : environ 60 % du record, affiché pendant la partie. L'atteindre rapporte 25 pièces, même quand le record est loin. |
| « Trop de publicités » | 4 % | 3 % | Aucun changement (règle déjà raisonnable). |

Les parts de la V10 se calculent sur moins de départs (602 au lieu de 625). La lassitude reste la première cause : c'est l'usure naturelle d'un jeu, qu'aucune récompense n'efface. Pour la faire baisser, il faudra du nouveau contenu (têtes, événements) après le lancement.

Comparaison complète (1000 joueurs, 60 jours, moyenne de 3 tirages) :

| | V9 | V10 |
|---|---|---|
| Revenus le lendemain (J1) | 46,4 % | 49,6 % |
| Encore là à 7 jours (J7) | 27,6 % | 36,3 % |
| Encore là à 30 jours (J30) | 7,8 % | 8,3 % |
| Minutes jouées par joueur | 28 | 37 |
| Séances sans aucun progrès | 31 % | 18 % |
| Revenu moyen par joueur | 0,115 € | 0,143 € |

Si les récompenses motivent deux fois moins que prévu, la V10 garde un avantage : J7 20,8 % au lieu de 18,6 %.

## Ce que ces chiffres disent du lancement

- Avec environ 0,12 € de revenu par installation, **acheter de la publicité pour attirer des joueurs n'est pas rentable** (une installation coûte souvent 0,50 à 1,50 € en France).
- Le jeu doit grandir par le **bouche-à-oreille** : la carte de score partageable, les duels par code et le défi du jour commun à tous. Les vidéos courtes (TikTok, Reels) d'une planche sauvée à 99 % sont le meilleur canal possible.
- **Lancement test conseillé :** publier d'abord dans un ou deux petits pays francophones (Belgique, Suisse) ou en « test ouvert » sur Google Play. Mesurer pendant 2 semaines, régler, puis lancer en France.

## Mesurer pendant le lancement test

Le jeu enregistre déjà des événements anonymes dans `analytics.jsonl` sur le téléphone. Pour voir les chiffres de tous les joueurs, il faut brancher un service (voir `docs/INTEGRATIONS.md`, section 4 ter).

| Si… | Alors… |
|---|---|
| beaucoup de `run_end` font moins de 40 s, ou ont `cause = coeurs` | les cœurs sont trop durs : monter `lifeMinValue` à 16, ou `lives` à 4 |
| peu de joueurs ont un `tutorial_done` | la 1re partie décourage : regarder le score de cette 1re partie |
| le retour le lendemain (`app_open` au jour 1) est sous 30 % | le problème est dans la 1re session : tutoriel, difficulté du début, temps de chargement |
| le retour au 7e jour est sous 10 % | il manque des objectifs : rapprocher les paliers, augmenter les pièces |
| peu d'`ad_rewarded` avec `placement = double_coins` | les pièces ne font pas envie : baisser le prix des premières têtes |
| la proportion `ad_interstitial` / `run_end` dépasse 1 pub pour 3 parties | vérifier la section `ads` |
| le rapport `iap_done` / `iap_start` est faible | souci de prix ou de store |

## Ce que je n'ai pas pu vérifier

- **Rien n'a tourné dans Unity ce soir** (Unity n'est pas disponible ici). Tout compile pour Android et iOS : les 6 configurations, avec et sans les paquets pub, achats et rappels. Les 78 tests des règles passent. Mais les nouveaux écrans n'ont été vus qu'en **maquettes** (`docs/apercu/ecrans.png` et `ecrans_progression.png`), pas dans le vrai jeu.
- **Les branchements Unity Ads, Unity IAP et Mobile Notifications** ont été vérifiés contre ma connaissance de leurs API, pas contre les vrais paquets. Si Unity signale une erreur après l'installation d'un paquet, c'est probablement un nom qui a changé ; c'est rapide à corriger.
- **Les chiffres de rétention et d'argent** viennent d'un modèle. Seul le lancement test donnera les vrais.

## À faire de ton côté

1. **Tester** dans Unity :
   - une partie complète ;
   - la fin de partie (pièces, XP, ×2) ;
   - la collection (acheter une tête) ;
   - la boutique (achats simulés dans l'éditeur) ;
   - les missions et le cadeau du jour.

   Pour aller vite, **Réglages → Prototype** propose, dans l'éditeur seulement, « Test : +1 000 pièces » et « Test : passer au jour suivant ». Ce dernier permet de revoir le cadeau et de nouvelles missions. Me dire ce qui cloche.
2. **Comptes** (quand tu voudras publier) :
   - Unity Ads (Game ID à mettre dans `GameApp.AdIds`) ;
   - les 6 produits dans Google Play Console et App Store Connect (identifiants dans `docs/INTEGRATIONS.md`) ;
   - l'adresse de la politique de confidentialité (`GameApp.PrivacyUrl`) ;
   - l'identifiant App Store (`GameApp.AppStoreId`).
3. **Paquets** à installer dans Unity (Package Manager) : Advertisement Legacy, In-App Purchasing **4.x**, Mobile Notifications. Chacun s'active tout seul.

## Unity Remote : pourquoi c'était écrasé et impossible à toucher

- **Écran écrasé.** Unity Remote envoie au téléphone l'image de la **vue Game** de l'éditeur, à sa taille. Si la vue Game est en paysage, l'image est écrasée sur le téléphone. Depuis hier, le jeu dessine de toute façon une colonne portrait sur un écran paysage. Mais pour Unity Remote, il faut mettre la vue Game en portrait : liste des résolutions en haut de la vue Game → « + » → 1170 × 2532 (iPhone), ou 9:16.
- **Rien ne se touche.** Il faut utiliser la vue **Game**, pas la vue **Simulator** (Unity Remote ne marche qu'avec Game). Puis : **Edit → Project Settings → Editor → Unity Remote → Device = Any iOS Device**, téléphone branché en USB **avant** d'appuyer sur Play. Sur PC Windows avec un iPhone, il faut aussi **iTunes** (ou « Appareils Apple ») installé pour que Unity voie le téléphone.
- **« Le personnage bouge tout seul »** : c'est voulu. Basile respire, se balance un peu et transpire quand ça penche. Si c'était autre chose (il glissait, tombait sans raison), dis-le-moi.
- Unity Remote est pratique pour viser au doigt, mais l'image est compressée et saccadée. Pour juger le rendu final, il faut une vraie installation (Android : Build And Run ; iPhone : un Mac avec Xcode).
