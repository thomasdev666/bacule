# Têtes de partenaires (BDE, influenceurs…)

Une tête de partenaire = un personnage complet (tête, coiffure, accessoire, tenue) avec **le logo du partenaire cousu sur la poitrine**, offert avec un code. Purement cosmétique, comme toutes les têtes.

## Règle : invisibles tant que le code n'est pas entré

Une tête de partenaire n'apparaît **nulle part** (collection, onglet Légendes, compteur « x / y ») tant que le joueur n'a pas entré son code (`HeadCatalog.IsVisible`). Personne ne sait qu'elle existe avant. Une fois le code accepté, elle arrive en grand (`UI/HeadRevealScreen.cs`) et rejoint la collection, rangée avec les Légendes.

## Samb'bach (BDE) : retiré en 1.4.1

Le partenariat n'a pas eu lieu. Le code « Samb'bach » ne marche plus, la tête, sa coiffe du carnaval, sa tenue et le logo du BDE sont supprimés du jeu et du dépôt. Les joueurs de la 1.4 qui l'avaient la perdent (ils reprennent Basile s'ils la portaient). **Le numéro 66 reste réservé** (`HeadCatalog.RetiredIds`) : ne jamais le réutiliser, sinon ces joueurs recevraient la nouvelle tête sans rien faire. Restent disponibles pour un prochain partenaire : la coiffure à tresse (`HairStyle.Cascade`), les cils (`HeadDef.Lashes`), la robe de fête (`Top.Dress`), les ballerines (`Shoe.Flats`) et le logo en broche (`Emblem.Patch`).

## Ajouter un partenaire

1. **Logo** : un PNG carré, détouré en rond (fond transparent autour), 384 px, renommé en `<clé>_logo.bytes` dans `Assets/Bascule/Resources/Sponsors/`.
2. **Tête** : à la fin de `HeadCatalog` (prochain identifiant libre à partir de 67, jamais un numéro de `RetiredIds`, jamais renuméroté), avec `UnlockKind.Code` ; sa tenue au même numéro dans `OutfitCatalog` avec `Emblem = Emblem.Patch` et `Patch = "<clé>"`.
3. **Code** : une ligne dans `PromoCodes.All` (`Core/Promo.cs`) : forme normalisée, code affiché, tête, nom du partenaire, clé du logo.
4. Relancer `Tools/ArtPack` (les outils lisent le logo au même endroit que le jeu), augmenter `ArtCache.ArtVersion`, regarder les aperçus : `python3 Tools/sponsor_preview.py Exports/art docs/apercu <clé> "<Nom>" <numéro>` (après `dotnet run --project Tools/ArtPreview -c Release -- Exports/art`).
5. **Apple** : donner le code dans les notes de vérification (`docs/APP_STORE.md`) ; un code n'offre que du cosmétique gratuit, jamais un contenu payant (règle 3.1.1).

Le partenaire doit avoir les droits sur tout ce qui est dans son logo et nous les transmettre (accord écrit).
