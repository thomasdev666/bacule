# Décisions prises pour le prototype

Toutes ces décisions se changent sans recoder : dans `bascule_tuning.json` ou, pour la mascotte et les têtes, dans `HeadCatalog.cs`.

## Section 13 du cahier des charges : questions ouvertes

1. **Sens de la bascule** : la planche penche **vers le côté le plus lourd** (`tiltDirection: 1`), comme le prévoit la section 4. L'autre sens est prêt (`-1`). Pour comparer les deux sur téléphone, ouvrir **Réglages → Prototype → Sens de bascule** en cours de test : c'est la question à trancher avec les premiers testeurs.
2. **Têtes et paliers** : 40 têtes.
   - 1 tête de départ (Basile).
   - 33 têtes de score : tous les 500 points jusqu'à 5 000, puis tous les 1 000 jusqu'à 15 000, tous les 2 500 jusqu'à 35 000, tous les 5 000 jusqu'à 60 000.
   - 6 têtes spéciales : série de 3 et de 7 jours de défi, sauvetage à 95 % ou plus, premier 128 envolé, combo ×4, top 100 mondial.
3. **Nom** : « Bascule » pour le jeu, « Basile » pour la mascotte (une seule variable : `mascotName` dans le JSON).
4. **Direction artistique** : après un premier essai en aplats, jugé trop simple, le jeu passe à un style d'illustration de jeu mobile. Les volumes ont un dégradé doux, une ombre propre, un contour sombre coloré et des reflets. La mascotte est articulée et plus détaillée, et les poids sont des poids en fonte de dessin animé. Le décor reste clair et en arrière-plan, pour que les poids et la mascotte restent au centre. C'est un paysage dont l'ambiance avance dans la journée tous les 2 500 points (matin, midi, doré, couchant, nuit). La police est ronde (Fredoka). Le détail est dans le README, section « Direction artistique ».

## Décisions d'équilibrage (issues de la simulation)

- **Poids effectif des gros poids** (`torqueExponent: 0.85`). Avec la physique « réaliste » (exposant 1), un seul 64 posé un peu loin du pivot faisait perdre à coup sûr : il pesait plus que tout ce que le joueur pouvait poser en face. Avec 0,85, un 64 pèse comme un 34 environ. Il reste dangereux, mais on peut le compenser, et chaque fusion allège un peu la planche (récompense). Le 128 qui s'envole fait toujours basculer brutalement.
- **Sensibilité** (`degreesPerTorque: 2.6`) : en simulation, un joueur automatique appliqué tient environ 1,7 fois plus longtemps qu'un joueur qui lâche au hasard (environ 100 poids contre 60). C'est un point de départ : l'étape 2 du plan (réglages sur téléphone avec de vrais joueurs) reste indispensable.
- **Points** (`pointsPerValue: 3`, bonus 128 : 600) : une bonne première partie débloque 2 ou 3 têtes, et non 20 d'un coup. La collection se remplit sur la durée.
- **Délai de grâce** (`loseGraceSeconds: 0.55`) : la planche peut dépasser l'angle maximal pendant un peu plus d'une demi-seconde avant que la mascotte lâche. C'est ce qui rend possibles les « sauvetages de justesse » de la carte de score.

## Décisions de rétention et de monétisation (simulation de 1000 joueurs, docs/RAPPORT.md)

- **Pas de cœurs** (demande du porteur de projet) : un poids qui tombe coûte des points (4 × sa valeur) et casse le combo, c'est tout. La tension vient de la musique : couche tendue, tempo qui accélère et battement de cœur calé sur les temps quand la planche approche de la chute.
- **Musique ElevenLabs** : les deux morceaux fournis (calme et tension) remplacent la musique générée. Tempo vérifié à 95,7 BPM, raccord sur une mesure entière. Si on change de morceau, régler `MusicPlayer.BpmPerso` au tempo réel.
- **Musique** : un morceau de 32 mesures (1 min 20) en 8 parties, en do majeur ; les bruitages sont accordés sur la même gamme et la musique se baisse un instant sous les gros bruitages. Une vraie musique peut la remplacer (fichiers `calme` et `tension` dans `Resources/Musique`).
- **Pas d'écran d'explication au lancement** : on arrive directement sur le jeu. La politique de confidentialité reste accessible dans les réglages (obligatoire pour les stores).
- **Rebords de planche** (`endLipHeight`, `endLipWidth`) : simulation, 18 % de poids tombés au lieu de 22 % et parties plus longues ; la gravité et les collisions ne sont pas modifiées. Mettre 0 pour les retirer.
- **Fluidité** : aucune image n'est calculée pendant une partie (préparation en arrière-plan au lancement, `ArtCache.PrepareInBackground`).
- **Écran** : téléphones et tablettes (jusqu'au rapport 0,8, iPad compris) remplissent tout l'écran ; seul un écran plus large affiche une colonne portrait.
- **Têtes** : les anciens paliers (jusqu'à 60 000 points) laissaient 22 têtes sur 33 hors de portée. Il y a maintenant 20 têtes de record aux paliers resserrés, 13 de niveau (le niveau monte à chaque partie), 16 en pièces et 6 premium. Les identifiants des 40 têtes d'origine n'ont pas changé.
- **Pièces** : elles n'achètent que des têtes, jamais un avantage en partie (§9). La seconde chance se gagne toujours par une vidéo choisie, pour tout le monde (« Sans publicité » retire seulement les pubs imposées).
- **Pubs imposées** : seulement entre deux parties. Pas avant la 6e partie ; au plus une toutes les 3 parties et 3 minutes ; jamais juste après un record ou une vidéo choisie. C'est la règle qui garde presque toute la rétention pour le meilleur revenu parmi les règles raisonnables.
- **Offre de départ** : une seule fois, pendant 48 h, après la 8e partie (le joueur a accroché, la première pub imposée vient de passer).
- **Cadeau du jour** : il arrive après la 1re partie, jamais avant, pour qu'un nouveau joueur joue tout de suite.
- **Jour local** pour le cadeau et les missions (ils changent à minuit chez le joueur). Le défi du jour reste en UTC, pour être le même partout au même moment.

## Décisions de conception

- **Difficulté liée au nombre de poids lâchés, pas au score.** Le cahier des charges dit que les poids deviennent plus lourds avec le score. Ici, ils s'alourdissent avec le nombre de poids lâchés, qui suit le score. Raison : le défi du jour et les duels exigent que tout le monde reçoive exactement la même suite de poids, quoi qu'il fasse. La planche, elle, raccourcit bien avec le score.
- **Le 128 ne tombe jamais du ciel** : il se fabrique (les tirages vont jusqu'au 16).
- **Duel dès la v1, sans serveur** : la carte de score affiche un code défi (la graine de la partie, par exemple `K7QM-3XTA`). Un ami le saisit dans « J'ai un code défi » et joue la même suite de poids. Un lien profond qui ouvre directement l'appli pourra venir en v1.5.
- **Seconde chance** : on revient à l'instant de la chute, la planche est remise à plat et les poids les plus déséquilibrants du côté lourd sont retirés. Une seule fois par partie. L'enregistrement (record, têtes, classement) attend que le joueur ait choisi de revivre ou non.
- **Classement « amis »** : non inclus en v1 (il faut un système d'amis). Le classement propose Monde, Jour et Mes scores (1.4 : Monde, Jour et Amis ; « Mes scores » retiré).
- **Visuels et sons générés par le code** : aucun fichier image ni audio à gérer pendant le prototypage. Plus tard, on pourra remplacer une forme par un sprite dessiné par un graphiste sans toucher à la logique.
