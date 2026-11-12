# Pubs : mieux payées, sans en montrer plus

Objectif : chaque pub rapporte plus (eCPM), sans ajouter une seule pub imposée. Trois leviers, du plus fort au plus faible :

1. **Plus de réseaux en enchères dans LevelPlay.** Chaque pub est mise aux enchères entre tous les réseaux branchés. Plus il y a d'acheteurs, plus le prix monte.
2. **Pubs personnalisées** (1.4) : avec l'accord du joueur (15 ans ou plus), puis la demande de suivi d'iPhone. Elles se paient souvent 2 à 3 fois plus cher.
3. **Plus de pubs choisies**, toujours au choix du joueur (1.4) : coffre gratuit, doubler le coffre, tête et planche exclusives. Les vidéos récompensées sont les mieux payées.

Règles des pubs imposées (`AdPolicy`, section `ads` de `bascule_tuning.json`) : pas avant la 6e partie ; **jamais pendant les 4 premières parties de chaque lancement du jeu** (1.4 : un joueur de longue date ne voit plus de pub dès sa première partie) ; ensuite au plus une toutes les 4 parties et 3 minutes ; jamais après un record, une nouvelle tête, une vidéo choisie ou une partie de moins de 25 s. Un retour dans le jeu après plus de 30 min en arrière-plan compte comme un nouveau lancement.

## Ce que le code fait déjà (1.4)

| Sujet | Où |
|---|---|
| Âge (année de naissance), choix des pubs, réglage « Pubs personnalisées » | `Core/Privacy.cs`, `UI/OnboardScreens.cs`, réglages |
| Consentement transmis à LevelPlay et aux réseaux (Google, AppLovin, Meta, Unity, Chartboost, InMobi) | `LevelPlayAdService.ApplyPrivacy` |
| Moins de 15 ans : pas de personnalisation, signalé « enfant » à toutes les régies, pas de demande de suivi | `PrivacyRules.ChildDirected` |
| Demande de suivi d'iPhone (ATT), seulement après l'accord dans le jeu | `Plugins/iOS/BasculeTracking.mm`, `TrackingPermission` |
| Texte de la demande de suivi, 84 identifiants SKAdNetwork (tous les grands réseaux) | `Editor/IosBuildPost.cs`, `Editor/AdNetworkSettings.cs` |
| Identifiant AdMob (iPhone et Android) ajouté au build s'il est rempli | `AdNetworkSettings.AdMobIosAppId`, `AdMobAndroidAppId` |
| La régie ne démarre qu'une fois l'âge et le choix connus | `GameApp.StartAds` |

## À faire de ton côté, dans l'ordre

### 1. Choisir les réseaux (LevelPlay → Setup → Networks)

Commence par ces cinq. Ce sont les plus gros acheteurs en France, et ils sont tous en enchères (« bidding ») : aucun réglage de prix à faire.

| Réseau | Compte à créer | Ce que LevelPlay demande |
|---|---|---|
| **AppLovin** | dash.applovin.com | Report Key, SDK Key, puis une zone « Rewarded » et une « Interstitial » |
| **Google AdMob** | admob.google.com | Identifiant d'app (`ca-app-pub-…~…`), un bloc récompensé et un interstitiel |
| **Meta Audience Network** | business.facebook.com | Identifiant d'app Meta et des placements (compte Business vérifié) |
| **Mintegral** | mintegral.com | App Key, App ID, placements |
| **Liftoff (Vungle)** | publisher.vungle.com | App ID, référence des placements |

Ensuite, si tu veux aller plus loin : Pangle, InMobi, Moloco, DT Exchange, Chartboost.

Pour chaque réseau, remplis aussi **ses informations de paiement** (banque, formulaire fiscal). Chaque réseau te paie séparément, chaque mois, au-dessus d'un minimum.

### 2. Installer les adaptateurs dans Unity

Unity → **Ads Mediation → Network Manager** → **Install** sur chaque réseau activé dans le tableau de bord. Puis `Assets → External Dependency Manager → Android Resolver → Force Resolve`.

Ces modules sont installés dans le projet Unity de `Downloads\Bascule_V20.5\Bascule`, pas dans Git, comme LevelPlay.

### 3. Google AdMob : l'identifiant d'app, obligatoire

Sans lui, **le jeu plante au lancement** dès que l'adaptateur AdMob est installé. Mets-le dans `Assets/Bascule/Scripts/Editor/AdNetworkSettings.cs` :

```csharp
public const string AdMobIosAppId = "ca-app-pub-XXXXXXXXXXXXXXXX~YYYYYYYYYY";
public const string AdMobAndroidAppId = "ca-app-pub-XXXXXXXXXXXXXXXX~ZZZZZZZZZZ";
```

Ce n'est pas un secret. L'app iPhone et l'app Android ont chacune le leur.

### 4. Vérifier avant d'envoyer

Réglages du jeu : touche 7 fois la ligne de version pour voir l'état de la régie, puis 7 fois de plus pour ouvrir le **Test Suite LevelPlay** au lancement suivant. Il montre chaque réseau, son adaptateur et une pub de test.

### 5. App Store et Google Play

- **App Store Connect → Confidentialité de l'app** : les données de la régie deviennent « utilisées pour vous suivre » (voir `docs/APP_STORE.md`).
- **Google Play Console → Contenu de l'app** :
  - Identifiant publicitaire : **Oui**, pour la publicité.
  - Section sécurité des données : identifiants de l'appareil, données de pub. Partagées avec des régies, pour la publicité, après consentement.
- **Politique de confidentialité** (`docs/site/bascule/confidentialite.html`) : déjà mise à jour (âge, consentement, réseaux). À remettre en ligne.

## Des pubs imposées courtes

Le jeu ne peut pas choisir la durée d'une pub : c'est le réseau qui l'envoie. On la règle dans le tableau de bord de chaque réseau, sur le bloc **interstitiel** seulement (les vidéos récompensées sont choisies par le joueur, elles peuvent rester longues) :

- **Google AdMob** : dans le bloc interstitiel, garder seulement les pubs « display » (texte, image, rich media) et décocher la vidéo, si tu veux des pubs imposées vraiment courtes (elles se paient un peu moins).
- **Unity Ads, Liftoff, Mintegral, Pangle et les autres** : sur le placement interstitiel, choisir une pub **passable** (bouton « Passer » ou croix après 5 s) quand le réseau le propose.

## État au 12/11/2026 (iPhone seulement : le jeu n'est pas encore sur Google Play)

- **Google AdMob** : compte créé (éditeur `pub-3087780473555937`), app iOS « Bascule » associée à l'App Store, identifiant `ca-app-pub-3087780473555937~5965751815` déjà dans `AdNetworkSettings.AdMobIosAppId` (on peut donc installer l'adaptateur AdMob sans plantage sur iPhone). Validation en attente du fichier `app-ads.txt`.
- **Liftoff** : app créée (App ID `6af5f5eab828b7d5f62cc750`), placements Interstitiel `DEFAULT-2531405` et Récompensé `BASCULE-7622057` (à saisir dans LevelPlay → Instances) ; validation en attente de la lecture du fichier `app-ads.txt`.
- **Mintegral** : compte en cours de validation ; clés à saisir ensuite dans LevelPlay (APP Key dans « APP Setting », Skey et Secret dans Account → API Tools).
- **Unity Ads et ironSource Ads** (LevelPlay) : lignes ajoutées (liste d'Unity : Monetization → Settings → Organization → App-ads.txt ; ironSource : éditeur `698899`). Dans Unity, renseigner ensuite l'« App-ads URL » si la page le demande.
- **AppLovin** : inscription faite, compte en attente de confirmation par AppLovin.
- **Pangle** : app « Bascule » créée (ID 8932945, en test) ; compléter les informations de l'entreprise pour l'activer, puis créer les placements. Sa ligne app-ads.txt viendra ensuite.
- **InMobi** : compte créé (ORPHEE X), app Bascule iOS (App Key 10000254834) avec placements ; liste app-ads.txt d'InMobi ajoutée (ligne DIRECT `8a84b603e05f47fbbef99ff2d12ba74b`).
- **Chartboost** : app Bascule créée (en attente de l'intégration du SDK, normal) ; créer des emplacements en enchères (« bidding ») seulement.
- **DT Exchange** : inscription en cours de vérification.
- **Meta Audience Network** : pas commencé (la page « Monétisation » de Meta Business Suite concerne les Pages Facebook, pas les applis).
- **app-ads.txt** : en ligne sur https://orpheex.fr/app-ads.txt depuis le 12/11/2026 (vérifié : texte brut, accessible aux robots de Google). Version complète dans `docs/site/app-ads.txt`, à publier à la racine de orpheex.fr (le site du développeur indiqué sur la fiche App Store est `https://orpheex.fr/bascule`).

Chaque nouvelle régie donne sa ligne : l'ajouter à la fin du fichier. Les adaptateurs installés dans Unity avant un build peuvent être activés plus tard depuis LevelPlay, sans nouvelle version ; un adaptateur absent du build demande une nouvelle version.

## Avant chaque publication sur l'App Store (à ne pas oublier)

Unity → **Ads Mediation → Network Manager** → **Install** sur chaque régie : Google AdMob, Liftoff, InMobi, Chartboost, Mintegral, AppLovin, Pangle, DT Exchange. Puis `Assets → External Dependency Manager → iOS Resolver` si Unity le propose. Un adaptateur absent du build ne s'active jamais depuis LevelPlay : il faudrait une nouvelle version.

## Tous les liens pour brancher les réseaux

Dans LevelPlay : **Setup → SDK Networks**, choisir le réseau, entrer ses identifiants, puis ses blocs (interstitiel et récompensé) pour iPhone et pour Android. Guide général : https://docs.unity.com/grow/levelplay/platform/get-started/networks

| Réseau | Créer le compte | Guide LevelPlay (identifiants à copier) |
|---|---|---|
| Unity Ads | https://cloud.unity.com | https://docs.unity.com/en-us/grow/levelplay/sdk/unity/networks/guides/unity-ads |
| Google AdMob | https://admob.google.com | https://docs.unity.com/en-us/grow/levelplay/sdk/unity/networks/guides/google-admob |
| Meta Audience Network | https://business.facebook.com | https://docs.unity.com/en-us/grow/levelplay/sdk/unity/networks/guides/meta-audience-network |
| AppLovin | https://dash.applovin.com | https://docs.unity.com/en-us/grow/levelplay/sdk/react/networks/guides/applovin |
| Mintegral | https://www.mintegral.com | https://docs.unity.com/en-us/grow/levelplay/sdk/android/networks/guides/mintegral |
| Liftoff Monetize (Vungle) | https://publisher.vungle.com | https://docs.unity.com/grow/levelplay/sdk/unity/networks/guides/liftoff-monetize |
| Pangle | https://www.pangleglobal.com | https://docs.unity.com/en-us/grow/levelplay/sdk/unity/networks/guides/pangle |
| Moloco | https://www.moloco.com | https://docs.unity.com/en-us/grow/levelplay/sdk/android/networks/guides/moloco |
| DT Exchange (Digital Turbine) | https://console.fyber.com | https://docs.unity.com/en-us/grow/levelplay/sdk/unity/networks/guides/dt-exchange |
| Chartboost | https://dashboard.chartboost.com | https://docs.unity.com/en-us/grow/levelplay/sdk/android/networks/guides/chartboost |
| InMobi | https://publisher.inmobi.com | dans la liste « Mediated networks » de la doc LevelPlay |

Unity Ads, Mintegral, Liftoff et l'enchère Google ont une **configuration automatique** dans LevelPlay (ils créent l'app et les blocs pour toi) : commence par eux. Chartboost et DT Exchange n'acceptent plus que des blocs en enchères (« bidding ») : ne crée pas de blocs à prix fixe. Liste complète des réseaux et des versions d'adaptateurs : https://docs.unity.com/en-us/grow/levelplay/sdk/unity/mediation-networks

## Pour gagner encore plus (tableau de bord LevelPlay)

- **Laisse tourner les enchères 1 à 2 semaines** avant de juger l'eCPM. Les réseaux apprennent ce que vaut ton public.
- **Pas de prix plancher trop haut** au début : un plancher trop haut fait baisser le taux de remplissage (des pubs non vues = 0 €).
- **Placements séparés** (optionnel) : `revive`, `chest_double`, `exclusive_head`, `video_chest`… (`Placements` dans le code). Ils permettent de voir lequel rapporte le plus dans les rapports.
- **Google et l'Europe** : sans plateforme de consentement certifiée par Google (TCF), AdMob montre des pubs « limitées » aux joueurs européens. Les autres réseaux utilisent le consentement transmis par LevelPlay. Pour aller au bout, il faudra brancher la plateforme de consentement de Google (UMP), qui remplacerait l'écran « Des pubs qui te ressemblent ? ». C'est une étape pour plus tard, une fois AdMob actif et les revenus mesurés.
