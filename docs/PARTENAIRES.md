# Têtes de partenaires (BDE, influenceurs…)

Une tête de partenaire = un personnage complet (tête, coiffure, accessoire, tenue) avec **le logo du partenaire cousu sur la poitrine**, offert avec un code. Purement cosmétique, comme toutes les têtes.

## Samb'bach (BDE) : dans le jeu

- **Tête n° 66** « Samb'bach » (`UnlockKind.Code`, rareté légendaire), tenue n° 66. Le BDE a les droits sur son logo et nous les a transmis.
- **Code** : « Samb'bach ». Majuscules, espaces, apostrophes, tirets et accents ne comptent pas (SAMB'BACH, samb bach, Sambbach…). Un code ne sert qu'une fois par joueur ; la tête reste acquise après une remise à zéro de saison.
- **Personnage** : une Brésilienne, grand sourire, cils recourbés (`HeadDef.Lashes`).
- **Coiffure unique** (`HairStyle.Cascade`, à elle seule) : raie profonde, grande vague brillante balayée sur le front, tout ramené en une grosse tresse qui retombe sur l'épaule droite, du brun aux pointes miel (balayage brésilien), tenue par une bague dorée ; de l'autre côté, cheveux lissés derrière l'oreille et une créole dorée.
- **Coiffe du carnaval** (`Accessory.Samba`) : plumes vertes, jaunes et bleues, diadème doré avec une étoile sur une pierre verte.
- **Tenue** (`Top.Dress`) : robe de fête vert émeraude ajustée à la taille, manches ballon et col à volants jaunes, ceinture nouée d'un gros nœud, jupe à trois volants (émeraude, jaune, émeraude) bordée de bleu ; le logo du BDE en broche dorée sur le corsage (`Emblem.Patch`). Silhouette fine (bras et jambes plus minces que Basile, sans biceps), bracelets dorés, ballerines dorées (`Shoe.Flats`).
- **Logo** : `Assets/Bascule/Resources/Sponsors/sambbach_logo.bytes` (un PNG de 384 px : le disque intérieur du logo, détouré en rond). L'original est dans `Tools/Sponsors/`.
- **Aperçus** : `docs/apercu/sponsor_sambbach*.png` (dont la saisie du code : `sponsor_sambbach_code.png`).

```
dotnet run --project Tools/ArtPreview -c Release -- Exports/art
python3 Tools/sponsor_preview.py Exports/art docs/apercu sambbach
```

## Ajouter un partenaire

1. **Logo** : un PNG carré, détouré en rond (fond transparent autour), 384 px, renommé en `<clé>_logo.bytes` dans `Assets/Bascule/Resources/Sponsors/`.
2. **Tête** : à la fin de `HeadCatalog` (prochain identifiant libre, jamais renuméroté), avec `UnlockKind.Code` ; sa tenue au même numéro dans `OutfitCatalog` avec `Emblem = Emblem.Patch` et `Patch = "<clé>"`.
3. **Code** : une ligne dans `PromoCodes.All` (`Core/Promo.cs`) : forme normalisée, code affiché, tête, nom du partenaire, clé du logo.
4. Relancer `Tools/ArtPack` (les outils lisent le logo au même endroit que le jeu), augmenter `ArtCache.ArtVersion`, regarder les aperçus.
5. **Apple** : donner le code dans les notes de vérification (`docs/APP_STORE.md`) ; un code n'offre que du cosmétique gratuit, jamais un contenu payant (règle 3.1.1).

Le partenaire doit avoir les droits sur tout ce qui est dans son logo et nous les transmettre (accord écrit).
