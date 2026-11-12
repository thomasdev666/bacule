# Pubs : mieux payées, sans en montrer plus

Objectif : chaque pub rapporte plus (eCPM), sans ajouter une seule pub imposée. Trois leviers, du plus fort au plus faible :

1. **Plus de réseaux en enchères dans LevelPlay.** Chaque pub est mise aux enchères entre tous les réseaux branchés. Plus il y a d'acheteurs, plus le prix monte.
2. **Pubs personnalisées** (1.4) : avec l'accord du joueur (15 ans ou plus), puis la demande de suivi d'iPhone. Elles se paient souvent 2 à 3 fois plus cher.
3. **Plus de pubs choisies**, toujours au choix du joueur (1.4) : coffre gratuit, doubler le coffre, tête et planche exclusives. Les vidéos récompensées sont les mieux payées.

Les règles des pubs imposées ne changent pas (`AdPolicy`) : pas avant la 6e partie, au plus une toutes les 3 parties et 3 minutes, jamais après un record.

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

## Pour gagner encore plus (tableau de bord LevelPlay)

- **Laisse tourner les enchères 1 à 2 semaines** avant de juger l'eCPM. Les réseaux apprennent ce que vaut ton public.
- **Pas de prix plancher trop haut** au début : un plancher trop haut fait baisser le taux de remplissage (des pubs non vues = 0 €).
- **Placements séparés** (optionnel) : `revive`, `chest_double`, `exclusive_head`, `video_chest`… (`Placements` dans le code). Ils permettent de voir lequel rapporte le plus dans les rapports.
- **Google et l'Europe** : sans plateforme de consentement certifiée par Google (TCF), AdMob montre des pubs « limitées » aux joueurs européens. Les autres réseaux utilisent le consentement transmis par LevelPlay. Pour aller au bout, il faudra brancher la plateforme de consentement de Google (UMP), qui remplacerait l'écran « Des pubs qui te ressemblent ? ». C'est une étape pour plus tard, une fois AdMob actif et les revenus mesurés.
