using System;
using System.Collections.Generic;

namespace Bascule.Core
{
    /// <summary>Haut de la tenue.</summary>
    public enum Top { Tank, TShirt, Sweater, Hoodie, Jersey, Shirt, Jacket, Suit, Robe, Space }
    /// <summary>Bas de la tenue.</summary>
    public enum Bottom { Shorts, Trousers, Jeans, Joggers }
    public enum Shoe { Sneakers, HighTops, Boots, Dress }
    /// <summary>Motif du tissu du haut.</summary>
    public enum Motif { None, Stripes, Pinstripe, Dots, Stars, Plaid }
    /// <summary>Écusson sur la poitrine.</summary>
    public enum Emblem { None, B, Star, Heart, Bolt, Moon, Crown, Ring }

    /// <summary>Tenue complète d'un personnage (une par tête, fixe).</summary>
    public sealed class OutfitDef
    {
        public Top Top;
        public string Main, Accent;
        public Motif Motif;
        public Emblem Emblem;
        public Bottom Bottom;
        public string BottomColor;
        public Shoe Shoe;
        public string ShoeColor;
        public string Sock = "#FFFFFF";
        /// <summary>Ceinture d'haltérophile (le look sportif de Basile).</summary>
        public bool Belt;

        /// <summary>Signature (pour vérifier que deux têtes n'ont pas la même tenue).</summary>
        public string Signature => $"{Top}|{Main}|{Accent}|{Motif}|{Emblem}|{Bottom}|{BottomColor}|{Shoe}|{ShoeColor}";
    }

    /// <summary>
    /// Une tenue différente pour chaque tête, assortie au personnage (le Roi en robe royale, le Magicien en robe
    /// étoilée, le Pirate en marinière…). La tenue suit la tête choisie : elle ne change jamais en pleine partie.
    /// Toujours ajouter à la fin (identifiants de têtes sauvegardés). Une tête sans entrée reçoit une variante de
    /// couleurs calculée à partir de son identifiant.
    /// </summary>
    public static class OutfitCatalog
    {
        static OutfitDef O(Top top, string main, string accent, Motif motif, Emblem emblem, Bottom bottom, string bottomColor,
            Shoe shoe, string shoeColor, string sock = "#FFFFFF", bool belt = false) => new OutfitDef
        {
            Top = top, Main = main, Accent = accent, Motif = motif, Emblem = emblem, Bottom = bottom, BottomColor = bottomColor,
            Shoe = shoe, ShoeColor = shoeColor, Sock = sock, Belt = belt,
        };

        static readonly Dictionary<int, OutfitDef> Table = new Dictionary<int, OutfitDef>
        {
            [0] = O(Top.Tank, "#E63946", "#FFFFFF", Motif.None, Emblem.B, Bottom.Shorts, "#2B3A67", Shoe.Sneakers, "#3A86FF", "#FFFFFF", true), // Basile
            [1] = O(Top.TShirt, "#7FC8A9", "#FFFFFF", Motif.None, Emblem.Ring, Bottom.Shorts, "#E9D8A6", Shoe.Sneakers, "#F1FAEE"),       // Tranquille
            [2] = O(Top.TShirt, "#FFB703", "#FB8500", Motif.Dots, Emblem.None, Bottom.Shorts, "#219EBC", Shoe.Sneakers, "#FB8500"),       // Pétillante
            [3] = O(Top.Jacket, "#222831", "#EEEEEE", Motif.None, Emblem.None, Bottom.Jeans, "#3D5A80", Shoe.HighTops, "#E63946"),        // Le Malin
            [4] = O(Top.Sweater, "#F4A261", "#E76F51", Motif.Stripes, Emblem.None, Bottom.Trousers, "#6D597A", Shoe.Sneakers, "#F1FAEE"), // Frisette
            [5] = O(Top.Shirt, "#A8DADC", "#FFFFFF", Motif.None, Emblem.None, Bottom.Trousers, "#6C757D", Shoe.Dress, "#3A2A20"),         // Lunettes
            [6] = O(Top.Hoodie, "#3A86FF", "#1D3557", Motif.None, Emblem.None, Bottom.Joggers, "#2B2D42", Shoe.HighTops, "#F1FAEE"),      // Bonnet
            [7] = O(Top.TShirt, "#CDB4DB", "#FFC8DD", Motif.None, Emblem.Heart, Bottom.Jeans, "#4A6FA5", Shoe.Sneakers, "#FFC8DD"),       // Chignon
            [8] = O(Top.Jersey, "#2A9D8F", "#FFFFFF", Motif.None, Emblem.Star, Bottom.Shorts, "#264653", Shoe.Sneakers, "#E9C46A", "#2A9D8F"), // Casquette
            [9] = O(Top.Shirt, "#9C6644", "#6F4518", Motif.Plaid, Emblem.None, Bottom.Jeans, "#344E6B", Shoe.Boots, "#5C3A21"),           // Moustache
            [10] = O(Top.TShirt, "#FFD166", "#EF476F", Motif.None, Emblem.Bolt, Bottom.Shorts, "#118AB2", Shoe.HighTops, "#EF476F"),      // Afro
            [11] = O(Top.Jacket, "#16161D", "#E5383B", Motif.None, Emblem.None, Bottom.Jeans, "#22223B", Shoe.Boots, "#16161D"),          // Crête
            [12] = O(Top.Tank, "#06D6A0", "#FFFFFF", Motif.None, Emblem.Star, Bottom.Shorts, "#073B4C", Shoe.Sneakers, "#FFD166"),        // Bandeau
            [13] = O(Top.Shirt, "#FF7F51", "#FFE066", Motif.Dots, Emblem.None, Bottom.Shorts, "#3D5A80", Shoe.Sneakers, "#F1FAEE"),       // Le Cool
            [14] = O(Top.Sweater, "#FFAFCC", "#FFFFFF", Motif.None, Emblem.Heart, Bottom.Trousers, "#A2D2FF", Shoe.Sneakers, "#FFFFFF"),  // Nœud
            [15] = O(Top.Hoodie, "#8338EC", "#FF006E", Motif.None, Emblem.Bolt, Bottom.Joggers, "#3A0CA3", Shoe.Sneakers, "#FF006E"),     // Hérisson
            [16] = O(Top.Hoodie, "#6C757D", "#ADB5BD", Motif.None, Emblem.Ring, Bottom.Joggers, "#343A40", Shoe.HighTops, "#F8F9FA"),     // Casque
            [17] = O(Top.TShirt, "#90BE6D", "#F9C74F", Motif.Stripes, Emblem.None, Bottom.Shorts, "#577590", Shoe.Sneakers, "#F94144"),   // Rouquine
            [18] = O(Top.Sweater, "#B8C0FF", "#FFFFFF", Motif.Stars, Emblem.None, Bottom.Trousers, "#B8C0FF", Shoe.Sneakers, "#FFFFFF"),  // Endormi (pyjama)
            [19] = O(Top.Shirt, "#F1FAEE", "#1D3557", Motif.None, Emblem.None, Bottom.Trousers, "#4A4E69", Shoe.Dress, "#2B2D42"),        // Prof
            [20] = O(Top.Sweater, "#C9ADA7", "#9A8C98", Motif.Dots, Emblem.None, Bottom.Trousers, "#4A4E69", Shoe.Dress, "#5C4033"),      // Mamie
            [21] = O(Top.Jacket, "#7F5539", "#EDE0D4", Motif.None, Emblem.None, Bottom.Trousers, "#6B705C", Shoe.Dress, "#3A2A20"),       // Papi
            [22] = O(Top.TShirt, "#9B5DE5", "#F15BB5", Motif.Stars, Emblem.None, Bottom.Shorts, "#00BBF9", Shoe.Sneakers, "#F15BB5"),     // Violette
            [23] = O(Top.Jersey, "#3A86FF", "#FFBE0B", Motif.None, Emblem.Star, Bottom.Shorts, "#1D3557", Shoe.Sneakers, "#FFBE0B", "#3A86FF"), // Bleuet
            [24] = O(Top.Space, "#7BDFF2", "#2EC4B6", Motif.None, Emblem.Ring, Bottom.Trousers, "#7BDFF2", Shoe.Boots, "#8D99AE"),        // Martien
            [25] = O(Top.Robe, "#CDB4DB", "#FFFFFF", Motif.Stars, Emblem.None, Bottom.Trousers, "#A2D2FF", Shoe.Sneakers, "#FFFFFF"),     // Rêveuse
            [26] = O(Top.TShirt, "#FF99C8", "#FCF6BD", Motif.Dots, Emblem.Heart, Bottom.Shorts, "#A9DEF9", Shoe.Sneakers, "#E4C1F9"),     // Bonbon
            [27] = O(Top.Tank, "#212529", "#FFD60A", Motif.None, Emblem.Bolt, Bottom.Shorts, "#6C757D", Shoe.Sneakers, "#FFD60A", "#FFFFFF", true), // Costaud
            [28] = O(Top.Sweater, "#E9EDC9", "#CCD5AE", Motif.Stripes, Emblem.None, Bottom.Trousers, "#6B705C", Shoe.Sneakers, "#FEFAE0"),// Inquiet
            [29] = O(Top.Jersey, "#F77F00", "#003049", Motif.Stripes, Emblem.None, Bottom.Shorts, "#003049", Shoe.Sneakers, "#F77F00", "#F77F00"), // Grimace
            [30] = O(Top.Tank, "#0077B6", "#90E0EF", Motif.None, Emblem.B, Bottom.Shorts, "#03045E", Shoe.Sneakers, "#00B4D8", "#FFFFFF", true), // Effort
            [31] = O(Top.Jacket, "#6A040F", "#FFBA08", Motif.None, Emblem.None, Bottom.Jeans, "#1B263B", Shoe.Boots, "#0B0B0F"),          // Teigne
            [32] = O(Top.Jersey, "#FFD700", "#111111", Motif.None, Emblem.Star, Bottom.Shorts, "#111111", Shoe.Sneakers, "#FFD700", "#111111"), // Champion
            [33] = O(Top.Robe, "#9D0208", "#FFD60A", Motif.None, Emblem.Crown, Bottom.Trousers, "#370617", Shoe.Boots, "#3A2A20"),        // Le Roi
            [34] = O(Top.Hoodie, "#2A9D8F", "#E9C46A", Motif.None, Emblem.Star, Bottom.Joggers, "#264653", Shoe.Sneakers, "#E9C46A"),     // Assidu
            [35] = O(Top.Jersey, "#4CC9F0", "#4361EE", Motif.None, Emblem.Heart, Bottom.Shorts, "#3A0CA3", Shoe.Sneakers, "#4CC9F0", "#4361EE"), // Fidèle
            [36] = O(Top.TShirt, "#F8F9FA", "#E63946", Motif.None, Emblem.Bolt, Bottom.Jeans, "#457B9D", Shoe.Sneakers, "#E63946"),       // Miraculé
            [37] = O(Top.Jacket, "#8B5E34", "#F3E9DC", Motif.None, Emblem.None, Bottom.Trousers, "#6B4F2F", Shoe.Boots, "#3A2A20"),       // Aéronaute
            [38] = O(Top.Hoodie, "#F72585", "#4CC9F0", Motif.Stars, Emblem.Bolt, Bottom.Joggers, "#7209B7", Shoe.HighTops, "#4CC9F0"),    // Combo
            [39] = O(Top.Suit, "#F8F4E3", "#D4AF37", Motif.None, Emblem.Star, Bottom.Trousers, "#F8F4E3", Shoe.Dress, "#D4AF37"),         // Légende
            [40] = O(Top.Suit, "#FFFFFF", "#ADB5BD", Motif.None, Emblem.None, Bottom.Trousers, "#495057", Shoe.Dress, "#111111"),         // Chef
            [41] = O(Top.Sweater, "#FFE5EC", "#FB6F92", Motif.Dots, Emblem.None, Bottom.Shorts, "#90BE6D", Shoe.Sneakers, "#FFFFFF"),     // Fleurette
            [42] = O(Top.TShirt, "#2B2D42", "#FFFFFF", Motif.Stripes, Emblem.None, Bottom.Jeans, "#5C677D", Shoe.Sneakers, "#FFFFFF"),    // Minou
            [43] = O(Top.Shirt, "#FF006E", "#FFBE0B", Motif.Dots, Emblem.None, Bottom.Shorts, "#3A86FF", Shoe.Sneakers, "#FFBE0B"),       // Fêtard
            [44] = O(Top.Suit, "#2B2D42", "#FFFFFF", Motif.Pinstripe, Emblem.None, Bottom.Trousers, "#2B2D42", Shoe.Dress, "#0B0B0F"),    // Gentleman
            [45] = O(Top.TShirt, "#FFFFFF", "#C1121F", Motif.Stripes, Emblem.None, Bottom.Trousers, "#3D405B", Shoe.Boots, "#2B1D0E"),    // Pirate (marinière)
            [46] = O(Top.Robe, "#8D6E63", "#6D4C41", Motif.None, Emblem.None, Bottom.Trousers, "#5D4037", Shoe.Boots, "#3E2723"),         // Viking
            [47] = O(Top.Robe, "#4361EE", "#FFD60A", Motif.Stars, Emblem.Moon, Bottom.Trousers, "#3A0CA3", Shoe.Boots, "#3A0CA3"),        // Magicien
            [48] = O(Top.Hoodie, "#FFC8DD", "#FFAFCC", Motif.None, Emblem.Heart, Bottom.Joggers, "#BDE0FE", Shoe.Sneakers, "#FFAFCC"),    // Chaton rose
            [49] = O(Top.Jacket, "#1D3557", "#F1FAEE", Motif.None, Emblem.None, Bottom.Trousers, "#F1FAEE", Shoe.Boots, "#2B1D0E"),       // Corsaire
            [50] = O(Top.Suit, "#FFF8F0", "#E63946", Motif.None, Emblem.None, Bottom.Trousers, "#2B2D42", Shoe.Dress, "#2B2D42"),         // Cheffe
            [51] = O(Top.Robe, "#A0E7E5", "#FBE7C6", Motif.Stars, Emblem.Star, Bottom.Trousers, "#B4F8C8", Shoe.Sneakers, "#FFAEBC"),     // Fée
            [52] = O(Top.Robe, "#22223B", "#9A8C98", Motif.Stars, Emblem.Moon, Bottom.Trousers, "#22223B", Shoe.Boots, "#0B0B0F"),        // Mage noir
            [53] = O(Top.Suit, "#6A994E", "#F2E8CF", Motif.None, Emblem.None, Bottom.Trousers, "#6A994E", Shoe.Dress, "#3A2A20"),         // Dandy
            [54] = O(Top.Robe, "#A0522D", "#D4A373", Motif.Plaid, Emblem.None, Bottom.Trousers, "#6F4E37", Shoe.Boots, "#3E2723"),        // Jarl
            [55] = O(Top.TShirt, "#4CC9F0", "#FFB703", Motif.Dots, Emblem.Star, Bottom.Shorts, "#F72585", Shoe.Sneakers, "#FFB703"),      // Anniversaire
            [56] = O(Top.Space, "#F8F9FA", "#E63946", Motif.None, Emblem.None, Bottom.Trousers, "#F8F9FA", Shoe.Boots, "#ADB5BD"),        // Astronaute
            [57] = O(Top.Space, "#8D99AE", "#00F5D4", Motif.None, Emblem.Ring, Bottom.Trousers, "#6C757D", Shoe.Boots, "#495057"),        // Robot
            [58] = O(Top.Hoodie, "#FFF0F7", "#CDB4DB", Motif.Stars, Emblem.Heart, Bottom.Joggers, "#A2D2FF", Shoe.Sneakers, "#FFC8DD"),   // Licorne
            [59] = O(Top.Jacket, "#C1121F", "#16161D", Motif.None, Emblem.None, Bottom.Jeans, "#16161D", Shoe.Boots, "#16161D"),          // Diablotin
            [60] = O(Top.Robe, "#FFFFFF", "#FFE066", Motif.None, Emblem.Star, Bottom.Trousers, "#FFFFFF", Shoe.Sneakers, "#FFE066"),      // Ange
            [61] = O(Top.Tank, "#FFD45A", "#FFFFFF", Motif.None, Emblem.B, Bottom.Shorts, "#C8901E", Shoe.Sneakers, "#FFD45A", "#FFFFFF", true), // Basile doré
            [62] = O(Top.Suit, "#1F2645", "#F8F7F2", Motif.Pinstripe, Emblem.None, Bottom.Trousers, "#1F2645", Shoe.Dress, "#1C1B22"),    // Challenger
            [63] = O(Top.Jacket, "#7B2CBF", "#FFD60A", Motif.Stars, Emblem.Star, Bottom.Jeans, "#2B2D42", Shoe.Sneakers, "#FF4FA3"),    // Superstar
            [64] = O(Top.Hoodie, "#3A86FF", "#FFBE0B", Motif.None, Emblem.Heart, Bottom.Jeans, "#22223B", Shoe.Sneakers, "#FF006E"),     // Duo
            [65] = O(Top.Shirt, "#2D6A4F", "#FFD60A", Motif.Plaid, Emblem.None, Bottom.Shorts, "#40916C", Shoe.Sneakers, "#FFD60A"),     // Chanceux
        };

        static readonly Top[] FallbackTops = { Top.TShirt, Top.Hoodie, Top.Jersey, Top.Sweater, Top.Shirt, Top.Jacket };
        static readonly string[] FallbackColors = { "#E63946", "#3A86FF", "#06D6A0", "#FFB703", "#8338EC", "#FF006E", "#2A9D8F", "#F77F00" };

        /// <summary>Tenue de la tête <paramref name="headId"/> (toujours la même).</summary>
        public static OutfitDef For(int headId)
        {
            if (Table.TryGetValue(headId, out var o)) return o;
            // Nouvelle tête sans tenue écrite : une variante calculée, stable.
            var rng = new GameRng(GameRng.HashSeed("outfit-" + headId));
            int k = (int)(rng.NextFloat() * 1000);
            return O(FallbackTops[k % FallbackTops.Length], FallbackColors[k % FallbackColors.Length], "#FFFFFF",
                (Motif)(k % 3), (Emblem)(k % 5), (Bottom)(k % 4), "#2B2D42", (Shoe)(k % 2), FallbackColors[(k / 7) % FallbackColors.Length]);
        }

        public static int Count => Table.Count;
    }
}
