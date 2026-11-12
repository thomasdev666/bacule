using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bascule.Core
{
    /// <summary>Code d'un partenaire (BDE, créateur…) : il offre une tête cosmétique, une seule fois par joueur.</summary>
    public sealed class PromoCode
    {
        /// <summary>Forme normalisée du code (minuscules, sans espaces, apostrophes, tirets ni accents).</summary>
        public string Key;
        /// <summary>Le code tel qu'on l'écrit (affiché aux joueurs).</summary>
        public string Display;
        /// <summary>Tête offerte (UnlockKind.Code).</summary>
        public int HeadId;
        /// <summary>Nom du partenaire, affiché quand le code est accepté.</summary>
        public string Partner;
        /// <summary>Logo cousu sur la tenue : image Resources/Sponsors/&lt;Patch&gt;_logo.bytes (PNG), ou null.</summary>
        public string Patch;
    }

    public enum PromoResult { Ok, Empty, Unknown, AlreadyUsed }

    /// <summary>
    /// Réglages → Code promo. Les codes sont écrits dans le jeu : on en ajoute à chaque version pour un nouveau partenaire
    /// (sa tête à la fin de HeadCatalog, sa tenue dans OutfitCatalog, son logo dans Resources/Sponsors). Rien de payant
    /// ne se débloque par code : seulement des têtes offertes, introuvables ailleurs.
    /// </summary>
    public static class PromoCodes
    {
        public static readonly PromoCode[] All =
        {
            // BDE Samb'bach : la tête « Samb'bach », avec le logo du BDE en broche sur sa robe.
            new PromoCode { Key = "sambbach", Display = "Samb'bach", HeadId = HeadCatalog.SambBachId, Partner = "le BDE Samb'bach", Patch = "sambbach" },
        };

        /// <summary>Majuscules, espaces, apostrophes, tirets et accents ne comptent pas : « Samb'bach », « SAMB BACH », « sambbach ».</summary>
        public static string Normalize(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            var sb = new StringBuilder(code.Length);
            foreach (char ch in code.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        public static PromoCode Find(string code)
        {
            string k = Normalize(code);
            if (k.Length == 0) return null;
            foreach (var p in All) if (p.Key == k) return p;
            return null;
        }

        /// <summary>Utilise un code : la tête est débloquée et le code marqué comme utilisé (une seule fois).</summary>
        public static PromoResult Redeem(Profile p, string code, out PromoCode promo)
        {
            promo = null;
            if (Normalize(code).Length == 0) return PromoResult.Empty;
            promo = Find(code);
            if (promo == null) return PromoResult.Unknown;
            if (p.redeemedCodes == null) p.redeemedCodes = new List<string>();
            if (p.redeemedCodes.Contains(promo.Key)) return PromoResult.AlreadyUsed;
            p.redeemedCodes.Add(promo.Key);
            if (!p.unlockedHeads.Contains(promo.HeadId)) p.unlockedHeads.Add(promo.HeadId);
            return PromoResult.Ok;
        }

        /// <summary>Après une remise à zéro de saison : les têtes des codes déjà utilisés restent acquises (comme un achat).</summary>
        public static void Regrant(Profile p)
        {
            if (p.redeemedCodes == null) return;
            foreach (var key in p.redeemedCodes)
                foreach (var promo in All)
                    if (promo.Key == key && !p.unlockedHeads.Contains(promo.HeadId)) p.unlockedHeads.Add(promo.HeadId);
        }
    }
}
