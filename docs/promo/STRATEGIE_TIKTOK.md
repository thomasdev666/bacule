# Stratégie vidéo TikTok / Reels / Shorts de Bascule (point au 13/11/2026)

À donner tel quel à une nouvelle conversation pour reprendre le travail.

## Le contexte

- **Bascule** : jeu mobile d'équilibre (iPhone, en français), éditeur ORPHÉE X. Basile porte une planche, le joueur lâche des poids, deux poids identiques fusionnent (1+1=2… jusqu'au 128 qui s'envole en montgolfière), la planche penche, on perd quand tout tombe.
- **La 1.4 vient de sortir** : duel en direct entre amis (3 min max, le premier qui lâche a perdu, sinon le meilleur score gagne), roue de la chance, événement de la semaine, classement avec podium, 67 têtes, codes promo partenaires (ex. BDE Samb'bach).
- **Objectif des vidéos** : de la visibilité et des téléchargements. Chaque vidéo finit par **« Cherche "Bascule" sur l'App Store »** : un téléchargement fait après cette recherche fait monter le jeu sur le mot « bascule » (aujourd'hui des applis de pesée passent devant, voir `docs/STORE.md`).
- **Compétition dans l'école** prévue la semaine prochaine (5 jours, code Samb'bach) : bon moment pour une vidéo par jour.

## Ce que disent les recherches (novembre 2026)

1. Les **3 premières secondes** décident de tout : image, texte et son doivent accrocher en même temps, sur un moment fort.
2. La vidéo doit ressembler à une **vidéo TikTok, pas à une pub** : format « POV », style joueur, montage rapide.
3. Le jeu doit se **comprendre en regardant**, sans explication.
4. **Défis difficiles** et **« presque raté »** retiennent le mieux.
5. La **régularité** compte plus qu'un coup de chance : publier souvent, tester plusieurs accroches.

Sources : SplitMetrics, AppGrowing, TikTok Creative Center (hyper-casual), AppsFlyer (tendances 2025), Segwise (T4 2025), Unity, FindClout, bugnet.io. Ce sont surtout des blogs d'agences : des tendances, pas des chiffres garantis.

## Les vidéos déjà faites (animations avec les dessins et la musique du jeu)

| Fichier | Durée | Contenu |
|---|---|---|
| `docs/promo/bascule_lancement.mp4` | 20,8 s | « POV : tu crois que c'est facile » → poids et fusions → « ça penche… » → « NON NON NON » → « SAUVÉ ! » → fusion de trop, K.O., « 2 310 points · Tu fais mieux ? » → carte rouge/bleu « NOUVEAU · DUEL EN DIRECT · Défie tes potes » → « Cherche "Bascule" sur l'App Store · Gratuit » |
| `docs/promo/bascule_lancement_accroche.mp4` | 22,8 s | **3 s d'accroche en plus** : gros plan sur l'accueil, un doigt s'arrête à un jeton du bouton JOUER (« N'appuie surtout pas… / …tu vas plus pouvoir t'arrêter »), cœur qui bat, appui, zoom dans le bouton, éclair blanc → la vidéo ci-dessus |

Refaire : `python3 Tools/promo_video.py Exports/art Exports/audio docs/promo/bascule_lancement.mp4` puis `python3 Tools/promo_intro.py Exports/art docs/promo/bascule_lancement.mp4 docs/promo/bascule_lancement_accroche.mp4` (après `dotnet run --project Tools/ArtPreview -- Exports/art` et `dotnet run --project Tools/AudioPreview -- Exports/audio`).

## L'avis de Thomas (à respecter)

- La première animation n'est **pas assez dynamique ni représentative du jeu**.
- Le format voulu, inspiré d'un reel Instagram : **accroche qui intrigue** (un doigt à un jeton de distance, 2-3 s) → **transition** → le doigt **appuie sur JOUER** dans Bascule → on **joue un peu** → **un super moment** → **20 s au total**.
- Les **vraies images** (vrai doigt, vraie capture d'écran d'iPhone, vraies réactions) marchent mieux que des animations : la prochaine étape est de **monter de vraies prises** filmées par Thomas, ou de vraies parties enregistrées.

## Publication

- **Quand** : le soir, entre 18 h et 21 h (à vérifier avec les statistiques TikTok : heures où le public est connecté).
- **Légende** : `2 310 points. Personne de ma classe ne fait mieux. Cherche « Bascule » sur l'App Store (gratuit)`
- **Mots-dièse** (4-5) : `#jeumobile #jeuiphone #bascule #defi #pourtoi`
- **Commentaire épinglé** : `Ton record ? Le meilleur de la semaine gagne une tête exclusive.` (seulement si le lot est vraiment offert)
- Répondre à tous les commentaires la première heure. Même vidéo sur TikTok, Reels et Shorts, sans filigrane.

## Formats à filmer ensuite (les plus efficaces)

1. **Duel filmé en vrai** : deux personnes côte à côte, deux iPhone, les réactions (15 s).
2. **Enregistrement d'écran + visage** : la meilleure partie, la réaction quand ça tombe.
3. **« Jour X : je bats mon record »** : une vidéo par jour, même accroche, le score qui monte.
4. **Défi de l'école** : « le record de la promo est à X, qui le bat ? », classement à la fin.
5. **L'accroche du doigt filmée en vrai** devant l'iPhone (5 s verticales) pour remplacer le doigt dessiné.

Une vidéo par jour pendant la compétition, en changeant seulement les 2 premières secondes (tester les accroches).

## Prochaine étape : les statistiques TikTok avec Treg

Installer Treg dans la session (pas d'installateur Windows : passer par la session cloud) et connecter le compte TikTok par la vraie page de connexion de TikTok (jamais de mot de passe dans la conversation) :

```
curl -fsSL https://treg.to/install.sh | sh
treg login
treg connections connect --provider tiktok --capability post
treg connections ls
```

Puis analyser toutes les vidéos : rétention (à quelle seconde on décroche), temps de visionnage moyen, % vu en entier, vues selon l'heure et le jour de publication, sources de trafic (Pour toi, profil, recherche). En tirer : les accroches qui marchent, la bonne durée, les heures de publication, puis refaire les vidéos en corrigeant exactement ces points.
