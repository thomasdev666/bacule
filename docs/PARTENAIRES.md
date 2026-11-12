# Têtes de partenaires (BDE, influenceurs…)

Une tête de partenaire = un personnage complet (tête, coiffure, accessoire, tenue) avec **le logo du partenaire cousu sur la poitrine**, offert avec un code. Purement cosmétique, comme toutes les têtes.

## Samb'bach (BDE) : en validation

- **Personnage** : une Brésilienne, grand sourire, cils recourbés (`HeadDef.Lashes`).
- **Coiffure unique** (`HairStyle.Cascade`, à elle seule) : raie profonde, grande vague brillante balayée sur le front, tout ramené en une grosse tresse qui retombe sur l'épaule droite, du brun aux pointes miel (balayage brésilien), tenue par une bague dorée ; de l'autre côté, cheveux lissés derrière l'oreille et une créole dorée.
- **Coiffe du carnaval** (`Accessory.Samba`) : plumes vertes, jaunes et bleues, diadème doré avec une étoile sur une pierre verte.
- **Tenue** (`Top.Dress`) : robe de fête vert émeraude ajustée à la taille, manches ballon et col à volants jaunes, ceinture nouée d'un gros nœud, jupe à trois volants (émeraude, jaune, émeraude) bordée de bleu ; le logo du BDE en broche dorée sur le corsage (`Emblem.Patch`). Silhouette fine (bras et jambes plus minces que Basile, sans biceps), bracelets dorés, ballerines dorées (`Shoe.Flats`).
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
