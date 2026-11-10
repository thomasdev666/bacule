using System;
using System.Collections.Generic;

namespace Bascule.Core
{
    // =====================================================================================================
    // Ce que l'on achète avec des pièces (docs/ECONOMIE.md). Règle d'or : rien de tout cela ne change le score,
    // la physique ou la difficulté. Le classement reste équitable, que l'on ait payé ou non.
    // =====================================================================================================

    public enum PlankPattern { Grain, Bamboo, Grip, Surf, Marble, Candy, Gold }

    /// <summary>Apparence de la planche. Même taille, même poids, mêmes rebords : seul le dessin change.</summary>
    public sealed class PlankSkin
    {
        public int Id;
        public string Fr, En;
        public int Price;
        public Rgba Base, Dark;
        public PlankPattern Pattern;
        public string Name(string lang) => lang == "en" ? En : Fr;
    }

    public static class PlankCatalog
    {
        // Ne jamais renuméroter : les identifiants sont sauvegardés chez les joueurs.
        public static readonly IReadOnlyList<PlankSkin> All = new List<PlankSkin>
        {
            new PlankSkin { Id = 0, Fr = "Bois", En = "Wood", Price = 0, Base = Rgba.FromHex("#C4834F"), Dark = Rgba.FromHex("#8E5530"), Pattern = PlankPattern.Grain },
            new PlankSkin { Id = 1, Fr = "Bambou", En = "Bamboo", Price = 500, Base = Rgba.FromHex("#B9CF5E"), Dark = Rgba.FromHex("#6E8A2A"), Pattern = PlankPattern.Bamboo },
            new PlankSkin { Id = 2, Fr = "Skate", En = "Skateboard", Price = 800, Base = Rgba.FromHex("#FF5A8A"), Dark = Rgba.FromHex("#2B2D42"), Pattern = PlankPattern.Grip },
            new PlankSkin { Id = 3, Fr = "Surf", En = "Surfboard", Price = 1100, Base = Rgba.FromHex("#F4FBFF"), Dark = Rgba.FromHex("#1FA9C9"), Pattern = PlankPattern.Surf },
            new PlankSkin { Id = 4, Fr = "Marbre", En = "Marble", Price = 1500, Base = Rgba.FromHex("#F2F0EC"), Dark = Rgba.FromHex("#8E8A99"), Pattern = PlankPattern.Marble },
            new PlankSkin { Id = 5, Fr = "Sucre d'orge", En = "Candy cane", Price = 2000, Base = Rgba.FromHex("#FF4F6D"), Dark = Rgba.FromHex("#B3203E"), Pattern = PlankPattern.Candy },
            new PlankSkin { Id = 6, Fr = "Or massif", En = "Solid gold", Price = 5000, Base = Rgba.FromHex("#FFCC3D"), Dark = Rgba.FromHex("#B07A12"), Pattern = PlankPattern.Gold },
        };

        public static bool Exists(int id)
        {
            foreach (var s in All) if (s.Id == id) return true;
            return false;
        }

        public static PlankSkin Get(int id)
        {
            foreach (var s in All) if (s.Id == id) return s;
            return All[0];
        }
    }

    public static class Cosmetics
    {
        public static bool OwnsPlank(Profile p, int id) => id == 0 || p.ownedPlanks.Contains(id);

        public static bool BuyPlank(Profile p, PlankSkin s)
        {
            if (s == null || OwnsPlank(p, s.Id)) return false;
            if (!Wallet.Spend(p, s.Price)) return false;
            p.ownedPlanks.Add(s.Id);
            p.selectedPlank = s.Id;
            return true;
        }

        public static bool SelectPlank(Profile p, int id)
        {
            if (!OwnsPlank(p, id)) return false;
            p.selectedPlank = id;
            return true;
        }
    }

    public enum PrizeKind { Head, Plank, Coins }

    /// <summary>Un lot de la machine à surprises : une tête, une planche ou des pièces.</summary>
    public sealed class Prize
    {
        public PrizeKind Kind;
        public HeadDef Head;
        public PlankSkin Plank;
        public int Coins;
        /// <summary>Chance de sortir (0 à 1), affichée au joueur avant l'ouverture.</summary>
        public float Chance;
        public bool Jackpot => Kind == PrizeKind.Coins && Coins >= SurpriseBox.JackpotCoins;
    }

    /// <summary>
    /// Machine à surprises : 1 000 pièces (ou un ticket gratuit du cadeau du jour) pour un lot au hasard.
    /// Têtes de la boutique (55 %), planches (15 %), pièces (30 % : 400, 1 200 ou le jackpot de 3 000).
    /// Jamais de doublon : têtes et planches déjà gagnées ne sortent plus (leur part va aux autres lots).
    /// Dans chaque famille, les objets chers sortent plus rarement (chance proportionnelle à 1/prix).
    /// Toutes les chances sont affichées avant l'ouverture (règle 3.1.1 d'Apple).
    /// </summary>
    public static class SurpriseBox
    {
        public const int Price = 1000;
        public const int JackpotCoins = 3000;
        const float HeadShare = 0.55f, PlankShare = 0.15f;
        static readonly int[] CoinPrizes = { 400, 1200, JackpotCoins };
        static readonly float[] CoinShares = { 0.20f, 0.08f, 0.02f };

        public static List<Prize> Odds(Profile p)
        {
            var heads = new List<HeadDef>();
            foreach (var h in HeadCatalog.All) if (h.Unlock == UnlockKind.Shop && !p.unlockedHeads.Contains(h.Id)) heads.Add(h);
            var planks = new List<PlankSkin>();
            foreach (var s in PlankCatalog.All) if (s.Price > 0 && !Cosmetics.OwnsPlank(p, s.Id)) planks.Add(s);
            var list = new List<Prize>();
            if (heads.Count == 0 && planks.Count == 0) return list; // plus rien à collectionner : machine fermée
            float hs = heads.Count > 0 ? HeadShare : 0f, ps = planks.Count > 0 ? PlankShare : 0f, cs = 0f;
            foreach (var c in CoinShares) cs += c;
            float total = hs + ps + cs;
            float hw = 0f; foreach (var h in heads) hw += Weight(h.UnlockValue);
            float pw = 0f; foreach (var s in planks) pw += Weight(s.Price);
            foreach (var h in heads) list.Add(new Prize { Kind = PrizeKind.Head, Head = h, Chance = hs / total * Weight(h.UnlockValue) / hw });
            foreach (var s in planks) list.Add(new Prize { Kind = PrizeKind.Plank, Plank = s, Chance = ps / total * Weight(s.Price) / pw });
            for (int i = 0; i < CoinPrizes.Length; i++) list.Add(new Prize { Kind = PrizeKind.Coins, Coins = CoinPrizes[i], Chance = CoinShares[i] / total });
            return list;
        }

        static float Weight(int price) => 1000f / Math.Max(100, price);

        public static bool Available(Profile p) => Odds(p).Count > 0;

        /// <summary>
        /// Ouvre la machine : paie (ticket gratuit d'abord s'il y en a un et que <paramref name="useTicket"/>), tire un
        /// lot et le donne. Renvoie le lot, ou null (pas assez de pièces ni de ticket, ou plus rien à gagner).
        /// </summary>
        public static Prize Open(Profile p, uint seed, bool useTicket = false)
        {
            var odds = Odds(p);
            if (odds.Count == 0) return null;
            bool ticket = useTicket && p.surpriseTickets > 0;
            if (!ticket && p.coins < Price) return null;
            var rng = new GameRng(seed ^ (uint)((p.surprisesOpened + 1) * 2246822519u));
            float r = rng.NextFloat(), acc = 0f;
            var won = odds[odds.Count - 1];
            foreach (var o in odds) { acc += o.Chance; if (r < acc) { won = o; break; } }
            if (ticket) p.surpriseTickets--;
            else if (!Wallet.Spend(p, Price)) return null;
            p.surprisesOpened++;
            switch (won.Kind)
            {
                case PrizeKind.Head: p.unlockedHeads.Add(won.Head.Id); break;
                case PrizeKind.Plank: p.ownedPlanks.Add(won.Plank.Id); break;
                default: Wallet.Add(p, won.Coins); break;
            }
            return won;
        }
    }

    /// <summary>
    /// Protège-série : si l'on manque un jour, la série du cadeau du jour continue (un protège-série par jour manqué).
    /// Il est utilisé tout seul, au retour. Au plus <see cref="Max"/> en réserve.
    /// </summary>
    public static class StreakShield
    {
        public const int Price = 300, Max = 2;

        public static bool CanBuy(Profile p) => p.streakShields < Max && p.coins >= Price;

        public static bool Buy(Profile p)
        {
            if (p.streakShields >= Max || !Wallet.Spend(p, Price)) return false;
            p.streakShields++;
            return true;
        }

        /// <summary>La série continue-t-elle entre <paramref name="last"/> et <paramref name="today"/> ? Utilise les protège-séries si besoin.</summary>
        public static bool Continues(string last, string today, int shields, out int used)
        {
            used = 0;
            int gap = Dates.DaysBetween(last, today);
            if (gap == 1) return true;
            if (gap < 2) return false;
            if (gap - 1 > shields) return false;
            used = gap - 1;
            return true;
        }
    }
}
