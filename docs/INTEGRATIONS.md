# Brancher les services (classement, partage, pub, achats, rappels, stores)

Le jeu fonctionne entièrement hors ligne sans rien de ce qui suit. Chaque service s'ajoute indépendamment des autres.

## 1. Classement en ligne : Unity Gaming Services (plan gratuit)

1. Dans Unity : **Edit → Project Settings → Services**, lier le projet à un projet Unity Cloud (créer une organisation si besoin).
2. **Window → Package Manager → Unity Registry**, installer **Leaderboards** (`com.unity.services.leaderboards`). L'authentification et le cœur UGS s'installent avec.
3. Sur le tableau de bord Unity Cloud → **Leaderboards**, créer deux classements, avec exactement ces identifiants :
   - `bascule_monde_s2` : ordre décroissant, stratégie « garder le meilleur score ».
   - `bascule_jour_s2` : pareil, avec une **remise à zéro quotidienne à 00:00 UTC**.
   - (Saison 2, `Core.Seasons` : les anciens `bascule_monde` et `bascule_jour` ne servent plus, tout le monde repart de zéro.)
4. C'est tout. Le define `BASCULE_UGS` s'active tout seul quand le paquet est présent (voir `Bascule.Runtime.asmdef`), et `UgsLeaderboardService` remplace le mode hors ligne. Connexion anonyme, pseudo demandé à la première visite du classement.

**Amis** : rien de plus à configurer. Chaque joueur a un code ami (Classement → Amis → « Mon code ami », partagé par message). L'onglet Amis affiche les records de ses amis dans `bascule_monde_s2`.

**Pseudos** : filtrés dans le jeu (3 à 16 caractères, grossièretés refusées, `Core/Social.cs`). Dans le tableau de bord Unity Cloud, on peut aussi supprimer un joueur signalé.

**Code ami court (Cloud Save, gratuit)** : Package Manager → **Cloud Save** (`com.unity.services.cloudsave`, 3.x). Le define `BASCULE_CLOUDSAVE` s'active tout seul. Dans le tableau de bord Unity Cloud → **Cloud Save → Queries** (Index), créer un index : entité **Player**, accès **Public**, clé **`friend_code`** (type chaîne). Sans cet index, la recherche par code court échoue (le code long reste accepté).

**Amis réciproques et invitations au duel (Cloud Save)** : créer de la même façon (entité **Player**, accès **Public**, type chaîne) six index de plus : **`link1`**, **`link2`**, **`link3`**, **`link4`**, **`link5`** (« j'ai ajouté ce joueur » : l'autre nous ajoute à son tour) et **`duel_to`** (invitation au duel d'un ami, vue sur son écran d'accueil). Sans eux, l'ajout reste à sens unique et les invitations n'arrivent pas (le code et le lien de duel marchent toujours).

**Modération (obligatoire pour l'App Store, règle 1.2)** : chaque ligne du classement a un bouton « Signaler ». Le joueur peut signaler le pseudo (e-mail prérempli vers `GameApp.ContactEmail`, avec l'identifiant du joueur) ou le bloquer (il disparaît de son classement et de ses amis). Lire cette boîte chaque jour : pour supprimer un pseudo, tableau de bord Unity Cloud → Leaderboards → chercher l'identifiant du joueur et supprimer son score (ou le joueur dans Player Management).

**Suppression des données** : Réglages → « Supprimer mes données en ligne » efface le compte en ligne (RGPD, et règle de l'App Store pour les applis avec compte).

> Ce code est vérifié hors Unity contre une copie des interfaces de Unity Gaming Services (`Tools/CompileCheck/PackageStubs.cs`). Si Unity signale une erreur après l'installation, elle viendra d'un nom d'API qui a changé entre deux versions du SDK : c'est rapide à corriger.

## 2. Partage natif de la carte de score : NativeShare (gratuit, licence MIT)

1. **Package Manager → + → Add package from git URL** : `https://github.com/yasirkula/UnityNativeShare.git` (il faut Git installé sur l'ordinateur).
2. Le define `BASCULE_NATIVESHARE` s'active tout seul : le bouton Partager ouvre alors la feuille de partage du téléphone (WhatsApp, Snapchat, Instagram, TikTok…). La carte PNG et le texte avec le code défi sont joints.

Sans NativeShare, le bouton copie le texte dans le presse-papiers et enregistre l'image dans `Application.persistentDataPath/bascule_score.png`.

## 3. Publicités : Unity LevelPlay (gratuit)

> **Depuis le 31 janvier 2026**, le paquet Advertisement Legacy (`com.unity.ads`) ne permet plus de gagner de l'argent : Unity demande de passer par **LevelPlay** (paquet **Ads Mediation**, `com.unity.services.levelplay`). Le jeu le prend en priorité : define `BASCULE_LEVELPLAY`, classe `LevelPlayAdService`. Retirer Advertisement Legacy du projet.
>
> 1. Package Manager → Unity Registry → **Ads Mediation** (LevelPlay). Retirer **Advertisement Legacy**.
> 2. Tableau de bord LevelPlay : ajouter l'app iOS (`fr.orpheex.bascule`), noter la **clé d'app** (App Key), créer un bloc **Rewarded** et un bloc **Interstitial**, noter leurs identifiants, activer le réseau Unity Ads.
> 3. Remplir `GameApp.AdIds.LevelPlayAppKey`, `LevelPlayRewarded`, `LevelPlayInterstitial`.
> 4. Pubs non personnalisées : `SetConsent(false)` et `do_not_sell = true`, déclarés avant `LevelPlay.Init`.
>
> La suite de cette section (Unity Ads direct) reste comme solution de secours.


Le jeu affiche deux sortes de pubs, toujours **non personnalisées** et **jamais pendant une partie** :

- **Récompensées**, au choix du joueur : revivre, doubler les pièces d'une partie, doubler le cadeau du jour, pièces gratuites (5 par jour), changer une mission. Plafond : 20 par jour.
- **Imposées (interstitiels)**, seulement quand on quitte l'écran de fin, et seulement si `Core/Economy.cs → AdPolicy` l'autorise : pas avant la 6e partie, au plus une toutes les 3 parties **et** toutes les 3 minutes, pas juste après une vidéo choisie, pas après un record ou une nouvelle tête, pas après une partie de moins de 25 s, 12 par jour au maximum. Tout se règle dans la section `ads` de `bascule_tuning.json`.

Sans régie branchée, `SimulatedAdService` affiche une fausse pub pour tester tout le parcours.

1. Créer un compte sur [Unity Ads (Unity Cloud → Monetization)](https://cloud.unity.com), ajouter le projet, et noter le **Game ID** Android et iOS.
2. Créer deux blocs publicitaires : `Rewarded_Android` / `Rewarded_iOS` (récompensé) et `Interstitial_Android` / `Interstitial_iOS` (interstitiel). Ce sont les noms par défaut, déjà dans `GameApp.AdIds`.
3. Package Manager → **Advertisement Legacy** (`com.unity.ads`). Le define `BASCULE_UNITYADS` s'active tout seul.
4. Remplir `GameApp.AdIds.Game` avec le Game ID (iOS et Android). `TestMode` suit tout seul le type de build : pubs de test dans un build de développement, vraies pubs sinon. Pour tester la vraie version sur ton téléphone, l'ajouter comme appareil de test dans Unity Ads, et ne jamais cliquer sur ses propres pubs (compte suspendu).
5. Dans le tableau de bord Unity Ads, régler le public sur « contenu pour toute la famille / public mixte ». Le code déclare déjà au SDK l'absence de consentement au ciblage (`gdpr.consent = false`, `privacy.consent = false`, `privacy.mode = mixed`, `user.nonbehavioral = true`).
6. Le joueur qui a acheté « Sans publicité » ou l'offre de départ ne voit plus aucune pub imposée. La seconde chance et les vidéos qui donnent des pièces restent proposées : elles sont toujours au choix.

> Autre régie possible (Unity LevelPlay, AdMob) : écrire une classe qui implémente `IAdService` (`CanShowRewarded`, `CanShowInterstitial`, `ShowRewarded`, `ShowInterstitial`) et la créer à la place dans `GameApp.Awake`. AdMob : `TagForChildDirectedTreatment` ou `TagForUnderAgeOfConsent`, et `MaxAdContentRating = G`.

## 4. Achats in-app : Unity IAP (version 5.x)

Catalogue (`Core/Economy.cs → Shop.Products`) : uniquement du confort ou du cosmétique. Les pièces n'achètent que des têtes.

| Identifiant | Type | Prix conseillé | Contenu |
|---|---|---|---|
| `bascule.starter` | non consommable | 3,99 € | sans pub + 2 000 pièces + tête Basile doré. Proposé une seule fois, 48 h, après la 8e partie |
| `bascule.removeads` | non consommable | 2,99 € | plus de pubs imposées entre les parties |
| `bascule.coins.small` | consommable | 0,99 € | 600 pièces |
| `bascule.coins.medium` | consommable | 2,99 € | 2 200 pièces (+20 %) |
| `bascule.coins.large` | consommable | 6,99 € | 6 000 pièces (+40 %) |
| `bascule.legends` | non consommable | 4,99 € | 5 têtes exclusives |

1. Package Manager → **In-App Purchasing**, **version 5.x** (la 4.x n'est plus prise en charge depuis juin 2026). Le code utilise l'API « Legacy » que la 5.x fournit encore (avertissements « obsolete » normaux) ; l'assembly s'appelle `Unity.Purchasing`. Le define `BASCULE_IAP` s'active pour [5.0.0, 6.0.0).
2. Créer ces 6 produits avec **exactement ces identifiants** dans Google Play Console (Monétiser → Produits) et App Store Connect (Achats intégrés).
3. Les prix affichés viennent du store, dans la devise du joueur. Les prix du tableau ne servent que tant que le store n'a pas répondu.
4. iOS : le bouton **Restaurer mes achats** (boutique et réglages) est obligatoire pour la validation Apple ; il est déjà là.
5. Avant un gros lancement, ajouter la vérification des reçus (Unity IAP fournit `CrossPlatformValidator`).

## 4 bis. Rappels : Mobile Notifications (gratuit)

1. Package Manager → **Mobile Notifications** (`com.unity.mobile.notifications`). Le define `BASCULE_NOTIFICATIONS` s'active tout seul.
2. Le jeu demande l'autorisation **après la 3e partie**, avec d'abord sa propre fenêtre d'explication (sinon iOS ne redemande jamais). Le joueur peut les couper dans Réglages → Rappels.
3. Au plus un rappel par jour, vers 18 h 30 : cadeau du jour prêt (le lendemain), série du défi en danger (le soir même), « Basile s'ennuie » (3 jours après). Tout est annulé quand le joueur revient dans l'appli.

## 4 ter. Statistiques d'usage

Le jeu note des événements anonymes (`Services/EngagementServices.cs → Analytics`) dans `analytics.jsonl` sur le téléphone : ouverture, début et fin de partie (score, durée, cause), tutoriel, niveaux, missions, cadeau, pubs vues, achats. Pour les voir de tous les joueurs pendant le lancement test, brancher un service (Unity Analytics, GameAnalytics, Firebase) en implémentant `IAnalyticsSink` et l'ajouter avec `App.Stats.AddSink(...)`. Ce que chaque mesure permet de décider est dans `docs/RAPPORT.md`, section « Mesurer ».

## 4 quater. Duel en temps réel : Multiplayer Services (Relay)

Le duel relie deux téléphones par le **relais** de Unity (pas besoin d'être sur le même Wi-Fi) ; une « session » privée pour deux donne le code à 6 caractères. Pas de Netcode : le jeu envoie ses propres petits messages (Unity Transport), environ 15 photos de partie par seconde (~1 à 2 Ko/s par joueur).

1. **Package Manager → Unity Registry → « Multiplayer Services »** (`com.unity.services.multiplayer`, 1.0 ou plus). Il installe aussi Unity Transport. Le define `BASCULE_MULTIPLAYER` s'active seul (`Bascule.Runtime.asmdef`).
2. **Dashboard Unity Cloud** (même projet que le classement) → **Multiplayer → Relay** : activer. **Lobby** (utilisé par les sessions) : activer aussi.
3. Rien d'autre à configurer : la connexion anonyme est déjà celle du classement.
4. **Tester** : deux téléphones (ou un téléphone et l'éditeur). Accueil → Duel → « Défier un ami » sur le premier, « Rejoindre un duel » + code sur le second. Le bouton Duel n'apparaît sur l'accueil que si le paquet est installé.
5. **Lien d'invitation** : mettre `docs/site/bascule/duel.html` sur `https://orpheex.fr/bascule/duel.html`. Le schéma `bascule://` est déclaré par « Bascule → Configurer le projet » (`PlayerSettings.iOS.iOSUrlSchemes`) et vérifié après chaque build iOS (`Editor/IosBuildPost.cs`).

**Plan gratuit** : Relay et Lobby sont gratuits jusqu'à un certain nombre de joueurs connectés en même temps (CCU) et de bande passante par mois ; au-delà, paiement à l'usage. Vérifier les chiffres à jour sur la page de tarifs de Unity Gaming Services et dans le Dashboard (Usage), et y régler une **alerte de dépense**. Un duel = 2 joueurs connectés et environ 3 Mo pour 3 minutes.

Réglages (section `duel` de `bascule_tuning.json`) : compte à rebours, pression qui monte avec le temps (`pressureFromSeconds`, `sensitivityPerMinute`, `sensitivityMax`), rafales communes (`firstGustSeconds`, `gustGapMin/Max`, `gustStrength*`), fenêtre du dernier poids (`finalWindowSeconds`), égalité (`tieWindowSeconds`), pièces (`coinsWin/Lose/Draw`), tête Challenger (`challengerWins`), photos par seconde, délai d'abandon (`timeoutSeconds`).

## 5. Publication

- **Identifiant** : `fr.orpheex.bascule` (réglé automatiquement par « Bascule → Configurer le projet »). Il ne pourra plus changer après la première mise en ligne : le valider avant.
- **Android** : IL2CPP et ARM64 sont déjà configurés. Générer un **AAB** (Build Settings → *Build App Bundle*), avec une clé de signature (Player Settings → Publishing Settings), à garder précieusement.
- **Classification d'âge** : remplir le questionnaire IARC (Google Play) et l'âge (App Store) honnêtement : pubs, achats, classement avec pseudos.
- **Google Play, programme « Familles »** : si le jeu cible les moins de 13 ans, il faut des régies certifiées Familles et aucune donnée publicitaire personnalisée. C'est ce que prévoit la section 3 ci-dessus.
- **Confidentialité** : publier une page (même simple) et mettre son adresse dans `GameApp.PrivacyUrl`. Les stores la demandent. Le texte d'accueil du jeu est dans `Core/Loc.cs` (clés `consent_*`).
- **Catégorie « Enfants » de l'App Store** : à éviter. Elle interdit presque toutes les régies et impose un contrôle parental avant chaque achat. Choisir plutôt « Jeux → Familial / Réflexion », avec une classification 4+ ou 9+.
- **Pseudos** : le classement affiche des pseudos choisis par des mineurs. Prévoir un filtre de gros mots avant le lancement public (UGS ne filtre pas tout seul).
