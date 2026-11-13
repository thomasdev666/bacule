# Vidéo de lancement de la 1.4 (TikTok, Reels, Shorts)

Fichier : `docs/promo/bascule_lancement.mp4` (1080 × 1920, 30 images/s, 20,8 s, musique et bruitages du jeu).
Refaite par `python3 Tools/promo_video.py Exports/art Exports/audio docs/promo/bascule_lancement.mp4` (après `dotnet run --project Tools/AudioPreview -- Exports/audio`).

C'est une **animation** faite avec les dessins du jeu, pas une capture de l'iPhone. Elle sert d'annonce. Les vidéos qui percent le plus restent les vraies captures, filmées par un joueur (voir « À filmer toi-même »).

## Version avec accroche (à publier en premier)

`docs/promo/bascule_lancement_accroche.mp4` (22,8 s) : 3 s d'accroche devant la vidéo ci-dessus. Gros plan sur l'accueil du jeu, un doigt s'arrête à un jeton du bouton JOUER (« N'appuie surtout pas… / …tu vas plus pouvoir t'arrêter »), cœur qui bat, appui, zoom dans le bouton, éclair blanc, et la partie démarre.
Refaite par `python3 Tools/promo_intro.py Exports/art docs/promo/bascule_lancement.mp4 docs/promo/bascule_lancement_accroche.mp4`.

Le doigt est dessiné. La même accroche filmée avec un vrai doigt devant l'iPhone sera encore plus efficace : filmer 5 s en vertical, puis remplacer les 3 premières secondes.

## Ce que disent les recherches (novembre 2026)

- Les 3 premières secondes décident de tout : l'image, le texte et le son doivent accrocher en même temps, sur un moment fort du jeu.
- La vidéo doit ressembler à une vidéo TikTok, pas à une pub : format « POV », style joueur, montage rapide.
- Pour un jeu sans tutoriel, le jeu lui-même est la vidéo : on doit comprendre la règle en regardant.
- Les défis difficiles gardent mieux les joueurs ; le « presque raté » crée l'attente.
- La régularité compte plus qu'un seul coup de chance : publier souvent, plusieurs accroches.

Sources : [SplitMetrics](https://splitmetrics.com/blog/intro-to-app-marketing-on-tiktok-hook-entertain-analyze-optimize/), [AppGrowing](https://appgrowing.net/blog/en/why-do-the-first-3-to-6-seconds-matter-in-mobile-video-ads/), [TikTok Creative Center](https://ads.tiktok.com/business/creativecenter/quicktok/online/tiktok-hyper-casual-game-creative-tips/pc/en), [AppsFlyer, tendances 2025](https://gamedevreports.substack.com/p/appsflyer-creative-trends-in-games), [Segwise, T4 2025](https://mobile-game-creative-insights-report-q4-2025.segwise.ai/), [Unity](https://blog.unity.com/games/how-to-make-killer-creatives-for-your-hypercasual-game), [FindClout](https://findclout.com/blog/how-to-market-an-indie-game-on-tiktok), [bugnet.io](https://bugnet.io/blog/tiktok-marketing-for-indie-games). Ce sont surtout des blogs d'agences : des tendances, pas des chiffres garantis.

## Déroulé de la vidéo

| Temps | Image | Texte |
|---|---|---|
| 0 – 2,5 s | La planche penche déjà fort, Basile grimace | POV : tu crois que c'est facile |
| 1 – 7 s | Poids lâchés, fusions (8+8, 4+4, 16+16), score qui monte | ça penche… |
| 8,5 – 10 s | Erreur : la planche part à 23°, écran rouge | NON NON NON |
| 10,3 – 12 s | Un 16 posé de l'autre côté rattrape tout | SAUVÉ ! |
| 12 – 14,6 s | Fusion de trop (64), tout tombe | allez, un dernier… puis K.O. · 2 310 points · Tu fais mieux ? |
| 14,6 – 17,7 s | Carte rouge contre bleu | NOUVEAU · DUEL EN DIRECT · Défie tes potes |
| 17,7 – 20,8 s | Basile content, logo | Cherche « Bascule » sur l'App Store · Gratuit |

La fin dit « cherche Bascule » exprès : chaque téléchargement fait après cette recherche fait monter le jeu sur le mot « bascule » (docs/STORE.md).

## Publication

- **Quand** : le jour où la 1.4 est en ligne, pas avant (le duel doit exister). Entre 18 h et 21 h.
- **Légende** : `2 310 points. Personne de ma classe ne fait mieux. Cherche « Bascule » sur l'App Store (gratuit)`
- **Mots-dièse** (4 ou 5, pas plus) : `#jeumobile #jeuiphone #bascule #defi #pourtoi`
- **Commentaire épinglé** : `Ton record ? Le meilleur de la semaine gagne une tête exclusive.`
- Répondre à chaque commentaire la première heure. Publier la même vidéo sur TikTok, Reels et Shorts, sans filigrane d'une autre appli.

## À filmer toi-même (les formats qui percent le plus)

1. **Duel filmé en vrai** : deux personnes côte à côte, deux iPhone, on voit les réactions. 15 s.
2. **Enregistrement d'écran + visage** : ta meilleure partie, avec ta réaction quand ça tombe.
3. **« Jour X : je bats mon record »** : une vidéo par jour, même accroche, le score qui monte.
4. **Défi de l'école** : « le record de la promo est à X, qui le bat ? », avec le classement à la fin.

Une vidéo par jour pendant la semaine de la compétition, en changeant seulement les 2 premières secondes.
