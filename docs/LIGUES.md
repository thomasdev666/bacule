# Duel classé, ligues et fantômes (1.5)

## Ce que voit le joueur

- Écran **Duel** : sa carte de ligue (écusson de sa division, place, points, temps avant le lundi), le gros bouton **« Duel classé »**, puis les duels entre amis comme avant.
- **Duel classé** : le jeu cherche un adversaire tout seul, sans code. Au bout de 20 s sans personne, il propose le **fantôme** d'un joueur (clairement annoncé).
- **Points de la semaine** : victoire +30, égalité +15, défaite +8 ; contre un fantôme +10 / +5 / +2 ; abandon 0. Les duels entre amis ne rapportent pas de points (sinon deux amis s'arrangeraient).
- **Divisions**, lues sur la place dans le classement de la semaine : Légende (1 à 3), Élite (4 à 10), Challenger (11 à 30), Espoir (les autres).
- **Lundi 00:00 UTC** (1 h ou 2 h du matin en France) : tout repart de zéro. Au premier lancement suivant : « Semaine terminée ! », la division, la place, et les pièces (500 / 250 / 100).
- Pas de revanche en classé : « Nouveau duel classé » cherche un autre adversaire.

Aperçus : `docs/apercu/ecrans_duel_classe_1_5.png`, `docs/apercu/ecrans_ligue_1_5.png`, `docs/apercu/ecrans_amis_1_5.png` (refaits par `python3 Tools/screens_preview_v15.py Exports/art docs/apercu`).

## À faire UNE FOIS dans le tableau de bord Unity (10 minutes)

Unity Cloud → projet Bascule → environnement **production** → **Leaderboards** → **Create leaderboard** :

| Champ | Valeur |
|---|---|
| Name / ID | `bascule_ligue` (exactement) |
| Sort order | **Descending** (le plus de points en premier) |
| Update type | **Keep best** |
| Reset | **Scheduled**, tous les **7 jours**, premier départ un **lundi à 00:00 UTC** (cron équivalent : `0 0 * * 1`) |
| Archive | **cochée** (« Archive scores on reset ») : sans elle, pas de récompense du lundi |
| Tiers / Buckets | aucun (les divisions sont calculées par le jeu) |

Rien d'autre : les fantômes utilisent l'index Cloud Save `friend_code` déjà en place, et le matchmaking utilise le service Multiplayer déjà activé pour le duel.

Vérification : `python3 Tools/verif_publication.py ../bacule-ios --rapide` doit afficher « Classement bascule_ligue existe » et « remise à zéro le lundi 00:00 UTC » en OK. Tant que le classement n'existe pas, le jeu marche quand même : les points sont gardés sur le téléphone et l'écran Ligue affiche « Le classement des ligues arrive ».

## Comment ça marche

**Matchmaking** (`DuelController.FindMatch`, `UgsDuelNetwork.QuickMatchAsync`) : chaque joueur cherche une partie publique « classée » qui attend (propriété indexée `classe-p<version du protocole>`), pendant 2 à 7 s tirées au hasard ; s'il n'en trouve pas, il ouvre la sienne. Pendant qu'il attend, il regarde toutes les 2 à 4 s si un autre joueur attend aussi dans sa partie : celui dont l'identifiant de partie est le plus grand ferme la sienne et rejoint l'autre (`ShouldYieldAsync`). Deux joueurs arrivés au même instant se trouvent donc toujours. Au bout de `league.searchSeconds` (20 s) : fantôme.

**Fantômes** (`Core/League.cs` : `GhostRecorder`, `GhostRecording`, `GhostPlayback` ; `DuelLink`) : pendant un duel classé, 5 photos par seconde de ma partie sont gardées (les mêmes que celles envoyées à l'adversaire). Si ma partie est **complète** (j'ai lâché, ou le temps s'est écoulé), elle est compressée et publiée dans mes données publiques Cloud Save (`ghost`, `ghost_at`), à la place de la précédente. Une partie arrêtée parce que l'autre a lâché, ou un abandon, n'est pas publiée. Pour en trouver un, le jeu part d'une lettre au hasard dans l'index `friend_code` et prend un joueur qui a un fantôme de moins de 60 jours, jamais le sien, pas un joueur bloqué, de préférence pas un des 8 derniers affrontés.

**Synchronisation** : le fantôme rejoue avec **sa graine** et **son compte à rebours**. Le joueur reçoit donc les mêmes poids, dans le même ordre, et les mêmes rafales aux mêmes instants (les rafales sont tirées de la graine, `DuelRules.GustSchedule`). Le fantôme lâche au temps exact de sa partie d'origine ; si le joueur lâche avant, le fantôme « lève les bras » après la fenêtre du dernier poids, comme un vrai adversaire.

**Contre un fantôme** : moins de points de ligue, moitié des pièces, et la victoire ne compte pas pour la tête « Challenger » (15 victoires contre de vrais joueurs).

**Récompense du lundi** (`League.RewardPending`, `League.Claim`, `GameApp.CheckLeagueReward`) : au lancement, si une semaine où j'ai marqué des points est finie et pas encore soldée, le jeu lit ma place dans l'archive de cette semaine (les 100 premiers) et donne les pièces de ma division, une seule fois.

## Limites connues

- **Triche** : les points sont envoyés par le téléphone, comme les scores du classement. Acceptable à cette échelle ; un contrôle par le serveur (Cloud Code) est possible plus tard.
- **Les premiers jours**, il n'y a aucun fantôme : si personne ne cherche, le jeu dit « Personne pour l'instant ». Les fantômes arrivent dès les premiers duels classés joués.
- **Une ancienne version du jeu** ne voit pas les parties classées (et réciproquement si le protocole change un jour).
- Les **skins par division** ne sont pas encore faits : `LeagueState.bestDivision` garde déjà la meilleure division atteinte, pour les donner plus tard.

## Réglages (`bascule_tuning.json`, section `league`)

Points (`winPoints`…), tailles des divisions (`legendPlaces`, `elitePlaces`, `challengerPlaces`), pièces (`legendCoins`…), temps de recherche (`searchSeconds`), fantômes (`ghostSnapshotsPerSecond`, `ghostMinSeconds`, `ghostMaxAgeDays`). Quand il y aura plus de joueurs, élargir les divisions ici (une mise à jour du jeu est nécessaire : le fichier est dans l'application).
