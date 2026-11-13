using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bascule.Core;
using Bascule.Duel;

namespace Bascule.DuelTests
{
    // ======================================================================= Horloge virtuelle

    /// <summary>File des suites de « await » : tout tourne sur un seul fil, dans l'ordre, comme dans Unity.</summary>
    public sealed class SimContext : SynchronizationContext
    {
        readonly Queue<(SendOrPostCallback cb, object state)> _queue = new Queue<(SendOrPostCallback, object)>();
        public override void Post(SendOrPostCallback d, object state) { lock (_queue) _queue.Enqueue((d, state)); }
        public override void Send(SendOrPostCallback d, object state) => d(state);
        public override SynchronizationContext CreateCopy() => this;

        public void Pump()
        {
            for (int guard = 0; guard < 100000; guard++)
            {
                (SendOrPostCallback cb, object state) item;
                lock (_queue)
                {
                    if (_queue.Count == 0) return;
                    item = _queue.Dequeue();
                }
                item.cb(item.state);
            }
            throw new Exception("boucle sans fin dans les suites asynchrones");
        }
    }

    /// <summary>Temps simulé (s) : minuteries et attentes, sans attendre pour de vrai.</summary>
    public sealed class Sim
    {
        public float Now { get; private set; }
        public readonly SimContext Context = new SimContext();
        readonly List<(float at, long seq, Action act)> _timers = new List<(float, long, Action)>();
        long _seq;

        public void At(float at, Action act) => _timers.Add((at, _seq++, act));

        public Task Delay(float seconds)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            At(Now + Math.Max(0f, seconds), () => tcs.TrySetResult(true));
            return tcs.Task;
        }

        public void Advance(float dt)
        {
            Now += dt;
            UnityEngine.Time.unscaledTime = Now;
            while (true)
            {
                Context.Pump();
                int best = -1;
                for (int i = 0; i < _timers.Count; i++)
                {
                    var t = _timers[i];
                    if (t.at > Now) continue;
                    if (best < 0 || t.at < _timers[best].at || (t.at == _timers[best].at && t.seq < _timers[best].seq)) best = i;
                }
                if (best < 0) break;
                var run = _timers[best];
                _timers.RemoveAt(best);
                run.act();
            }
            Context.Pump();
        }
    }

    // ======================================================================= Réseau simulé

    /// <summary>Conditions du réseau (Wi-Fi, 4G, relais lent…).</summary>
    public sealed class NetConditions
    {
        public string Name = "wifi";
        /// <summary>Connexion aux services de Unity (s).</summary>
        public float SignIn = 0.3f;
        /// <summary>Création d'une partie (session + relais) (s).</summary>
        public float Create = 0.8f;
        /// <summary>Le serveur sait si le code existe (s), puis la jointure se termine (JoinReturn).</summary>
        public float JoinCheck = 0.5f;
        public float JoinReturn = 1.2f;
        /// <summary>Liaison directe par le relais, comptée depuis JoinCheck : peut arriver avant ou après le retour de la jointure.</summary>
        public float LinkDelay = 0.4f;
        /// <summary>La liaison n'aboutit jamais (réseau qui bloque).</summary>
        public bool NeverLink;
        /// <summary>Trajet d'un message (s) et irrégularité.</summary>
        public float Latency = 0.03f, Jitter = 0.01f;
        /// <summary>Part des photos perdues (envoi non fiable).</summary>
        public double Loss;
        /// <summary>Le relais oublie un téléphone resté en arrière-plan plus longtemps (s).</summary>
        public float RelayForgetAfter = 10f;
        /// <summary>Après l'oubli, l'autre téléphone voit la coupure au bout de (s).</summary>
        public float PeerNoticesDeadRelay = 15f;
        /// <summary>Premières créations de partie qui échouent (serveur surchargé).</summary>
        public int CreateFailures;
        /// <summary>Premières recherches de duel classé qui échouent.</summary>
        public int SearchFailures;
        /// <summary>Code ami : publication et recherche (Cloud Save) (s).</summary>
        public float PublishDelay = 0.6f, QueryDelay = 0.4f;
        /// <summary>Lancement par le lien : les services sont connectés à partir de (s).</summary>
        public float OnlineAt;
        /// <summary>Temps de réponse du serveur pour quitter une partie (s).</summary>
        public float LeaveDelay = 0.3f;

        public override string ToString() => Name;

        public static NetConditions Wifi() => new NetConditions();
        public static NetConditions SlowMobile() => new NetConditions
        {
            Name = "4g-lente", SignIn = 1.2f, Create = 2.5f, JoinCheck = 1.5f, JoinReturn = 3.5f, LinkDelay = 6f,
            Latency = 0.15f, Jitter = 0.08f, Loss = 0.08,
        };
        public static NetConditions WifiAndMobile() => new NetConditions
        {
            Name = "wifi+4g", SignIn = 0.6f, Create = 1.2f, JoinCheck = 0.8f, JoinReturn = 2f, LinkDelay = 2.5f,
            Latency = 0.09f, Jitter = 0.05f, Loss = 0.03,
        };
    }

    /// <summary>Le monde : serveurs de Unity (parties, relais, codes amis) et téléphones.</summary>
    public sealed class World
    {
        public readonly Sim Sim = new Sim();
        public NetConditions Net;
        public readonly Random Rng;
        public readonly Tuning Tuning = LoadTuning();
        public readonly Dictionary<string, FakeNet> Sessions = new Dictionary<string, FakeNet>();
        public readonly Dictionary<string, string> OpenDuels = new Dictionary<string, string>();
        /// <summary>Parties classées ouvertes qui attendent un adversaire (file du duel classé).</summary>
        public readonly List<FakeNet> RankedOpen = new List<FakeNet>();
        public readonly List<Device> Devices = new List<Device>();
        int _codes;

        public World(NetConditions net = null, int seed = 1)
        {
            Net = net ?? NetConditions.Wifi();
            Rng = new Random(seed);
            UnityEngine.Time.unscaledTime = 0f;
        }

        public string NewCode() => "S" + (++_codes).ToString("00000");

        public Device AddDevice(string name, string friendCode, float fallAt = float.PositiveInfinity, float scoreRate = 20f)
        {
            var d = new Device(this, name, friendCode) { };
            d.Game.FallAt = fallAt;
            d.Game.ScoreRate = scoreRate;
            Devices.Add(d);
            return d;
        }

        /// <summary>Deux téléphones reliés par le relais : chacun voit la liaison à sa prochaine image.</summary>
        public void TryLink(FakeNet host, FakeNet guest, int guestGen)
        {
            if (guest.Gen != guestGen || host.Guest != guest || !host.RelayAlive || !guest.RelayAlive) return;
            host.OfferLink(guest);
            guest.OfferLink(host);
        }

        public float Lag() => Math.Max(0.005f, Net.Latency + (float)(Rng.NextDouble() * 2 - 1) * Net.Jitter);

        /// <summary>Recherche du duel ouvert d'un ami (GameApp.ResolveDuelCode) : attend les services, puis la requête.</summary>
        public async Task<string> Resolve(string code)
        {
            if (!Devices.Any(d => d.FriendCode == code)) return null;
            while (Sim.Now < Net.OnlineAt) await Sim.Delay(0.25f);
            await Sim.Delay(Net.QueryDelay);
            return OpenDuels.TryGetValue(code, out var s) ? s : null;
        }

        public void Step(float dt = 1f / 60f)
        {
            Sim.Advance(dt);
            foreach (var d in Devices) d.Tick(dt);
            Sim.Context.Pump();
        }

        public void RunFor(float seconds)
        {
            float end = Sim.Now + seconds;
            while (Sim.Now < end) Step();
        }

        /// <summary>Avance jusqu'à <paramref name="done"/> ; faux au bout de <paramref name="maxSeconds"/>.</summary>
        public bool RunUntil(Func<bool> done, float maxSeconds)
        {
            float end = Sim.Now + maxSeconds;
            while (Sim.Now < end)
            {
                if (done()) return true;
                Step();
            }
            return done();
        }

        static Tuning LoadTuning()
        {
            // Les vrais réglages du jeu (bascule_tuning.json), section « duel ».
            var t = new Tuning();
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
            if (dir == null) return t;
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(dir.FullName, "Assets/Bascule/Resources/bascule_tuning.json")));
            if (!doc.RootElement.TryGetProperty("duel", out var duel)) return t;
            foreach (var p in duel.EnumerateObject())
            {
                var f = typeof(DuelTuning).GetField(p.Name, BindingFlags.Public | BindingFlags.Instance);
                if (f == null) continue;
                if (f.FieldType == typeof(float)) f.SetValue(t.duel, (float)p.Value.GetDouble());
                else if (f.FieldType == typeof(int)) f.SetValue(t.duel, p.Value.GetInt32());
                else if (f.FieldType == typeof(bool)) f.SetValue(t.duel, p.Value.GetBoolean());
            }
            return t;
        }
    }

    /// <summary>
    /// Faux réseau d'un téléphone, qui se comporte comme UgsDuelNetwork : étapes une par une (créer, rejoindre,
    /// quitter), liaison vue à l'image suivante, messages fiables dans l'ordre, photos qui peuvent se perdre, départ
    /// annoncé deux fois (coupure de la liaison + départ de la salle), relais qui oublie un téléphone en arrière-plan.
    /// </summary>
    public sealed class FakeNet : IDuelNetwork
    {
        readonly World W;
        public readonly string Name;
        public FakeNet(World w, string name) { W = w; Name = name; }

        public bool Available => true;
        public bool IsHost { get; private set; }
        public string Code { get; private set; } = "";
        public bool Linked { get; private set; }
        public bool RelayAlive => _relay;
        public bool HasGuest => Linked || Guest != null;
        public event Action PeerLinked;
        public event Action PeerLost;
        public event Action<byte[]> Received;

        public int Gen { get; private set; }
        public FakeNet Guest { get; internal set; }
        public bool Paused;
        public int Created, Joins;

        bool _relay;
        FakeNet _peer, _offer, _host;
        float _reliableTail;
        readonly List<(float at, long seq, FakeNet from, byte[] data)> _inbox = new List<(float, long, FakeNet, byte[])>();
        readonly List<(float at, Action act)> _events = new List<(float, Action)>();
        long _seq;
        Task _queue = Task.CompletedTask;

        Task<T> Serial<T>(Func<Task<T>> step)
        {
            var prev = _queue;
            var tcs = new TaskCompletionSource<T>();
            _queue = tcs.Task;
            Run();
            return tcs.Task;

            async void Run()
            {
                try { await prev; } catch (Exception) { }
                try { tcs.SetResult(await step()); }
                catch (Exception e) { tcs.SetException(e); }
            }
        }

        public Task<string> HostAsync() => Serial(HostNow);

        async Task<string> HostNow()
        {
            await LeaveNow();
            IsHost = true;
            await W.Sim.Delay(W.Net.SignIn);
            await W.Sim.Delay(W.Net.Create);
            if (W.Net.CreateFailures > 0) { W.Net.CreateFailures--; return "duel_create_failed"; }
            _relay = true;
            Code = W.NewCode();
            W.Sessions[Code] = this;
            Created++;
            return null;
        }

        public Task<string> JoinAsync(string code) => Serial(() => JoinNow(code));

        async Task<string> JoinNow(string code)
        {
            await LeaveNow();
            IsHost = false;
            int gen = Gen;
            Joins++;
            await W.Sim.Delay(W.Net.SignIn);
            await W.Sim.Delay(W.Net.JoinCheck);
            if (code == null || !W.Sessions.TryGetValue(code, out var host) || !host.RelayAlive || (host.Guest != null && host.Guest != this))
            {
                await W.Sim.Delay(Math.Max(0f, W.Net.JoinReturn - W.Net.JoinCheck));
                return "duel_join_failed";
            }
            _relay = true;
            _host = host;
            Code = code;
            host.Guest = this;
            // Liaison par le relais pendant que la jointure se termine (elle peut arriver avant son retour).
            if (!W.Net.NeverLink) W.Sim.At(W.Sim.Now + W.Net.LinkDelay, () => W.TryLink(host, this, gen));
            await W.Sim.Delay(Math.Max(0f, W.Net.JoinReturn - W.Net.JoinCheck));
            return null;
        }

        public Task<string> QuickMatchAsync(float joinSeconds) => Serial(() => QuickMatchNow(joinSeconds));

        public int YieldChecks;

        public async Task<bool> ShouldYieldAsync()
        {
            string mine = Code;
            YieldChecks++;
            await W.Sim.Delay(W.Net.QueryDelay);
            if (Code != mine || string.IsNullOrEmpty(mine) || Guest != null) return false;
            return W.RankedOpen.Exists(h => h != this && h.RelayAlive && h.Guest == null && string.CompareOrdinal(h.Code, mine) < 0);
        }

        /// <summary>Comme le SDK : cherche une partie classée ouverte (une tentative par seconde), sinon ouvre la sienne.</summary>
        async Task<string> QuickMatchNow(float joinSeconds)
        {
            await LeaveNow();
            IsHost = false;
            int gen = Gen;
            await W.Sim.Delay(W.Net.SignIn);
            if (W.Net.SearchFailures > 0) { W.Net.SearchFailures--; return "duel_search_failed"; }
            float until = W.Sim.Now + joinSeconds;
            while (true)
            {
                await W.Sim.Delay(W.Net.JoinCheck);
                var host = W.RankedOpen.Find(h => h != this && h.RelayAlive && h.Guest == null);
                if (host != null)
                {
                    Joins++;
                    _relay = true;
                    _host = host;
                    Code = host.Code;
                    host.Guest = this;
                    W.RankedOpen.Remove(host);
                    if (!W.Net.NeverLink) W.Sim.At(W.Sim.Now + W.Net.LinkDelay, () => W.TryLink(host, this, gen));
                    await W.Sim.Delay(Math.Max(0f, W.Net.JoinReturn - W.Net.JoinCheck));
                    return null;
                }
                if (W.Sim.Now >= until) break;
                await W.Sim.Delay(1f);
            }
            await W.Sim.Delay(W.Net.Create);
            IsHost = true;
            _relay = true;
            Code = W.NewCode();
            W.Sessions[Code] = this;
            W.RankedOpen.Add(this);
            Created++;
            return null;
        }

        public Task LeaveAsync() => Serial(async () => { await LeaveNow(); return true; });

        async Task LeaveNow()
        {
            Gen++;
            var peer = _peer;
            var host = _host;
            var guest = Guest;
            string code = Code;
            bool hadSession = !string.IsNullOrEmpty(code);
            Linked = false;
            _peer = _offer = _host = null;
            Guest = null;
            Code = "";
            W.RankedOpen.Remove(this);
            _relay = false;
            _inbox.Clear();
            float lag = W.Lag();
            if (hadSession && W.Sessions.TryGetValue(code, out var s) && s == this)
            {
                // L'hôte supprime la partie : l'invité reçoit « partie supprimée ».
                W.Sessions.Remove(code);
                if (guest != null) guest.Schedule(W.Sim.Now + lag, () => { if (guest.Code == code) guest.FireLost(); });
            }
            else if (host != null)
            {
                // L'invité quitte : le serveur le retire tout de suite ; l'hôte reçoit « joueur parti » un peu après
                // (seulement si sa liaison ne répond plus, comme UgsDuelNetwork).
                if (host.Guest == this) host.Guest = null;
                host.Schedule(W.Sim.Now + lag, () => { if (!host.Linked) host.FireLost(); });
            }
            if (peer != null) peer.Schedule(W.Sim.Now + lag * 0.9f, () => peer.Disconnected(this));
            if (hadSession) await W.Sim.Delay(W.Net.LeaveDelay);
        }

        /// <summary>Le relais a oublié ce téléphone (arrière-plan trop long) : ses liaisons ne marchent plus.</summary>
        public void ForgetRelay()
        {
            if (!_relay) return;
            _relay = false;
            var peer = _peer;
            if (peer != null) peer.Schedule(W.Sim.Now + W.Net.PeerNoticesDeadRelay, () => peer.Disconnected(this));
            Schedule(W.Sim.Now, () => { if (Linked) { Linked = false; _peer = null; FireLost(); } });
        }

        internal void OfferLink(FakeNet other) => _offer = other;

        void Schedule(float at, Action act) => _events.Add((at, act));

        void Disconnected(FakeNet from)
        {
            if (_peer != from || !Linked) return;
            Linked = false;
            _peer = null;
            FireLost();
        }

        void FireLost() => PeerLost?.Invoke();

        public void Send(byte[] data, bool reliable)
        {
            if (!Linked || _peer == null || !_relay || data == null) return;
            if (!reliable && W.Rng.NextDouble() < W.Net.Loss) return;
            float at = W.Sim.Now + W.Lag();
            if (reliable) { at = Math.Max(at, _peer._reliableTail); _peer._reliableTail = at; }
            _peer._inbox.Add((at, _seq++, this, (byte[])data.Clone()));
        }

        public void Tick()
        {
            float now = W.Sim.Now;
            // Liaison proposée par le relais : vue à cette image (une nouvelle connexion remplace l'ancienne).
            if (_offer != null && _relay && _offer.RelayAlive)
            {
                var other = _offer;
                _offer = null;
                if (Linked && _peer == other) { }
                else
                {
                    _peer = other;
                    Linked = true;
                    PeerLinked?.Invoke();
                }
            }
            // Événements de la salle et coupures, dans l'ordre.
            for (int guard = 0; guard < 1000; guard++)
            {
                int i = _events.FindIndex(e => e.at <= now);
                if (i < 0) break;
                var e = _events[i];
                _events.RemoveAt(i);
                e.act();
            }
            // Messages arrivés.
            var ready = _inbox.Where(m => m.at <= now).OrderBy(m => m.at).ThenBy(m => m.seq).ToList();
            foreach (var m in ready)
            {
                _inbox.Remove(m);
                if (m.from != _peer || !Linked || !_relay) continue;
                Received?.Invoke(m.data);
            }
        }
    }

    // ======================================================================= Partie simulée

    /// <summary>
    /// Une partie de Bascule vue par le duel : score qui monte, chute à FallAt, bras levés quand on l'arrête. Comme la
    /// vraie GameSession, le TEMPS DE PARTIE (RunTime) ne tourne pas pendant l'arrivée de Basile (ArriveSeconds) et se
    /// fige dès que la partie s'arrête (chute, bras levés). La relecture indépendante de la 1.5 a montré que le banc,
    /// sans ça, ne voyait pas un fantôme décalé de 1,6 s.
    /// </summary>
    public sealed class FakeGame
    {
        public DuelController C;
        public float FallAt = float.PositiveInfinity;
        public float ScoreRate = 20f;
        public float FallAnimation = 1.5f, RaiseArms = 0.6f;
        public float ArriveSeconds = 1.6f;
        /// <summary>Vitesse du temps de partie (0,35 pendant un ralenti de sauvetage).</summary>
        public float TimeScale = 1f;
        public bool Running, Locked;
        public float RunTime;
        public int Score;
        public int Starts;
        bool _falling, _stopping;
        float _endIn, _playTime, _arrive;

        public void Start(uint seed)
        {
            Running = true;
            RunTime = 0f;
            _playTime = 0f;
            _arrive = 0f;
            Score = 0;
            _falling = _stopping = false;
            Starts++;
        }

        public bool FinishRun()
        {
            if (!Running || _falling || _stopping) return false;
            _stopping = true;
            _endIn = RaiseArms;
            return true;
        }

        public void Stop() { Running = false; }

        public void Tick(float dt)
        {
            if (!Running) return;
            if (_falling || _stopping)
            {
                // Chute ou bras levés : le temps de partie est figé, l'animation se joue au temps réel.
                _endIn -= dt;
                if (_endIn <= 0f)
                {
                    Running = false;
                    C.OnLocalRunEnded(new RunResult { Score = Score, Seconds = RunTime });
                }
                return;
            }
            if (_arrive < ArriveSeconds) { _arrive += dt; return; } // Basile arrive : le temps de partie attend
            RunTime += dt * TimeScale;
            if (!Locked)
            {
                _playTime += dt * TimeScale;
                Score = (int)(_playTime * ScoreRate);
                if (_playTime >= FallAt)
                {
                    _falling = true;
                    _endIn = FallAnimation;
                    C.OnLocalLoseStarted();
                }
            }
        }

        public void Capture(DuelSnapshot s)
        {
            s.TimeMs = (uint)(RunTime * 1000f);
            s.Score = Score;
            s.Angle = 2f;
            s.Weights.Clear();
        }
    }

    /// <summary>Un téléphone : réseau, contrôleur du duel (le vrai) et partie. Branché comme dans GameApp.</summary>
    public sealed class Device
    {
        public readonly World W;
        public readonly string Name, FriendCode;
        public readonly FakeNet Net;
        public readonly DuelController C;
        public readonly FakeGame Game = new FakeGame();
        public readonly OpponentView View = new OpponentView();
        public readonly List<string> Notices = new List<string>();
        public readonly List<DuelController.Phase> Phases = new List<DuelController.Phase>();
        public int Results;
        public readonly DuelLink Link;
        public readonly List<GhostRecording> Recordings = new List<GhostRecording>();
        public int NoOpponent;
        public bool Paused { get; private set; }
        float _pausedAt;

        public Device(World w, string name, string friendCode)
        {
            W = w;
            Name = name;
            FriendCode = friendCode;
            Net = new FakeNet(w, name);
            Link = new DuelLink(Net) { RunTime = () => Game.RunTime };
            C = new DuelController(Link, () => w.Tuning);
            C.Rand = () => (float)w.Rng.NextDouble();
            C.Recorded += r => Recordings.Add(r);
            C.NoOpponent += () => NoOpponent++;
            Game.C = C;
            C.AttachView(View);
            C.StartRun = seed => Game.Start(seed);
            C.FinishRun = () => Game.FinishRun();
            C.Capture = snap => Game.Capture(snap);
            C.RunTime = () => Game.RunTime;
            C.Score = () => Game.Score;
            C.LockInput = on => Game.Locked = on;
            C.Delay = ms => w.Sim.Delay(ms / 1000f);
            C.ResolveCode = code => w.Resolve(code);
            C.Notice += k => Notices.Add(k);
            C.ResultReady += () => { Results++; Game.Stop(); };
            // Code ami → partie du moment (GameApp.OnDuelHosted / CloseOpenDuel).
            C.Hosted += session =>
            {
                w.Sim.At(w.Sim.Now + w.Net.PublishDelay, () => { if (C.Code == session && C.Current == DuelController.Phase.Hosting) w.OpenDuels[FriendCode] = session; });
            };
            C.Changed += () =>
            {
                if (Phases.Count == 0 || Phases[Phases.Count - 1] != C.Current) Phases.Add(C.Current);
                if (C.Current == DuelController.Phase.None || C.Current == DuelController.Phase.Countdown) w.OpenDuels.Remove(FriendCode);
            };
        }

        public DuelHello Hello => new DuelHello { Name = Name, Head = 3, Plank = 1 };

        public void Host() => C.Host(Hello);
        public void Search() => C.FindMatch(Hello);
        public void Join(string code) => C.Join(Hello, code);

        /// <summary>L'appli passe en arrière-plan (Snap, Messages, Centre de contrôle) : plus d'image ici.</summary>
        public void Pause()
        {
            Paused = true;
            Net.Paused = true;
            _pausedAt = W.Sim.Now;
            C.OnAppPaused();
        }

        public void Resume()
        {
            Paused = false;
            Net.Paused = false;
            C.OnAppResumed(W.Sim.Now - _pausedAt);
        }

        public void Tick(float dt)
        {
            if (Paused)
            {
                if (W.Sim.Now - _pausedAt > W.Net.RelayForgetAfter) Net.ForgetRelay();
                return;
            }
            Game.Tick(dt);
            C.Tick(dt);
        }

        public bool InMatch => C.InMatch;
        public bool Done => C.Current == DuelController.Phase.Results;
        public override string ToString() => Name + " [" + C.Current + "] " + string.Join(",", Notices);
    }
}
