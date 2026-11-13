using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Bascule.Core
{
    // =====================================================================================================
    // Duel classé (1.5) : adversaire trouvé tout seul (sans code), points de la semaine, divisions, récompense
    // du lundi, et « fantômes » (la partie enregistrée d'un vrai joueur quand personne n'attend).
    // Règles en C# pur, testées sans Unity (Tests/Bascule.Core.Tests et Tests/Bascule.Duel.Tests).
    //
    // - Seuls les duels classés (matchmaking) rapportent des points ; les duels entre amis jamais (sinon deux amis
    //   s'arrangeraient pour monter).
    // - Les points repartent de zéro chaque lundi à 00:00 UTC (le classement en ligne « bascule_ligue » fait la même
    //   remise à zéro et garde la semaine finie en archive, pour la récompense).
    // - La division se lit sur la place dans le classement de la semaine : Légende (1 à 3), Élite (4 à 10),
    //   Challenger (11 à 30), Espoir (les autres). Les tailles sont des réglages : on les élargit quand il y a du monde.
    // =====================================================================================================

    /// <summary>Réglages du duel classé (section « league » de bascule_tuning.json).</summary>
    [Serializable]
    public class LeagueTuning
    {
        /// <summary>Points contre un vrai joueur : victoire, égalité, défaite (on monte en jouant, pas seulement en gagnant).</summary>
        public int winPoints = 30;
        public int drawPoints = 15;
        public int losePoints = 8;
        /// <summary>Points contre un fantôme (moins : il ne réagit pas).</summary>
        public int ghostWinPoints = 10;
        public int ghostDrawPoints = 5;
        public int ghostLosePoints = 2;
        /// <summary>
        /// Victoire par abandon de l'adversaire avant ce temps de jeu (s) : aucun point, victoire non comptée. Sinon deux
        /// complices se retrouveraient dans la file et l'un abandonnerait tout de suite, encore et encore.
        /// </summary>
        public float forfeitFullAfterSeconds = 15f;
        /// <summary>Places de chaque division (les suivants sont « Espoir »).</summary>
        public int legendPlaces = 3;
        public int elitePlaces = 7;
        public int challengerPlaces = 20;
        /// <summary>Pièces données le lundi selon la division de la semaine finie.</summary>
        public int legendCoins = 500;
        public int eliteCoins = 250;
        public int challengerCoins = 100;
        /// <summary>Recherche d'un adversaire : au-delà (s), on propose un fantôme.</summary>
        public float searchSeconds = 20f;
        /// <summary>On cherche d'abord une partie qui attend pendant ce temps (s), puis on ouvre la sienne.</summary>
        public float quickJoinSeconds = 3f;
        /// <summary>
        /// J'attends dans ma partie ouverte : toutes les requeueMin à requeueMax secondes, je regarde si un autre joueur
        /// attend dans la sienne. Si elle passe avant la mienne (ordre des identifiants, même règle pour tous), je referme
        /// la mienne et je le rejoins. Sans ça, deux joueurs arrivés en même temps attendraient chacun de son côté.
        /// </summary>
        public float requeueMinSeconds = 2f;
        public float requeueMaxSeconds = 4f;
        /// <summary>Temps laissé pour se relier à l'adversaire trouvé (s) ; au-delà, on cherche quelqu'un d'autre.</summary>
        public float linkSeconds = 12f;
        /// <summary>Fantômes : photos enregistrées par seconde, durée minimale de jeu (s) pour être publié, âge maximal (jours).</summary>
        public int ghostSnapshotsPerSecond = 5;
        public float ghostMinSeconds = 10f;
        public int ghostMaxAgeDays = 60;
    }

    public enum Division { Legend = 0, Elite = 1, Challenger = 2, Hopeful = 3 }

    public static class League
    {
        /// <summary>Classement en ligne de la semaine (remise à zéro le lundi 00:00 UTC, archive gardée). docs/LIGUES.md.</summary>
        public const string Board = "bascule_ligue";

        /// <summary>Clé de la semaine : la date de son lundi (UTC), « 2026-11-09 ». Change le lundi à 00:00 UTC.</summary>
        public static string WeekKey(DateTime utc)
        {
            var d = utc.Date;
            int back = ((int)d.DayOfWeek + 6) % 7; // lundi = 0
            return d.AddDays(-back).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Lundi 00:00 UTC de la semaine <paramref name="weekKey"/> ; null si la clé est illisible.</summary>
        public static DateTime? WeekStart(string weekKey)
        {
            if (DateTime.TryParseExact(weekKey ?? "", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var d)) return d;
            return null;
        }

        /// <summary>Prochaine remise à zéro (lundi 00:00 UTC).</summary>
        public static DateTime NextReset(DateTime utc) => WeekStart(WeekKey(utc)).Value.AddDays(7);

        /// <summary>Division d'une place du classement (1 = premier) ; 0 ou moins = pas classé : Espoir.</summary>
        public static Division DivisionOf(int rank, LeagueTuning t)
        {
            if (rank <= 0) return Division.Hopeful;
            if (rank <= t.legendPlaces) return Division.Legend;
            if (rank <= t.legendPlaces + t.elitePlaces) return Division.Elite;
            if (rank <= t.legendPlaces + t.elitePlaces + t.challengerPlaces) return Division.Challenger;
            return Division.Hopeful;
        }

        /// <summary>Dernière place d'une division (0 pour Espoir : pas de limite).</summary>
        public static int LastPlace(Division d, LeagueTuning t) =>
            d == Division.Legend ? t.legendPlaces : d == Division.Elite ? t.legendPlaces + t.elitePlaces
            : d == Division.Challenger ? t.legendPlaces + t.elitePlaces + t.challengerPlaces : 0;

        /// <summary>Pièces du lundi pour cette division.</summary>
        public static int Reward(Division d, LeagueTuning t) =>
            d == Division.Legend ? t.legendCoins : d == Division.Elite ? t.eliteCoins : d == Division.Challenger ? t.challengerCoins : 0;

        /// <summary>
        /// Points d'un duel classé. <paramref name="iForfeited"/> : j'ai abandonné ou quitté (0). <paramref name="wonByForfeit"/> :
        /// l'adversaire est parti ; avant forfeitFullAfterSeconds de jeu, seulement les points d'une défaite.
        /// </summary>
        public static int Points(DuelOutcome o, bool vsGhost, bool iForfeited, bool wonByForfeit, float playedSeconds, LeagueTuning t)
        {
            if (iForfeited) return 0;
            if (HollowWin(o, wonByForfeit, playedSeconds, t)) return 0;
            if (vsGhost) return o == DuelOutcome.Win ? t.ghostWinPoints : o == DuelOutcome.Draw ? t.ghostDrawPoints : t.ghostLosePoints;
            return o == DuelOutcome.Win ? t.winPoints : o == DuelOutcome.Draw ? t.drawPoints : t.losePoints;
        }

        /// <summary>
        /// Victoire « creuse » : l'adversaire est parti avant forfeitFullAfterSeconds de jeu. Elle ne rapporte ni points ni
        /// victoire comptée (deux complices ne peuvent pas enchaîner des abandons éclair).
        /// </summary>
        public static bool HollowWin(DuelOutcome o, bool wonByForfeit, float playedSeconds, LeagueTuning t) =>
            o == DuelOutcome.Win && wonByForfeit && playedSeconds < t.forfeitFullAfterSeconds;

        /// <summary>Nouvelle semaine : les points du profil repartent de zéro (la semaine finie reste notée pour la récompense).</summary>
        public static void Roll(LeagueState s, string weekKey)
        {
            if (s.week == weekKey) return;
            // Horloge du téléphone reculée : on ne revient jamais à une semaine antérieure.
            if (!string.IsNullOrEmpty(s.week) && string.CompareOrdinal(weekKey, s.week) < 0) return;
            if (s.points > 0 && !string.IsNullOrEmpty(s.week)) { s.lastWeek = s.week; s.lastPoints = s.points; }
            s.week = weekKey;
            s.rank = 0;
            s.points = 0;
            s.duels = 0;
            s.wins = 0;
        }

        /// <summary>Enregistre un duel classé terminé ; renvoie les points gagnés.</summary>
        public static int Award(LeagueState s, string weekKey, DuelOutcome o, bool vsGhost, bool iForfeited, bool wonByForfeit, float playedSeconds, LeagueTuning t)
        {
            Roll(s, weekKey);
            int pts = Points(o, vsGhost, iForfeited, wonByForfeit, playedSeconds, t);
            s.points += pts;
            s.duels++;
            if (o == DuelOutcome.Win) s.wins++;
            s.totalDuels++;
            return pts;
        }

        /// <summary>Une semaine finie attend sa récompense (on y a marqué des points et on ne l'a pas encore lue).</summary>
        public static bool RewardPending(LeagueState s, string currentWeek)
        {
            Roll(s, currentWeek);
            return !string.IsNullOrEmpty(s.lastWeek) && s.lastWeek != s.rewardedWeek && string.CompareOrdinal(s.lastWeek, currentWeek) < 0;
        }

        /// <summary>
        /// Récompense de la semaine finie, donnée une seule fois. <paramref name="rank"/> : place finale lue dans l'archive
        /// du classement (0 = introuvable : pas de pièces, mais la semaine est soldée).
        /// </summary>
        public static int Claim(Profile p, int rank, LeagueTuning t, out Division division)
        {
            var s = p.league;
            division = DivisionOf(rank, t);
            if (string.IsNullOrEmpty(s.lastWeek) || s.lastWeek == s.rewardedWeek) return 0;
            s.rewardedWeek = s.lastWeek;
            s.lastRank = rank;
            int coins = Reward(division, t);
            if (coins > 0) Wallet.Add(p, coins);
            if (rank > 0 && (s.bestDivision < 0 || (int)division < s.bestDivision)) s.bestDivision = (int)division;
            return coins;
        }
    }

    /// <summary>Ligue du joueur (dans le profil). Les points de la semaine sont aussi envoyés au classement en ligne.</summary>
    [Serializable]
    public class LeagueState
    {
        /// <summary>Semaine en cours (League.WeekKey) et points marqués cette semaine.</summary>
        public string week = "";
        public int points;
        public int duels;
        public int wins;
        /// <summary>Dernière semaine finie où l'on a marqué des points, ses points, et la semaine déjà récompensée.</summary>
        public string lastWeek = "";
        public int lastPoints;
        public string rewardedWeek = "";
        public int lastRank;
        /// <summary>Meilleure division atteinte en fin de semaine (-1 : aucune), pour de futures récompenses (skins).</summary>
        public int bestDivision = -1;
        public int totalDuels;
        /// <summary>Dernière place connue cette semaine (affichage hors ligne).</summary>
        public int rank;
        /// <summary>Fantômes déjà affrontés récemment (identifiants de joueur), pour ne pas retomber sur le même.</summary>
        public List<string> recentGhosts = new List<string>();

        public void Sanitize()
        {
            week = week ?? ""; lastWeek = lastWeek ?? ""; rewardedWeek = rewardedWeek ?? "";
            points = Math.Max(0, points); duels = Math.Max(0, duels); wins = Math.Max(0, wins);
            if (recentGhosts == null) recentGhosts = new List<string>();
            while (recentGhosts.Count > 8) recentGhosts.RemoveAt(0);
        }
    }

    // =====================================================================================================
    // Fantômes. Pendant un duel classé, chaque joueur envoie déjà des photos de sa partie à l'adversaire : on en
    // garde quelques-unes par seconde. Si sa partie est « complète » (il a lâché, ou le temps s'est écoulé : pas
    // interrompue par la chute de l'autre), elle devient son fantôme, publié tout seul. Rejouée avec la même
    // graine, elle donne au joueur les mêmes poids et les mêmes rafales, aux mêmes instants.
    // =====================================================================================================

    /// <summary>La partie enregistrée d'un joueur : de quoi la rejouer comme un adversaire en direct.</summary>
    public sealed class GhostRecording
    {
        public const byte Format = 1;
        public uint Seed;
        /// <summary>Compte à rebours de la partie d'origine (s) : la partie rejouée garde le même, les rafales tombent pareil.</summary>
        public float Countdown;
        public DuelHello Who = new DuelHello();
        /// <summary>Identifiant du joueur d'origine (rempli à la lecture, pas enregistré).</summary>
        public string PlayerId = "";
        /// <summary>
        /// Photos, dans l'ordre. TimeMs = TEMPS DE PARTIE d'origine (GameSession.RunTime) : il ne tourne pas pendant l'arrivée
        /// de Basile ni pendant un passage en arrière-plan, et ralentit pendant un sauvetage. FellMs et EndMs aussi.
        /// </summary>
        public readonly List<DuelSnapshot> Snapshots = new List<DuelSnapshot>();
        /// <summary>Il a lâché à ce temps de partie (ms), avec ce score ; 0 = pas de chute (temps écoulé).</summary>
        public uint FellMs;
        public int FellScore;
        /// <summary>Fin de sa partie : temps de partie (ms), score final, temps tenu depuis « Partez ! » (ms).</summary>
        public uint EndMs;
        public int FinalScore;
        public uint SurvivedMs;

        public bool Fell => FellMs > 0;

        /// <summary>Score affiché au temps de partie <paramref name="timeMs"/> (dernière photo avant).</summary>
        public int ScoreAt(uint timeMs)
        {
            int score = 0;
            foreach (var s in Snapshots) { if (s.TimeMs > timeMs) break; score = s.Score; }
            return score;
        }

        /// <summary>
        /// Utilisable comme fantôme : une partie complète (chute, ou temps écoulé), assez longue, avec des photos jusqu'au
        /// bout. Une partie arrêtée parce que l'adversaire avait lâché n'est pas complète.
        /// </summary>
        public bool Usable(DuelTuning duel, LeagueTuning league)
        {
            if (Seed == 0 || Snapshots.Count < 10 || EndMs == 0 || Countdown < 0.5f || Countdown > 30f) return false;
            float played = SurvivedMs / 1000f;
            if (played < league.ghostMinSeconds) return false;
            bool timeUp = duel.timeLimitSeconds > 0f && played >= duel.timeLimitSeconds - 2f;
            return Fell || timeUp;
        }

        /// <summary>Format binaire compressé (quelques dizaines de Ko pour un duel de 3 minutes).</summary>
        public byte[] Encode()
        {
            var w = new ByteWriter();
            w.U8(Format);
            w.U32(Seed);
            w.U16((ushort)Math.Max(0, Math.Min(60000, Math.Round(Countdown * 1000f))));
            w.Str(Who.Name ?? "", 24); w.U16((ushort)Math.Max(0, Who.Head)); w.U8((byte)Math.Max(0, Who.Plank));
            w.U32(FellMs); w.I32(FellScore); w.U32(EndMs); w.I32(FinalScore); w.U32(SurvivedMs);
            w.U32((uint)Snapshots.Count);
            foreach (var s in Snapshots)
            {
                var packet = DuelProtocol.Snapshot(s);
                w.U16((ushort)packet.Length);
                foreach (var b in packet) w.U8(b);
            }
            var raw = w.ToArray();
            using (var ms = new MemoryStream())
            {
                using (var z = new DeflateStream(ms, CompressionLevel.Optimal, true)) z.Write(raw, 0, raw.Length);
                return ms.ToArray();
            }
        }

        /// <summary>Lit un fantôme ; null s'il est abîmé, d'un autre format ou trop gros.</summary>
        public static GhostRecording Decode(byte[] data)
        {
            if (data == null || data.Length < 8) return null;
            try
            {
                byte[] raw;
                using (var input = new MemoryStream(data))
                using (var z = new DeflateStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    var buf = new byte[8192];
                    int n;
                    while ((n = z.Read(buf, 0, buf.Length)) > 0)
                    {
                        output.Write(buf, 0, n);
                        if (output.Length > MaxRawBytes) return null;
                    }
                    raw = output.ToArray();
                }
                var r = new ByteReader(raw);
                if (r.U8() != Format) return null;
                var g = new GhostRecording { Seed = r.U32(), Countdown = r.U16() / 1000f };
                g.Who = new DuelHello { Name = r.Str(), Head = r.U16(), Plank = r.U8() };
                g.FellMs = r.U32(); g.FellScore = r.I32(); g.EndMs = r.U32(); g.FinalScore = r.I32(); g.SurvivedMs = r.U32();
                uint count = r.U32();
                if (count > MaxSnapshots) return null;
                uint last = 0;
                for (uint i = 0; i < count; i++)
                {
                    int len = r.U16();
                    var packet = new byte[len];
                    for (int k = 0; k < len; k++) packet[k] = r.U8();
                    if (r.Truncated || DuelProtocol.Kind(packet) != DuelMsg.Snapshot) return null;
                    var s = DuelProtocol.ReadSnapshot(packet);
                    if (i > 0 && s.TimeMs <= last) continue; // jamais en arrière
                    last = s.TimeMs;
                    g.Snapshots.Add(s);
                }
                return r.Truncated ? null : g;
            }
            catch (Exception) { return null; }
        }

        public const int MaxSnapshots = 4000;
        public const int MaxRawBytes = 4 * 1024 * 1024;

        public string ToBase64() => Convert.ToBase64String(Encode());

        public static GhostRecording FromBase64(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 3 * 1024 * 1024) return null;
            try { return Decode(Convert.FromBase64String(text)); }
            catch (Exception) { return null; }
        }
    }

    /// <summary>Enregistre ma partie pendant un duel classé (quelques photos par seconde, parmi celles déjà prises pour l'adversaire).</summary>
    public sealed class GhostRecorder
    {
        GhostRecording _rec;
        uint _nextMs;
        uint _stepMs = 200;

        public bool Active => _rec != null;

        public void Begin(uint seed, float countdown, DuelHello me, int snapshotsPerSecond)
        {
            _rec = new GhostRecording { Seed = seed, Countdown = countdown, Who = new DuelHello { Name = me?.Name ?? "", Head = me?.Head ?? 0, Plank = me?.Plank ?? 0 } };
            _stepMs = (uint)(1000 / Math.Max(1, Math.Min(20, snapshotsPerSecond)));
            _nextMs = 0;
        }

        /// <summary>Une photo de ma partie (celle qui part chez l'adversaire) : gardée si assez de temps a passé.</summary>
        public void Add(DuelSnapshot s)
        {
            if (_rec == null || s == null || _rec.Snapshots.Count >= GhostRecording.MaxSnapshots) return;
            if (_rec.Snapshots.Count > 0 && s.TimeMs < _nextMs) return;
            _rec.Snapshots.Add(s.Clone());
            _nextMs = s.TimeMs + _stepMs;
        }

        public void Fell(uint timeMs, int score, DuelSnapshot last)
        {
            if (_rec == null || _rec.FellMs != 0) return;
            _rec.FellMs = Math.Max(1u, timeMs);
            _rec.FellScore = score;
            Force(last);
        }

        /// <summary>Fin de ma partie ; renvoie l'enregistrement (à publier s'il est Usable) et s'arrête.</summary>
        public GhostRecording End(uint endMs, int finalScore, uint survivedMs, DuelSnapshot last)
        {
            if (_rec == null) return null;
            Force(last);
            _rec.EndMs = Math.Max(1u, endMs);
            _rec.FinalScore = finalScore;
            _rec.SurvivedMs = survivedMs;
            var r = _rec;
            _rec = null;
            return r;
        }

        public void Cancel() => _rec = null;

        void Force(DuelSnapshot s)
        {
            if (s == null || _rec.Snapshots.Count >= GhostRecording.MaxSnapshots) return;
            if (_rec.Snapshots.Count > 0 && s.TimeMs <= _rec.Snapshots[_rec.Snapshots.Count - 1].TimeMs) return;
            _rec.Snapshots.Add(s.Clone());
        }
    }

    /// <summary>
    /// Rejoue un fantôme comme un adversaire : rend, au fil du temps, les messages qu'il aurait envoyés en direct
    /// (présentation, départ, photos, chute, fin). Son horloge est le TEMPS DE PARTIE : DuelLink l'avance du temps de la
    /// partie du joueur, donc le fantôme est toujours au même instant de partie que lui (mêmes poids, mêmes rafales,
    /// tirés de la même graine, et sa chute au bon moment).
    /// </summary>
    public sealed class GhostPlayback
    {
        readonly GhostRecording _g;
        readonly float _finalWindow;
        int _next;
        bool _hello, _start, _fell, _finished;
        float _clock, _keepAlive;
        /// <summary>Le joueur a lâché à ce temps (s) : le fantôme lève les bras après la fenêtre du dernier poids.</summary>
        float _stopAt = float.PositiveInfinity;
        byte[] _lastPacket;

        public GhostPlayback(GhostRecording g, float finalWindowSeconds) { _g = g; _finalWindow = finalWindowSeconds; }

        public bool Finished => _finished;
        public float Clock => _clock;

        /// <summary>Le vrai joueur vient de lâcher : comme en direct, le fantôme a encore la fenêtre du dernier poids.</summary>
        public void PlayerFell() { if (float.IsPositiveInfinity(_stopAt)) _stopAt = _clock + _finalWindow; }

        /// <summary>Avance de <paramref name="dt"/> secondes ; ajoute à <paramref name="output"/> les messages à remettre (dans l'ordre).</summary>
        public void Advance(float dt, List<byte[]> output)
        {
            if (!_hello) { _hello = true; output.Add(DuelProtocol.Hello(_g.Who)); return; }
            if (!_start) { _start = true; _clock = 0f; output.Add(DuelProtocol.Start(_g.Seed, _g.Countdown)); return; }
            _clock += Math.Max(0f, dt);
            uint nowMs = (uint)(_clock * 1000f);
            if (_finished)
            {
                // Sa partie est finie : la dernière photo repart de temps en temps, comme un adversaire encore relié.
                _keepAlive += dt;
                if (_keepAlive >= 0.5f && _lastPacket != null) { _keepAlive = 0f; output.Add(_lastPacket); }
                return;
            }
            uint endMs = _g.EndMs;
            bool cut = _clock >= _stopAt && nowMs < endMs && !(_g.Fell && _g.FellMs <= nowMs);
            uint limit = cut ? nowMs : Math.Min(nowMs, endMs);
            while (_next < _g.Snapshots.Count && _g.Snapshots[_next].TimeMs <= limit)
            {
                // La chute part avant les photos d'après la chute, comme en direct.
                if (_g.Fell && !_fell && _g.Snapshots[_next].TimeMs > _g.FellMs) EmitFell(output);
                _lastPacket = DuelProtocol.Snapshot(_g.Snapshots[_next]);
                output.Add(_lastPacket);
                _next++;
            }
            if (_g.Fell && !_fell && nowMs >= _g.FellMs) EmitFell(output);
            if (cut)
            {
                // Le joueur a lâché le premier : le fantôme s'arrête là, debout, avec son score du moment.
                _finished = true;
                // « Partez ! » dans son temps de partie : la fin de sa partie moins le temps qu'il a tenu.
                float go = ((_g.Fell ? _g.FellMs : _g.EndMs) - (float)_g.SurvivedMs) / 1000f;
                output.Add(DuelProtocol.Finished(_g.ScoreAt(nowMs), (uint)Math.Max(0f, (_clock - Math.Max(0f, go)) * 1000f)));
            }
            else if (nowMs >= endMs)
            {
                _finished = true;
                output.Add(DuelProtocol.Finished(_g.FinalScore, _g.SurvivedMs));
            }
        }

        void EmitFell(List<byte[]> output)
        {
            _fell = true;
            output.Add(DuelProtocol.Fell(_g.FellMs, _g.FellScore));
        }
    }
}
