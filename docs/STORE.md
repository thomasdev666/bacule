# Fiche store de Bascule (App Store et Google Play)

Tout est prêt à copier-coller. Le jeu est publié en français.

## Identité

| Champ | Valeur |
|---|---|
| Nom de l'appli | **Bascule** (le mot seul : c'est la correspondance exacte avec la recherche « bascule ») |
| Sous-titre (App Store, 30 caractères) | Jeu d'équilibre et duel d'amis |
| Éditeur | Orphex |
| Identifiant | `fr.orpheex.bascule` |
| Catégorie | Jeux → Parties rapides (en ligne aujourd'hui) + **Réflexion** en deuxième sous-catégorie |
| Âge | 12+ (App Store) / Ados – PEGI 7 ou 12 selon le questionnaire IARC (Google Play) : publicités, achats intégrés, classement en ligne avec pseudos |
| URL d'assistance | https://orpheex.fr/bascule |
| URL de confidentialité | https://orpheex.fr/bascule/confidentialite |
| Icône | `Assets/Bascule/Icone/icone_1024.png` (1024 × 1024, sans transparence) |

## Description courte (Google Play, 80 caractères)

Garde Basile en équilibre : pose les poids, fusionne-les et bats ton record !

## Description

Bascule, le jeu d'équilibre où Basile tient une planche sur ses épaules… et c'est toi qui poses les poids !

Glisse le doigt pour viser, relâche pour lâcher. Deux poids identiques qui se touchent fusionnent : 1 + 1 = 2, 2 + 2 = 4… jusqu'au 128 qui s'envole en montgolfière. Mais attention : la planche penche du côté le plus lourd. Rattrape-la juste à temps, sinon Basile lâche tout !

NOUVEAU : LE DUEL EN DIRECT
Défie un ami en un geste et jouez en même temps, avec les mêmes poids. 3 minutes maximum : le premier qui lâche a perdu, sinon le meilleur score gagne.

• Facile à prendre en main, difficile à lâcher : une partie dure une à deux minutes.
• Un score infini : la planche raccourcit et les poids s'alourdissent à mesure que tu progresses.
• Le classement mondial, le classement du jour et le classement entre amis.
• 66 têtes à collectionner pour Basile : records, niveaux, roue de la chance, duels, exploits.
• La roue de la chance chaque jour, l'événement de la semaine, des missions et des coffres.
• 14 mondes à traverser selon ton score, du village jusqu'à la Lune.
• Une musique qui s'emballe quand la chute approche… et le cœur de Basile qui bat de plus en plus vite.
• Jouable hors ligne, d'une seule main.

Bascule est gratuit. Il contient des publicités entre les parties (personnalisées seulement avec ton accord, à partir de 15 ans) et des achats facultatifs : sans publicité, pièces, têtes. Aucun achat ne donne d'avantage en partie.

## Mots-clés (App Store, 100 caractères)

Français (France) :

```
poids,planche,empiler,fusion,arcade,reflexion,casual,defi,record,physique,tour,adresse,basile,1v1
```

Ne pas répéter les mots du nom ni du sous-titre (bascule, jeu, équilibre, duel, amis : Apple les compte déjà), pas d'espace après les virgules, pas de nom d'un autre jeu (motif de refus 2.3.7). « balance » reste dehors : il attire les applis de pesée.

## Être premier sur la recherche « bascule » (plan 1.4)

Constat du 12/11/2026 (recherche App Store France) : le jeu est sorti le 11/11, il a 0 note et n'est pas encore dans les 28 premiers résultats de « bascule ». Apple traite « bascule » comme « balance » et affiche des applis de pesée. Aucune ne s'appelle « Bascule » : dès que le jeu a des téléchargements et des notes, le nom exact doit passer devant. Personne ne peut garantir la première place : Apple classe avec le texte **et** avec les téléchargements, les notes et le temps.

Ce qui compte, dans l'ordre :

1. **Téléchargements après avoir tapé « bascule »** : c'est le signal le plus fort. Pour la compétition de l'école, demander de **chercher « Bascule » dans l'App Store** (pas de lien direct, pas de QR code vers la fiche) puis de télécharger.
2. **Notes** : viser 20 à 30 notes 5 étoiles la première semaine (Réglages → Aide → « Noter Bascule »). Ne jamais cocher « Réinitialiser les notes ».
3. **Nom exact « Bascule »**, sous-titre et mots-clés ci-dessus.
4. **Deuxième langue : Anglais (Royaume-Uni)**. L'App Store France lit aussi les mots-clés de cette langue : 100 caractères de plus. App Store Connect → Informations sur l'app → ajouter « Anglais (R.-U.) » :
   - Nom : `Bascule` — Sous-titre : `Balance game: stack and merge`
   - Mots-clés : `gratuit,hors ligne,sans wifi,copains,classement,multijoueur,chute,tomber,puzzle,stack,merge,weights`
   - Description : commencer par « Game in French. » puis la description traduite ; mêmes captures.
5. **Achats intégrés mis en avant** (ils apparaissent dans la recherche et sur la fiche) : voir plus bas.
6. **Événements dans l'app** (ils apparaissent aussi dans la recherche) : voir plus bas.
7. **Captures avec titre** : Apple lit le texte des captures. Voir `docs/store/captures`.
8. **Deuxième sous-catégorie** : Réflexion.

### Achats intégrés mis en avant

App Store Connect → Monétisation → Achats intégrés → chaque produit : nom affiché, description, **image 1024 × 1024** (`docs/store/achats/<identifiant>.png`), puis « Promouvoir » (Achats intégrés → Promotion sur l'App Store) dans cet ordre. Les envoyer en vérification **avec la version 1.4**.

| Identifiant | Nom affiché (30 max) | Description (45 max) |
|---|---|---|
| `bascule.starter` | Offre de départ Bascule | Sans pub imposée, 2 000 pièces, Basile doré |
| `bascule.removeads` | Bascule sans publicité | Plus aucune pub imposée entre les parties |
| `bascule.legends` | Pack Légendes Bascule | 5 têtes exclusives pour habiller Basile |
| `bascule.coins.large` | 6 000 pièces Bascule | Pour des têtes, des planches et des surprises |
| `bascule.coins.medium` | 2 200 pièces Bascule | Pour des têtes, des planches et des surprises |
| `bascule.coins.small` | 600 pièces Bascule | Pour des têtes, des planches et des surprises |

Les « lots d'apps » (App Bundles) d'Apple ne sont pas possibles ici : il faut au moins deux apps payantes du même éditeur. Les lots de Bascule sont ces achats (Offre de départ, Pack Légendes).

### Événements dans l'app

App Store Connect → Événements dans l'app → « + ». Images sans texte dans `docs/store/evenements` (carte 1920 × 1080, page de détail 1080 × 1920). Lien : `bascule://duel` (à tester sur TestFlight : il doit ouvrir le jeu). 31 jours maximum par événement, 5 publiés à la fois.

| | Duel (à publier avec la 1.4) | Défi de la semaine (à recréer chaque semaine ou chaque mois) |
|---|---|---|
| Badge | Mise à jour majeure | Défi |
| Nom (30) | Duel en direct entre amis | Défi de la semaine |
| Description courte (50) | Défie tes amis : 3 minutes, le meilleur gagne ! | Atteins l'objectif, gagne la tête de la semaine |
| Description longue (120) | Nouveau : invite un ami en un geste et jouez en même temps, avec les mêmes poids. Le premier qui lâche a perdu. | Chaque semaine, un nouvel objectif et trois paliers de récompenses. Joue et débloque la tête exclusive. |
| Images | `duel_carte_1920x1080.png`, `duel_detail_1080x1920.png` | `semaine_carte_1920x1080.png`, `semaine_detail_1080x1920.png` |
| Objectif | Attirer de nouveaux utilisateurs | Informer les utilisateurs actifs |

### Texte promotionnel (170 max, modifiable sans nouvelle version)

```
Nouveau : le DUEL EN DIRECT ! Défie tes amis, 3 minutes max, le premier qui lâche a perdu. Garde Basile en équilibre, fusionne les poids et bats ton record.
```

### Captures avec titre (1290 × 2796)

Six captures prêtes dans `docs/store/captures`, dans l'ordre : jeu d'équilibre, duel en direct, fusion (128), classement mondial, 66 têtes, cadeaux chaque jour. Elles sont faites avec les aperçus du jeu (`docs/apercu`) ; pour utiliser de vraies captures de l'iPhone, les mettre à la place dans `docs/apercu` et relancer :
`python3 Tools/store_listing_assets.py Exports/art docs/apercu docs/store`

## Captures d'écran

À faire dans Unity, en jouant :
1. Bascule → Format → **Captures App Store (1290 × 2796)**.
2. Play, puis **Ctrl+Maj+K** (Bascule → Capture d'écran pour les stores) à chaque écran : accueil, partie avec plein de poids, fusion en combo, 128 qui s'envole, fin de partie avec record, collection, classement.
3. Les images sont dans `Builds/Captures`. Google Play accepte les mêmes (1080 × 1920 minimum).

Conseil : 5 captures, avec la plus spectaculaire en premier (une planche bien chargée qui penche, avec « COMBO » à l'écran).

## Fichier pour Google Play (.aab)

1. Unity : **Bascule → Android : construire le fichier Google Play (.aab)**.
2. La première fois, il demande une **clé d'envoi** (keystore) : Edit → Project Settings → Player → Android → Publishing Settings → Keystore Manager → Create New. Enregistrer le fichier `.keystore` **hors du projet** (et en sauvegarde : clé USB, Drive privé), noter le mot de passe. Sans cette clé, plus aucune mise à jour possible. Unity oublie les mots de passe à chaque redémarrage : les retaper avant chaque construction.
3. Le fichier sort dans `Builds/fr.orpheex.bascule_<versionCode>.aab` (le nom doit reprendre le nom du paquet) (IL2CPP + ARM64, API Android la plus récente installée ; versionCode augmenté tout seul).
4. Play Console → l'app → Tests → Test interne (ou Production) → Créer une release → importer le `.aab`. Accepter la **signature d'application par Google Play** (Google garde la clé finale, la tienne sert à envoyer).
