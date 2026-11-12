# Arène : duels classés, divisions et saisons (plan, à lancer plus tard)

Aujourd'hui, le duel se joue entre amis (code, invitation). L'arène ajoute des adversaires **inconnus** de même niveau, trouvés automatiquement, avec des divisions et un classement des duels.

Ce n'est pas encore activé : il faut **assez de joueurs connectés en même temps**. Sans eux, l'attente devient longue et les joueurs partent.

## Quand l'activer

| Signal (LevelPlay → Cohorts, ou Unity Cloud) | Seuil conseillé |
|---|---|
| Joueurs actifs par jour | 1 500 ou plus |
| Joueurs connectés au même moment, le soir | 40 ou plus |
| Duels entre amis par jour | 100 ou plus |

En dessous, garder les duels entre amis, et peut-être un **« duel contre un fantôme »** :
- on rejoue la partie enregistrée d'un vrai joueur de même niveau ;
- c'est jouable seul, sans attente, et c'est un bon test de l'arène avant d'y passer.

## Divisions

Chaque victoire rapporte des **trophées**, chaque défaite en retire un peu (jamais moins que le palier de la division).

| Division | Trophées | Couleur |
|---|---|---|
| Bois | 0 à 199 | marron |
| Bronze | 200 à 499 | bronze |
| Argent | 500 à 899 | argent |
| Or | 900 à 1 399 | or |
| Platine | 1 400 à 1 999 | turquoise |
| Diamant | 2 000 à 2 799 | bleu glacier |
| Champion | 2 800 et plus | violet |
| **Légende** | top 100 mondial des trophées | arc-en-ciel |

**Points par match :**
- victoire : +30 trophées ;
- défaite : −20 ;
- adversaire bien plus fort : jusqu'à +40 si tu gagnes, −10 seulement si tu perds.

Le **classement mondial (MMR, type Elo)** reste caché : il sert seulement à trouver des adversaires de même niveau.

## Matchmaking

- **Unity Multiplayer Services → Matchmaker**. Ce service fait partie des mêmes services que les sessions et le relais déjà utilisés par le duel. Une file « arène » met en face deux joueurs de MMR proche ; l'écart accepté s'élargit avec l'attente (100 points au départ, +50 toutes les 5 s).
- **Attente maximale de 30 s**, puis « Personne de ton niveau : essaie contre un fantôme ? ».
- **Région** : Europe (comme le relais du duel).
- **Le jeu ne change pas** : mêmes poids pour les deux (même graine), mêmes rafales, le premier qui lâche perd. Tout le code du duel (`DuelController`, `DuelRules`) est réutilisé.

## Saisons (4 semaines)

- **Classement des duels** : un classement UGS `bascule_arene_sN` qui garde les trophées. Il est remis à zéro à chaque saison, avec la même règle de remise à zéro que les saisons du jeu.
- **Fin de saison** : les trophées redescendent à moitié du chemin vers 0 (« réinitialisation douce »).
- **Récompenses de fin de saison, uniquement cosmétiques** :

  | Division atteinte | Récompense |
  |---|---|
  | Bronze et plus | un coffre |
  | Or et plus | une planche de saison |
  | Diamant et plus | une tête de saison |
  | Légende | une couronne animée sur le portrait, et le nom dans « Les légendes de la saison » |

## Équité et triche

- Le serveur ne fait pas tourner la partie. Chaque téléphone envoie ses événements : poids posés, chute, score.
- Contrôles simples :
  - durée cohérente avec le score ;
  - même graine des deux côtés ;
  - abandon = défaite.
- Seules les **victoires contre un vrai adversaire** comptent pour le classement.
- Plus tard, si besoin : les trophées passent par **Cloud Code**, une petite fonction serveur de Unity, au lieu d'être écrits par le téléphone.

## Interface prévue

- **Onglet « Arène » dans l'écran du duel** :
  - ta division (gros écusson), tes trophées et la barre vers la division suivante ;
  - un bouton « Trouver un adversaire » ;
  - une animation de recherche (radar, portraits qui défilent).
- **Fin de duel** : « +30 🏆 », et l'écusson qui grandit au passage de division.
- **Classement** : un nouvel onglet « Arène » (podium et « Ta place », comme les autres).

## Coût

- **Matchmaker et relais** : dans la limite gratuite de Unity tant que les duels restent courts (2 à 3 minutes). Mettre une alerte de dépense dans Unity Cloud.
- **Code** : environ 1 à 2 semaines de développement, car le duel existe déjà. La plus grosse partie est l'interface et les récompenses de saison.
