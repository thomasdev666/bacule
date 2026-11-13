# Bascule : notes pour Claude Code

Jeu mobile Unity 6 (portrait, Android/iOS). Le cahier des charges et les choix faits sont dans `README.md` et `docs/DECISIONS.md`.

## Architecture
- `Assets/Bascule/Scripts/Core` : règles en **C# pur** (`noEngineReferences: true`). Pas de `UnityEngine` ici. C# 9 maximum.
- `Assets/Bascule/Scripts/Runtime` : le jeu Unity. `GameApp` se lance tout seul (`RuntimeInitializeOnLoadMethod`) et construit tout par le code : pas de prefab, pas de scène à câbler.
- Services optionnels activés par des defines d'asmdef (`versionDefines`) : `BASCULE_UGS`, `BASCULE_NATIVESHARE`, `BASCULE_INPUTSYSTEM`, `BASCULE_UNITYADS`, `BASCULE_IAP` (IAP 4.x), `BASCULE_NOTIFICATIONS`.
- Réglages de gameplay : `Assets/Bascule/Resources/bascule_tuning.json`. Les noms de champs doivent correspondre à `Core/Tuning.cs` (un test le vérifie).
- API Unity qui diffèrent selon la version : passer par `Rigidbody2DCompat` (`velocity` / `linearVelocity`).

## Vérifier sans Unity (SDK .NET 8)
- `dotnet test Tests/Bascule.Core.Tests` : tests des règles.
- `dotnet test Tests/Bascule.Duel.Tests` : 179 duels simulés (vrai `DuelController`, faux réseau : 4G, Snap, pauses, abandons…). Toute modification du duel doit y passer ; un nouveau bug de duel = un nouveau scénario.
- `dotnet build Tools/CompileCheck` : compile Core et Runtime contre les DLL de référence Unity (NuGet). Ajouter `-p:ExtraDefines=UNITY_ANDROID` pour vérifier le code Android.
- `python3 Tools/balance_sim.py 20` : simulation d'équilibrage (pymunk).
- `python3 Tools/game_library.py 80` puis `python3 Tools/player_sim.py 1000 60` : modèle de 1000 joueurs (rétention, pubs, achats). Garder `player_sim.py` (DESIGNS["apres"], paliers, prix) synchronisé avec `Core/Economy.cs` et `Core/HeadCatalog.cs`.
- Paquets optionnels (pub, achats, rappels) : `dotnet build Tools/CompileCheck -p:ExtraDefines="BASCULE_STUBS;BASCULE_UNITYADS;BASCULE_IAP;BASCULE_NOTIFICATIONS;UNITY_ANDROID"` (et `UNITY_IOS`) compile ce code contre `Tools/CompileCheck/PackageStubs.cs`.
- Musique et bruitages : `dotnet run --project Tools/AudioPreview -- Exports/audio` exporte des WAV (dont `demo_partie.wav`, la montée de tension) pour écouter sans Unity.
- Icône : `IconArtist.AppIcon` (Core/UiArt.cs) ; après une modification, `dotnet run --project Tools/ArtPreview -- Exports/art` puis copier `Exports/art/icone/icone_1024.png` dans `Assets/Bascule/Icone/`.
- Aperçus : `dotnet run --project Tools/ArtPreview -- Exports/art`, puis `python3 Tools/render_preview.py Exports/art docs/apercu` et `python3 Tools/scene_preview.py Exports/art docs/apercu ecrans` (écrans 1.4 : duel, classement, premier lancement, vidéos de la boutique : `python3 Tools/screens_preview_v14.py Exports/art docs/apercu`). Toujours regarder les images après une modification des dessins.
- Après toute modification d'un dessin (`Core/*Art*.cs`, `Toon.cs`) : relancer `dotnet run --project Tools/ArtPack -c Release` (images dessinées à l'avance dans `Resources/ArtPack`, chargées au lancement) et augmenter `ArtCache.ArtVersion`. Toute nouvelle image doit être ajoutée à `Core/ArtCatalog.cs` (un test vérifie que le pack est complet).
- Performances (pratiques mobiles) : aucun dessin au lancement (ArtPack), éléments d'interface animés dans leur propre sous-canvas (`Ui.Isolate`), 60 images/s par défaut, rien de transparent dessiné pour rien, écritures disque hors du fil principal.
- Les constantes du squelette (`MascotRig`) et des poids (`WeightArtist`) sont recopiées dans `Tools/scene_preview.py` : les garder synchronisées.

- Économie (docs/ECONOMIE.md) : les pièces n'achètent que du style (têtes, planches `Core/Cosmetics.cs`, machine à surprises sans doublon avec chances affichées, protège-série). Jamais rien qui change le score. Garder `PLANK_PRICES`, `SURPRISE_PRICE`, `SHIELD_PRICE` de `Tools/player_sim.py` synchronisés (`python3 Tools/player_sim.py 1000 60 economie`).
- Textes : chaque `Text` créé par `Ui.Label` reçoit `FitText`, qui réduit la taille jusqu'à ce que le texte tienne dans sa zone. Les libellés de boutons restent sur une ligne.
- Partage (docs/PARTAGE.md) : titres, frise de tension et résumé dans `Core/Brag.cs` ; carte 9:16 dans `Runtime/UI/ScoreCard.cs`, recopiée dans `Tools/scene_preview.py` (`share_card`, image `docs/apercu/carte_partage.png`) : garder les positions synchronisées.

## Conventions
- Textes visibles : toujours via `Loc.Get` (FR et EN dans `Core/Loc.cs`), jamais en dur.
- Interface : `UI/Ui.cs` (taille de texte ≥ `Ui.MinFont`, référence 1080 × 1920).
- Un bouton dont on change le texte ensuite doit être créé avec un libellé non vide (sinon `Ui.ButtonLabel` renvoie null).
- Ne jamais renuméroter les têtes (`HeadCatalog`) : leurs identifiants sont sauvegardés chez les joueurs.
- Monétisation : rien qui donne un avantage en partie contre de l'argent ; les pubs imposées passent uniquement par `AdPolicy`. Pubs personnalisées : seulement avec `PrivacyRules.Personalized` (15 ans et plus + accord) ; réseaux et identifiants : `docs/PUBS.md`, `Editor/AdNetworkSettings.cs`.
- Commentaires et documentation en français. Le jeu est publié en français uniquement (`Loc.FrenchOnly`).
- Identité : éditeur Orphex, identifiant `fr.orpheex.bascule`, pages légales dans `docs/site/bascule` (à mettre sur orpheex.fr).
- Fiche App Store (docs/STORE.md) : images des achats mis en avant, des événements et captures avec titre dans `docs/store`, refaites par `python3 Tools/store_listing_assets.py Exports/art docs/apercu docs/store`.
- **Avant chaque envoi à Apple** : lancer `python3 Tools/verif_publication.py ../bacule-ios` (tout doit être OK) et faire passer à Thomas les tests sur iPhone de `docs/AVANT_PUBLICATION.md`. Ne jamais renommer un classement ni une clé Cloud Save sans copier les données (`Tools/copy_leaderboards.py`).
