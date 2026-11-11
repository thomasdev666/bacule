using System;
using System.Collections.Generic;
using System.Text;

namespace Bascule.Core
{
    // =====================================================================================================
    // Duel en temps réel (1 contre 1, par code). Règles, messages réseau et lissage de la vue de l'adversaire.
    // Tout ici est du C# pur, testé sans Unity ; la partie réseau et l'affichage sont dans Runtime/Duel.
    //
    // Principe :
    // - les deux joueurs reçoivent la même graine : mêmes poids, dans le même ordre, et mêmes rafales au même moment ;
    // - chacun joue sa partie sur son téléphone (aucun décalage réseau dans sa propre physique) ;
    // - chacun envoie 15 fois par seconde une photo de sa partie (planche, poids, score) : l'autre la voit en direct ;
    // - le premier qui lâche perd. L'autre a encore quelques secondes pour poser un dernier poids, puis c'est fini ;
    // - la pression monte avec le temps (planche plus sensible, rafales plus fortes) : un duel dure 1 à 3 minutes,
    //   plus longtemps seulement si les deux sont très forts.
    // =====================================================================================================

    /// <summary>Réglages du duel (section « duel » de bascule_tuning.json).</summary>
    [Serializable]
    public class DuelTuning
    {
        /// <summary>Compte à rebours avant le début (secondes).</summary>
        public float countdownSeconds = 3f;
        /// <summary>La planche devient plus sensible à partir de ce temps (s), de sensitivityPerMinute par minute, jusqu'à sensitivityMax.</summary>
        public float pressureFromSeconds = 20f;
        public float sensitivityPerMinute = 0.3f;
        public float sensitivityMax = 1.75f;
        /// <summary>Rafales communes : la première à firstGustSeconds, puis toutes les gustGapMin à gustGapMax secondes.</summary>
        public float firstGustSeconds = 25f;
        public float gustGapMin = 20f;
        public float gustGapMax = 28f;
        /// <summary>Force des rafales du duel : première, gain à chaque rafale, maximum (part du couple de défaite).</summary>
        public float gustStrengthStart = 0.45f;
        public float gustStrengthStep = 0.05f;
        public float gustStrengthMax = 0.72f;
        /// <summary>Le vent seul ne pousse jamais l'équilibre au-delà de cette part de l'angle max (équité, par joueur).</summary>
        public float gustMaxPeakRatio = 0.92f;
        /// <summary>Quand l'adversaire lâche : temps laissé pour poser un dernier poids (s).</summary>
        public float finalWindowSeconds = 3f;
        /// <summary>Deux chutes à moins de cet écart (s) sont simultanées : le meilleur score gagne.</summary>
        public float tieWindowSeconds = 0.3f;
        /// <summary>Pièces : gagnant, perdant, égalité.</summary>
        public int coinsWin = 60;
        public int coinsLose = 25;
        public int coinsDraw = 40;
        /// <summary>Victoires pour la tête « Challenger ».</summary>
        public int challengerWins = 15;
        /// <summary>Photos de la partie envoyées par seconde.</summary>
        public int snapshotsPerSecond = 20;
        /// <summary>Sans nouvelles de l'adversaire pendant ce temps (s), il est considéré comme parti.</summary>
        public float timeoutSeconds = 8f;
    }

    public enum DuelOutcome { Win, Lose, Draw }

    /// <summary>Une rafale du duel, identique pour les deux joueurs.</summary>
    public struct DuelGust
    {
        public float At;       // secondes depuis le début du duel
        public int Direction;  // +1 vers la droite
        public float Strength; // part du couple de défaite
    }

    public static class DuelRules
    {
        /// <summary>Sensibilité de la planche selon le temps de duel (la pression monte, même si on ne marque pas).</summary>
        public static float Sensitivity(float seconds, DuelTuning t)
        {
            if (seconds <= t.pressureFromSeconds) return 1f;
            float s = 1f + t.sensitivityPerMinute * ((seconds - t.pressureFromSeconds) / 60f);
            return s > t.sensitivityMax ? t.sensitivityMax : s;
        }

        /// <summary>Rafales du duel, tirées de la graine : les deux téléphones calculent exactement la même liste.</summary>
        public static List<DuelGust> GustSchedule(uint seed, DuelTuning t, float untilSeconds = 900f)
        {
            var rng = new GameRng(seed ^ 0xD0E1A5u);
            var list = new List<DuelGust>();
            float at = t.firstGustSeconds;
            int n = 0;
            while (at < untilSeconds && n < 200)
            {
                list.Add(new DuelGust
                {
                    At = at,
                    Direction = rng.NextFloat() < 0.5f ? -1 : 1,
                    Strength = Math.Min(t.gustStrengthMax, t.gustStrengthStart + t.gustStrengthStep * n),
                });
                at += t.gustGapMin + (t.gustGapMax - t.gustGapMin) * rng.NextFloat();
                n++;
            }
            return list;
        }

        /// <summary>
        /// Qui gagne ? Le premier qui lâche perd. Deux chutes presque en même temps (moins de tieWindow) : le meilleur
        /// score gagne ; même score : égalité. Null = pas encore tombé.
        /// </summary>
        public static DuelOutcome Decide(float? myFall, float? theirFall, int myScore, int theirScore, DuelTuning t)
        {
            if (myFall == null && theirFall == null) return myScore > theirScore ? DuelOutcome.Win : myScore < theirScore ? DuelOutcome.Lose : DuelOutcome.Draw;
            if (myFall == null) return DuelOutcome.Win;
            if (theirFall == null) return DuelOutcome.Lose;
            if (Math.Abs(myFall.Value - theirFall.Value) <= t.tieWindowSeconds)
                return myScore > theirScore ? DuelOutcome.Win : myScore < theirScore ? DuelOutcome.Lose : DuelOutcome.Draw;
            return myFall.Value > theirFall.Value ? DuelOutcome.Win : DuelOutcome.Lose;
        }

        public static int Coins(DuelOutcome o, DuelTuning t) => o == DuelOutcome.Win ? t.coinsWin : o == DuelOutcome.Lose ? t.coinsLose : t.coinsDraw;

        /// <summary>Enregistre un duel terminé dans le profil ; renvoie les pièces gagnées.</summary>
        public static int Record(Profile p, DuelOutcome o, DuelTuning t)
        {
            p.duelsPlayed++;
            if (o == DuelOutcome.Win) p.duelWins++;
            int coins = Coins(o, t);
            Wallet.Add(p, coins);
            return coins;
        }

        /// <summary>
        /// Missions de duel (jouer, gagner) : avance, pièces des missions réussies et bonus des 3 missions du jour.
        /// Renvoie les pièces gagnées ainsi ; <paramref name="done"/> reçoit les missions réussies.
        /// </summary>
        public static int ApplyMissions(Profile p, DuelOutcome o, EconomyTuning t, out List<Mission> done)
        {
            done = Missions.ApplyDuel(p.missions, o == DuelOutcome.Win);
            int coins = 0;
            foreach (var m in done) coins += m.reward;
            if (done.Count > 0 && !p.missions.bonusClaimed && Missions.AllDone(p.missions))
            {
                p.missions.bonusClaimed = true;
                coins += t.missionsBonus;
            }
            Wallet.Add(p, coins);
            return coins;
        }

        /// <summary>Têtes débloquées par les victoires en duel (la « Challenger ») : ajoutées au profil, renvoyées pour l'annonce.</summary>
        public static List<HeadDef> UnlockHeads(Profile p)
        {
            var list = new List<HeadDef>();
            var run = new RunAchievements { DuelWins = p.duelWins };
            foreach (var h in HeadCatalog.All)
            {
                if (h.Unlock != UnlockKind.DuelWins || p.unlockedHeads.Contains(h.Id)) continue;
                if (HeadCatalog.IsEarned(h, run)) { p.unlockedHeads.Add(h.Id); list.Add(h); }
            }
            return list;
        }

        /// <summary>Nom de l'adversaire tel qu'on l'affiche : pseudo filtré, sinon <paramref name="fallback"/>.</summary>
        public static string CleanName(string name, string fallback)
        {
            var c = PseudoFilter.Clean(name);
            return string.IsNullOrEmpty(c) ? fallback : c.Replace('_', ' ');
        }

        /// <summary>Lien d'invitation (page web) : https://orpheex.fr/bascule/duel.html?c=CODE.</summary>
        public const string InviteUrl = "https://orpheex.fr/bascule/duel.html";
        /// <summary>Lien qui ouvre directement le jeu (schéma déclaré dans l'application) : bascule://duel/CODE.</summary>
        public const string AppScheme = "bascule";

        public static string InviteLink(string code) => InviteUrl + "?c=" + Uri.EscapeDataString(code ?? "");

        /// <summary>
        /// Code de duel contenu dans un lien (bascule://duel/CODE, …/duel.html?c=CODE) ; null si ce n'est pas un lien de duel.
        /// </summary>
        public static string CodeFromLink(string url)
        {
            if (string.IsNullOrEmpty(url) || url.IndexOf("duel", StringComparison.OrdinalIgnoreCase) < 0) return null;
            string raw = null;
            int q = url.IndexOf("c=", StringComparison.OrdinalIgnoreCase);
            if (q >= 0 && q > 0 && (url[q - 1] == '?' || url[q - 1] == '&'))
            {
                raw = url.Substring(q + 2);
                int amp = raw.IndexOfAny(new[] { '&', '#' });
                if (amp >= 0) raw = raw.Substring(0, amp);
            }
            else
            {
                string path = url;
                int cut = path.IndexOfAny(new[] { '?', '#' });
                if (cut >= 0) path = path.Substring(0, cut);
                path = path.TrimEnd('/');
                int slash = path.LastIndexOf('/');
                if (slash >= 0) raw = path.Substring(slash + 1);
                if (raw != null && raw.Equals("duel", StringComparison.OrdinalIgnoreCase)) raw = null;
            }
            if (string.IsNullOrEmpty(raw)) return null;
            var code = NormalizeCode(Uri.UnescapeDataString(raw));
            return code != null && code.Length >= 4 && code.Length <= 12 ? code : null;
        }

        /// <summary>Code de duel lisible : majuscules, sans espaces ni tirets.</summary>
        public static string NormalizeCode(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var sb = new StringBuilder();
            foreach (char raw in text)
            {
                char c = char.ToUpperInvariant(raw);
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) sb.Append(c);
            }
            // Un lien « …/duel?c=ABC123 » ou un message entier : on garde le dernier mot de 6 à 8 caractères.
            string s = sb.ToString();
            return s.Length >= 4 && s.Length <= 12 ? s : ExtractCode(text);
        }

        static string ExtractCode(string text)
        {
            string best = null;
            var cur = new StringBuilder();
            foreach (char raw in text + " ")
            {
                char c = char.ToUpperInvariant(raw);
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) cur.Append(c);
                else
                {
                    if (cur.Length >= 5 && cur.Length <= 10) best = cur.ToString();
                    cur.Clear();
                }
            }
            return best;
        }
    }

    // =====================================================================================================
    // Messages réseau. Format binaire compact, petit-boutiste, versionné (DuelProtocol.Version).
    // =====================================================================================================

    public enum DuelMsg : byte
    {
        Hello = 1,     // fiable : qui je suis (pseudo, tête, planche)
        Start = 2,     // fiable, de l'hôte : graine + compte à rebours
        Snapshot = 3,  // non fiable, 15/s : photo de ma partie
        Fell = 4,      // fiable : j'ai lâché (temps, score)
        Finished = 5,  // fiable : ma partie est finie (score final)
        Rematch = 6,   // fiable : je veux une revanche
        Leave = 7,     // fiable : je quitte
    }

    /// <summary>Un poids vu dans la photo : identifiant stable (pour le lissage), valeur, position par rapport au pivot.</summary>
    public struct DuelWeight
    {
        public ushort Id;
        public int Value;
        public float X, Y, Rotation;
    }

    /// <summary>Photo d'une partie à un instant (envoyée 15 fois par seconde).</summary>
    public sealed class DuelSnapshot
    {
        public uint TimeMs;
        public int Score;
        public float Angle;      // degrés, + = côté droit en bas
        public float Danger;     // 0..2
        public int HeldValue;    // 0 = aucun
        public float HeldX;      // position du poids tenu (par rapport au pivot)
        public float Wind;       // -1..1 : vent ressenti (signe = direction)
        public readonly List<DuelWeight> Weights = new List<DuelWeight>();

        public DuelSnapshot Clone()
        {
            var c = new DuelSnapshot { TimeMs = TimeMs, Score = Score, Angle = Angle, Danger = Danger, HeldValue = HeldValue, HeldX = HeldX, Wind = Wind };
            c.Weights.AddRange(Weights);
            return c;
        }
    }

    public sealed class DuelHello
    {
        public string Name = "";
        public int Head;
        public int Plank;
    }

    public static class DuelProtocol
    {
        public const byte Version = 1;
        /// <summary>Au plus autant de poids dans une photo (au-delà, les plus bas ne sont pas envoyés).</summary>
        public const int MaxWeights = 120;

        static int Exp(int value) { int e = 0; while ((1 << e) < value && e < 15) e++; return value <= 0 ? 0 : e + 1; }
        static int FromExp(int e) => e <= 0 ? 0 : 1 << (e - 1);
        static short Q(float v, float scale) => (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, Math.Round(v * scale)));

        public static byte[] Hello(DuelHello h)
        {
            var w = new ByteWriter();
            w.U8((byte)DuelMsg.Hello); w.U8(Version);
            w.Str(h.Name ?? "", 24); w.U16((ushort)Math.Max(0, h.Head)); w.U8((byte)Math.Max(0, h.Plank));
            return w.ToArray();
        }

        public static byte[] Start(uint seed, float countdown)
        {
            var w = new ByteWriter();
            w.U8((byte)DuelMsg.Start); w.U8(Version);
            w.U32(seed); w.U16((ushort)Math.Max(0, Math.Min(60000, countdown * 1000f)));
            return w.ToArray();
        }

        public static byte[] Snapshot(DuelSnapshot s)
        {
            var w = new ByteWriter();
            w.U8((byte)DuelMsg.Snapshot); w.U8(Version);
            w.U32(s.TimeMs); w.I32(s.Score);
            w.I16(Q(s.Angle, 100f));
            w.U8((byte)Math.Max(0, Math.Min(255, Math.Round(s.Danger * 127f))));
            w.U8((byte)Exp(s.HeldValue)); w.I16(Q(s.HeldX, 100f));
            w.I8((sbyte)Math.Max(-127, Math.Min(127, Math.Round(s.Wind * 127f))));
            int n = Math.Min(MaxWeights, s.Weights.Count);
            w.U8((byte)n);
            for (int i = 0; i < n; i++)
            {
                var d = s.Weights[i];
                w.U16(d.Id); w.U8((byte)Exp(d.Value));
                w.I16(Q(d.X, 100f)); w.I16(Q(d.Y, 100f)); w.I16(Q(d.Rotation, 10f));
            }
            return w.ToArray();
        }

        public static byte[] Fell(uint timeMs, int score) { var w = new ByteWriter(); w.U8((byte)DuelMsg.Fell); w.U8(Version); w.U32(timeMs); w.I32(score); return w.ToArray(); }
        public static byte[] Finished(int score, uint survivedMs) { var w = new ByteWriter(); w.U8((byte)DuelMsg.Finished); w.U8(Version); w.I32(score); w.U32(survivedMs); return w.ToArray(); }
        public static byte[] Simple(DuelMsg kind) { var w = new ByteWriter(); w.U8((byte)kind); w.U8(Version); return w.ToArray(); }

        /// <summary>Type du message, ou 0 s'il est invalide ou d'une autre version.</summary>
        public static DuelMsg Kind(byte[] data) => data != null && data.Length >= 2 && data[1] == Version ? (DuelMsg)data[0] : 0;

        public static DuelHello ReadHello(byte[] data)
        {
            var r = new ByteReader(data, 2);
            return new DuelHello { Name = r.Str(), Head = r.U16(), Plank = r.U8() };
        }

        public static void ReadStart(byte[] data, out uint seed, out float countdown)
        {
            var r = new ByteReader(data, 2);
            seed = r.U32(); countdown = r.U16() / 1000f;
        }

        public static DuelSnapshot ReadSnapshot(byte[] data, DuelSnapshot into = null)
        {
            var r = new ByteReader(data, 2);
            var s = into ?? new DuelSnapshot();
            s.Weights.Clear();
            s.TimeMs = r.U32(); s.Score = r.I32();
            s.Angle = r.I16() / 100f;
            s.Danger = r.U8() / 127f;
            s.HeldValue = FromExp(r.U8()); s.HeldX = r.I16() / 100f;
            s.Wind = r.I8() / 127f;
            int n = r.U8();
            for (int i = 0; i < n; i++)
                s.Weights.Add(new DuelWeight { Id = r.U16(), Value = FromExp(r.U8()), X = r.I16() / 100f, Y = r.I16() / 100f, Rotation = r.I16() / 10f });
            return s;
        }

        public static void ReadFell(byte[] data, out uint timeMs, out int score) { var r = new ByteReader(data, 2); timeMs = r.U32(); score = r.I32(); }
        public static void ReadFinished(byte[] data, out int score, out uint survivedMs) { var r = new ByteReader(data, 2); score = r.I32(); survivedMs = r.U32(); }
    }

    /// <summary>Écriture binaire petit-boutiste, sans allocation par valeur.</summary>
    public sealed class ByteWriter
    {
        byte[] _buf = new byte[256];
        int _len;

        void Need(int n)
        {
            if (_len + n <= _buf.Length) return;
            var b = new byte[Math.Max(_buf.Length * 2, _len + n)];
            Buffer.BlockCopy(_buf, 0, b, 0, _len);
            _buf = b;
        }

        public void U8(byte v) { Need(1); _buf[_len++] = v; }
        public void I8(sbyte v) => U8(unchecked((byte)v));
        public void U16(ushort v) { Need(2); _buf[_len++] = (byte)v; _buf[_len++] = (byte)(v >> 8); }
        public void I16(short v) => U16(unchecked((ushort)v));
        public void U32(uint v) { Need(4); for (int i = 0; i < 4; i++) _buf[_len++] = (byte)(v >> (8 * i)); }
        public void I32(int v) => U32(unchecked((uint)v));

        /// <summary>Texte UTF-8 (au plus <paramref name="maxBytes"/> octets), précédé de sa longueur.</summary>
        public void Str(string s, int maxBytes)
        {
            var b = Encoding.UTF8.GetBytes(s ?? "");
            int n = Math.Min(Math.Min(maxBytes, 255), b.Length);
            // Ne pas couper un caractère accentué en deux.
            while (n > 0 && n < b.Length && (b[n] & 0xC0) == 0x80) n--;
            U8((byte)n); Need(n); Buffer.BlockCopy(b, 0, _buf, _len, n); _len += n;
        }

        public byte[] ToArray() { var r = new byte[_len]; Buffer.BlockCopy(_buf, 0, r, 0, _len); return r; }
    }

    /// <summary>Lecture binaire petit-boutiste ; lit des zéros au-delà de la fin (message tronqué) au lieu de planter.</summary>
    public sealed class ByteReader
    {
        readonly byte[] _d;
        int _p;
        public ByteReader(byte[] data, int offset = 0) { _d = data ?? new byte[0]; _p = offset; }
        public bool Truncated { get; private set; }
        public byte U8() { if (_p >= _d.Length) { Truncated = true; return 0; } return _d[_p++]; }
        public sbyte I8() => unchecked((sbyte)U8());
        public ushort U16() => (ushort)(U8() | (U8() << 8));
        public short I16() => unchecked((short)U16());
        public uint U32() { uint v = 0; for (int i = 0; i < 4; i++) v |= (uint)U8() << (8 * i); return v; }
        public int I32() => unchecked((int)U32());
        public string Str()
        {
            int n = U8();
            if (_p + n > _d.Length) { Truncated = true; n = Math.Max(0, _d.Length - _p); }
            var s = Encoding.UTF8.GetString(_d, _p, n);
            _p += n;
            return s;
        }
    }

    /// <summary>
    /// Lissage de la vue de l'adversaire. Les photos arrivent 15 fois par seconde, avec un peu d'irrégularité :
    /// on affiche la partie avec un léger retard (~120 ms) et on interpole entre deux photos, pour un mouvement
    /// aussi fluide que si on jouait soi-même. Les poids sont suivis par identifiant (un poids qui vient d'apparaître
    /// ou de fusionner n'est pas « glissé » depuis un autre).
    /// </summary>
    public sealed class SnapshotBuffer
    {
        readonly List<DuelSnapshot> _snaps = new List<DuelSnapshot>();
        readonly List<float> _arrivals = new List<float>();
        readonly DuelSnapshot _out = new DuelSnapshot();
        readonly Dictionary<ushort, DuelWeight> _prev = new Dictionary<ushort, DuelWeight>();
        float _offset = float.MaxValue;
        public float DelaySeconds = 0.12f;
        /// <summary>
        /// Délai adaptatif : il suit l'irrégularité du réseau (photos qui arrivent en avance ou en retard). Assez long
        /// pour toujours avoir une photo d'avance (pas d'image figée puis qui saute), le plus court possible sinon.
        /// </summary>
        public bool Adaptive;
        public float MinDelay = 0.08f, MaxDelay = 0.35f;
        float _jitter, _interval = 1f / 15f;
        public int Count => _snaps.Count;
        public DuelSnapshot Latest => _snaps.Count > 0 ? _snaps[_snaps.Count - 1] : null;
        /// <summary>Heure locale de la dernière photo reçue.</summary>
        public float LastArrival { get; private set; } = -1f;

        public void Clear() { _snaps.Clear(); _arrivals.Clear(); _offset = float.MaxValue; LastArrival = -1f; _jitter = 0f; }

        /// <summary>Ajoute une photo reçue à l'heure locale <paramref name="localTime"/> (s). Les photos en retard sont ignorées.</summary>
        public void Add(DuelSnapshot s, float localTime)
        {
            if (_snaps.Count > 0 && s.TimeMs <= _snaps[_snaps.Count - 1].TimeMs) return;
            if (_snaps.Count > 0)
            {
                float gap = (s.TimeMs - _snaps[_snaps.Count - 1].TimeMs) / 1000f;
                if (gap > 0f && gap < 0.5f) _interval += (gap - _interval) * 0.1f;
            }
            _snaps.Add(s);
            _arrivals.Add(localTime);
            LastArrival = localTime;
            // Décalage horloge locale - horloge de l'adversaire : le plus petit observé (le trajet le plus rapide).
            float off = localTime - s.TimeMs / 1000f;
            if (off < _offset) _offset = off;
            // Le décalage peut dériver un peu : on le laisse remonter très lentement.
            else _offset += (off - _offset) * 0.002f;
            if (Adaptive && _offset < float.MaxValue)
            {
                // Retard de cette photo par rapport au trajet le plus rapide : monte vite, redescend lentement.
                float late = off - _offset;
                _jitter = late > _jitter ? _jitter + (late - _jitter) * 0.5f : _jitter + (late - _jitter) * 0.02f;
                float want = _interval * 1.6f + _jitter * 1.3f;
                want = want < MinDelay ? MinDelay : (want > MaxDelay ? MaxDelay : want);
                // Le délai change en douceur : l'adversaire ne fait jamais de saut en avant ou en arrière.
                DelaySeconds += (want - DelaySeconds) * 0.08f;
            }
            while (_snaps.Count > 40) { _snaps.RemoveAt(0); _arrivals.RemoveAt(0); }
        }

        /// <summary>Partie de l'adversaire à afficher maintenant (interpolée). Null tant qu'aucune photo n'est arrivée.</summary>
        public DuelSnapshot Sample(float localTime)
        {
            if (_snaps.Count == 0) return null;
            float remote = localTime - _offset - DelaySeconds;
            uint target = remote <= 0 ? 0 : (uint)(remote * 1000f);
            int i = _snaps.Count - 1;
            while (i > 0 && _snaps[i - 1].TimeMs > target) i--;
            var b = _snaps[i];
            var a = i > 0 ? _snaps[i - 1] : b;
            float k = b.TimeMs == a.TimeMs ? 1f : Clamp01((target - (float)a.TimeMs) / (b.TimeMs - a.TimeMs));
            if (target >= _snaps[_snaps.Count - 1].TimeMs) { a = b = _snaps[_snaps.Count - 1]; k = 1f; }

            _out.TimeMs = (uint)(a.TimeMs + (b.TimeMs - a.TimeMs) * k);
            _out.Score = k < 0.5f ? a.Score : b.Score;
            _out.Angle = a.Angle + (b.Angle - a.Angle) * k;
            _out.Danger = a.Danger + (b.Danger - a.Danger) * k;
            _out.HeldValue = b.HeldValue;
            _out.HeldX = a.HeldValue == b.HeldValue ? a.HeldX + (b.HeldX - a.HeldX) * k : b.HeldX;
            _out.Wind = a.Wind + (b.Wind - a.Wind) * k;
            _out.Weights.Clear();
            _prev.Clear();
            foreach (var w in a.Weights) _prev[w.Id] = w;
            foreach (var w in b.Weights)
            {
                if (_prev.TryGetValue(w.Id, out var p) && p.Value == w.Value)
                    _out.Weights.Add(new DuelWeight
                    {
                        Id = w.Id, Value = w.Value,
                        X = p.X + (w.X - p.X) * k, Y = p.Y + (w.Y - p.Y) * k,
                        Rotation = p.Rotation + DeltaAngle(p.Rotation, w.Rotation) * k,
                    });
                else _out.Weights.Add(w);
            }
            return _out;
        }

        static float DeltaAngle(float a, float b)
        {
            float d = (b - a) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return d;
        }

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
