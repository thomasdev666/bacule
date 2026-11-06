namespace Bascule.Core
{
    /// <summary>
    /// Suite des poids à lâcher. Elle ne dépend que de la graine et du numéro du lâcher, pas du score
    /// ni de ce que fait le joueur : avec la même graine, tout le monde reçoit exactement la même suite
    /// (défi du jour, duel). La difficulté monte avec le nombre de poids lâchés (qui suit le score).
    /// </summary>
    public sealed class SpawnSequence
    {
        readonly SpawnTier[] _tiers;
        readonly GameRng _rng;

        /// <summary>Poids actuellement tenu au-dessus de la mascotte.</summary>
        public int Current { get; private set; }
        /// <summary>Poids suivant, affiché en haut de l'écran.</summary>
        public int Next { get; private set; }
        /// <summary>Nombre de poids déjà lâchés.</summary>
        public int DropIndex { get; private set; }

        public uint RngState => _rng.State;

        public SpawnSequence(uint seed, SpawnTuning tuning)
        {
            _tiers = tuning.tiers != null && tuning.tiers.Length > 0 ? tuning.tiers : SpawnTuning.DefaultTiers();
            _rng = new GameRng(seed);
            Current = Draw(0);
            Next = Draw(1);
        }

        /// <summary>Le joueur a lâché le poids courant : le suivant devient courant. Renvoie la valeur lâchée.</summary>
        public int Advance()
        {
            int dropped = Current;
            DropIndex++;
            Current = Next;
            Next = Draw(DropIndex + 1);
            return dropped;
        }

        public void Restore(uint rngState, int dropIndex, int current, int next)
        {
            _rng.Restore(rngState);
            DropIndex = dropIndex;
            Current = WeightCatalog.Normalize(current);
            Next = WeightCatalog.Normalize(next);
        }

        public SpawnTier TierFor(int dropIndex)
        {
            SpawnTier tier = _tiers[0];
            foreach (var t in _tiers)
                if (dropIndex >= t.fromDrop) tier = t;
            return tier;
        }

        int Draw(int dropIndex)
        {
            var tier = TierFor(dropIndex);
            float total = 0f;
            foreach (var c in tier.chances) total += c > 0 ? c : 0;
            float roll = _rng.NextFloat() * total;
            for (int i = 0; i < tier.values.Length; i++)
            {
                float c = tier.chances[i] > 0 ? tier.chances[i] : 0;
                if (roll < c) return tier.values[i];
                roll -= c;
            }
            return tier.values[tier.values.Length - 1];
        }
    }
}
