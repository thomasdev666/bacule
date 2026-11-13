# Versions

Chaque zip porte son numéro (Bascule_V3.zip = version 3.0, affichée dans Réglages).

## 1.4.1 (build 9) – à sortir quand on veut, rien d'urgent
- **Classements** : la 1.4 lisait `bascule_monde` / `bascule_jour` alors que la version publiée avant écrivait dans `bascule_monde_s2` / `bascule_jour_s2`. Corrigé sans mise à jour le 13/11/2026 : records des 48 joueurs copiés dans les classements de la 1.4 (`Tools/copy_leaderboards.py`). Les noms restent ceux de la 1.4.
- **Record renvoyé à chaque lancement** : un joueur qui met à jour, réinstalle ou change d'iPhone apparaît tout de suite, sans rejouer. Scores en attente sous les noms `_s2` envoyés aux classements actuels.
- Version 1.4.1, build iOS 9, version Android 41.

## 1.4 (build 8) – tout ce qui a été fait depuis la 1.2 (la 1.3 n'est jamais sortie)
- **Rien n'est remis à zéro** : les joueurs de la 1.2 gardent tout (record, têtes, pièces, niveau, planches, missions, série, code ami, amis, achats). La remise à zéro de « saison » prévue pour le lancement est désactivée (`Seasons.ResetOldSaves = false`, `Seasons.OnLoad`) ; une sauvegarde de la 1.2 est seulement marquée de la saison en cours (test `A12SaveIsKeptIntactWhenUpdating`). Aucun champ sauvegardé de la 1.2 n'a été renommé ni changé de type (vérifié), fichiers `profile.json` et `run.json` inchangés. Classements : `bascule_monde` et `bascule_jour`, les mêmes que la 1.2.
- Version 1.4, build iOS 8, version Android 40 (`Editor/BasculeSetup.cs`).
- **Rendez-vous qui font revenir** (rapport `docs/RAPPORT_1_3.md`, `Core/LiveOps.cs`) :
  - **Quête du débutant** : 3 étapes récompensées (1re partie : 150 pièces ; 500 points : un coffre ; 5 parties : la planche Bambou), bouton « Quête 1/3 » sur l'accueil et écran dédié ;
  - **Premier coffre dès la 1re partie** (ensuite toutes les 3 parties) ;
  - **Âge et pubs demandés à la fin de la 1re partie** (le pseudo reste au premier lancement) ; la régie démarre seulement après ;
  - **Rappels** demandés juste après le premier coffre, au plus un par jour (roue prête, cadeau, Superstar, fin d'événement, série, retour) ;
  - **Événement de la semaine** (lundi 00:00 UTC) : un monde à l'honneur, un objectif commun (points, fusions, parties, poids posés, temps de tenue), 3 paliers (150 pièces, surprise gratuite, tête de la semaine) ;
  - **Roue de la chance** : un tour gratuit toutes les 24 h, 2 tours en plus par jour en vidéo ; 8 lots avec chances affichées (pièces 50/100/250, coffre, surprise, protège-série, pièces ×2 pendant 3 parties, jackpot : tête « Chanceux », introuvable ailleurs) ; ampoules, cliquetis à chaque part, rebond, lot révélé en grand ;
  - **Parrainage « Duo »** : tête Duo dès qu'un ami et toi êtes amis tous les deux ; « Inviter un ami » après chaque duel (code ami et lien du jeu).
- **Nouvelles têtes** (ajoutées à la fin) : 64 Duo, 65 Chanceux (trèfle à quatre feuilles), 66 Samb'bach. 67 têtes.
- **Code promo** (Réglages → Compte et achats → « Code promo ») : le code d'un partenaire offre sa tête, une seule fois par joueur (majuscules, espaces et apostrophes ne comptent pas). Premier partenaire : le BDE **Samb'bach**, code « Samb'bach » → la tête « Samb'bach » : une Brésilienne à la coiffure unique (grosse tresse sur l'épaule, balayage miel, bague dorée), coiffe du carnaval, robe de fête émeraude à volants avec le logo du BDE en broche dorée, silhouette fine, bracelets et ballerines dorés (`Core/Promo.cs`, `docs/PARTENAIRES.md`). Le code est donné à Apple dans les notes de vérification. Code accepté : la tête apparaît en grand (`UI/HeadRevealScreen.cs`) : le personnage complet, avec sa tenue et sa planche à rebords, tombe du ciel sur des rayons qui tournent, atterrit en pliant les genoux dans des confettis vert-jaune-bleu, puis « Code accepté ! », son nom, « Offerte par le BDE Samb'bach » et « La porter » / « Plus tard ».
- Maquettes : la planche a maintenant ses rebords, comme dans le jeu (`Tools/scene_preview.py`, image `planches/jeu_0.png`).
- **Arène (plus tard)** : plan complet dans `docs/ARENE.md` (divisions, matchmaking Unity, saisons de 4 semaines, récompenses cosmétiques), à lancer vers 1 500 joueurs par jour.
- **Premier lancement** : après « Bienvenue ! », choix du pseudo (idée au hasard avec le dé, « Plus tard » possible), année de naissance (roue neutre), puis « Des pubs qui te ressemblent ? » pour les 15 ans et plus (Accepter / Refuser, même taille). Un joueur déjà installé ne voit que ce qui lui manque (`UI/OnboardScreens.cs`, `Core/Privacy.cs`).
- **Pubs personnalisées** : consentement transmis à LevelPlay et aux réseaux ; demande de suivi d'iPhone (ATT) seulement après l'accord ; moins de 15 ans : jamais personnalisées, signalé « enfant » à toutes les régies. Réglage « Pubs personnalisées » dans Réglages → Aide. La régie ne démarre qu'une fois l'âge et le choix connus.
- **Réseaux publicitaires** : 84 identifiants SKAdNetwork, identifiant AdMob ajouté au build s'il est rempli (`Editor/AdNetworkSettings.cs`), guide `docs/PUBS.md`.
- **Boutique « Gratuit avec une vidéo »** : la **Superstar** (tête n° 63, lunettes en étoile, exclusive) après 8 vidéos, la planche assortie **« Scène étoilée »** après 8 vidéos, coffre gratuit (3 par jour). Chaque vidéo compte, même des jours après (`Core/AdRewards.cs`). Coffre de fin de partie : bouton **« Doubler »** (vidéo).
- **Duel** : nouvelle entrée (bannière VS, règles, amis à défier en un geste, code), code en grandes tuiles, invitation reçue en grand (« Léa te défie ! ») sur n'importe quel menu, vérifiée toutes les 4 s (au lieu de 20 s, seulement sur l'accueil). Relais en Europe d'office (plus de mesure de toutes les régions) : le code arrive plus vite.
- **Amis** : les liens d'amitié sont republiés à chaque lancement (un ajout fait hors ligne ou avec une ancienne version finit par arriver), vérification des amis réciproques toutes les 30 s sur les menus. « Défier » dans le menu d'un ami.
- **Classement** : onglets en verre, podium des 3 premiers (couronne, socles or, argent, bronze), bouton « Défier » sur chaque ami, « Ta place » toujours visible en bas. Mondial et du jour : 50 joueurs à la fois, « Voir les 50 suivants » jusqu'à 500 ; plus loin, « Autour de toi » montre les 3 joueurs juste devant et juste derrière soi (`GetPlayerRangeAsync`).
- **Premier lancement** au même style que le duel et l'invitation : fenêtre violet nuit, rayons, tête du joueur avec son pseudo en étiquette mis à jour en direct, roue des années sur verre, pubs expliquées en trois lignes de verre.
- **Fin de partie** : pastille « +12 places · 34e mondial » (ou du jour) dans la carte du score, dès que le score est envoyé.
- **Notes sur l'App Store** : l'identifiant de l'app était resté à zéro dans le lien de notation (corrigé) ; iPhone : fenêtre de notation d'Apple directement après un record (jusqu'à 3 fois, à 4 jours d'écart).
- Aperçus : `python3 Tools/screens_preview_v14.py Exports/art docs/apercu`.
- **Le 128 expliqué** : dès le premier 64 d'une partie, « Un 64 ! Fusionne-le avec un autre 64 : le 128 s'envole et rapporte +984 points » (sur 3 parties, tant que le joueur n'a jamais fait de 128) ; le tutoriel finit par « Monte jusqu'au 128 » ; à chaque envol, grande bannière dorée **« MONTGOLFIÈRE ! +984 points d'un coup »** sous la planche (`UI/BalloonBanner.cs`, aperçu `ecran_montgolfiere.png`), et au premier 128 : « Le 128 est le plus gros coup du jeu : refais-en pour exploser ton record ! ».
- **Roue réorganisée** : chaque lot en image au milieu de sa part, son nom près du bord doré (là où la part est la plus large). Vraies pièces d'or dessinées (`UiArtist.CoinPile`) : 1 pièce pour 50, 2 pour 100, un tas de 3 pour 250, 6 pour le jackpot ; « Boost » = une pièce marquée ×2 ; « Série » = flamme. Au gain, une pluie de pièces jaillit du lot (6 pour 50, 10 pour 100, 18 pour 250, 28 pour le jackpot).
- **Événement de la semaine plus long** : 50 000 points à cumuler (au lieu de 15 000), ou 900 fusions, 30 parties, 1 200 poids posés, 40 min de tenue (même difficulté, environ 25 à 30 parties). « Semaine du volcan » devient **« Semaine brûlante »**.
- **Rendez-vous de l'accueil redessinés** : petits médaillons ronds (112 px, discrets) avec un dessin en couleur (petite roue qui tourne avec son repère rouge, bloc-notes coché, calendrier étoilé), anneau d'avancement doré, étiquette sombre. Rangés sur les côtés sous la planche de Basile, ancrés en bas (ils ne touchent plus le record, « Reprendre » ni « Jouer », sur tous les écrans) : la roue à gauche, la quête à droite, l'événement dessous (puis à sa place une fois la quête finie).
- **Notifications du jeu qu'on chasse d'un geste** (pièces gagnées, mission réussie, messages en haut de l'écran), comme sur le téléphone : on la fait glisser sur le côté ou vers le haut, elle suit le doigt en pâlissant et s'en va (petite vibration) ; lâchée trop tôt, elle revient se poser ; un simple toucher la range aussi. Tant qu'on la tient, elle ne disparaît pas toute seule. Pendant une partie, elle ne capte jamais le doigt (on vise avec) (`SwipeDismiss` dans `UI/Ui.cs`).
- **« Noter Bascule »** dans Réglages → Aide : ouvre directement la page « Écrire un avis » de l'App Store.
- Maquettes : le bouton Duel porte « Direct » (plus « Bientôt »).
- **Retours de test (1.4, suite)** :
  - **Année de naissance en roue** (`UI/WheelPicker.cs`) : une seule colonne qu'on fait défiler vers le haut ou le bas avec le doigt (avec élan, comme le réglage de l'heure d'un téléphone), qui se cale sur une année ; l'année au centre, dans la bande bleue, est celle choisie ; flèches haut et bas, toucher une année la fait venir au centre, petit clic à chaque année. Toujours neutre : « Fais défiler » au centre au départ, « Valider » grisé tant qu'aucune année n'est choisie.
  - **Duel limité à 3 minutes** (`duel.timeLimitSeconds` = 180) : le chrono du duel compte à rebours (3:00 → 0:00, les 10 dernières secondes en rouge qui battent et cliquettent) ; à 0:00, si personne n'a lâché, « Temps écoulé ! Le meilleur score gagne », les deux lèvent les bras et le meilleur score gagne (égalité à score égal). Le premier qui lâche avant perd toujours (`DuelRules.TimeUp`, `DuelController.EndByTime`, test `DuelEndsAfterThreeMinutesAndBestScoreWins`).
  - **Fin du duel refaite** : titre qui claque avec rayons et confettis pour une victoire, carte VS aux coins arrondis (toi en bleu, l'ami en rouge) avec portrait, nom, score en grand et temps tenu ; le gagnant porte une couronne qui tombe sur sa tête et flotte dans un halo doré, le perdant pâlit ; étiquette « Temps écoulé · 3:00 » quand le duel s'est fini au temps ; pièces gagnées qui défilent (tas de pièces) ; bandeau vers la tête Challenger (aperçus `ecrans_duel_fin_1_4.png`).
  - **Entrée du duel** : la progression vers la tête Challenger remonte juste sous la carte VS (elle collait aux boutons du code), le code est tout en bas avec de l'air ; la carte VS a de vrais coins arrondis (le bleu, le rouge et les rayons sont découpés à la forme du panneau ; avant, un RectMask2D les coupait en rectangle).
  - **Fin de partie** : la pastille « Te voilà classé ! · 3e mondial » se pose dans la carte, à la place du mot « SCORE » (elle débordait au-dessus et cachait le titre de la partie).
  - **Événement de la semaine** : « Palier N » en haut de chaque lot, le nom du lot dessous sur deux lignes au besoin (« Surprise gratuite » chevauchait « Palier 2 »).
  - **Tête débloquée avec un code** : « La porter » et « Plus tard » plus espacés.
  - **Collection** : les têtes de partenaires (code promo) sont rangées dans l'onglet **Légendes**, en couleur avec leur nom ; la toucher ouvre la saisie du code.
  - **Classement** : l'onglet « Mes scores » est retiré (Monde, Jour, Amis).
  - **Boutique** : la carte de la Superstar disparaît une fois la tête gagnée (8 vidéos), celle de la planche « Scène étoilée » aussi : la boutique s'allège (la tête se choisit dans la collection, la planche dans la liste des planches).
  - **Accueil** : la petite roue est bien au centre de son médaillon.
  - **Pubs imposées** : plus jamais pendant les 4 premières parties de chaque lancement du jeu (même pour un joueur de longue date : avant, son compteur sauvegardé pouvait déclencher une pub dès la première partie), puis au plus une toutes les 4 parties et 3 minutes (avant : 3 parties). Retour après plus de 30 min en arrière-plan = nouveau lancement. Test `NoInterstitialDuringFirstFourGamesOfEachLaunch`. Réglages pour des pubs courtes et liens de tous les réseaux : `docs/PUBS.md`.
  - **Roue** : « Retourner en vidéo · 2 » devient « Encore un tour (vidéo) » avec l'icône vidéo, et une pastille dorée « 2 restants » (« Dernier ! ») sur le coin du bouton.
  - **Classement, « Ta place »** : le rang est dans une pastille rouge, bien dans le bloc (il débordait en bas à gauche) ; « Ta place » au-dessus du pseudo.
  - **Boutique et réglages** : un peu d'air à gauche et à droite de la liste (le contour des titres « Gratuit avec une vidéo », « Planches »… était coupé net).
  - **Boutons trop proches** (vérifiés dans tout le jeu) : « Super ! » et « Doubler » du coffre (ils se touchaient), « Récupérer » et « Doubler » du cadeau du jour, Valider / Plus tard du pseudo, Accepter / Refuser / En savoir plus des pubs, Jouer / Retour de la quête et de l'événement, Jouer / Retour du défi du jour, Ouvrir / Annuler de la machine à surprises, Porter / Plus tard, invitation au duel, Copier / Amis du duel, Rejouer / Partager / Accueil de la fin de partie, Reprendre / Jouer / Défi du jour de l'accueil : au moins 20 px entre deux boutons.
- **Décors plus tôt** (`Difficulty.ThemeScores`) : la plupart des parties s'arrêtent avant 8 000, les 20 000 sont rares. Prairie au fil de la journée jusqu'à 1 800, puis plage 2 500, forêt 3 300, désert 4 200, montagne 5 200, ville 6 300, ville la nuit 7 500, volcan 9 500, banquise 12 000, Lune 15 000 ; au-delà de 18 000, mondes déjà vus recolorés tous les 2 500 points. La difficulté ne change pas (elle ne dépend pas du décor).
- **Rafales plus douces** (`Core/Wind.cs`) : de 22 % à 52 % du couple de défaite (avant : 42 % à 60 %). Les 3 premières rafales sont « douces » : le vent seul ne dépasse pas 55 % de l'angle max et il s'efface dès que la planche penche (danger de 50 % à 80 %) : elles ne font jamais perdre. La limite d'équité passe désormais avant la force minimale. Duel : de 30 % à 64 % (avant : 45 % à 72 %).

## Aussi dans la 1.4 – mondes, tenues, coffre, partage
- **Retours de test** :
  - **Amis réciproques** : quand tu ajoutes quelqu'un, il te retrouve dans ses amis (vérifié toutes les 20 s à l'accueil). **Retirer des amis** depuis le classement des amis (toucher le joueur) ; un ami retiré ne revient pas tout seul.
  - **Défier un ami** : dans le salon du duel, « Mes amis » liste tes amis avec un bouton « Inviter » ; l'ami reçoit « Défi en duel ! » sur son écran d'accueil et rejoint d'un toucher (invitation valable 5 min).
  - **Duel plus fluide** : 20 photos/s au lieu de 15, délai de lissage adaptatif (plus d'image figée puis qui saute), vue de l'adversaire redessinée à 60 images/s.
  - **Compte à rebours du duel** : contour plus fin.
  - **Missions du jour de duel** : « Joue 1-2 duels avec un ami », « Gagne 1-2 duels » (seulement si le duel est disponible).
  - **Têtes un peu moins faciles au début** : paliers 300, 700, 1 200, 1 800, 2 500, 3 200… Une très grosse partie débloque toujours toutes les têtes qu'elle atteint.
  - **Pièces** : une pièce pour 40 points (au lieu de 25), niveaux et bonus des missions un peu moins généreux (les pièces déjà gagnées sont gardées).
  - **Classement du jour** : le meilleur score de la journée, toutes parties confondues (plus seulement le défi du jour).
- ~~Lancement : tout le monde repart à zéro~~ : **abandonné pour la 1.4**, rien n'est remis à zéro (voir plus haut). Le code de remise à zéro (`Seasons.Reset`) reste disponible pour une vraie nouvelle saison annoncée aux joueurs.
- **Relecture (robustesse et validation Apple)** :
  - partage iOS : correction d'une erreur d'édition de liens du plugin (`extern "C"`), plus de double feuille de partage ; texte `NSPhotoLibraryAddUsageDescription` ajouté (sans lui, « Enregistrer l'image » fermait l'app et Apple refuse le build) ;
  - pièces **achetées** et pas encore dépensées rendues lors de la remise à zéro (règle Apple 3.1.1 : une monnaie payée ne peut pas expirer) ; les pièces gagnées en jouant repartent bien de zéro ;
  - application fermée sur l'écran de fin : la partie est enregistrée sans être comptée deux fois si le joueur revient et prend sa seconde chance ; copie de secours de la sauvegarde si l'app est coupée pendant une écriture ;
  - seconde chance : le bouton reste proposé si la pub n'est pas disponible ; une seule pub à la fois (double toucher) ; « Restaurer mes achats » répond toujours ;
  - lien d'invitation au duel reçu pendant le coffre, la machine à surprises ou une pub : il attend le retour à l'accueil ;
  - sauvegarde abîmée : têtes ou planches inconnues retirées, pseudo refiltré ; pseudos des autres joueurs filtrés aussi à l'affichage ;
  - mémoire : anciennes textures de planche libérées, décor de l'adversaire libéré à la fin d'un duel, moins d'allocations à chaque image (néon, décor, mascotte, HUD) ;
  - typographie française : espace insécable avant « ! ? : ; % » (plus de « ! » seul en début de ligne) ;
  - écran de bienvenue : le duel n'est annoncé que s'il est disponible dans la version.
- **Défi du jour suivi en direct** : pendant la partie, une barre sous l'objectif avance avec le défi (« 32 / 60 » fusions, « 1:30 / 3:00 » de tenue…), devient verte « Défi réussi ! » ou rouge « Raté : un poids est tombé » pour les défis sans perte (`DailyChallenge.Track`).
- **Têtes gagnées dans le bilan** : halo doré et étiquette rouge « NOUVEAU ! » sur chaque tête débloquée par la partie.
- **Coffre** : il ne montre plus que son contenu (les pièces de niveau, missions et objectif sont déjà dans le bilan) ; en attendant d'être ouvert, il tremble beaucoup plus, saute, respire, lâche des étincelles à chaque secousse (avec une petite vibration), et les secousses se rapprochent à chaque tap.
- **Classement du jour vide** : explique qu'il se remplit avec les parties du défi du jour.
- **Écran de bienvenue** (`WelcomeScreen`), montré une fois à tout le monde : « Bienvenue ! Merci d'avoir téléchargé Bascule ! », Basile sur des rayons qui tournent, étiquette « C'est le lancement », ce qui attend le joueur (14 mondes, 63 têtes et leurs tenues, défi du jour, duel), et les classements tout neufs.
- **Nouvel écran du défi du jour** (`DailyScreen`) : le défi en grand sur une carte violette, la récompense en pastille dorée (verte « Réussi aujourd'hui ✓ » une fois fait), ton meilleur score du jour et ta série dans deux tuiles, la règle en deux lignes, le numéro du défi et un compte à rebours jusqu'au suivant. Avant : une fenêtre générique où le texte passait sous le bouton.
- **Petit village en bois dès le début** (`Core/Village.cs`) : sur les collines de la prairie (0 à 2 500 points depuis la 1.3), chalets en rondins au toit rouge, cheminées qui fument, jardinières fleuries, barrière de bois ; au loin des cabanes et un moulin à vent. La nuit, les fenêtres s'allument. Les chalets restent sur les côtés : Basile et sa planche restent bien dégagés.
- **14 mondes jusqu'à 30 000** (`Core/SceneBiomes.cs`, `Theme.All`, `Difficulty.ThemeScores`) : la prairie au fil de la journée (0 à 4 000), puis la **plage** (5 500 : mer, voilier, palmiers, coquillages), la **forêt** (7 000 : sapins, champignons), le **désert** (9 000 : mesas, dunes, cactus), la **montagne** (11 500 : sommets enneigés, sapins sous la neige), la **ville** (14 000 : gratte-ciel au loin, devant des rangées de maisons de ville colorées et un parc avec arbres et réverbères) et **la ville la nuit** (17 000, fenêtres allumées), le **volcan** (20 000 : cratère, coulées et fissures de lave), la **banquise** (23 500 : icebergs, igloo, pingouin) et la **Lune** (27 000 : cratères, drapeau, la Terre dans le ciel, pas de nuages).
- **Au-delà de 30 000** : un monde déjà vu revient tous les 3 000 points dans une autre teinte (7 teintes : rose, lavande, or, menthe, corail, violet, citron vert). 63 décors différents avant la moindre répétition, sans aucune image en plus.
- **Mémoire** : seuls le monde affiché et le suivant sont chargés ; le suivant se charge pendant la partie, les autres sont libérés (décor du joueur et de l'adversaire en duel pris en compte).
- **Une tenue par tête** (`Core/Outfits.cs`, `Core/OutfitArt.cs`) : 63 tenues toutes différentes, assorties au personnage (marinière du Pirate, robe étoilée du Magicien, robe royale du Roi, combinaison spatiale de l'Astronaute, pyjama étoilé de l'Endormi, maillot du Champion, costume du Gentleman…). Hauts : débardeur, t-shirt, pull à col roulé, sweat à capuche, maillot, chemise, blouson ouvert, costume, robe, combinaison. Bas : short, pantalon, jean, jogging. Chaussures : baskets, montantes, bottes, chaussures de ville. Motifs (rayures, fines rayures, pois, étoiles, carreaux) et écussons (B, étoile, cœur, éclair, lune, couronne, anneau). La tenue suit la tête choisie et ne change jamais en pleine partie. Clé de corps `peau_h<id>`. Mémoire : seuls les corps affichés et les deux derniers quittés restent chargés (`ArtCache.UseMascot`) ; images des corps en ASTC 6×6 (`Editor/ArtPackImport.cs`).
- **Coffre** : se déverrouille en tapant n'importe où sur l'écran ; ondes de choc et confettis à chaque tap, couleur qui monte (or, orange, rose, violet), rayon de lumière, ouverture avec trois anneaux arc-en-ciel et pluie de confettis, compteur de pièces qui défile, tête gagnée qui tourne sur elle-même. Nouveau dessin : bande dorée rivetée, pierres bleues, étoile sur le couvercle, intérieur plus lumineux.
- **Partage** : la vraie fenêtre de partage d'iOS (Messages, Snap, Insta…) avec l'image du score (`Plugins/iOS/BasculeShare.mm`), et celle d'Android ; avant, sans paquet NativeShare, le jeu se contentait de copier le texte (« texte copié »).
- **Nouvelles têtes** : pastille rouge avec le nombre sur le bouton Têtes de l'accueil et sur chaque onglet de la collection, étiquette « NOUVEAU ! » sur la tête ; la collection s'ouvre sur l'onglet des nouveautés, la pastille disparaît une fois vues.
- ArtPack régénéré, `ArtVersion` 18. 162 tests.

## V1.3 (1.3, build 4) – duel en direct, annonce de rafale, prêt pour la validation Apple
- **Duel en temps réel entre amis** (`Core/Duel.cs`, `Runtime/Duel/`, `UI/DuelScreens.cs`) :
  - Accueil → Duel → « Défier un ami » crée un code (6 caractères) et une invitation à envoyer (Snap, messages…) : `https://orpheex.fr/bascule/duel.html?c=CODE` ouvre le jeu (`bascule://duel/CODE`), sinon l'App Store, et garde le code à copier. « Rejoindre un duel » : saisir le code.
  - Même graine pour les deux : **mêmes poids, mêmes rafales au même instant**. Le premier qui lâche perd ; deux chutes à moins de 0,3 s : le meilleur score gagne.
  - **Vue en direct de l'adversaire** : une vraie petite scène du jeu (son Basile, sa tête, sa planche, ses poids, son décor) rejouée à partir de 15 photos par seconde, lissée, filmée par une seconde caméra (640 × 640, 30 images/s). Image nette, très peu de données. Cadre arrondi : le cadre rouge du danger et le voile rouge suivent exactement les coins.
  - Compte à rebours 3-2-1 (Basile entre pendant « 3… 2… », le premier poids attend « Partez ! »), chrono, scores en direct, « {nom} penche à 88 % ! », « Rafale dans 0:08 · pour vous deux ».
  - La pression monte avec le temps (planche plus sensible dès 20 s, rafales communes dès 25 s, de plus en plus fortes) : un duel dure 1 à 3 minutes.
  - L'adversaire lâche : **K.O. !**, 3 s pour poser un dernier poids (anneau qui se vide), puis Basile lève les bras sous les confettis. Si c'est toi qui lâches, sa vue grandit au centre : tu le regardes tenir.
  - **Tête Challenger** à l'effigie du créateur : cheveux courts bouclés, fine moustache, peau hâlée, et le seul personnage en **costume croisé** bleu marine à fines rayures (chemise blanche, pantalon assorti, chaussures de ville). Nouvelle notion de tenue (`HeadDef.Outfit`, `MascotArtist.BuildSuit`, clé de corps `peau_suit`). ArtPack régénéré, `ArtVersion` 15.
  - Résultat : victoire / défaite / égalité, temps tenus, scores, pièces (60 / 25 / 40), progression vers la **tête Challenger** (15 victoires), Revanche (les deux doivent accepter), Accueil.
  - Abandon (bouton ✕ en bas à gauche, ou quitter l'application) : défaite ; l'adversaire sans nouvelles pendant 8 s : victoire. Pas de pause en duel.
  - Le bouton Duel n'apparaît que si le paquet réseau est installé (plus aucun « Bientôt » dans le jeu).
- **Défi du jour, un vrai défi** : un exploit bien plus dur qu'une mission, le même pour tout le monde, à réussir en une seule partie, souvent avec deux conditions (« 4 000 points et un combo ×4 », « faire s'envoler le 128 sans perdre un seul poids », « tenir 3 min », « résister à 2 rafales »…). 28 défis qui tournent dans un ordre fixe : aucun ne revient avant 4 semaines (`Core.DailyChallenge.All`, test sur 3 ans de dates). 150 pièces, une fois par jour. Affiché dans la fenêtre du défi, pendant la partie et dans le bilan. Le vent ne change pas : toujours à partir de 5 000 points.
- **Missions plus originales** : résister à 2 ou 3 rafales (seulement si le record dépasse 6 500 : le vent arrive à 5 000), tenir 1 min 30 à 3 min sans tomber, faire des points sans perdre un seul poids ; combo ×4 dès le niveau 5.
- **Prochaine tête** : « Fais 6 500 en une partie pour la débloquer (record : 6 347) » au lieu de « Encore 153 pts », qui laissait croire que les points s'additionnaient.
- **Coffre** : trois étoiles dorées au lieu de trois ronds.
- **Néon des combos** (`UI/NeonEdge.cs`) : tout le contour de l'écran s'illumine, comme à l'activation de Siri sur iPhone (dégradé bleu, violet, rose, orange qui tourne, épaisseur qui ondule). Plus le combo est long, plus il est intense, épais, rapide et long. Dessiné en maillage, sans image.
- **Classements à jour tout de suite** : le score part dès la fin de la partie (avant : en quittant l'écran de fin), et le classement attend les envois en cours avant de s'afficher.
- Bouton « Défi du jour » de l'accueil : toujours le même libellé (plus de « Défi du jour fait - meilleur : … »).
- **Annonce de rafale** beaucoup plus visible (`UI/GustBanner.cs`) : grande banderole bleue qui entre depuis le côté du vent, « RAFALE ! » en très gros, flèches qui défilent, traits d'air, jauge jusqu'au souffle ; pendant le souffle, elle se resserre en pastille en bas pour laisser voir la pile.
- **Validation Apple** (détails et notes pour la vérification : `docs/APP_STORE.md`) :
  - `ITSAppUsesNonExemptEncryption = NO` ajouté à chaque build (plus de question « conformité à l'export ») ; langue de développement : français (`Editor/IosBuildPost.cs`) ;
  - manifeste de confidentialité `Assets/Plugins/iOS/PrivacyInfo.xcprivacy` (pas de suivi, données déclarées, API à raison obligatoire) ;
  - jamais de fausse pub dans une version publiée sans régie (`NoAdService`) ; identifiant publicitaire non collecté (`is_deviceid_optout`) ;
  - politique de confidentialité à jour (LevelPlay, code ami, duel, durée de conservation).
- 155 tests.

## V1.2 (1.2, build 3) – rafales de vent, fin de partie plus tendue, barre d'XP animée, code ami court
- **Fin de partie plus tendue** (retour TestFlight : « vers 8 000 – 9 000, on ne peut plus perdre »).
  - Cause trouvée : le bras de levier est fixe (`referenceHalfLength`), donc la planche qui raccourcissait avec le score donnait **moins** de couple, donc moins de danger. Elle arrête maintenant de raccourcir à 4 000 points (`shrinkUntilScore`) : avant 4 000, rien ne change.
  - **Planche plus sensible** à partir de 4 000 points : +5 % d'inclinaison par tranche de 1 000 points, jusqu'à +35 % (`Difficulty.Sensitivity`, `PlankModel.Sensitivity`).
  - **Rafales de vent** (`Core/Wind.cs`, `Runtime/Game/WindLayer.cs`) : à 5 000, 8 000, 10 000 puis tous les 2 500 points, et entre les paliers au plus toutes les 45 à 75 s (plus tôt si le joueur stagne : moins de 200 points en 20 s). Jamais deux rafales à moins de 25 s, jamais pendant le souffle du 128, la stabilisation d'un 64, ni quand la planche est déjà en danger. Annonce de 1,8 s (bandeau bleu « RAFALE » avec chevrons, traits d'air, souffle qui monte, petite vibration), puis 3 s de souffle (traits d'air, feuilles, nuages qui filent, légère secousse). Équité : le vent seul ne pousse jamais l'équilibre au-delà de 85 % de l'angle max ; il punit seulement le joueur qui ne réagit pas (poser de l'autre côté). Première rafale : un conseil explique quoi faire. Tout se règle dans la section `wind` de `bascule_tuning.json`.
  - Simulation (`python3 Tools/balance_sim.py haut 40 900`, `LEGACY=1` pour la 1.1) : jusqu'à 5 000 points, mêmes résultats qu'en 1.1 ; au-delà, la partie se resserre. Les robots meurent plus tôt qu'un vrai joueur : le réglage final se juge en TestFlight.
- **Barre d'XP animée** en fin de partie : elle se remplit niveau par niveau, s'arrête à chaque niveau gagné (« Niv. 7 +150 » qui jaillit, son, vibration, pièces ajoutées au compteur), puis repart. Plus aucun bandeau par-dessus l'écran de fin : l'objectif atteint, les missions, le bonus et les pièces doublées jaillissent aussi dans la carte, l'un après l'autre, et leurs pièces rejoignent le compteur à ce moment-là.
- **Saisie sans carré noir** (pseudo, code ami, duel) : la barre de saisie native d'iOS au-dessus du clavier est masquée (`shouldHideMobileInput`) ; on écrit directement dans le champ du jeu.
- **Signaler** : petit drapeau discret au lieu du gros bouton. Signaler ouvre l'e-mail sans masquer le joueur ; seul « Bloquer » le masque.
- **Tête proposée en fin de partie** : si plusieurs têtes de la boutique sont payables, on en propose une différente à chaque partie (`HeadCatalog.SuggestedShopHead`).
- **Code ami court** (« 4K7-Q2M », 6 caractères sans 0/O/1/I/L) via Cloud Save : publié en donnée publique indexée, retrouvé par requête (`FriendCode.Short`, `UgsLeaderboardService.PublishFriendCodeAsync` / `FindPlayerByCodeAsync`). Affiché sur le bouton « Mon code ». Les anciens codes longs restent acceptés. Sans le paquet Cloud Save, le code long reste utilisé.
- Nouvelle icône `Flag` (ArtPack régénéré, `ArtVersion` 13). 144 tests.

## V1.1 (1.1) – iOS 15 minimum, iPhone uniquement, premier envoi sur l’App Store
- **iOS 15 minimum** (au lieu de 13) : Apple refusera les apps qui ciblent iOS 13 à partir d'avril 2027 (avertissement ITMS-90068 reçu après le premier envoi).
- **Numéro de version** passé à 1.1 (au lieu de 20.6) : le numéro affiché sur l'App Store est celui de la version. Le build 20.5 est déjà sur App Store Connect et ne peut pas être réutilisé.
- **iPhone uniquement** (plus de version iPad) : Apple n'exige plus de captures d'écran iPad.
- **Poids tenu bien visible sur iPhone** : il se plaçait à une hauteur fixe, sans tenir compte de l'encoche ni de l'écran plus étroit. Sur iPhone, il restait caché derrière la ligne « Objectif ». Le HUD donne maintenant à chaque image le bas réel de l'interface du haut (`WorldLayout.HudBottomScreenY`), et le poids se place juste dessous (`WorldLayout.SpawnY`). Il descend d'environ 1 unité sur iPhone, un peu moins sur iPad.
- **Vraies pubs et vrais achats prêts** :
  - les pubs de test ne s'affichent plus que dans les builds de développement (`AdIds.TestMode = Debug.isDebugBuild`) : la version de l'App Store montre les vraies pubs ;
  - Unity IAP **5.x** (la 4.x n'est plus prise en charge) : le code garde l'API « Legacy » fournie par la 5.x, référence à l'assembly `Unity.Purchasing`, `PurchaseFailureDescription` dans `UnityEngine.Purchasing` ; les services Unity démarrent avant l'initialisation ;
  - si la boutique ne répond pas, le message est « Boutique indisponible : vérifie ta connexion » (au lieu de « Bientôt disponible »).
- **Pubs via LevelPlay** : Unity a arrêté la monétisation par le paquet Advertisement Legacy (31 janvier 2026). Nouveau `LevelPlayAdService` (paquet Ads Mediation, define `BASCULE_LEVELPLAY`), prioritaire sur l'ancien service. Pubs non personnalisées (`SetConsent(false)`, `do_not_sell`). Rappels du SDK repassés sur le fil principal ; la récompense attend 0,4 s après la fermeture (certaines régies l'envoient après).
- **Erreurs `FitText` détruit** (Console, au lancement) : un texte prévenait encore son ajusteur déjà détruit. Garde ajoutée et désinscription à la destruction.
- **Diagnostic des pubs (caché)** : dans Réglages, toucher 7 fois la ligne de version affiche l'état de LevelPlay (démarrage, pub récompensée et pub entre parties : prête ou code d'erreur). 7 fois de plus : le Test Suite LevelPlay s'ouvre au prochain lancement (état de chaque réseau). Les pubs qui ne chargent pas sont réessayées sans limite (toutes les 10 à 60 s).
- **Numéro de build iOS séparé** (`BasculeSetup.BuildNumber`, « 2 ») : à augmenter à chaque envoi à Apple.
- **Classement en ligne conforme à l'App Store** (règle 1.2, contenu créé par les joueurs) :
  - bouton **Signaler** sur chaque joueur du classement : fenêtre « Signaler le pseudo » / « Bloquer ce joueur » (`PlayerMenuScreen`) ;
  - un signalement masque le joueur tout de suite et ouvre un e-mail prérempli (pseudo + identifiants) vers `GameApp.ContactEmail` ;
  - un joueur bloqué disparaît du classement et de la liste d'amis (`Core/Social.cs → Moderation`, liste `Profile.blocked`) ; Réglages → « Débloquer les joueurs » ;
  - les règles du pseudo s'affichent dans la fenêtre où on le choisit ;
  - nouveau test (134 tests).

## V20.5 (20.5) – le lot tombe pile dans le cadre
- **Recalage à l'écran** (`SurpriseScreen.AlignWinner`) : sur la fin du défilement, le jeu mesure où sont vraiment le cadre doré et la case gagnée, et aligne leurs centres. Le lot finit exactement en face du sélecteur, quelle que soit la taille de l'écran. Si le calcul est déjà juste, rien ne bouge.
- **Cadre doré plus large** (324 px au lieu de 280) : une bordure d'or bien visible tout autour de la case gagnée, même quand elle grossit. On voit nettement que le lot est dedans.

## V20.4 (20.4) – carrousel d'avant, plus long
- Retour au carrousel d'avant, sans fausse fin : il démarre vite, ralentit et s'arrête sur le lot gagné.
- Il tourne un peu plus longtemps (5,5 s au lieu de 4,2 s) pour laisser le temps de voir passer les lots.
- Le lot gagné tombe pile sous le cadre doré (le décalage aléatoire est retiré).

## V20.3 (20.3) – décor plus visible (la machine est revenue en V20.4)
- **Décor par étapes plus nettes** : le premier changement arrive à 300 points (au lieu de 500), puis le midi, le doré à 800 (au lieu de 1 500), le couchant à 2 000 et la nuit à 4 000. Le cycle reprend après la nuit comme avant. Le décor change en fondu.
- **Machine à surprises** : le carrousel tourne 5,5 secondes (au lieu de 4,2) et ralentit jusqu'à s'arrêter sur le lot gagné. Le lot tombe pile sous le cadre doré. Avec « Animations réduites », la version courte (0,6 s) est gardée.
- Tests du décor mis à jour (133 tests).

## V20.2 (20.2) – textes lisibles, musique sans accroc à la boucle
- **Textes foncés lisibles** (cadeau du jour, étiquettes) : un titre a deux contours et une ombre, et on n'en coupait qu'un. Les lettres foncées restaient cernées de foncé, collées et illisibles. Nouveau `Ui.Plain` : il coupe tous les contours d'un texte posé sur un fond clair.
- **« Bientôt » sur le bouton Duel** : étiquette rouge cerclée de blanc, plus grande, texte blanc cerné.
- **Musique : plus d'accroc à la fin de la boucle (vers 1 min 17)**. Le garde-fou qui relance la musique quand elle s'arrête la relançait à tort au moment où le morceau revenait au début.
  - Web : il ne relance plus qu'après une vraie coupure du son du navigateur.
  - Téléphone : il ne relance que si la lecture n'avance plus du tout.

  Les fichiers `calme.ogg` et `tension.ogg` ont été vérifiés : pas de silence ni de fondu aux bouts, et le raccord fin → début est propre. Ils ne sont pas modifiés.

## V20.1 (20.1) – textes du cadeau du jour
- Les étiquettes « Aujourd'hui » et « Demain » sont dans le haut de leur tuile, à la place de « Jour N ». Elles ne débordent plus sur la tuile voisine, comme sur le titre du jour 7.
- Textes courts dans les petites tuiles (« Coffre », « Surprise ») : rien ne dépasse ni ne touche l'image. Le grand cadeau du jour 7 garde « Planche Bambou » et ajoute « · Demain » à son titre quand c'est le lendemain.

## V20 (20.0) – coffre à tapoter, cadeaux du jour variés, machine à lots, 64 plus doux
- **Coffre à tapoter** (comme dans les jeux à coffres) : il faut le toucher 3 fois.
  - À chaque coup, il saute et tremble plus fort, le couvercle se soulève, la lumière fuit davantage et le son monte (« Encore ! Il bouge… », « Encore une fois ! »).
  - Au troisième, il s'ouvre : éclair blanc, couvercle qui s'envole, pluie de pièces, puis la tête (s'il y en a une) qui sort après les pièces.
- **Cadeau du jour refait** (`GiftScreen`) : 7 jours en tuiles, avec ce qu'on y gagne.

  | Jour | Récompense |
  |---|---|
  | 1 | 50 pièces |
  | 2 | un coffre |
  | 3 | 120 pièces |
  | 4 | une surprise gratuite (ticket de la machine) |
  | 5 | 200 pièces |
  | 6 | un coffre |
  | 7 | le gros cadeau : une planche et 300 pièces (600 pièces si on a déjà toutes les planches) |

  - Les jours passés sont cochés, celui d'aujourd'hui brille, celui de demain est annoncé.
  - Le coffre du jour s'ouvre comme celui des parties, le ticket ouvre la machine gratuitement, la planche propose « Porter ».
  - Réglage : `dailyGift` (pièces) et `dailyGiftItems` (objet du jour) dans `bascule_tuning.json`. Le rappel du lendemain dit ce qui attend le joueur.
- **Machine à surprises à lots variés** (`SurpriseBox`) :
  - têtes 55 %, planches 15 %, pièces 30 % (400 : 20 %, 1 200 : 8 %, jackpot 3 000 : 2 %) ;
  - jamais de doublon : têtes et planches déjà gagnées ne sortent plus, leur part va aux autres lots ;
  - chaque lot et sa chance sont affichés avant d'ouvrir ; le carrousel mélange têtes, planches et pièces ;
  - la valeur moyenne en pièces reste bien sous le prix (test).
  - Les tickets du cadeau du jour donnent une ouverture gratuite (« Gratuit » dans la boutique).
- **Un peu plus facile quand un 64 apparaît d'un coup** : pendant 1,5 s, la planche est amortie 1,6 fois plus et la tension monte 0,6 fois moins vite. On peut toujours perdre (test `BigMergeSteadiesThePlankButCanStillLose`). Réglages `bigMerge*` dans `bascule_tuning.json`.
- **Bilan** : les noms des nouvelles têtes restent dans la carte (le texte était centré sur le bas de sa case et débordait).
- **Message de partage réécrit**, comme un message entre amis, sans la frise de carrés de couleur (elle reste sur l'image). Par exemple :
  > 🏆 Nouveau record sur Bascule : 2 508 points !
  > Mon titre : « Roi du combo », avec un combo ×5 🔥
  >
  > Tu crois faire mieux ? 😏 Joue exactement la même partie que moi avec le code VQZM-S5EY
  > 👉 orpheex.fr/bascule
- **Duel : « Bientôt »**. Le bouton porte une étiquette « Bientôt » et ouvre « Les duels arrivent bientôt ! », sans champ de code pour l'instant.
- `Tools/player_sim.py` est synchronisé (cadeau du jour, machine à lots). Le modèle de 1 000 joueurs garde 32,8 % des joueurs après 7 jours, contre 31,8 % avant.
- Maquettes : `docs/apercu/ecran_cadeau.png`, `ecran_coffre_ferme.png`, `ecran_surprise_offer.png`.

## V19.7 (19.7) – machine à surprises en carrousel
- **Nouvel écran de la machine à surprises** (`SurpriseScreen`), à la place de la petite fenêtre au texte minuscule :
  - les têtes encore à gagner sont présentées en cartes : silhouette avec « ? », halo et bande de la couleur de leur rareté, rareté écrite, chance en gros (« 12 % ») ;
  - les cartes sont triées de la plus probable à la plus rare ;
  - en bas, « Ouvrir · 1 000 » et « Annuler ».
- **Carrousel à l'ouverture** :
  - les têtes défilent à l'horizontale dans une bande, avec un cadre doré et deux flèches au centre, et un petit clic à chaque tête qui passe ;
  - le défilement démarre vite puis ralentit longtemps (4,2 s), pour le suspense ;
  - il s'arrête sur la tête gagnée, qui grossit, avec lumière et rayons, son nom et sa rareté, puis « Porter » ou « Plus tard ».
  - La tête est tirée et payée avant l'animation (`SurpriseBox.Open`, chances inchangées et affichées) : le carrousel ne fait que la montrer. Avec « Animations réduites », il dure 0,6 s.
- **Tête au hasard** : la tête change maintenant au début de chaque partie (Rejouer, Jouer, Défi du jour), et non plus à la fin de la partie précédente.
- Maquettes : `docs/apercu/ecran_surprise_offer.png`, `ecran_surprise_spin.png` et `ecran_surprise_reveal.png`.

## V19.6 (19.6) – coffre mis en valeur, bilan plus clair, cœur sur le web
- **Écran du coffre** :
  - lumière dorée et rayons dès qu'il est fermé ;
  - coffre plus grand (700 px), avec des étincelles qui scintillent autour ;
  - en attendant le toucher, il tremble par à-coups et son couvercle se soulève un peu, avec un filet de lumière, comme si quelque chose bougeait dedans ;
  - fond presque opaque : le bilan derrière ne le brouille plus.
- **Bilan, ligne du coffre** :
  - coffre gagné : un vrai bouton « bonbon » doré, « Ouvrir le coffre ! », avec un petit coffre qui gigote dedans ;
  - sinon : petit coffre, texte foncé lisible (« Prochain coffre dans 2 parties », « Coffre ouvert : +180 pièces ») et trois cases.
- **Bilan resserré** : score, record, détail, niveau, coffre et objectif mieux espacés ; plus rien ne se touche. Les boutons « Ouvrir le coffre ! » et « Voir » font au moins 130 px de haut, la taille prévue pour les boutons « bonbon » (sinon leurs bords arrondis sont écrasés).
- **Bilan, prochain objectif** : quand une tête de la boutique est déjà payable, on la voit en couleur avec « Tu peux t'offrir Chef ! » et un bouton « Voir » qui ouvre les têtes de la boutique. Avant, on lisait « Acheter Chef ? » avec une barre pleine.
- **Web : battement de cœur quand on va perdre**, joué simplement au rythme de la musique (`MusicPlayer.WebHeart`). Il avait été coupé sur le web avec les corrections du son.
- **Web : les boutons ne grossissent plus à la fin de la partie.** Les sous-canvas d'animation (`Ui.Isolate`) ne sont plus ajoutés sur le web : un canvas imbriqué créé dans un écran caché pouvait s'afficher quelques instants à la mauvaise échelle.
- Maquettes à jour : `Tools/scene_preview.py` montre l'écran du coffre (fermé et ouvert), la nouvelle jauge, le bouton du coffre et l'interrupteur « Au hasard ».

## V19.5 (19.5) – pilules et verre qui ne s'étirent plus
- **Verre liquide corrigé partout** (Record, dock, pastilles, onglets) :
  - le petit reflet blanc reste dans le bout gauche de l'image, la partie jamais étirée en 9-slice, au lieu de s'allonger en traînée sur les éléments larges ;
  - toute pilule ou tout verre plus fin que son image garde des bouts bien ronds : `Ui.Image` y ajoute `CapsuleSlice`.
- **Nouvelles lignes de récompense** dans l'écran du coffre : étiquettes crème bien rondes, cerclées de sombre, avec une ombre et un reflet. Pastille de couleur à gauche, titre et détail sur deux lignes, pièce et gain juste à côté à droite.
- Aperçu : `docs/apercu/coffre.png`. Il est fait comme Unity, en découpant les images en 9 parties. `ArtCache.ArtVersion` passe à 12.

## V19.4 (19.4) – nouveau coffre
- **Coffre redessiné** (`UiArtist.ChestBase` et `ChestLid`) :
  - planches de bois verticales veinées, plus claires en haut ;
  - cadre de bois foncé ;
  - coins dorés en équerre avec rivets ;
  - couvercle bombé avec reflet et sangles dorées ;
  - grosse serrure dorée avec une pierre rouge ;
  - ombre au sol.
- Le coffre est plus grand à l'écran (640 px de large au lieu de 560).
- Aperçus : `docs/apercu/coffre_ferme_ouvert.png` et `docs/apercu/coffre.png`. `ArtCache.ArtVersion` passe à 11.

## V19.3 (19.3) – coffre à ouvrir, récompenses regroupées, tête au hasard
- **Un vrai coffre à ouvrir** (toutes les 3 parties). Dans le bilan, la ligne du coffre devient un bouton doré « Ouvrir le coffre ! ». Un écran montre alors le coffre (`UiArtist.ChestBase` et `ChestLid`, dans l'ArtPack) :
  - il sautille ;
  - on le touche, il tremble ;
  - le couvercle s'envole, une lumière d'or jaillit avec des pièces, et le contenu apparaît (pièces, parfois une tête).

  Si le joueur touche Rejouer ou Accueil sans l'avoir ouvert, le coffre s'ouvre d'abord. Ses pièces s'ajoutent au total du bilan à l'ouverture. Ouvrir le coffre enregistre la partie : on ne peut plus revivre ensuite.
- **Fin des notifications à la chaîne.** Les récompenses de la partie (objectif, missions, bonus, niveau) ne défilent plus l'une après l'autre sur le titre.
  - Avec un coffre : elles sont listées, toutes ensemble, sous le coffre.
  - Sans coffre : un seul message les regroupe, posé sur le bas du bilan, et il disparaît quand on quitte l'écran.
- **Tête au hasard** : un interrupteur dans la collection, « Au hasard ». À chaque partie, Basile porte une autre tête gagnée, jamais deux fois la même de suite. Choisir une tête précise coupe le hasard. Code : `HeadCatalog.PickRandom` (testé).
- **Barres de progression refaites** (prochaine tête en partie, bilan, collection) :
  - bouts bien ronds quelle que soit la hauteur (`CapsuleSlice`), au lieu d'une image étirée qui finissait en pointe ;
  - rail sombre cerné et remplissage avec reflet ;
  - la prochaine tête dans un médaillon rond.
- **La planche rétrécit moins vite** : 0,14 unité par tranche de 1 000 points au lieu de 0,25. Elle atteint sa taille minimale vers 17 000 points au lieu de 9 600 (`shrinkPer1000Points`).
- **« Perdu ! -64 » reste dans l'écran** : le texte est sur deux lignes et assez loin du bord.
- **Plus de message au changement de décor** : le décor change en fondu, sans annonce.
- Aperçu : `docs/apercu/coffre.png`.

## V19.2 (19.2) – nouvel écran de chargement
- **Les lettres de BASCULE tombent sur la planche de Basile.** Elles arrivent une à une, d'un bout à l'autre (B, E, A, L, S, U, C). Chaque lettre fait pencher la planche du côté chargé, puis le ressort la rééquilibre, comme en partie. Des étincelles de la couleur de la lettre jaillissent à chaque arrivée.
- La bulle du niveau à bulle sert de barre de progression, avec un pourcentage et une phrase au hasard en dessous.
- Mot complet : les lettres sautent en vague, puis l'écran s'efface sur l'accueil.
- **Appli (iPhone, Android)** : `Runtime/UI/BootCurtain.cs`.
  - Le décor est celui de l'accueil : ciel, soleil qui tourne, nuages qui avancent, collines.
  - Durée : environ 2 s. Un toucher n'importe où passe l'animation.
  - L'accueil se prépare pendant ce temps, derrière l'écran.
- **Web** : la page de chargement (`WebGLTemplates/Bascule/index.html`) suit le vrai téléchargement. Le jeu reprend ensuite exactement sa dernière image et s'efface tout de suite.
- **Animations réduites** : les lettres sont déjà posées et le fondu est rapide.
- La planche et ses rebords sont dessinés à l'avance dans l'ArtPack (`scene_boot_plank`, `ArtCache.ArtVersion` 9). Le démarrage ne dessine rien.
- Aperçus : `docs/apercu/chargement_appli.png` et `docs/apercu/chargement_web.png`.

## V19.1 (19.1) – rebords de planche finis
- **Rebords assortis à chaque planche**, dessinés par le code (`SceneArtist.PlankLip`) :
  - Bois : rondin coupé avec une pousse verte ;
  - Bambou : nœud de bambou avec une feuille ;
  - Skate : plot noir et rose avec une petite roue ;
  - Surf : plot turquoise avec un ballon de plage ;
  - Marbre : petite colonne ;
  - Sucre d'orge : crosse rayée ;
  - Or massif : pommeau doré.

  L'ancien rectangle marron uni est remplacé. Les rebords apparaissent aussi dans l'aperçu des planches de la boutique.
- **Posés à 100 % sur la planche** : chaque rebord est placé là où le dessus devient plat, après l'arrondi du bout, et la zone qui retient les poids est au même endroit. Le test `EveryLipSitsFullyOnThePlank` vérifie, colonne par colonne de pixels et à deux résolutions, qu'aucun rebord ne dépasse dans le vide. Ce test échoue avec l'ancienne position.
- Les planches Surf et Skate ont des bouts un peu moins arrondis, pour que leur rebord repose sur le plat.
- Aperçu : `docs/apercu/rebords.png`.

## V19 (19.0) – verre liquide, effets, décor qui évolue, 128 sans défaite immédiate
- **Le 128 ne fait plus perdre la partie dans la seconde.**
  - Son poids s'efface progressivement en 1,6 s au lieu de 0,2 s : la planche penche sans claquer.
  - Pendant les 2,5 s qui suivent, la planche est mieux amortie et on ne peut pas perdre : le temps de rattraper l'équilibre avec le poids suivant.
  - Réglages dans `bascule_tuning.json` : `balloonLiftSeconds`, `balloonGraceSeconds`, `balloonGraceDamping`. Un test vérifie que la défaite revient si rien n'est rattrapé.
- **Verre liquide** :
  - les pastilles du haut, le score, le dock, les onglets et les étiquettes sont en verre bleu nuit translucide, avec un liseré lumineux et des reflets ;
  - chaque bouton reçoit un reflet blanc bombé, un liseré et un point de lumière : il a l'air de verre coloré.

  Images `ui_glass` et `ui_gloss`, pack régénéré (ArtVersion 8).
- **Effets plus accrocheurs** :
  - les points d'une fusion grossissent avec le poids et prennent sa couleur ;
  - le combo grossit et change de couleur à chaque maillon (étoiles, onde et secousse dès ×3, éclair plein écran dès ×4) ;
  - l'éclair est aussi présent sur les 32, les 64 et le 128 ;
  - le score du haut bondit plus fort sur les gros gains et brille en doré ;
  - un grand chiffre doré et des confettis à chaque millier de points ;
  - l'éclair est coupé en mode « animations réduites ».
- **Le décor évolue avec le score**, avec l'annonce « Nouveau décor : … » et des confettis :
  - matin, plein soleil (500), heure dorée (1 500), couchant (3 000), nuit étoilée (5 000) ;
  - ensuite, une nouvelle journée tous les 3 000 points.

## V18.5 (18.5)
- **Record et pièces de nouveau à la bonne taille** : l'ajustement automatique des textes (V17) rétrécissait les étiquettes qui suivent la largeur de leur texte (« Record : … », compteur de pièces, prix). Au premier affichage leur zone était vide, le texte se réduisait, et il restait minuscule ensuite. Ces étiquettes gardent maintenant leur taille normale.

## V18.4 (18.4) – son et fluidité pendant les combos
- **Le son ne reste plus bloqué (web, Safari sur iPhone).** Quand le navigateur suspendait son contexte audio, le jeu relançait la musique dans le vide : on n'entendait que des « tic ».
  - Le contexte audio est relancé à chaque toucher et au retour sur la page (`Assets/Plugins/WebGL/BasculeAudio.jslib`).
  - La musique ne repart qu'une fois le son revenu.
- **Moins de charge audio sur le web** : une seule couche de musique (la couche « tension » reste sur l'appli), sans changement de vitesse ni battement de cœur. Au plus 2 nouveaux bruitages par image et 3 à 4 en un quart de seconde.
- **Le jeu suit pendant les combos** : les éclats, la poussière, la sueur et les confettis sont recyclés au lieu d'être créés puis détruits à chaque fois, avec 140 au plus à l'écran. Sur le web, il y en a deux fois moins.
- **L'explication du 128 se lit** : plus aucun autre conseil ne la remplace, et elle ne se ferme plus au poids suivant (6 secondes).

## V18.3 (18.3)
- **Explication du 128** : « Un 128 s'envole : gros bonus de points, mais ta planche se rééquilibre d'un coup ! »
  - Elle s'affiche au-dessus de la planche quand le joueur fabrique son premier 64, au moment où il comprend ce qui arrive en en fusionnant deux.
  - S'il n'a jamais eu de 64 à l'écran, elle s'affiche au premier 128.
  - Elle ne s'affiche qu'une seule fois.

## V18.2 (18.2) – réglages et écran du téléphone
- **Réglages** : les flèches des lignes « Compte et achats » et « Aide » sont dans la carte, au lieu de dépasser le bord.
- **iPhone (version web)** : la barre d'état est transparente (`black-translucent`). Le fond bleu du jeu remonte jusqu'en haut, sous la Dynamic Island, au lieu d'une bande grise.
- **Dock du bas** : remonté au-dessus de la barre d'accueil de l'iPhone. Basile se décale automatiquement pour rester au-dessus.

## V18.1 (18.1)
- **Boutique** :
  - le titre est centré sur l'écran, loin du bouton retour et du compteur de pièces ;
  - sur la machine à surprises, la pastille et le bouton de prix sont un peu plus petits, et « 13 têtes à gagner » est une étiquette violette à gauche du bouton ;
  - les étoiles restent dans la carte.

## V18 (18.0) – têtes plus rares, collection réorganisée
- **Têtes plus rares** (docs/ECONOMIE.md, section 3 bis) :
  - paliers de score de 250 à 75 000 points (au lieu de 250 à 50 000), avec seulement quatre têtes faciles au début ;
  - niveaux de 3 à 60 (au lieu de 3 à 45) ;
  - boutique de 600 à 8 000 pièces (au lieu de 400 à 4 000) ;
  - machine à surprises à 1 000 pièces ;
  - une tête dans 8 % des coffres seulement (au lieu de 16 %, plus de tête garantie au 2e coffre) ;
  - exploits plus durs : séries de 5 et 14 jours, sauvetage à 97 %, combo ×5.
- **Rareté** : Commune (gris), Rare (bleu), Épique (violet), Légendaire (or), avec un halo et une étiquette sur chaque tête.
- **Collection refaite** :
  - en haut, la tête portée en grand, avec sa rareté et la progression (18 / 62) ;
  - des onglets Records, Niveaux, Boutique, Exploits et Légendes, avec le compte de chacun ;
  - une grille triée par difficulté, où la prochaine tête à gagner est signalée.
- **Mise en page** :
  - le titre « Boutique » (et « Têtes ») ne touche plus le compteur de pièces ;
  - sur la carte de la machine à surprises, le texte ne passe plus sous le bouton ;
  - le nom de la planche équipée est dans le même style que les autres.
- Aperçus : `docs/apercu/ecran_tetes.png`, `docs/apercu/ecran_boutique_pieces.png`.

## V17 (17.0) – économie, finitions de l'interface, textes qui tiennent
- **Économie** (docs/ECONOMIE.md). Les pièces n'achètent que du style, et rien ne change le score :
  - **Machine à surprises** (600 pièces) : une tête de la boutique au hasard, jamais une que l'on a déjà. Les chances sont affichées avant d'ouvrir, et les têtes chères sortent plus rarement.
  - **6 planches à collectionner** : Bambou, Skate, Surf, Marbre, Sucre d'orge et **Or massif** (5 000 pièces, l'objet de prestige). Même taille et même physique, seul le dessin change.
  - **Protège-série** (300 pièces, 2 au plus) : la série du cadeau du jour continue si l'on oublie un jour.
  - Quand il manque des pièces, la boutique défile jusqu'aux paquets.
  - Modèle de 1000 joueurs : 38,2 % encore là après 7 jours (au lieu de 36,3 %), 9,6 % après 30 jours (au lieu de 8,3 %), 41 minutes jouées par joueur (au lieu de 37).
- **Plus aucun texte qui dépasse.** Le « Best Fit » de Unity ne réduisait rien quand le débordement vertical était autorisé, c'est-à-dire partout. Chaque texte réduit maintenant sa taille jusqu'à tenir dans sa zone. Les boutons restent sur une ligne et l'icône se resserre si la place manque (par exemple « Changer » dans les missions). Ça marche aussi avec la grande taille de texte des Réglages.
- **Réglages refaits** : sections en cartes, interrupteurs animés, pastilles d'icône colorées, choix de la taille du texte en trois « A ». Les **outils de prototype** (sens de bascule, recharger les réglages) ne sont plus visibles dans la version publiée.
- **Panneau de partage web dans le style du jeu** : boutons « bonbon » faits avec les images du jeu, rayons qui tournent, carte qui apparaît en rebondissant, bouton fermer rond.
- **Menus** : ouverture en fondu, médailles or, argent et bronze dans le classement, ta ligne bien visible, score et record affichés dans la pause.
- **Correctifs** :
  - Le limiteur de son n'est plus activé sur le web, où Unity ne le prend pas en charge.
  - L'avertissement `FindObjectOfType` de Unity 6 est corrigé.
  - Le README est à jour : identifiant, réglages itch.io pour jouer sur téléphone, guides.

## V16.1 (16.1) – sauvegarde et partage sur le web
- **Web : les scores et la progression ne s'enregistraient pas.** La sauvegarde passait par un fil d'arrière-plan, et le web n'en a pas : l'écriture ne partait jamais vers le navigateur. Sur le web, la sauvegarde s'écrit maintenant tout de suite, puis elle est enregistrée dans le navigateur (`Assets/Plugins/WebGL/BasculeFs.jslib`).
- L'appli iPhone et Android ne change pas : elle sauvegardait déjà correctement sur le téléphone.
- **Web : le bouton Partager ne montrait rien.** NativeShare n'existe pas dans un navigateur, et le presse-papiers de Unity n'atteint pas celui du téléphone. Le bouton ouvre maintenant un panneau avec :
  - l'image de fin ;
  - **Partager** (la feuille de partage du téléphone, avec l'image) ;
  - **Enregistrer l'image** ;
  - **Copier le texte** (le résumé à coller en légende) ;
  - **Fermer**.

  Le code ami utilise le même panneau. Fichiers : `Assets/Plugins/WebGL/BasculeShare.jslib` et `Runtime/Services/WebShareService.cs`.
- Appli iPhone et Android : le partage s'ouvre seulement si le paquet **NativeShare** est installé (docs/INTEGRATIONS.md, section 2). Sans lui, le texte est copié, mais l'image reste dans les fichiers de l'appli.

## V16 (16.0) – son de l'appli sans craquement
- **Cause du craquement trouvé** : chaque bruitage arrêtait une source audio (`Stop`) pour la réutiliser. Un son arrêté en pleine vibration fait un clic, et pendant un combo les sons s'arrêtaient ainsi à la chaîne. Les bruitages se superposent maintenant avec `PlayOneShot` : aucun son n'est coupé. Le hasard sur la hauteur des chocs est retiré, ce qui évite aussi des décalages de note.
- Le nombre de bruitages par image reste limité (4), et le volume baisse un peu quand ils se cumulent.
- Le tampon audio agrandi, la réserve de voix et le limiteur de V15 restent en place.

## V15 (15.0) – son des combos, identifiant définitif
- **Son pendant les combos** : une cascade de fusions lançait des dizaines de sons dans la même image (un « pop » et une note par fusion, plus les chocs). Corrections :
  - **Plus de sons coupés en plein milieu** : on prend d'abord une voix libre. Sinon, seul un son important (note de combo, montgolfière, chute) prend la place de celui qui est presque éteint. Les sons secondaires sont ignorés au lieu de couper les autres. Chaque coupure faisait un « clic », d'où le craquement.
  - **4 nouveaux sons par image au plus** : au-delà, les chocs et les pops ne s'entendent pas, ils faisaient seulement saturer. Les notes de combo passent toujours.
  - **12 voix sur téléphone** au lieu de 8 (8 sur le web).
  - **Tampon audio plus grand** (1024 échantillons, environ 21 ms) : pendant un combo, le téléphone calcule beaucoup (fusions, confettis, textes). Avec le petit tampon par défaut, le son n'était plus fourni à temps (craquements, coupures). Le décalage reste imperceptible pour ce jeu.
- **Identifiant de l'application** : `fr.orpheex.bascule`, assorti au site orpheex.fr. Rien n'était encore publié : c'était le dernier moment pour le changer.

## V14.2 (14.2)
- Le site est **orpheex.fr**, partout :
  - les liens du jeu (confidentialité, aide) ;
  - le résumé partagé et la carte de score (orpheex.fr/bascule) ;
  - les pages légales et la fiche store.

## V14.1 (14.1)
- Le nom de l'éditeur s'affiche « ORPHÉE X » dans le jeu (rideau de démarrage et page de chargement web). Rien d'autre ne change : le site orphex.fr, l'identifiant fr.orphex.bascule, les pages légales et la fiche store.

## V14 (14.0) – chargement aux couleurs de Bascule
- **Page de chargement web** (`Assets/WebGLTemplates/Bascule`), choisie automatiquement par « Bascule → Version web ». Elle remplace le logo Unity et sa barre grise par :
  - le ciel du matin et les collines ;
  - le logo BASCULE coloré qui se balance ;
  - l'icône de Basile ;
  - une barre de progression jaune ;
  - des petites phrases qui défilent (« Basile s'échauffe… », « On cire la planche… ») ;
  - « ORPHEX » en bas.

  Aperçu : `docs/apercu/chargement_web.png`. L'icône et le nom « Bascule » servent aussi quand on ajoute le jeu à l'écran d'accueil de l'iPhone.
- **Plus de « Made with Unity »** : l'écran de démarrage Unity est désactivé (autorisé avec Unity 6) et le fond de démarrage est bleu ciel.
- **Rideau de démarrage dans le jeu** : même ciel et même logo, il cache les toutes premières images puis s'efface en fondu, sans flash entre le chargement et l'accueil. Il marche aussi sur iPhone et Android.

## V13 (13.0) – haut et bas de l'accueil retouchés
- **Niveau** (en haut à gauche) : l'étiquette est un peu plus haute. « Niv. 7 » est en haut, la barre d'XP est plus fine juste en dessous, et les deux ne se touchent plus. Les bouts de la barre restent ronds.
- **Pastilles des boutons du bas** (missions, boutique) : petite pastille ronde de 50 au lieu de 76, posée sur le coin et presque entièrement hors du bouton. L'icône reste visible et un liseré blanc la détache du bouton rouge de la Boutique.

## V12 (12.0) – accueil équilibré, bannières propres, son fiable
- **Accueil réorganisé** :
  - Le record descend, bien détaché du logo. Son étiquette s'ajuste au texte : le trophée ne chevauche plus « Garde Basile en équilibre ! ».
  - Jouer et Défi du jour sont recentrés.
  - Les cinq accès (Têtes, Missions, Boutique, Classement, Duel) forment un dock en bas, sous les pieds de Basile. Sur les téléphones hauts, le décor et Basile remontent un peu sur l'accueil pour lui faire de la place, puis redescendent en douceur quand la partie commence. Le jeu lui-même ne change pas.
- **Bannières de récompense refaites** :
  - pastille à gauche, titre et détail au milieu, pièces à droite, avec des marges égales ;
  - le détail passe sur deux lignes si besoin et la bannière grandit avec lui ;
  - plus rien ne déborde ni ne touche le bord.
- **Son** :
  - **Grésillement** : la musique, le battement de cœur et jusqu'à 8 bruitages s'additionnaient et dépassaient le maximum, d'où la saturation. Les niveaux sont baissés et chaque bruitage est un peu moins fort quand d'autres jouent déjà. Un limiteur doux sur le mélange final arrondit les pics au lieu de les écrêter.
  - **Musique qui s'arrête** : le garde-fou relançait la musique chaque seconde sur une fausse alerte, ce qui pouvait la faire grésiller au lieu de la réparer. Il faut maintenant deux constats de suite, et il y a au plus une relance toutes les 5 s. Sur le web, la musique démarre avec un lancement simple.
  - **Retour dans l'appli** (appel, verrouillage, Siri) : les deux couches de musique repartent proprement, au même endroit.
  - **Web** : la musique est importée en 32 kHz. Le navigateur garde 20 Mo en mémoire au lieu de 60, et Safari sur iPhone ne coupe plus le son.
  - La vitesse de la musique (qui monte avec le danger) n'est recalculée que quand le changement s'entend.

## V11 (11.0) – un jeu dont on parle
Le détail et les sources sont dans docs/PARTAGE.md.
- **Chute au ralenti** : quand la planche bascule, l'action ralentit environ une seconde et Basile lâche ses « derniers mots » (« MA PLANCHE ! », « C'était le vent ! »…). C'est le moment qu'on filme. La partie est déjà perdue à ce moment-là : le résultat ne change pas.
- **Titre de la partie** : sur l'écran de fin et la carte, selon ce qui s'est passé (« Funambule de l'extrême », « Roi du combo », « Mains de beurre », « Chute express »…). Il y a des exploits et des échecs drôles : les deux font réagir.
- **Carte de partage refaite** au format vertical 9:16 (Stories, TikTok photo, Snapchat). Elle montre :
  - la **vraie photo de la chute** dans un polaroïd, sans l'interface ;
  - le titre, le score et le record ou le sauvetage de justesse ;
  - la **frise de tension** ;
  - le code défi et orphex.fr/bascule.
- **Résumé « à la Wordle »** copié avec le partage, à coller en légende, par exemple :
  `BASCULE ⚖️ / 4 820 points · Funambule de l'extrême / 🟩🟨🟧🟥🟩💥 / 😱 Sauvé à 97 % de la chute / Tu fais mieux ? Même partie avec le code K7PM-3QXZ`.
  - Pour le Défi du jour, le résumé donne le numéro du jour (« Défi du jour n°12 »), pour comparer entre amis.
- **« À 120 points du record ! »** sur l'écran de fin quand le record est frôlé.
- Le bouton Partager s'anime aussi après un sauvetage fou ou un record frôlé.
- **Mode vidéo** (Réglages) : pendant la partie, le nom du jeu et le code défi restent affichés en bas de l'écran, et les conseils sont masqués. Il suffit de lancer l'enregistrement d'écran du téléphone.
- Aperçu de la carte : docs/apercu/carte_partage.png.

## V10 (10.0) – récompenses et finitions
- **Coffre** : il se remplit d'une case par partie et s'ouvre toutes les 3 parties (pièces, parfois une tête de la boutique). Les cases sont visibles sur l'écran de fin.
- **Objectif de partie** (mode infini) : environ 60 % du record, affiché sous le niveau à bulle. Il passe au vert et rapporte 25 pièces une fois atteint.
- **Parties spéciales** : une partie sur 4 (après les 4 premières), annoncée par un ruban sur le bouton Jouer (pièces ×2, XP ×2 ou coffre +2). Le score n'est jamais modifié.
- **Messages** : la grosse pilule sombre du bas (« +50 ») est remplacée par une étiquette crème compacte en haut de l'écran, avec une icône. Les pièces gagnées (cadeau, pièces gratuites, ×2) ont une vraie bannière avec l'icône de pièce.
- **Conseil « Glisse pour viser »** : plus de panneau sombre qui pulse, juste un texte cerné qui apparaît et disparaît en fondu.
- **Musique** : elle ne se coupe plus quand on enchaîne les poids. Avant, les bruitages prenaient la place de la musique dans les voix audio. La musique a maintenant la priorité, et les bruitages identiques trop rapprochés sont regroupés. Si le système audio coupe quand même la musique (téléphone, navigateur), elle redémarre toute seule au bon endroit en moins d'une seconde.
- Analyse des départs refaite sur 1000 joueurs (docs/RAPPORT.md, section V10).

## V9 (9.0) – optimisation complète
- **Démarrage instantané** : les 473 images du jeu sont dessinées à l'avance (Tools/ArtPack, 9,9 Mo) au lieu d'être calculées au lancement. C'était la cause de la lenteur au démarrage, des saccades de Basile et du logo, et de l'erreur « OOM » (manque de mémoire) sur le web.
- Images compressées pour le GPU en haute qualité (ASTC) : 4 fois moins de mémoire.
- Musique en OGG Vorbis : plus de grésillement à la fin de la boucle (le MP3 ajoute un silence).
- Interface : chaque élément animé (logo, boutons qui pulsent, rayons) a son propre sous-canvas, Unity ne recalcule plus toute l'interface à chaque image. Voile de danger coupé quand il est invisible.
- 60 images/s par défaut (moins de chauffe, donc pas de ralentissement au bout d'un moment) ; 120 en option dans les réglages.
- Physique : contacts réutilisés, traitement des contacts allégé.
- Web : mémoire réservée d'emblée, textures au format des téléphones, plus de zoom bloqué quand le clavier s'ouvre (duel, pseudo).
- Fin de partie : vraies bannières de récompense (mission réussie, bonus, niveau) avec icône et pièces gagnées. Missions : icône propre à chaque type (le poids à fabriquer est montré).
- Saisie (pseudo, duel, code ami) : curseur et sélection visibles.

## V8 (8.0)
- Identité définitive : Bascule, éditée par Orphex, identifiant `fr.orphex.bascule`. Jeu en français uniquement (choix de langue retiré).
- Icône de l'application (Basile qui tient sa planche chargée de poids, sur un soleil), installée automatiquement dans le projet.
- Classement : onglet **Amis** avec code ami à partager ; pseudos filtrés (longueur, grossièretés) ; « Supprimer mes données en ligne » dans les réglages ; lien « Aide et contact ».
- Classement et collection : portraits chargés au fur et à mesure (plus de blocage à l'ouverture).
- Pages pour le site orphex.fr : aide et politique de confidentialité (`docs/site/bascule`).
- Fiche store prête à copier (`docs/STORE.md`).
- Menus Unity : « Android : construire l'APK de test », « Capture d'écran pour les stores » (Ctrl+Maj+K), format « Captures App Store ».
- Correction : la mascotte n'est plus téléportée si l'écran change de taille pendant son arrivée.

## V7 (7.0)
- Moins de petits freezes : les sauvegardes et les statistiques s'écrivent sur un fil à part (plus d'attente du disque), et la sauvegarde de partie en cours est faite toutes les 10 s au lieu de 5 s.
- « Défi du jour » ouvre d'abord une fenêtre qui explique la règle, le record du jour et la série, avec le bouton « Jouer ».
- Basile arrive en marchant au début de chaque partie, la planche sur les épaules (il n'apparaissait pas avant).
- Accueil allégé : le badge de série est déplacé dans la fenêtre du défi.

## V6 (6.0)
- Musique : les deux morceaux créés avec ElevenLabs (calme et tension, 96 BPM, même durée) sont intégrés dans `Assets/Bascule/Resources/Musique` (`calme.mp3`, `tension.mp3`). Le jeu les utilise à la place de la musique générée par le code.
- Boucle raccordée sans clic : la fin fond dans le début sur une mesure entière (2,5 s), pour que le battement de cœur reste calé sur les temps.
- Pour revenir à la musique générée : supprimer ces deux fichiers.

## V5 (5.0)
- Correction du blocage (et du plantage de Unity) à l'ouverture de « Têtes » : la collection s'ouvre tout de suite et les portraits apparaissent au fur et à mesure (préparés en priorité en arrière-plan), au lieu de 62 dessins d'un coup.
- Préparation en arrière-plan sans aucune fonction de Unity (cause probable du plantage) : cache d'images dans un format brut sans perte, rechargé sans décodage (plus rapide).
- Portraits utiles en partie et en boutique préparés dès le lancement.

## V4 (4.0)
- Fluidité : toutes les images sont dessinées en arrière-plan dès le lancement puis chargées pendant les menus ; plus aucun calcul d'image en pleine partie (nouveau poids, expression, ambiance à 2 500 points).
- Le corps de Basile n'est plus redessiné à chaque lancement ni à chaque changement de tête.
- La carte de score n'est fabriquée qu'au moment de partager (plus d'à-coup à la défaite) ; textes flottants recyclés.
- Jouabilité : petits rebords en bois aux bouts de la planche (vraies collisions), planche un peu moins nerveuse et un peu plus de temps pour la rattraper. Gravité et collisions inchangées.
- Garde-fou anti-bug relevé : il ne freine jamais une chute normale, seulement une vitesse anormale.
- Unity Remote réglé pour le moins de décalage ; nouveau menu « Bascule → Version web » pour jouer sur iPhone dans Safari, sans décalage.

## V3 (3.0)
- Images en haute résolution (interface ×2, décor ×2 à ×3, portraits 384 px) : nettes sur iPhone Pro et iPad. Aucune image compressée.
- Dynamic Island et barre d'accueil respectés ; simulés aussi dans l'éditeur et Unity Remote (format iPhone 14 Pro).
- Physique : le poids créé par une fusion ne chevauche plus les autres (il faisait sauter la pile), vitesses plafonnées, correction des chevauchements plus douce, calcul plus précis.
- 120 images par seconde sur les écrans qui le permettent.
- Numéro de version dans les réglages.

## V2
- Plus de cœurs ; battement de cœur calé sur le tempo de la musique près de la chute.
- Musique de 1 min 20 en 8 parties ; bruitages accordés et mieux dosés.
- Tablettes plein écran ; vue Game au format téléphone pour Unity Remote ; plus d'écran d'explication au lancement ; arrivée de la mascotte visible.

## V1
- Progression (niveaux, pièces, missions, cadeau du jour, 62 têtes), boutique, pubs encadrées, tutoriel, rapport de simulation.
