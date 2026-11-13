// Le strict nécessaire de Unity pour faire tourner DuelController hors du moteur (horloge réglée par la simulation).
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public static class Time
    {
        public static float unscaledTime;
    }

    public static class Mathf
    {
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Sign(float f) => f >= 0f ? 1f : -1f;
    }

    public static class Debug
    {
        public static readonly List<string> Warnings = new List<string>();
        public static void Log(object message) { }
        public static void LogWarning(object message) { lock (Warnings) Warnings.Add(message?.ToString()); }
    }
}

namespace Bascule
{
    /// <summary>Bilan d'une partie (seuls les champs lus par le duel).</summary>
    public sealed class RunResult
    {
        public int Score;
        public float Seconds;
    }
}

namespace Bascule.Duel
{
    /// <summary>Vue de l'adversaire : on compte seulement les appels.</summary>
    public sealed class OpponentView
    {
        public int Begins, Ends, Defeats, Cheers, Applies;
        public void Begin(int head, int plankSkin) => Begins++;
        public void End() => Ends++;
        public void Defeat(float direction) => Defeats++;
        public void Cheer() => Cheers++;
        public void Apply(Bascule.Core.DuelSnapshot s, float dt) => Applies++;
    }
}
