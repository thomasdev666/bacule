namespace Bascule.Core
{
    /// <summary>
    /// Suite des poids à lâcher. Elle ne dépend que de la graine et du numéro du lâcher, pas du score
    /// ni de ce que fait le joueur : avec la même graine, tout le monde reçoit exactement la même suite
    /// (défi du jour, duel). La difficulté monte avec le nombre de poids lâchés (qui suit le score).
    /// Seule exception : le renfort après un 128 (Reinforce), qui remplace un poids sans décaler la suite tirée.
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

        /// <summary>
        /// Renfort : remplace le poids qui arrive dans la main par un plus gros (après un 128, planche déséquilibrée).
        /// Le tirage n'est pas touché : le poids suivant et ceux d'après restent ceux de la graine, la même suite que
        /// l'adversaire en duel. Renvoie false si le poids en main est déjà assez gros.
        /// </summary>
        public bool Reinforce(int value)
        {
            value = WeightCatalog.Normalize(value);
            if (WeightCatalog.IsMax(value) || value <= Current) return false;
            Current = value;
            return true;
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

    /// <summary>Renfort après un 128 : quel gros poids donner selon le déséquilibre (voir SpawnTuning.rescue*).</summary>
    public static class Rescue
    {
        /// <param name="imbalance">|couple sans le 128 qui s'envole| / couple de défaite.</param>
        /// <param name="losingTorque">Couple de défaite (PlankModel.LosingTorque).</param>
        /// <returns>Le plus petit renfort qui compense le déséquilibre posé à rescueReach du bout (le plus gros sinon),
        /// 0 si la planche n'en a pas besoin.</returns>
        public static int ValueFor(float imbalance, float losingTorque, SpawnTuning t, PlankTuning plank)
        {
            var values = t.rescueValues;
            if (t.rescueChecks <= 0 || t.rescueMax <= 0 || values == null || values.Length == 0) return 0;
            if (imbalance < t.rescueFromRatio || losingTorque <= 0f) return 0;
            foreach (int v in values)
                if (WeightCatalog.TorqueWeight(v, plank) * t.rescueReach / losingTorque >= imbalance) return v;
            return values[values.Length - 1];
        }
    }
}
