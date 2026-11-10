# Fiche store de Bascule (App Store et Google Play)

Tout est prêt à copier-coller. Le jeu est publié en français.

## Identité

| Champ | Valeur |
|---|---|
| Nom de l'appli | Bascule |
| Sous-titre (App Store, 30 caractères) | Le jeu d'équilibre addictif |
| Éditeur | Orphex |
| Identifiant | `fr.orpheex.bascule` |
| Catégorie | Jeux → Réflexion (secondaire : Arcade) |
| Âge | 12+ (App Store) / Ados – PEGI 7 ou 12 selon le questionnaire IARC (Google Play) : publicités, achats intégrés, classement en ligne avec pseudos |
| URL d'assistance | https://orpheex.fr/bascule |
| URL de confidentialité | https://orpheex.fr/bascule/confidentialite |
| Icône | `Assets/Bascule/Icone/icone_1024.png` (1024 × 1024, sans transparence) |

## Description courte (Google Play, 80 caractères)

Garde Basile en équilibre : pose les poids, fusionne-les et bats ton record !

## Description

Basile tient une planche sur ses épaules… et c'est toi qui poses les poids !

Glisse le doigt pour viser, relâche pour lâcher. Deux poids identiques qui se touchent fusionnent : 1 + 1 = 2, 2 + 2 = 4… jusqu'au 128 qui s'envole en montgolfière. Mais attention : la planche penche du côté le plus lourd. Rattrape-la juste à temps, sinon Basile lâche tout !

• Facile à prendre en main, difficile à lâcher : une partie dure une à deux minutes.
• Un score infini : la planche raccourcit et les poids s'alourdissent à mesure que tu progresses.
• Le défi du jour : la même suite de poids pour tout le monde, chaque jour.
• Le classement mondial et le classement entre amis, avec ton code ami.
• Plus de 60 têtes à collectionner pour Basile : records, niveaux, boutique, exploits.
• Des missions chaque jour, un cadeau quotidien qui grossit, des niveaux à gagner.
• Une musique qui s'emballe quand la chute approche… et le cœur de Basile qui bat de plus en plus vite.
• Jouable hors ligne, d'une seule main.

Bascule est gratuit. Il contient des publicités non personnalisées entre les parties et des achats facultatifs (sans publicité, pièces, têtes). Aucun achat ne donne d'avantage en partie.

## Mots-clés (App Store, 100 caractères)

équilibre,poids,fusion,planche,arcade,réflexion,casual,défi,record,2048,empiler,balance,physique

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
