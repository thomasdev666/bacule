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
        /// <summary>
        /// Codes en service. Aucun pour l'instant (le code « Samb'bach » a été retiré en 1.4.1 : partenariat annulé ; un
        /// joueur qui le tape voit « code inconnu »). Exemple pour un prochain partenaire :
        /// new PromoCode { Key = "nomdupartenaire", Display = "NomDuPartenaire", HeadId = 67, Partner = "le club X", Patch = "nomdupartenaire" }
        /// </summary>
        public static readonly PromoCode[] All = { };

        /// <summary>Majuscules, espaces, apostrophes, tirets et accents ne comptent pas : « Club X », « CLUB-X », « clubx ».</summary>
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

        /// <summary>Code en service correspondant à ce texte (<paramref name="codes"/> : All, ou une liste de test).</summary>
        public static PromoCode Find(string code, IReadOnlyList<PromoCode> codes = null)
        {
            string k = Normalize(code);
            if (k.Length == 0) return null;
            foreach (var p in codes ?? All) if (p.Key == k) return p;
            return null;
        }

        /// <summary>Utilise un code : la tête est débloquée et le code marqué comme utilisé (une seule fois).</summary>
        public static PromoResult Redeem(Profile p, string code, out PromoCode promo, IReadOnlyList<PromoCode> codes = null)
        {
            promo = null;
            if (Normalize(code).Length == 0) return PromoResult.Empty;
            promo = Find(code, codes);
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
