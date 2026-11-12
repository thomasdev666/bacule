# Têtes de partenaires (BDE, influenceurs…)

Une tête de partenaire = un personnage complet (tête, coiffure, accessoire, tenue) avec **le logo du partenaire cousu sur la poitrine**, offert avec un code. Purement cosmétique, comme toutes les têtes.

## Samb'bach (BDE) : en validation

- **Personnage** : une Brésilienne aux longs cheveux ondulés (`HairStyle.Waves`), coiffe du carnaval aux couleurs du Brésil (plumes vertes, jaunes et bleues, diadème doré, créoles : `Accessory.Samba`), grand sourire (`Expression.Happy`).
- **Tenue** : maillot jaune à liserés verts façon Brésil, short bleu, chaussettes blanches, baskets vertes ; le logo du BDE en écusson brodé (bord orange, points de couture) au milieu de la poitrine (`Emblem.Patch`).
- **Logo** : `Tools/Sponsors/sambbach_logo.png` (le disque intérieur du logo fourni, détouré en rond, 512 px) ; l'original est à côté.
- **Aperçus** : `docs/apercu/sponsor_sambbach*.png`.

```
dotnet run --project Tools/ArtPreview -c Release -- Exports/art
python3 Tools/sponsor_preview.py Exports/art docs/apercu sambbach
```

La tête n'est **pas encore dans le jeu** : elle est décrite dans `Tools/ArtPreview/Program.cs` (`SponsorPreviews`). Le pack d'images du jeu n'a pas changé.

## Une fois le visuel validé

1. Ajouter la tête à la fin de `HeadCatalog` (identifiant 66, jamais renuméroté) avec un nouveau mode de déblocage « code », et sa tenue à `OutfitCatalog`.
2. Donner le logo au dessin : le copier dans `Assets/Bascule/Resources/Sponsors/`, le lire au lancement et l'enregistrer avec `MascotArtist.RegisterPatch` (le jeu), et faire de même dans `Tools/ArtPack` ; ajouter les images à `Core/ArtCatalog.cs`, relancer ArtPack, augmenter `ArtCache.ArtVersion`.
3. **Réglages → Code promo** : un champ de saisie ; un code connu (insensible aux majuscules, aux espaces et aux apostrophes : « Samb'bach », « SAMBBACH »…) débloque sa tête une seule fois par joueur. Les codes sont écrits dans le jeu (liste dans Core), ce qui permet d'en ajouter pour d'autres partenaires à chaque version.
