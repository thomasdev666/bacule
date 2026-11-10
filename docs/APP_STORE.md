# Faire valider Bascule par Apple du premier coup

Ce qui est déjà réglé dans le projet, puis ce qu'il reste à faire à la main dans App Store Connect, avec les textes prêts à coller.

## Déjà réglé dans le projet (version 1.3, build 4)

| Motif de refus fréquent | Ce que fait Bascule |
|---|---|
| 2.1 Fonction incomplète, bouton « Bientôt » | Plus aucun « Bientôt » dans le jeu. Le bouton Duel n'apparaît que si le duel fonctionne. Plus de code « défi » impossible à utiliser sur la carte de score. |
| 2.1 Fausse pub ou pub de test | Pubs simulées seulement dans l'éditeur et les versions de développement. Une version publiée sans régie n'affiche aucune pub (`NoAdService`). |
| 2.3.1 Fonctions cachées | Les outils de test (pièces, jour suivant) n'existent que dans les versions de développement. Le diagnostic des pubs (7 touches sur la version) affiche seulement l'état de la régie et peut ouvrir son outil de test : rien n'est débloqué ni offert. |
| 3.1.1 Achats | Tout passe par l'achat intégré d'Apple. « Restaurer mes achats » est présent (réglages et boutique). Les pièces n'achètent que du style, jamais de score. |
| 4.0 / 4.2 Contenu minimal | Jeu complet hors ligne. En ligne : classement, défi du jour, duel en direct. |
| 1.2 Contenu des utilisateurs | Pseudos filtrés (`PseudoFilter`). Signaler (drapeau) et Bloquer depuis le classement. Règles publiées sur la page d'aide. Contact sous 24 h. Pas de discussion entre joueurs. Noms des adversaires de duel filtrés aussi. |
| 5.1.1 Suppression des données | Réglages → « Supprimer mes données en ligne ». Il n'y a pas de compte à créer. |
| 5.1.2 Suivi publicitaire | Pas de demande ATT. Pubs non personnalisées (`SetConsent(false)`, `do_not_sell`), identifiant publicitaire non collecté (`is_deviceid_optout`). |
| Manifeste de confidentialité (ITMS-91053) | `Assets/Plugins/iOS/PrivacyInfo.xcprivacy` : pas de suivi, données collectées, API à raison obligatoire (UserDefaults CA92.1, dates de fichiers C617.1). Les SDK fournissent le leur. |
| Conformité à l'export (question bloquante avant TestFlight) | `ITSAppUsesNonExemptEncryption = NO` ajouté à chaque build (`Editor/IosBuildPost.cs`). |
| ITMS-90068 (iOS trop ancien) | iOS 15 minimum, iPhone uniquement. |
| Lien d'invitation | Schéma `bascule://` déclaré (réglages du projet et vérification après build). |

## À faire avant d'envoyer la 1.3

1. **Unity** : ouvrir le projet, « Bascule → Configurer le projet » (version 1.3, build 4, schéma `bascule`). Installer **Multiplayer Services** (Package Manager). Dashboard Unity : activer **Relay** et **Lobby**, et régler une alerte de dépense.
2. **Site orpheex.fr** : mettre en ligne `docs/site/bascule/` à jour (`index.html`, `confidentialite.html`, `duel.html`, `style.css`), **avant** l'envoi. Apple ouvre les liens d'aide et de confidentialité.
   - Remplir le dernier passage surligné de `confidentialite.html` : forme juridique, adresse et SIREN d'Orphex (je ne peux pas les inventer).
   - Section « Âge » : la page dit « 13 ans et plus », mais la pub vise les 11–17 ans. Choisir une seule règle et l'aligner avec la classification App Store.
3. **TestFlight, sur deux iPhone** :
   - un achat de test et « Restaurer mes achats » ;
   - une pub récompensée et une pub entre deux parties ;
   - le classement : pseudo, Signaler, Bloquer ;
   - un duel complet : invitation envoyée par Snap ou Messages, lien touché, compte à rebours, K.O., revanche ;
   - quitter l'application pendant un duel (l'autre doit gagner après 8 s).
4. **Vidéo du duel** (fortement conseillé) : filmer l'écran d'un duel sur deux iPhone (30 à 60 s), la mettre en ligne en lien non répertorié (YouTube ou Drive) et ajouter le lien dans les notes ci-dessous. Le testeur d'Apple n'a souvent qu'un appareil.

## Lancement (avant l'envoi)

- Tableau de bord Unity Cloud → **Leaderboards** : créer `bascule_monde_s2` (décroissant, garder le meilleur score) et `bascule_jour_s2` (pareil, remise à zéro quotidienne à 00:00 UTC). Sans eux, le classement reste vide et les scores ne partent pas.
- Au premier lancement de la nouvelle version, chaque joueur repart de zéro (`Core.Seasons`) ; ses achats définitifs et ses pièces achetées non dépensées sont conservés (règle 3.1.1) (obligatoire pour Apple : un achat définitif ne se perd jamais, et « Restaurer mes achats » les rend aussi).

## App Store Connect

### Notes pour la vérification (App Review Information → Notes)

À coller tel quel (en anglais : les testeurs d'Apple le lisent plus vite) :

```
Bascule is a free single-player balance game (French only). No login is required.

HOW TO PLAY: drag to aim, release to drop a weight on Basile's plank. Equal weights merge. Keep the plank balanced.

ONLINE FEATURES (internet required):
- Leaderboards (anonymous player, optional nickname). Nicknames are filtered. Any player can be reported (flag icon) or blocked from the leaderboard. Reports are handled within 24h at contact@orpheex.fr.
- Live duel between two friends (Home > Duel). It needs TWO devices: on device A tap "Défier un ami" to get a 6-character code, on device B tap "Rejoindre un duel" and enter the code (or open the invite link). Both players get the same weights; the first one whose plank falls loses. Only nickname, head and game state are exchanged; there is no chat.
  Demo video of a duel: [LIEN DE LA VIDÉO]

IN-APP PURCHASES: cosmetic items and "No ads" only; coins never change the score. "Restore purchases" is in Settings and in the Shop.

ADS: non-personalized ads via Unity LevelPlay. No App Tracking Transparency prompt because the app does not track users. Interstitials only appear between two games, never during a game.

Data deletion: Settings > "Supprimer mes données en ligne".
```

### Nouveautés de la version 1.3

```
Nouveau : le DUEL EN DIRECT !
Défie un ami : même partie, mêmes poids, et tu vois sa planche en direct. Le premier qui lâche a perdu.
Envoie l'invitation par message ou sur Snap, ton ami rejoint en un toucher.
15 victoires = la tête Challenger.
Et les rafales de vent s'annoncent maintenant en grand : prépare-toi à réagir !
```

### Confidentialité de l'app (App Privacy)

Mettre à jour les réponses pour qu'elles collent au manifeste et à la politique de confidentialité :

- **Collecte de données : Oui.**
- **Identifiants → Identifiant d'utilisateur** : fonctionnalité de l'app ; lié à l'utilisateur : oui ; suivi : non.
- **Contenu utilisateur → Contenu de jeu** (scores, partie envoyée pendant un duel) : fonctionnalité de l'app ; lié : oui ; suivi : non.
- **Contenu utilisateur → Autre contenu** (pseudo, code ami) : fonctionnalité de l'app ; lié : oui ; suivi : non.
- **Données collectées par la régie publicitaire** (Unity LevelPlay et ses réseaux) : comparer avec le guide « Apple privacy » de LevelPlay sur le site d'Unity. En général :
  - Identifiants → Identifiant de l'appareil ;
  - Données d'utilisation → Interaction avec le produit, Données publicitaires ;
  - Diagnostics ;
  - Localisation approximative (déduite de l'adresse IP).

  Finalités : publicité tierce et analyses. Liées : non. Suivi : non, car il n'y a pas de demande ATT et l'IDFA n'est pas lu.
- Déclarer un peu trop n'est jamais un motif de refus ; déclarer trop peu peut l'être.

### Classification d'âge

Répondre honnêtement :

- pas de violence ;
- pas de contenu pour adultes ;
- **publicités** : oui ;
- **achats intégrés** : oui ;
- **contenu généré par les utilisateurs** : oui (pseudos visibles), avec filtre, signalement et blocage ;
- **messagerie ou discussion** : non ;
- **jeux en ligne entre joueurs** : oui (duel avec des amis).

Ne pas choisir la catégorie « Enfants ». Elle interdit la plupart des régies publicitaires.

### Divers

- **Capture d'écran du duel** : facultative, mais elle montre la nouveauté.
- **Numéro de build** : à chaque nouvel envoi, augmenter `BuildNumber` dans `Editor/BasculeSetup.cs`. Apple refuse deux envois avec le même numéro.
