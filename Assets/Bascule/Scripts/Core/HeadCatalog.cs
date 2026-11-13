using System;
using System.Collections.Generic;

namespace Bascule.Core
{
    public enum HairStyle { None, Short, Spiky, Curly, Bun, Mohawk, Long, Afro, Pigtails, Swoosh, Curls,
        // Raie profonde, grande vague balayée sur le front, grosse tresse sur une seule épaule tenue par une bague dorée,
        // pointes couleur miel ; l'autre côté lissé derrière l'oreille. Dessinée pour une tête de partenaire (aucune tête ne
        // la porte depuis la 1.4.1) : réutilisable pour un prochain partenaire.
        Cascade }

    /// <summary>Tenue du corps : le débardeur de Basile, ou le costume croisé (tête Challenger).</summary>
    public enum Outfit { Tank, Suit }

    public enum Accessory
    {
        None, Glasses, Sunglasses, Cap, Beanie, Crown, Headband, Mustache, Freckles, Bow, Halo, Headphones, Antennas,
        // Boutique et têtes premium
        TopHat, Pirate, Viking, Chef, CatEars, Flower, PartyHat, Wizard, Astronaut, Unicorn, DevilHorns,
        // 1.3
        ThinMustache,
        // 1.4 : lunettes en étoile de la tête exclusive (gagnée avec des vidéos)
        StarGlasses,
        // Trèfle à quatre feuilles de la tête « Chanceux » (roue de la chance)
        Clover,
    }

    public enum UnlockKind
    {
        Default,      // débloquée d'office
        Score,        // atteindre un score en une partie
        DailyStreak,  // jours consécutifs de défi du jour
        NearMiss,     // sauvetage de justesse à X % ou plus
        Balloon,      // faire s'envoler un 128
        Combo,        // chaîne de fusions de X ou plus
        WorldTop,     // classement mondial : rang ≤ X (très rare)
        Premium,      // achat in-app (cosmétique uniquement)
        Level,        // atteindre un niveau de joueur (progresse avec le temps de jeu)
        Shop,         // achetée avec des pièces (UnlockValue = prix)
        DuelWins,     // gagner X duels en temps réel
        Ads,          // regarder X vidéos dans la boutique (tête exclusive, Core/AdRewards.cs)
        Friends,      // avoir X amis réciproques (parrainage « Duo »)
        Wheel,        // gros lot de la roue de la chance (Core/LiveOps.cs)
        Code,         // code d'un partenaire (Réglages → Code promo, Core/Promo.cs)
    }

    public enum Rarity { Common, Rare, Epic, Legendary }

    public sealed class HeadDef
    {
        public int Id;
        public string NameFr;
        public string NameEn;
        public UnlockKind Unlock;
        public int UnlockValue;
        public string Skin;
        public HairStyle Hair;
        public string HairColor;
        public Accessory Accessory;
        public Expression Expression;
        /// <summary>Têtes Premium : identifiant du produit qui les donne (Shop.Legends, Shop.Starter).</summary>
        public string Product;
        /// <summary>Tenue du corps (débardeur par défaut).</summary>
        public Outfit Outfit;
        /// <summary>Cils recourbés au coin des yeux (calque avant).</summary>
        public bool Lashes;
        /// <summary>Clé des images du corps : la peau et la tenue de cette tête (« #E8B48C_h12 », Core/Outfits.cs).</summary>
        public string BodyKey => Skin + "_h" + Id;

        public bool IsSpecial => Unlock != UnlockKind.Score && Unlock != UnlockKind.Default && Unlock != UnlockKind.Level && Unlock != UnlockKind.Shop && Unlock != UnlockKind.Premium;
        public string Name(string lang) => lang == "en" ? NameEn : NameFr;
    }

    /// <summary>Résultat d'une partie, utilisé pour débloquer les têtes.</summary>
    public struct RunAchievements
    {
        public int Score;
        public float BestSave;     // danger max dont on s'est sorti (NearMissTracker.BestSave)
        public int Balloons;
        public int BestChain;
        public int DailyStreak;
        public int WorldRank;      // 0 = inconnu
        public int DuelWins;       // victoires en duel (total)
    }

    /// <summary>
    /// 66 têtes : 1 de départ, 20 par palier de score, 13 par niveau de joueur, 6 spéciales (exploits),
    /// 16 à acheter avec des pièces, 6 premium (achats), la Challenger (duels), la Superstar (vidéos), Duo (amis), Chanceux (roue).
    /// Plus, plus tard, les têtes de partenaires (codes promo) : invisibles tant que le joueur n'a pas entré leur code
    /// (IsVisible). Les visuels sont dessinés par le code (HeadArtist).
    /// Simulation (docs/RAPPORT.md) : avec les anciens paliers (jusqu'à 60 000 points), 25 têtes sur 33 étaient
    /// hors de portée de presque tous les joueurs ; les niveaux et la boutique donnent un objectif à chaque partie.
    /// </summary>
    public static class HeadCatalog
    {
        const string Ink = "#2B2D42", Brown = "#6B3E26", Blond = "#F4C430", Red = "#E4572E",
            Purple = "#9B5DE5", Grey = "#D9D9D9", Blue = "#3A86FF", Teal = "#12B5A6", Pink = "#F25CA2", Black = "#1B1B24";
        const string S1 = "#FFD9B3", S2 = "#F1C27D", S3 = "#E0AC69", S4 = "#C68642", S5 = "#8D5524", S6 = "#FFE7D1",
            Alien = "#A8E6CF", Sky = "#B5C7FF", Rose = "#FFB8CF";

        /// <summary>
        /// Identifiants retirés, à ne JAMAIS réutiliser : des joueurs les ont dans leur sauvegarde (une nouvelle tête à ce
        /// numéro leur serait donnée sans rien faire). 66 : tête « Samb'bach » (partenariat annulé, retirée en 1.4.1).
        /// </summary>
        public static readonly int[] RetiredIds = { 66 };

        /// <summary>
        /// Tête affichée dans la collection et comptée dans « x / y » : toutes, sauf les têtes de partenaires (code promo)
        /// pas encore débloquées. Personne ne doit savoir qu'elles existent avant d'avoir entré le code.
        /// </summary>
        public static bool IsVisible(HeadDef h, ICollection<int> unlocked) =>
            h != null && (h.Unlock != UnlockKind.Code || (unlocked != null && unlocked.Contains(h.Id)));

        /// <summary>Nombre de têtes visibles pour ce joueur (le total affiché dans la collection).</summary>
        public static int VisibleCount(ICollection<int> unlocked)
        {
            int n = 0;
            foreach (var h in All) if (IsVisible(h, unlocked)) n++;
            return n;
        }

        public static readonly IReadOnlyList<HeadDef> All = Build();

        public static bool Exists(int id)
        {
            foreach (var h in All) if (h.Id == id) return true;
            return false;
        }

        public static HeadDef Get(int id)
        {
            foreach (var h in All) if (h.Id == id) return h;
            return All[0];
        }

        /// <summary>
        /// Paliers de score (20 têtes). V18 : plus rares (docs/ECONOMIE.md). Quelques têtes restent proches pour les
        /// débutants, mais la moitié demande un vrai niveau de jeu, et les dernières sont réservées aux meilleurs.
        /// </summary>
        public static List<int> ScoreThresholds() => new List<int>
        {
            300, 700, 1200, 1800, 2500, 3200, 4000, 5000, 6500, 8000, 10000, 12500, 15000, 17500, 20000, 25000, 30000, 40000, 50000, 75000,
        };

        /// <summary>Niveaux de joueur (13 têtes), espacés : une nouvelle tête de niveau toutes les quelques semaines de jeu.</summary>
        public static int[] LevelThresholds => new[] { 3, 6, 9, 12, 16, 20, 25, 30, 35, 40, 45, 50, 60 };

        /// <summary>Prix en pièces des 16 têtes de la boutique (environ 60 000 pièces pour toutes).</summary>
        public static int[] ShopPrices => new[] { 600, 800, 1000, 1300, 1600, 2000, 2400, 2900, 3400, 4000, 4600, 5200, 5800, 6500, 7200, 8000 };

        /// <summary>
        /// Rareté affichée dans la collection (couleur du halo et de l'étiquette). Calculée d'après la difficulté
        /// d'obtention : plus c'est long ou difficile à gagner, plus c'est rare.
        /// </summary>
        public static Rarity RarityOf(HeadDef h)
        {
            switch (h.Unlock)
            {
                case UnlockKind.Default: return Rarity.Common;
                case UnlockKind.Score: return h.UnlockValue < 3000 ? Rarity.Common : h.UnlockValue < 10000 ? Rarity.Rare : h.UnlockValue < 25000 ? Rarity.Epic : Rarity.Legendary;
                case UnlockKind.Level: return h.UnlockValue < 15 ? Rarity.Common : h.UnlockValue < 30 ? Rarity.Rare : h.UnlockValue < 45 ? Rarity.Epic : Rarity.Legendary;
                case UnlockKind.Shop: return h.UnlockValue < 2000 ? Rarity.Common : h.UnlockValue < 3500 ? Rarity.Rare : h.UnlockValue < 6000 ? Rarity.Epic : Rarity.Legendary;
                case UnlockKind.DailyStreak: return h.UnlockValue >= 14 ? Rarity.Epic : Rarity.Rare;
                case UnlockKind.Balloon: case UnlockKind.Combo: return Rarity.Rare;
                case UnlockKind.NearMiss: return Rarity.Epic;
                case UnlockKind.DuelWins: return Rarity.Epic;
                case UnlockKind.Ads: return Rarity.Legendary; // exclusive : on ne l'obtient que comme ça
                case UnlockKind.Friends: return Rarity.Rare;
                case UnlockKind.Wheel: return Rarity.Epic;
                case UnlockKind.Code: return Rarity.Legendary; // tête de partenaire : seulement avec son code
                default: return Rarity.Legendary; // classement mondial, têtes premium
            }
        }

        /// <summary>Tête de la boutique la moins chère pas encore achetée (objectif affiché en fin de partie).</summary>
        /// <summary>Têtes de la boutique pas encore à soi et déjà payables avec <paramref name="coins"/>, de la moins chère à la plus chère.</summary>
        public static List<HeadDef> AffordableShopHeads(ICollection<int> unlocked, int coins)
        {
            var list = new List<HeadDef>();
            foreach (var h in All)
                if (h.Unlock == UnlockKind.Shop && !unlocked.Contains(h.Id) && h.UnlockValue <= coins) list.Add(h);
            list.Sort((a, b) => a.UnlockValue != b.UnlockValue ? a.UnlockValue.CompareTo(b.UnlockValue) : a.Id.CompareTo(b.Id));
            return list;
        }

        /// <summary>
        /// Tête de la boutique à proposer en fin de partie : si plusieurs sont payables, on en change à chaque partie
        /// (le joueur n'aime peut-être pas la moins chère). Sinon, la prochaine à portée.
        /// </summary>
        public static HeadDef SuggestedShopHead(ICollection<int> unlocked, int coins, int rotation)
        {
            var affordable = AffordableShopHeads(unlocked, coins);
            if (affordable.Count > 0) return affordable[((rotation % affordable.Count) + affordable.Count) % affordable.Count];
            return NextShopHead(unlocked);
        }

        public static HeadDef NextShopHead(ICollection<int> unlocked)
        {
            HeadDef best = null;
            foreach (var h in All)
                if (h.Unlock == UnlockKind.Shop && !unlocked.Contains(h.Id) && (best == null || h.UnlockValue < best.UnlockValue))
                    best = h;
            return best;
        }

        public static HeadDef NextLevelHead(ICollection<int> unlocked)
        {
            HeadDef best = null;
            foreach (var h in All)
                if (h.Unlock == UnlockKind.Level && !unlocked.Contains(h.Id) && (best == null || h.UnlockValue < best.UnlockValue))
                    best = h;
            return best;
        }

        public static bool IsEarned(HeadDef h, RunAchievements a)
        {
            switch (h.Unlock)
            {
                case UnlockKind.Default: return true;
                case UnlockKind.Score: return a.Score >= h.UnlockValue;
                case UnlockKind.DailyStreak: return a.DailyStreak >= h.UnlockValue;
                case UnlockKind.NearMiss: return a.BestSave * 100f >= h.UnlockValue;
                case UnlockKind.Balloon: return a.Balloons >= h.UnlockValue;
                case UnlockKind.Combo: return a.BestChain >= h.UnlockValue;
                case UnlockKind.WorldTop: return a.WorldRank > 0 && a.WorldRank <= h.UnlockValue;
                case UnlockKind.DuelWins: return a.DuelWins >= h.UnlockValue;
                default: return false; // Premium : seulement par achat
            }
        }

        /// <summary>Prochaine tête à gagner au score (pour l'afficher en fin de partie). Null si tout est débloqué.</summary>
        /// <summary>
        /// Tête au hasard parmi celles gagnées, jamais celle déjà portée (s'il y en a au moins deux) : le joueur
        /// redécouvre sa collection. <paramref name="roll"/> donne un nombre dans [0, 1).
        /// </summary>
        public static int PickRandom(IList<int> unlocked, int current, Func<double> roll)
        {
            if (unlocked == null || unlocked.Count == 0) return 0;
            var pool = new List<int>();
            foreach (var id in unlocked) if (id != current && Get(id) != null && !pool.Contains(id)) pool.Add(id);
            if (pool.Count == 0) return current;
            int k = (int)(roll() * pool.Count);
            return pool[Math.Max(0, Math.Min(pool.Count - 1, k))];
        }

        public static HeadDef NextScoreHead(ICollection<int> unlocked)
        {
            HeadDef best = null;
            foreach (var h in All)
                if (h.Unlock == UnlockKind.Score && !unlocked.Contains(h.Id) && (best == null || h.UnlockValue < best.UnlockValue))
                    best = h;
            return best;
        }

        static List<HeadDef> Build()
        {
            var t = ScoreThresholds();
            var list = new List<HeadDef>
            {
                H(0, "Basile", "Basile", UnlockKind.Default, 0, S2, HairStyle.Short, Brown, Accessory.None, Expression.Calm),
            };

            // 33 têtes de score, de la plus tranquille à la plus grimaçante.
            var score = new (string fr, string en, string skin, HairStyle hair, string hc, Accessory acc, Expression ex)[]
            {
                ("Tranquille", "Easygoing", S1, HairStyle.Swoosh, Blond, Accessory.None, Expression.Calm),
                ("Pétillante", "Sparkly", S3, HairStyle.Pigtails, Red, Accessory.Freckles, Expression.Grin),
                ("Le Malin", "Sly", S4, HairStyle.Short, Ink, Accessory.None, Expression.Smirk),
                ("Frisette", "Curly", S2, HairStyle.Curly, Brown, Accessory.None, Expression.Happy),
                ("Lunettes", "Specs", S6, HairStyle.Short, Grey, Accessory.Glasses, Expression.Calm),
                ("Bonnet", "Beanie", S5, HairStyle.None, Ink, Accessory.Beanie, Expression.Grin),
                ("Chignon", "Bun", S1, HairStyle.Bun, Ink, Accessory.None, Expression.Wink),
                ("Casquette", "Cap", S3, HairStyle.Short, Brown, Accessory.Cap, Expression.Smirk),
                ("Moustache", "Mustache", S2, HairStyle.Short, Ink, Accessory.Mustache, Expression.Calm),
                ("Afro", "Afro", S5, HairStyle.Afro, Black, Accessory.None, Expression.Happy),
                ("Crête", "Mohawk", S1, HairStyle.Mohawk, Pink, Accessory.None, Expression.Tongue),
                ("Bandeau", "Headband", S4, HairStyle.Long, Ink, Accessory.Headband, Expression.Grin),
                ("Le Cool", "Cool", S3, HairStyle.Swoosh, Ink, Accessory.Sunglasses, Expression.Smirk),
                ("Nœud", "Bow", S6, HairStyle.Long, Blond, Accessory.Bow, Expression.Wink),
                ("Hérisson", "Hedgehog", S2, HairStyle.Spiky, Brown, Accessory.None, Expression.Surprised),
                ("Casque", "Headphones", S5, HairStyle.Short, Ink, Accessory.Headphones, Expression.Sleepy),
                ("Rouquine", "Ginger", S1, HairStyle.Curly, Red, Accessory.Freckles, Expression.Tongue),
                ("Endormi", "Sleepy", S3, HairStyle.Swoosh, Grey, Accessory.None, Expression.Sleepy),
                ("Prof", "Teacher", S6, HairStyle.None, Grey, Accessory.Glasses, Expression.Tense),
                ("Mamie", "Granny", S2, HairStyle.Bun, Grey, Accessory.Glasses, Expression.Happy),
                ("Papi", "Grandpa", S4, HairStyle.None, Grey, Accessory.Mustache, Expression.Grin),
                ("Violette", "Violet", S1, HairStyle.Pigtails, Purple, Accessory.Bow, Expression.Surprised),
                ("Bleuet", "Cornflower", S5, HairStyle.Spiky, Blue, Accessory.None, Expression.Tense),
                ("Martien", "Martian", Alien, HairStyle.None, Teal, Accessory.Antennas, Expression.Surprised),
                ("Rêveuse", "Dreamer", Sky, HairStyle.Long, Purple, Accessory.None, Expression.Sleepy),
                ("Bonbon", "Candy", Rose, HairStyle.Curly, Pink, Accessory.Bow, Expression.Tongue),
                ("Costaud", "Strongman", S3, HairStyle.Mohawk, Ink, Accessory.Headband, Expression.Grimace),
                ("Inquiet", "Worried", S1, HairStyle.Spiky, Blond, Accessory.None, Expression.Tense),
                ("Grimace", "Grimace", S4, HairStyle.Afro, Red, Accessory.None, Expression.Grimace),
                ("Effort", "Effort", S2, HairStyle.Short, Ink, Accessory.Headband, Expression.Effort),
                ("Teigne", "Tough", Alien, HairStyle.Spiky, Purple, Accessory.Sunglasses, Expression.Grimace),
                ("Champion", "Champion", S5, HairStyle.Swoosh, Blond, Accessory.Headphones, Expression.Grin),
                ("Le Roi", "The King", S1, HairStyle.Curly, Brown, Accessory.Crown, Expression.Smirk),
            };
            // Les 20 premières se gagnent au score, les 13 suivantes avec les niveaux de joueur.
            for (int i = 0; i < score.Length; i++)
            {
                var s = score[i];
                if (i < t.Count) list.Add(H(1 + i, s.fr, s.en, UnlockKind.Score, t[i], s.skin, s.hair, s.hc, s.acc, s.ex));
                else list.Add(H(1 + i, s.fr, s.en, UnlockKind.Level, LevelThresholds[Math.Min(LevelThresholds.Length - 1, i - t.Count)], s.skin, s.hair, s.hc, s.acc, s.ex));
            }

            int id = 1 + score.Length; // 34
            list.Add(H(id++, "Assidu", "Regular", UnlockKind.DailyStreak, 5, S3, HairStyle.Bun, Teal, Accessory.Headband, Expression.Grin));
            list.Add(H(id++, "Fidèle", "Faithful", UnlockKind.DailyStreak, 14, Sky, HairStyle.Swoosh, Blue, Accessory.Cap, Expression.Happy));
            list.Add(H(id++, "Miraculé", "Close Call", UnlockKind.NearMiss, 97, S6, HairStyle.Spiky, Ink, Accessory.None, Expression.Surprised));
            list.Add(H(id++, "Aéronaute", "Balloonist", UnlockKind.Balloon, 1, S2, HairStyle.Short, Red, Accessory.Glasses, Expression.Grin));
            list.Add(H(id++, "Combo", "Combo", UnlockKind.Combo, 5, Rose, HairStyle.Mohawk, Purple, Accessory.Sunglasses, Expression.Tongue));
            list.Add(H(id++, "Légende", "Legend", UnlockKind.WorldTop, 100, S4, HairStyle.Long, Blond, Accessory.Halo, Expression.Smirk));

            // 40 à 55 : boutique (pièces). Accessoires qu'on ne trouve nulle part ailleurs.
            var shop = new (string fr, string en, string skin, HairStyle hair, string hc, Accessory acc, Expression ex)[]
            {
                ("Chef", "Chef", S2, HairStyle.Short, Brown, Accessory.Chef, Expression.Happy),
                ("Fleurette", "Daisy", S1, HairStyle.Long, Blond, Accessory.Flower, Expression.Grin),
                ("Minou", "Kitty", S6, HairStyle.Short, Ink, Accessory.CatEars, Expression.Smirk),
                ("Fêtard", "Party", S3, HairStyle.Curly, Brown, Accessory.PartyHat, Expression.Tongue),
                ("Gentleman", "Gentleman", S2, HairStyle.Short, Ink, Accessory.TopHat, Expression.Smirk),
                ("Pirate", "Pirate", S4, HairStyle.Long, Black, Accessory.Pirate, Expression.Grin),
                ("Viking", "Viking", S1, HairStyle.Long, Red, Accessory.Viking, Expression.Grimace),
                ("Magicien", "Wizard", S6, HairStyle.Swoosh, Grey, Accessory.Wizard, Expression.Wink),
                ("Chaton rose", "Pink Kitty", Rose, HairStyle.Pigtails, Pink, Accessory.CatEars, Expression.Happy),
                ("Corsaire", "Corsair", S5, HairStyle.Short, Black, Accessory.Pirate, Expression.Smirk),
                ("Cheffe", "Head Chef", S3, HairStyle.Bun, Red, Accessory.Chef, Expression.Wink),
                ("Fée", "Fairy", Sky, HairStyle.Long, Purple, Accessory.Flower, Expression.Sleepy),
                ("Mage noir", "Dark Mage", Alien, HairStyle.Spiky, Black, Accessory.Wizard, Expression.Tense),
                ("Dandy", "Dandy", S5, HairStyle.Swoosh, Brown, Accessory.TopHat, Expression.Calm),
                ("Jarl", "Jarl", S2, HairStyle.Afro, Blond, Accessory.Viking, Expression.Effort),
                ("Anniversaire", "Birthday", S1, HairStyle.Pigtails, Teal, Accessory.PartyHat, Expression.Surprised),
            };
            id = 40;
            for (int i = 0; i < shop.Length; i++)
            {
                var s = shop[i];
                list.Add(H(id++, s.fr, s.en, UnlockKind.Shop, ShopPrices[i], s.skin, s.hair, s.hc, s.acc, s.ex));
            }

            // 56 à 61 : premium (achats). Les « Légendes » et la tête de l'offre de départ.
            list.Add(P(id++, "Astronaute", "Astronaut", Shop.Legends, S2, HairStyle.Short, Brown, Accessory.Astronaut, Expression.Surprised));
            list.Add(P(id++, "Robot", "Robot", Shop.Legends, "#C9D3E0", HairStyle.None, Ink, Accessory.Antennas, Expression.Calm));
            list.Add(P(id++, "Licorne", "Unicorn", Shop.Legends, "#FFF0F7", HairStyle.Long, Pink, Accessory.Unicorn, Expression.Happy));
            list.Add(P(id++, "Diablotin", "Imp", Shop.Legends, "#FF8A80", HairStyle.Spiky, Black, Accessory.DevilHorns, Expression.Smirk));
            list.Add(P(id++, "Ange", "Angel", Shop.Legends, S6, HairStyle.Swoosh, "#FFF1B8", Accessory.Halo, Expression.Sleepy));
            list.Add(P(id++, "Basile doré", "Golden Basile", Shop.Starter, "#FFD45A", HairStyle.Short, "#C8901E", Accessory.Sunglasses, Expression.Grin));
            // 1.3 : tête des duels (toujours ajouter à la fin : les identifiants sont sauvegardés).
            // Challenger : à l'effigie du créateur du jeu (boucles brunes, fine moustache, sourire en coin)
            // et le seul personnage en costume croisé bleu marine à fines rayures.
            var challenger = H(id++, "Challenger", "Challenger", UnlockKind.DuelWins, 15, "#D9A071", HairStyle.Curls, "#24170F", Accessory.ThinMustache, Expression.Grin);
            challenger.Outfit = Outfit.Suit;
            list.Add(challenger);
            // 1.4 : tête exclusive, gagnée en regardant des vidéos dans la boutique (AdRewards). Rien d'autre ne la donne.
            list.Add(H(id++, "Superstar", "Superstar", UnlockKind.Ads, 8, S3, HairStyle.Swoosh, "#FF4FA3", Accessory.StarGlasses, Expression.Grin));
            // Parrainage : dès qu'un ami et toi êtes amis tous les deux, vous gagnez chacun la tête « Duo ».
            list.Add(H(id++, "Duo", "Duo", UnlockKind.Friends, 1, S4, HairStyle.Pigtails, "#3A86FF", Accessory.Headband, Expression.Wink));
            // Gros lot de la roue de la chance (rien d'autre ne la donne).
            list.Add(H(id++, "Chanceux", "Lucky", UnlockKind.Wheel, 0, S1, HairStyle.Swoosh, "#2D6A4F", Accessory.Clover, Expression.Grin));
            // 66 : ancienne tête « Samb'bach » (partenariat annulé), retirée en 1.4.1. Numéro réservé (RetiredIds).
            id++;
            // Têtes de partenaires, offertes avec un code (Réglages → Code promo, Core/Promo.cs) : à ajouter ICI, à la suite
            // (67, 68…), avec UnlockKind.Code. Elles restent cachées tant que le joueur n'a pas entré le code (IsVisible).
            return list;
        }

        static HeadDef P(int id, string fr, string en, string product, string skin, HairStyle hair, string hairColor, Accessory acc, Expression ex)
        {
            var h = H(id, fr, en, UnlockKind.Premium, 0, skin, hair, hairColor, acc, ex);
            h.Product = product;
            return h;
        }

        static HeadDef H(int id, string fr, string en, UnlockKind kind, int value, string skin, HairStyle hair, string hairColor,
            Accessory acc, Expression ex) => new HeadDef
        {
            Id = id, NameFr = fr, NameEn = en, Unlock = kind, UnlockValue = value, Skin = skin, Hair = hair,
            HairColor = hairColor, Accessory = acc, Expression = ex,
        };
    }
}
