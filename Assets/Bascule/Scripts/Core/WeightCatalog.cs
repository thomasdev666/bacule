using System;

namespace Bascule.Core
{
    /// <summary>Les poids : valeurs en puissances de 2 de 1 à 128, couleur, taille, masse.</summary>
    public static class WeightCatalog
    {
        public const int MinValue = 1;
        public const int MaxValue = 128;
        public static readonly int[] Values = { 1, 2, 4, 8, 16, 32, 64, 128 };

        /// <summary>Couleurs franches, une par valeur. Le chiffre est toujours affiché (pas d'info par la couleur seule).</summary>
        static readonly string[] Colors =
        {
            "#FFC93C", // 1   jaune
            "#FF8A3D", // 2   orange
            "#FF4F5E", // 4   rouge
            "#F25CA2", // 8   rose
            "#9B5DE5", // 16  violet
            "#3A86FF", // 32  bleu
            "#12B5A6", // 64  turquoise
            "#7BD43A", // 128 vert (montgolfière)
        };

        public static bool IsValid(int value) => value >= MinValue && value <= MaxValue && (value & (value - 1)) == 0;

        public static bool IsMax(int value) => value >= MaxValue;

        /// <summary>Ramène n'importe quel entier à la puissance de 2 valide la plus proche (inférieure).</summary>
        public static int Normalize(int value)
        {
            if (value <= MinValue) return MinValue;
            if (value >= MaxValue) return MaxValue;
            int v = 1;
            while (v * 2 <= value) v *= 2;
            return v;
        }

        /// <summary>0 pour 1, 1 pour 2, … 7 pour 128.</summary>
        public static int IndexOf(int value)
        {
            int i = 0, v = Normalize(value);
            while (v > 1) { v >>= 1; i++; }
            return i;
        }

        public static string ColorHex(int value) => Colors[IndexOf(value)];

        public static Rgba Color(int value) => Rgba.FromHex(ColorHex(value));

        public static float Size(int value, WeightTuning t)
        {
            float s = t.baseSize * (float)Math.Pow(Normalize(value), t.sizeExponent);
            return s > t.maxSize ? t.maxSize : s;
        }

        /// <summary>Poids effectif dans le calcul du couple (voir PlankTuning.torqueExponent).</summary>
        public static float TorqueWeight(int value, PlankTuning t) => (float)Math.Pow(Normalize(value), t.torqueExponent);

        public static float Mass(int value, WeightTuning t) => (float)Math.Pow(Normalize(value), t.massExponent);
    }
}
