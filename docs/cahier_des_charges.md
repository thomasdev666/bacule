# Cahier des charges : BASCULE

Jeu mobile gratuit (Android et iOS), à score infini, avec classement mondial et partage de score. Projet développé à deux avec Unity et Claude Code. Nom de travail : **Bascule** (à valider).

---

## 1. Vision

Un jeu qu'on comprend en dix secondes et qu'on a envie de relancer pour battre son meilleur score. Le joueur aide une mascotte à tenir une planche en équilibre en y faisant tomber des poids. Le jeu doit être lisible par un collégien comme par une personne âgée, et les parties doivent pouvoir durer deux minutes dans le bus comme un quart d'heure.

**Ce qui le rend unique** : la planche est portée par un personnage qui peine physiquement. Son corps raconte l'état de la partie. Et son meilleur coup (fabriquer le plus gros poids) est aussi le plus risqué.

**Ce qui doit donner envie de partager** : la carte de score, les têtes à débloquer, les sauvetages de justesse.

---

## 2. Boucle de jeu

1. Un poids apparaît au-dessus de la mascotte, avec son chiffre.
2. Le joueur glisse le doigt pour viser la position, puis relâche pour le faire tomber.
3. Le poids atterrit sur la planche. Si un poids touche la planche ailleurs, la planche bascule selon les règles de la section 4.
4. Deux poids identiques qui se touchent fusionnent en un poids de valeur double.
5. Quand un poids sort de la planche (tombe sur le côté), il est perdu et le poids suivant arrive.
6. La partie se termine quand la planche bascule trop et que la mascotte lâche.

---

## 3. Les poids

- Valeurs en puissance de 2 : 1, 2, 4, 8, 16, 32, 64, 128.
- Chaque valeur a une couleur **et** un chiffre affiché, pour rester lisible sans se fier à la couleur seule (daltonisme).
- Taille du poids proportionnelle à sa valeur, mais plafonnée pour que le petit reste visible.
- Le poids suivant est toujours affiché en haut de l'écran.
- **Le 128 (poids maximal)** : quand il est créé, il s'envole comme une montgolfière et rapporte un gros bonus de points. Mais il retire le poids de la planche : le côté qui le portait devient d'un coup beaucoup plus léger, ce qui fait basculer la planche violemment.

---

## 4. Physique de la planche (règle centrale, à valider, voir section 13)

- La planche est un levier posé sur la mascotte. Son inclinaison dépend de la différence de poids entre ses deux côtés.
- L'inclinaison est bornée (par exemple entre -30° et +30°). Au-delà, la partie se termine.
- L'inclinaison doit être **progressive et amortie** (pas de bascule instantanée) : elle oscille un peu avant de se stabiliser, ce qui rend le jeu lisible et agréable.
- Un poids qui tombe sur le côté trop lourd fait basculer davantage ; un poids placé près du pivot bascule moins.
- Le paramétrage (force de l'inclinaison, amortissement, poids maximal avant défaite) doit être **dans un fichier de réglages séparé**, pour pouvoir tester sans recoder.

---

## 5. La mascotte (personnage principal)

- Une seule mascotte, toujours la même silhouette, pour qu'elle soit reconnaissable partout (pubs, icônes, réseaux).
- Elle porte la planche sur les épaules et les bras, et la soulève.
- **Ses réactions suivent l'inclinaison** :
  - planche stable : posture détendue, sourire ;
  - inclinaison moyenne : bras qui se crispent, front qui perle ;
  - inclinaison forte : grimace, tremblement, pieds qui glissent ;
  - près de la défaite : posture de dernier effort, yeux fermés.
- Elle doit aussi réagir aux événements : sourire quand un poids fusionne, petit saut quand le 128 s'envole, bras qui lâchent quand la partie s'arrête.

### Têtes à débloquer

- Une **tête par palier de score** (par exemple tous les 500 points au début, puis des paliers plus espacés). Une quarantaine de têtes au total à terme.
- Chaque tête a sa propre expression, du sourire tranquille à la grimace.
- Le joueur voit sa **collection** : les têtes débloquées et celles qui restent à obtenir.
- Têtes spéciales : records personnels, défis du jour, têtes très rares pour les meilleurs scores mondiaux.
- La tête s'affiche sur la mascotte pendant toute la partie et sur la carte de score.

---

## 6. Score et progression

- Points à chaque fusion, multipliés par la taille du poids créé.
- Un **combo** : si plusieurs fusions se déclenchent en chaîne en un seul coup, le multiplicateur monte.
- Bonus pour la montgolfière (128).
- Pas de niveaux pour la v1. La difficulté monte avec le score : les poids deviennent plus lourds et la planche se raccourcit petit à petit.
- Le score et la tête sont sauvegardés en ligne pour le classement.

---

## 7. Modes de jeu

- **Infini** (mode principal).
- **Défi du jour** : une même suite de poids pour tous les joueurs ce jour-là, classement du jour, série de jours consécutifs.
- **Duel** (v1.5 ou plus) : un lien envoyé à un ami, qui joue la même suite de poids.
- **Pause** : la partie se sauvegarde quand le joueur quitte l'application, et reprend au même endroit.
- **Mondes** (plus tard) : même mécanique, thèmes visuels différents, pour varier les pubs sans changer le code.

---

## 8. Partage et social

- **Carte de score** générée automatiquement : score, tête de la mascotte, un sauvetage de justesse (écart minimal avec la défaite), et un bouton « défier un ami ».
- Partage natif du téléphone (Snapchat, WhatsApp, TikTok, Instagram).
- **Classement mondial** et classement du jour (Unity Gaming Services ou équivalent, plan gratuit au départ).
- Connexion anonyme au lancement (pas de compte obligatoire). Un pseudo est demandé pour le classement.

---

## 9. Monétisation

- **Seconde chance** : une pub récompensée propose de continuer après une défaite. C'est le format principal.
- **Achats in-app** :
  - retirer les publicités ;
  - têtes ou thèmes premium (non cumulables avec le score) ;
  - plus tard : vêtements pour la mascotte, pour se montrer entre amis.
- **Aucune mécanique qui gêne le gameplay** : pas de « vie » payante, pas de bonus qui rendent la partie plus facile contre argent.
- **Public mineur** : régies publicitaires adaptées aux enfants, pas de publicité personnalisée, consentement RGPD, classification d'âge correcte sur App Store et Google Play.

---

## 10. Direction artistique

Objectifs : agréable à regarder en continu, très lisible, facile à reconnaître dans une vidéo de cinq secondes.

- Couleurs franches et saturées, avec une palette cohérente (une dizaine de couleurs maximum).
- Formes rondes et simples, traits nets, pas de dégradés ni d'effets lourds.
- Décor sobre et clair, pour que le poids, le chiffre et la mascotte restent au centre.
- Le fond en bois initialement envisagé est une option, pas une obligation. Piste à tester : fond uni coloré, qui change selon le thème ou le score.
- Animation : la mascotte doit « peser » physiquement (mouvements lents quand elle porte beaucoup, rebonds quand la planche se stabilise).
- Retours sonores et visuels sur chaque fusion et chaque chute, c'est là que se joue le plaisir.

---

## 11. Écrans

1. **Accueil** : la mascotte et un gros bouton « Jouer ». Accès au classement, à la collection de têtes et aux réglages.
2. **Partie** : planche, mascotte, poids suivant, score en haut, jauge de stabilité discrète.
3. **Fin de partie** : score, record, bouton « Revivre » (pub récompensée), bouton « Rejouer », bouton « Partager ».
4. **Carte de score** : image générée, à partager.
5. **Collection de têtes** : grille des têtes, débloquées ou grisées.
6. **Classement** : monde, amis, jour.
7. **Réglages** : son, vibrations, langue, accessibilité.

---

## 12. Exigences techniques et accessibilité

- **Plateforme** : Android et iOS, en portrait. Cible minimale : téléphones des quatre dernières années.
- **Moteur** : Unity (déjà maîtrisé par l'équipe). Développement assisté par Claude Code.
- **Une seule main**, zones tactiles larges, aucun geste compliqué (glisser pour viser, relâcher pour lâcher).
- **Aucune information uniquement par la couleur** : chaque poids a un chiffre.
- **Texte lisible** : taille minimale de police adaptée aux personnes âgées, option de taille de texte dans les réglages.
- **Hors ligne** : le jeu se lance et se joue sans connexion ; le classement se synchronise plus tard.
- **Performance** : 60 images par seconde sur téléphone d'entrée de gamme.
- **Sauvegarde locale** de la partie en cours et de la collection.

---

## 13. À confirmer avant de coder

1. **Sens de la bascule** : la planche penche-t-elle **vers le côté le plus lourd** (comportement naturel d'une balance) ou **vers le côté le plus léger** (effet de rebond, plus original mais moins intuitif) ? Le texte ci-dessus suppose le sens naturel dans la section 4. À trancher avant le prototype, car ça change tout le ressenti.
2. **Nombre exact de têtes** et seuils de score (proposition : une tête tous les 500 points, puis espacées).
3. **Nom définitif** du jeu et de la mascotte.
4. **DA** : bois ou fond uni coloré. Recommandation : fond uni et palette franche, plus lisible à la vidéo.

---

## 14. Plan de réalisation

1. **Prototype jouable** (planche, mascotte à trois états, poids, fusion, chute, score). Test : est-ce qu'un proche relance une partie sans qu'on le lui demande ?
2. **Réglages** : ajuster la physique et la difficulté, tester sur plusieurs téléphones.
3. **Version 1** : têtes, carte de score, classement, défi du jour, pubs récompensées, achats de base.
4. **Test fermé** avec quelques collégiens et quelques personnes âgées : observer où ils bloquent.
5. **Lancement** en France d'abord, avec des vidéos courtes sur TikTok et YouTube Shorts. Campagnes payantes seulement quand une vidéo marche déjà.
6. **Après lancement** : vêtements de la mascotte, duels entre amis, nouveaux thèmes.
