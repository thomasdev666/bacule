using System;
using System.Collections.Generic;
using Bascule.Core;
using UnityEngine;

namespace Bascule.Duel
{
    /// <summary>
    /// Déroulé d'un duel en temps réel, de la création du code jusqu'à la revanche.
    ///
    /// 1. Salle d'attente : l'hôte crée une partie (un code) ; l'ami la rejoint avec ce code ou le lien d'invitation.
    /// 2. Présentations : chacun envoie son nom, sa tête et sa planche (Hello). L'hôte tire la graine et donne le départ.
    /// 3. Compte à rebours (3 s) : Basile arrive avec sa planche pendant « 2… 1… ».
    /// 4. Partie : même graine, donc mêmes poids et mêmes rafales. Chacun joue chez lui et envoie 20 photos par seconde
    ///    de sa partie ; l'autre la voit en direct dans la petite vue (seconde caméra).
    /// 5. Le premier qui lâche perd. L'autre a quelques secondes pour poser un dernier poids, puis lève les bras.
    /// 6. Chacun envoie son score final (Finished) ; le résultat est calculé de la même façon des deux côtés
    ///    (Core.DuelRules.Decide). Pièces pour les deux, plus pour le gagnant ; « Challenger » à 15 victoires.
    ///
    /// Robustesse : sans nouvelles pendant timeoutSeconds, l'adversaire est considéré comme parti (victoire par
    /// abandon si la partie était commencée). Quitter l'application pendant un duel compte comme un abandon.
    /// </summary>
    public sealed class DuelController
    {
        public enum Phase { None, Connecting, Hosting, Greeting, Countdown, Playing, LastWeight, Waiting, Results }

        readonly IDuelNetwork _net;
        readonly Func<Tuning> _tuning;
        readonly SnapshotBuffer _buffer = new SnapshotBuffer { Adaptive = true };
        readonly DuelSnapshot _out = new DuelSnapshot();
        readonly DuelSnapshot _in = new DuelSnapshot();
        OpponentView _view;

        public Phase Current { get; private set; }
        public bool IsHost => _net.IsHost;
        public bool Available => _net.Available;
        public string Code => _net.Code;
        public DuelHello Me { get; private set; } = new DuelHello();
        public DuelHello Them { get; private set; }
        /// <summary>Nom de l'adversaire, filtré (jamais vide).</summary>
        public string TheirName { get; private set; } = "";
        public OpponentView View => _view;
        public DuelSnapshot Opponent { get; private set; }
        public uint Seed { get; private set; }

        /// <summary>Compte à rebours restant (s).</summary>
        public float Countdown { get; private set; }
        /// <summary>Temps restant pour poser le dernier poids (s).</summary>
        public float LastWeightLeft { get; private set; }
        public float LastWeightTotal => T.finalWindowSeconds;

        float? _myFall, _theirFall;
        int _myScore, _theirScore;
        uint _mySurvivedMs, _theirSurvivedMs;
        bool _myFinished, _theirFinished, _forfeit, _iForfeit;
        float _sendTimer, _lastHeard, _waitTimer, _goRunTime;
        bool _runStarted;
        bool _wantRematch, _theyWantRematch;

        // Résultat
        public DuelOutcome Outcome { get; private set; }
        public int CoinsWon { get; private set; }
        public List<HeadDef> NewHeads { get; private set; } = new List<HeadDef>();
        public int MyScore => _myScore;
        public int TheirScore => _theirScore;
        public float MySeconds => _mySurvivedMs / 1000f;
        public float TheirSeconds => _theirSurvivedMs / 1000f;
        public bool ByForfeit => _forfeit;
        public bool TheyWantRematch => _theyWantRematch;
        public bool IWantRematch => _wantRematch;
        public bool TheyFell => _theirFall != null;
        public bool IFell => _myFall != null;

        // ------------------------------------------------------------------ Branchements (GameApp)

        /// <summary>Lance la partie de duel avec cette graine (GameApp.StartRun en mode Duel).</summary>
        public Action<uint> StartRun;
        /// <summary>Arrête proprement la partie gagnée (GameSession.FinishDuelRun).</summary>
        public Func<bool> FinishRun;
        /// <summary>Photo de ma partie (GameSession.CaptureDuel).</summary>
        public Action<DuelSnapshot> Capture;
        /// <summary>Bloque le lâcher pendant le compte à rebours.</summary>
        public Action<bool> LockInput;
        /// <summary>Temps et score de ma partie en cours.</summary>
        public Func<float> RunTime;
        public Func<int> Score;
        /// <summary>Changement de phase (écrans).</summary>
        public event Action Changed;
        /// <summary>Message à montrer (clé Loc).</summary>
        public event Action<string> Notice;
        /// <summary>Résultat prêt (l'écran de fin s'affiche, les pièces sont données par GameApp).</summary>
        public event Action ResultReady;
        /// <summary>L'adversaire a lâché : fenêtre du dernier poids.</summary>
        public event Action TheyFellEvent;

        DuelTuning T => _tuning().duel;

        public DuelController(IDuelNetwork net, Func<Tuning> tuning)
        {
            _net = net;
            _tuning = tuning;
            _net.PeerLinked += OnLinked;
            _net.PeerLost += OnPeerLost;
            _net.Received += OnReceived;
        }

        public void AttachView(OpponentView view) => _view = view;

        public bool InDuel => Current != Phase.None;
        /// <summary>Une partie de duel est en cours (de « 3 » jusqu'au résultat).</summary>
        public bool InMatch => Current == Phase.Countdown || Current == Phase.Playing || Current == Phase.LastWeight || Current == Phase.Waiting;

        // ------------------------------------------------------------------ Salle d'attente

        public async void Host(DuelHello me)
        {
            if (Current != Phase.None) return;
            Reset(me);
            Current = Phase.Connecting;
            Changed?.Invoke();
            string error = await _net.HostAsync();
            if (Current != Phase.Connecting) { if (error == null) _ = _net.LeaveAsync(); return; } // annulé entre-temps
            if (error != null) { Fail(error); return; }
            Current = Phase.Hosting;
            Changed?.Invoke();
        }

        public async void Join(DuelHello me, string code)
        {
            if (Current != Phase.None) return;
            code = DuelRules.NormalizeCode(code);
            if (string.IsNullOrEmpty(code)) { Notice?.Invoke("duel_invalid"); return; }
            Reset(me);
            Current = Phase.Connecting;
            Changed?.Invoke();
            string error = await _net.JoinAsync(code);
            if (Current != Phase.Connecting) { if (error == null) _ = _net.LeaveAsync(); return; }
            if (error != null) { Fail(error); return; }
            // Relié : on attend le départ donné par l'hôte.
            Current = Phase.Greeting;
            _lastHeard = Time.unscaledTime;
            Changed?.Invoke();
            if (_net.Linked) OnLinked();
        }

        void Reset(DuelHello me)
        {
            Me = me ?? new DuelHello();
            Them = null;
            TheirName = "";
            _wantRematch = _theyWantRematch = false;
            ResetMatch();
        }

        void ResetMatch()
        {
            _myFall = _theirFall = null;
            _myScore = _theirScore = 0;
            _mySurvivedMs = _theirSurvivedMs = 0;
            _myFinished = _theirFinished = _forfeit = _iForfeit = false;
            _runStarted = false;
            _goRunTime = 0f;
            _buffer.Clear();
            Opponent = null;
            NewHeads = new List<HeadDef>();
            CoinsWon = 0;
            LastWeightLeft = 0f;
        }

        void Fail(string errorKey)
        {
            Current = Phase.None;
            Changed?.Invoke();
            Notice?.Invoke(errorKey);
            _ = _net.LeaveAsync();
        }

        /// <summary>Quitte le duel (salle d'attente, résultat). Pendant une partie : abandon (défaite).</summary>
        public void Leave()
        {
            if (Current == Phase.None) return;
            if (InMatch && !_myFinished)
            {
                Forfeit();
                return;
            }
            if (_net.Linked) _net.Send(DuelProtocol.Simple(DuelMsg.Leave), true);
            Current = Phase.None;
            _view?.End();
            Changed?.Invoke();
            _ = _net.LeaveAsync();
        }

        /// <summary>J'abandonne la partie en cours : défaite immédiate, l'adversaire est prévenu.</summary>
        public void Forfeit()
        {
            if (!InMatch) return;
            _iForfeit = true;
            if (_net.Linked) _net.Send(DuelProtocol.Simple(DuelMsg.Leave), true);
            _myFall = _myFall ?? (RunTime != null ? RunTime() : 0f);
            _myScore = Score != null ? Score() : _myScore;
            _theirFall = null;
            _forfeit = true;
            Decide();
            _ = _net.LeaveAsync();
        }

        // ------------------------------------------------------------------ Réseau

        void OnLinked()
        {
            _lastHeard = Time.unscaledTime;
            if (Current == Phase.Hosting) { Current = Phase.Greeting; Changed?.Invoke(); }
            _net.Send(DuelProtocol.Hello(Me), true);
        }

        void OnPeerLost()
        {
            if (Current == Phase.None) return;
            if (Current == Phase.Results) { _theyWantRematch = false; Changed?.Invoke(); return; }
            Gone();
        }

        /// <summary>L'adversaire est parti (déconnexion, application quittée, temps dépassé).</summary>
        void Gone()
        {
            switch (Current)
            {
                case Phase.Connecting:
                case Phase.Hosting:
                case Phase.Greeting:
                    Fail("duel_lost_link");
                    break;
                case Phase.Countdown:
                case Phase.Playing:
                case Phase.LastWeight:
                case Phase.Waiting:
                    // Abandon : victoire, sauf si j'avais déjà lâché (le résultat ne change pas).
                    _forfeit = _theirFall == null && _myFall == null;
                    if (_myFall == null && _theirFall == null) _theirFall = RunTime != null ? RunTime() : 0f;
                    _theirFinished = true;
                    if (_forfeit) Notice?.Invoke("duel_they_left");
                    if (_myFinished) Decide();
                    // Ma partie s'arrête là (bras levés) ; si elle n'a pas encore commencé, on conclut tout de suite.
                    else if (_myFall == null && (FinishRun == null || !FinishRun())) { _myFinished = true; Decide(); }
                    break;
                case Phase.Results:
                    _theyWantRematch = false;
                    Changed?.Invoke();
                    break;
            }
        }

        void OnReceived(byte[] data)
        {
            // Un paquet abîmé ou venu d'une autre version ne doit jamais interrompre le duel : on l'ignore.
            if (data == null || data.Length == 0) return;
            try { Handle(data); }
            catch (System.Exception e) { Debug.LogWarning("[Bascule] Duel : paquet ignoré (" + e.Message + ")"); }
        }

        void Handle(byte[] data)
        {
            _lastHeard = Time.unscaledTime;
            switch (DuelProtocol.Kind(data))
            {
                case DuelMsg.Hello:
                    Them = DuelProtocol.ReadHello(data);
                    TheirName = DuelRules.CleanName(Them.Name, Loc.Get("duel_friend"));
                    if (IsHost && Current == Phase.Greeting) StartMatch(GameRng.RandomSeed());
                    else Changed?.Invoke();
                    break;
                case DuelMsg.Start:
                    if (IsHost) break;
                    DuelProtocol.ReadStart(data, out uint seed, out float countdown);
                    BeginCountdown(seed, countdown);
                    break;
                case DuelMsg.Snapshot:
                    if (!InMatch) break;
                    var s = DuelProtocol.ReadSnapshot(data);
                    if (s != null) _buffer.Add(s, Time.unscaledTime);
                    break;
                case DuelMsg.Fell:
                    if (!InMatch || _theirFall != null) break;
                    DuelProtocol.ReadFell(data, out uint fellMs, out int fellScore);
                    _theirFall = fellMs / 1000f;
                    _theirScore = fellScore;
                    float dir = Opponent != null && Opponent.Angle != 0f ? Mathf.Sign(Opponent.Angle) : 1f;
                    _view?.Defeat(dir);
                    if (_myFall == null && Current == Phase.Playing)
                    {
                        Current = Phase.LastWeight;
                        LastWeightLeft = T.finalWindowSeconds;
                        TheyFellEvent?.Invoke();
                        Changed?.Invoke();
                    }
                    break;
                case DuelMsg.Finished:
                    if (!InMatch) break;
                    DuelProtocol.ReadFinished(data, out int score, out uint survived);
                    _theirScore = score;
                    _theirSurvivedMs = survived;
                    _theirFinished = true;
                    if (_theirFall == null && _myFall != null) _view?.Cheer();
                    if (_myFinished) Decide();
                    break;
                case DuelMsg.Rematch:
                    _theyWantRematch = true;
                    if (Current == Phase.Results && _wantRematch && IsHost) StartMatch(GameRng.RandomSeed());
                    else Changed?.Invoke();
                    break;
                case DuelMsg.Leave:
                    if (Current == Phase.Results) { _theyWantRematch = false; Notice?.Invoke("duel_they_left"); Changed?.Invoke(); _ = _net.LeaveAsync(); }
                    else Gone();
                    break;
            }
        }

        void StartMatch(uint seed)
        {
            float countdown = Mathf.Max(1f, T.countdownSeconds);
            _net.Send(DuelProtocol.Start(seed, countdown), true);
            BeginCountdown(seed, countdown);
        }

        void BeginCountdown(uint seed, float countdown)
        {
            ResetMatch();
            _wantRematch = _theyWantRematch = false;
            Seed = seed;
            Countdown = countdown;
            Current = Phase.Countdown;
            _lastHeard = Time.unscaledTime;
            _view?.Begin(Them != null ? Them.Head : 0, Them != null ? Them.Plank : 0);
            Changed?.Invoke();
        }

        // ------------------------------------------------------------------ Ma partie

        /// <summary>Je viens de lâcher (début de la chute).</summary>
        public void OnLocalLoseStarted()
        {
            if (Current != Phase.Playing && Current != Phase.LastWeight) return;
            if (_myFall != null) return;
            _myFall = RunTime != null ? RunTime() : 0f;
            _myScore = Score != null ? Score() : 0;
            _net.Send(DuelProtocol.Fell((uint)Mathf.Max(0f, _myFall.Value * 1000f), _myScore), true);
        }

        /// <summary>Ma partie est terminée (chute finie, ou bras levés après le dernier poids).</summary>
        public void OnLocalRunEnded(RunResult r)
        {
            if (!InMatch || _myFinished) return;
            _myFinished = true;
            _myScore = r.Score;
            _mySurvivedMs = (uint)Mathf.Max(0f, ((_myFall ?? r.Seconds) - _goRunTime) * 1000f);
            _net.Send(DuelProtocol.Finished(_myScore, _mySurvivedMs), true);
            if (_theirFinished) { Decide(); return; }
            Current = Phase.Waiting;
            _waitTimer = 0f;
            Changed?.Invoke();
        }

        void Decide()
        {
            if (Current == Phase.Results) return;
            if (_iForfeit) Outcome = DuelOutcome.Lose;
            else if (_forfeit) Outcome = DuelOutcome.Win;
            else Outcome = DuelRules.Decide(_myFall, _theirFall, _myScore, _theirScore, T);
            if (_theirSurvivedMs == 0 && _theirFall != null) _theirSurvivedMs = (uint)Mathf.Max(0f, (_theirFall.Value - _goRunTime) * 1000f);
            if (_mySurvivedMs == 0) _mySurvivedMs = (uint)Mathf.Max(0f, ((_myFall ?? (RunTime != null ? RunTime() : 0f)) - _goRunTime) * 1000f);
            LockInput?.Invoke(false);
            Current = Phase.Results;
            ResultReady?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>Appelé par GameApp une fois les pièces données.</summary>
        public void SetRewards(int coins, List<HeadDef> heads) { CoinsWon = coins; NewHeads = heads ?? new List<HeadDef>(); }

        public void AskRematch()
        {
            if (Current != Phase.Results || _wantRematch) return;
            if (!_net.Linked) { Notice?.Invoke("duel_they_left"); return; }
            _wantRematch = true;
            _net.Send(DuelProtocol.Simple(DuelMsg.Rematch), true);
            if (_theyWantRematch && IsHost) StartMatch(GameRng.RandomSeed());
            else Changed?.Invoke();
        }

        // ------------------------------------------------------------------ Chaque image

        public void Tick(float dt)
        {
            _net.Tick();
            if (Current == Phase.None) return;
            float now = Time.unscaledTime;

            // Plus de nouvelles : l'adversaire est parti.
            bool linkedPhase = Current != Phase.Connecting && Current != Phase.Hosting && !(Current == Phase.Results);
            if (linkedPhase && now - _lastHeard > T.timeoutSeconds) { _lastHeard = now; Gone(); }

            switch (Current)
            {
                case Phase.Countdown:
                    Countdown -= dt;
                    // Basile entre avec sa planche pendant « 3… 2… » ; le premier poids attend « Partez ! ».
                    if (!_runStarted) { _runStarted = true; StartRun?.Invoke(Seed); LockInput?.Invoke(true); }
                    if (Countdown <= 0f)
                    {
                        Countdown = 0f;
                        _goRunTime = RunTime != null ? RunTime() : 0f;
                        LockInput?.Invoke(false);
                        Current = Phase.Playing;
                        Changed?.Invoke();
                    }
                    break;
                case Phase.LastWeight:
                    LastWeightLeft -= dt;
                    if (LastWeightLeft <= 0f && _myFall == null)
                    {
                        LastWeightLeft = 0f;
                        if (FinishRun == null || !FinishRun()) { _myFinished = true; _myScore = Score != null ? Score() : _myScore; Decide(); }
                    }
                    break;
                case Phase.Waiting:
                    _waitTimer += dt;
                    // L'adversaire a encore sa fenêtre du dernier poids, puis sa fin ; au-delà, on conclut avec ce qu'on sait.
                    if (_waitTimer > T.finalWindowSeconds + T.timeoutSeconds) { _theirFinished = true; Decide(); }
                    break;
            }

            if (InMatch && _runStarted)
            {
                // Mes photos, 20 par seconde (non fiables : une photo perdue est remplacée par la suivante).
                _sendTimer -= dt;
                if (_sendTimer <= 0f && Capture != null)
                {
                    _sendTimer += 1f / Mathf.Max(5, T.snapshotsPerSecond);
                    if (_sendTimer < 0f) _sendTimer = 0f;
                    Capture(_out);
                    _net.Send(DuelProtocol.Snapshot(_out), false);
                }
            }

            // Vue de l'adversaire, lissée.
            if (_view != null && (InMatch || Current == Phase.Results))
            {
                var s = _buffer.Sample(now);
                if (s != null)
                {
                    Opponent = s;
                    if (_theirFall == null) _theirScore = Mathf.Max(_theirScore, s.Score);
                    _view.Apply(s, dt);
                }
            }
        }

        /// <summary>L'application passe en arrière-plan pendant un duel : c'est un abandon (le temps ne s'arrête pas chez l'autre).</summary>
        public void OnAppPaused()
        {
            if (InMatch && !_myFinished) Forfeit();
        }

        /// <summary>Temps du duel depuis « Partez ! » (s), arrêté quand je lâche.</summary>
        public float Elapsed
        {
            get
            {
                if (Current == Phase.Countdown || Current == Phase.None) return 0f;
                if (Current == Phase.Results) return _mySurvivedMs / 1000f;
                float t = _myFall ?? (RunTime != null ? RunTime() : 0f);
                return Mathf.Max(0f, t - _goRunTime);
            }
        }

        /// <summary>L'adversaire est relié et répond (pastille « en direct »).</summary>
        public bool Live => _net.Linked && Time.unscaledTime - _lastHeard < 1.5f;
    }
}
