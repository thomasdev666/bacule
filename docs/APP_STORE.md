# Faire valider Bascule par Apple du premier coup

Ce qui est déjà réglé dans le projet, puis ce qu'il reste à faire à la main dans App Store Connect, avec les textes prêts à coller.

## Déjà réglé dans le projet (version 1.4)

| Motif de refus fréquent | Ce que fait Bascule |
|---|---|
| 2.1 Fonction incomplète, bouton « Bientôt » | Plus aucun « Bientôt » dans le jeu. Le bouton Duel n'apparaît que si le duel fonctionne. Plus de code « défi » impossible à utiliser sur la carte de score. |
| 2.1 Fausse pub ou pub de test | Pubs simulées seulement dans l'éditeur et les versions de développement. Une version publiée sans régie n'affiche aucune pub (`NoAdService`). |
| 2.3.1 Fonctions cachées | Les outils de test (pièces, jour suivant) n'existent que dans les versions de développement. Le diagnostic des pubs (7 touches sur la version) affiche seulement l'état de la régie et peut ouvrir son outil de test : rien n'est débloqué ni offert. |
| 3.1.1 Achats | Tout passe par l'achat intégré d'Apple. « Restaurer mes achats » est présent (réglages et boutique). Les pièces n'achètent que du style, jamais de score. |
| 4.0 / 4.2 Contenu minimal | Jeu complet hors ligne. En ligne : classement, défi du jour, duel en direct. |
| 1.2 Contenu des utilisateurs | Pseudos filtrés (`PseudoFilter`). Signaler (drapeau) et Bloquer depuis le classement. Règles publiées sur la page d'aide. Contact sous 24 h. Pas de discussion entre joueurs. Noms des adversaires de duel filtrés aussi. |
| 5.1.1 Suppression des données | Réglages → « Supprimer mes données en ligne ». Il n'y a pas de compte à créer. |
| 5.1.2 Suivi publicitaire | Demande ATT seulement après l'accord du joueur dans le jeu (15 ans et plus), texte NSUserTrackingUsageDescription clair. Refus ou moins de 15 ans : pubs non personnalisées (`SetConsent(false)`, `do_not_sell`, `is_deviceid_optout`, enfant signalé). |
| Manifeste de confidentialité (ITMS-91053) | `Assets/Plugins/iOS/PrivacyInfo.xcprivacy` : pas de suivi, données collectées, API à raison obligatoire (UserDefaults CA92.1, dates de fichiers C617.1). Les SDK fournissent le leur. |
| Conformité à l'export (question bloquante avant TestFlight) | `ITSAppUsesNonExemptEncryption = NO` ajouté à chaque build (`Editor/IosBuildPost.cs`). |
| ITMS-90068 (iOS trop ancien) | iOS 15 minimum, iPhone uniquement. |
| Lien d'invitation | Schéma `bascule://` déclaré (réglages du projet et vérification après build). |

## À faire avant d'envoyer la 1.4

1. **Unity** : ouvrir le projet, « Bascule → Configurer le projet » (version 1.4, build 8, schéma `bascule`). Installer **Multiplayer Services** (Package Manager). Dashboard Unity : activer **Relay** et **Lobby**, et régler une alerte de dépense.
2. **Site orpheex.fr** : mettre en ligne `docs/site/bascule/` à jour (`index.html`, `confidentialite.html`, `duel.html`, `style.css`), **avant** l'envoi. Apple ouvre les liens d'aide et de confidentialité.
   - `confidentialite.html` : forme juridique (entrepreneur individuel) et SIREN 939 004 032 renseignés ; reste l'adresse de domiciliation à choisir (surlignée).
   - Section « Âge » : la page dit « 13 ans et plus », mais la pub vise les 11–17 ans. Choisir une seule règle et l'aligner avec la classification App Store.
3. **TestFlight, sur deux iPhone** :
   - un achat de test et « Restaurer mes achats » ;
   - une pub récompensée et une pub entre deux parties ;
   - le classement : pseudo, Signaler, Bloquer ;
   - un duel complet : invitation envoyée par Snap ou Messages, lien touché, compte à rebours, K.O., revanche ;
   - quitter l'application pendant un duel (l'autre doit gagner après 8 s).
4. **Vidéo du duel** (fortement conseillé) : filmer l'écran d'un duel sur deux iPhone (30 à 60 s), la mettre en ligne en lien non répertorié (YouTube ou Drive) et ajouter le lien dans les notes ci-dessous. Le testeur d'Apple n'a souvent qu'un appareil.

## Lancement (avant l'envoi)

- **Rien n'est remis à zéro** : les joueurs de la 1.2 gardent record, têtes, pièces, niveau, planches, missions, série, code ami et amis (`Seasons.ResetOldSaves = false`, test `A12SaveIsKeptIntactWhenUpdating`).
- **Classements** : `bascule_monde` et `bascule_jour` (ceux de la 1.4). La version publiée avant (export « 1.3 », affichée 1.2) écrivait dans `bascule_monde_s2` / `bascule_jour_s2` : les records des 48 joueurs ont été copiés le 13/11/2026 avec `Tools/copy_leaderboards.py`, à relancer tant que des joueurs ont l'ancienne version.

## App Store Connect

### Notes pour la vérification (App Review Information → Notes)

À coller tel quel (en anglais : les testeurs d'Apple le lisent plus vite) :

```
Bascule is a free balance game (French only). No account and no login are required: everything works right after install.

HOW TO PLAY: drag to aim, release to drop a weight on Basile's plank. Equal weights merge. Keep the plank balanced as long as possible.

FIRST LAUNCH: a one-time welcome screen appears ("Bienvenue !"), tap "C'est parti !" (see FIRST LAUNCH STEPS below).

ONLINE FEATURES (internet required, all optional):
- Leaderboards (Home > trophy): anonymous player, optional nickname. Nicknames are filtered. Tap any player to Report ("Signaler") or Block ("Bloquer"). Reports are handled within 24h at contact@orpheex.fr.
- Friends (Leaderboard > "Mon code ami" / "Ajouter un ami"): players add each other with a 6-character friend code. A friend can be removed at any time ("Retirer des amis").
- Live duel (Home > Duel). It needs TWO devices: on device A tap "Défier un ami" to get a 6-character code, on device B tap "Rejoindre un duel" and enter the code. A friend can also be invited from "Mes amis". Both players get the same weights; the first plank to fall loses. Only nickname, head and game state are exchanged. There is NO chat and no free text between players.
- A demo video of a full duel on two iPhones is attached to this submission.

IN-APP PURCHASES: cosmetics (heads, planks, coins) and "Sans publicité" (removes forced interstitials). Coins only buy cosmetics and never change the score or the gameplay. "Restaurer mes achats" is in Settings and in the Shop.

FIRST LAUNCH STEPS: after the welcome screen, the player chooses a nickname (can be skipped), then enters a year of birth (neutral grid of years). Players aged 15+ are then asked whether they accept personalized ads ("Accepter" / "Refuser", same size, same place). Only if they accept, the iOS App Tracking Transparency prompt is shown. Under 15: ads are never personalized, no ATT prompt, ad networks are told the user is a child. The choice can be changed anytime in Settings > "Pubs personnalisées".

ADS: Unity LevelPlay mediation. Interstitials only appear between two games, never during a game. All other ads are optional rewarded videos chosen by the player: second chance after losing, free chest, double the chest, and an exclusive cosmetic head and plank earned after 8 videos (cosmetic only, never a gameplay advantage).

PROMO CODE (Settings > "Code promo"): partner codes unlock one FREE cosmetic head that is not sold anywhere in the app. No paid content, no coins and no gameplay advantage can be unlocked with a code. No partner code is active in this version.

NOTIFICATIONS: optional, at most one local reminder per day, asked only after the first games.

DATA DELETION: Settings > "Supprimer mes données en ligne" deletes the online player, nickname, scores and friend code.
```

### Nouveautés de la version

Voir plus bas « Nouveautés de cette version » (texte à jour, avec le duel en direct).

### Confidentialité de l'app (App Privacy), à refaire pour la 1.4

Depuis la 1.4, les pubs peuvent être personnalisées (avec l'accord du joueur et la demande de suivi d'iOS). Il faut donc déclarer du **suivi** pour les données de la régie.

- **Collecte de données : Oui.**
- Données du jeu (non utilisées pour le suivi) :
  - **Identifiants → Identifiant d'utilisateur** : fonctionnalité de l'app ; lié à l'utilisateur : oui ; suivi : non.
  - **Contenu utilisateur → Contenu de jeu** (scores, partie envoyée pendant un duel) : fonctionnalité de l'app ; lié : oui ; suivi : non.
  - **Contenu utilisateur → Autre contenu** (pseudo, code ami) : fonctionnalité de l'app ; lié : oui ; suivi : non.
- Données de la régie publicitaire (Unity LevelPlay et ses réseaux) :
  - **Identifiants → Identifiant de l'appareil** : publicité tierce, analyses ; lié : non ; **utilisé pour le suivi : oui**.
  - **Données d'utilisation → Données publicitaires** : publicité tierce, analyses ; lié : non ; **suivi : oui**.
  - **Données d'utilisation → Interaction avec le produit** : publicité tierce, analyses ; lié : non ; suivi : oui.
  - **Localisation → Localisation approximative** (déduite de l'adresse IP) : publicité tierce ; lié : non ; suivi : oui.
  - **Diagnostics → Données de performance, données de plantage** : analyses ; lié : non ; suivi : non.
- Comparer avec le guide « Apple privacy » de LevelPlay et de chaque réseau activé (`docs/PUBS.md`). Déclarer un peu trop n'est jamais un motif de refus ; déclarer trop peu peut l'être.

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
- **Numéro de build** : Codemagic le règle tout seul : 100 + numéro du build Codemagic (étape « Numéro de build automatique » de `codemagic.yaml` dans bacule-ios). L'ancien calcul (dernier build TestFlight + 1) redonnait toujours 21 et Apple refusait le deuxième envoi d'une même version.
- **Toujours ouvrir le build TestFlight sur un iPhone avant de le soumettre** : le 1.4 (21) se fermait au lancement (régies dynamiques non embarquées, voir `docs/PUBS.md`).
- **Vidéo du duel** : la joindre dans App Review Information → **Pièce jointe** (fichier .mov ou .mp4), c'est le plus sûr. Sinon, mettre un lien non répertorié dans les notes.


## Page produit (iOS 27) : en-tête et résultats de recherche

Images prêtes (PNG sans transparence, faites avec les dessins du jeu) :

| Emplacement App Store Connect | Fichier | Format Apple |
|---|---|---|
| En-tête de la page produit | `docs/apercu/appstore_entete_3840x1646.png` | 21:9, 3840 × 1646 px |
| Résultats de recherche | `docs/apercu/appstore_recherche_2880x1920.png` | 3:2, de 1920 × 1280 à 3840 × 2560 px |

App Store Connect → l'app → la version → « Informations de la page produit » → En-tête / Résultats de recherche → **Charger**. Vérifier avec l'aperçu d'App Store Connect que rien d'important n'est coupé.

Pour les refaire après un changement de dessin : exporter les décors en haute définition (collines, sol, soleil, nuages en 3840 px, voir `Tools/app_store_assets.py`), puis
`python3 Tools/app_store_assets.py Exports/art <dossier des décors HD> docs/apercu`.

### Texte promotionnel (170 caractères maximum, modifiable sans nouvelle version)

```
Nouveau : défie tes amis en duel en un geste, gagne la Superstar exclusive et grimpe au classement ! Garde Basile en équilibre et bats ton record.
```

### Nouveautés de cette version (1.4)

```
La plus grosse mise à jour de Bascule ! Ta progression est conservée.
• DUEL EN DIRECT : défie un ami en un geste, 3 minutes max, le premier qui lâche a perdu (sinon, le meilleur score gagne).
• Roue de la chance chaque jour, événement de la semaine et quête du débutant, avec des têtes à gagner.
• Nouveau classement avec podium, et les places gagnées en fin de partie.
• Têtes exclusives : Superstar (vidéos), Duo (parrainage), Chanceux (roue)… 66 têtes en tout.
• Codes promo partenaires (Réglages → Code promo).
• Décors plus tôt, rafales plus douces, le 128 mieux expliqué.
• Choisis ton pseudo, et chasse les notifications du jeu d'un geste.
```

## Notes et avis : pourquoi ils n'apparaissent pas tout de suite

- **Dans le jeu, la fenêtre de notation est celle d'Apple** (`RequestStoreReview`, après un record, à partir de 5 parties, au plus 3 fois à 4 jours d'écart). Une note donnée là compte vraiment. Mais Apple décide seul de l'afficher : au plus 3 fois par an et par appareil, et jamais si le joueur a coupé « Notes et avis intégrés » (Réglages de l'iPhone → App Store).
- **Avec TestFlight ou une version de test, rien n'est envoyé** : la fenêtre s'affiche mais la note ne part nulle part. Seule la version téléchargée sur l'App Store compte.
- **Pays par pays** : une note donnée en France n'apparaît que sur la page française. Apple n'affiche la moyenne qu'à partir de quelques notes dans le pays (« Pas assez de notes » avant), avec un délai d'environ 24 à 48 h.
- **Une note seule (étoiles) n'est jamais affichée en texte** : elle compte dans la moyenne. Les avis écrits passent par une vérification d'Apple avant d'apparaître.
- **Nouvelle version** : dans App Store Connect, ne pas cocher « Réinitialiser le résumé des notes » en envoyant la 1.4, sinon la moyenne repart de zéro.
- **Où les voir** : App Store Connect → l'app → onglet App Store → **Notes et avis** (menu de gauche), par pays.
- **Toujours possible** : Réglages → Aide → « Noter Bascule » ouvre la page « Écrire un avis » de l'App Store, même quand la fenêtre d'Apple ne s'affiche plus.
