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
    ///    Au bout de 3 minutes (timeLimitSeconds), si personne n'a lâché : temps écoulé, les deux lèvent les bras et
    ///    le meilleur score gagne.
    /// 6. Chacun envoie son score final (Finished) ; le résultat est calculé de la même façon des deux côtés
    ///    (Core.DuelRules.Decide). Pièces pour les deux, plus pour le gagnant ; « Challenger » à 15 victoires.
    ///
    /// Robustesse : sans nouvelles pendant timeoutSeconds, l'adversaire est considéré comme parti (victoire par
    /// abandon si la partie était commencée). Rester hors de l'application plus longtemps que ce délai compte comme un
    /// abandon ; une courte sortie (Centre de contrôle, notification) non. Scénarios vérifiés sans téléphone par
    /// Tests/Bascule.Duel.Tests (deux contrôleurs reliés par un faux réseau : latence, pertes, 4G lente, pauses…).
    /// </summary>
    public sealed class DuelController
    {
        /// <summary>Searching (1.5) : duel classé, on cherche un adversaire (partie ouverte rejointe, ou la sienne qui attend).</summary>
        public enum Phase { None, Connecting, Hosting, Greeting, Countdown, Playing, LastWeight, Waiting, Results, Searching }

        readonly IDuelNetwork _net;
        readonly Func<Tuning> _tuning;
        readonly SnapshotBuffer _buffer = new SnapshotBuffer { Adaptive = true };
        readonly DuelSnapshot _out = new DuelSnapshot();
        readonly DuelSnapshot _in = new DuelSnapshot();
        OpponentView _view;

        public Phase Current { get; private set; }
        /// <summary>J'ai créé la partie (fixé dès « Défier un ami », sans attendre le réseau).</summary>
        public bool IsHost => _isHost;
        public bool Available => _net.Available;
        public string Code => _net.Code;
        public DuelHello Me { get; private set; } = new DuelHello();
        public DuelHello Them { get; private set; }
        /// <summary>Nom de l'adversaire, filtré (jamais vide).</summary>
        public string TheirName { get; private set; } = "";
        /// <summary>Tête de l'adversaire (salle d'attente, résultats).</summary>
        public int TheirHead => Them != null ? Them.Head : 0;
        public OpponentView View => _view;
        public DuelSnapshot Opponent { get; private set; }
        public uint Seed { get; private set; }

        /// <summary>Compte à rebours restant (s).</summary>
        public float Countdown { get; private set; }
        /// <summary>Temps restant pour poser le dernier poids (s).</summary>
        public float LastWeightLeft { get; private set; }
        public float LastWeightTotal => T.finalWindowSeconds;
        /// <summary>Temps restant avant la fin du duel (s).</summary>
        public float TimeLeft => DuelRules.TimeLeft(Elapsed, T);
        public bool HasTimeLimit => T.timeLimitSeconds > 0f;
        /// <summary>Le duel s'est terminé au temps (personne n'avait lâché) : le meilleur score gagne.</summary>
        public bool TimeUp { get; private set; }

        float? _myFall, _theirFall;
        int _myScore, _theirScore;
        uint _mySurvivedMs, _theirSurvivedMs;
        bool _myFinished, _theirFinished, _forfeit, _iForfeit;
        float _sendTimer, _lastHeard, _waitTimer, _goRunTime;
        bool _runStarted, _finishAsked;
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
        /// <summary>Partie créée (ou recréée) : code de session à publier sous le code ami et à renvoyer à l'ami invité.</summary>
        public event Action<string> Hosted;
        /// <summary>
        /// Code tapé ou reçu → code de session à rejoindre (un code ami renvoie au duel ouvert de cet ami) ; null : le code
        /// est déjà un code de session.
        /// </summary>
        public Func<string, System.Threading.Tasks.Task<string>> ResolveCode;
        string _joinInput;
        int _joinTries;
        bool _rejoining, _rehosting;
        /// <summary>Résultat prêt (l'écran de fin s'affiche, les pièces sont données par GameApp).</summary>
        public event Action ResultReady;
        /// <summary>L'adversaire a lâché : fenêtre du dernier poids.</summary>
        public event Action TheyFellEvent;
        /// <summary>Temps écoulé (3 minutes) : les deux parties s'arrêtent, le meilleur score gagne.</summary>
        public event Action TimeUpEvent;

        DuelTuning T => _tuning().duel;
        LeagueTuning L => _tuning().league;

        // ------------------------------------------------------------------ Duel classé (1.5)

        /// <summary>Duel classé : adversaire trouvé par le jeu (ou fantôme), qui rapporte des points de ligue. Pas de revanche.</summary>
        public bool Ranked { get; private set; }
        /// <summary>L'adversaire est un fantôme (la partie enregistrée d'un joueur).</summary>
        public bool VsGhost { get; private set; }
        /// <summary>J'ai abandonné ou quitté (aucun point).</summary>
        public bool IForfeited => _iForfeit;
        /// <summary>Recherche terminée sans personne (GameApp propose alors un fantôme).</summary>
        public event Action NoOpponent;
        /// <summary>Ma partie classée vient de finir : son enregistrement (à publier comme fantôme s'il est complet).</summary>
        public event Action<GhostRecording> Recorded;
        /// <summary>Tirage entre 0 et 1 (remplacé par une suite fixe dans les simulations).</summary>
        public Func<float> Rand = () => (float)_random.NextDouble();
        static readonly System.Random _random = new System.Random();
        readonly GhostRecorder _recorder = new GhostRecorder();
        float _searchStarted, _requeueAt, _guestSince = -1f;
        /// <summary>Temps de recherche écoulé (s) et temps maximal, pour l'écran d'attente.</summary>
        public float SearchElapsed => Current == Phase.Searching ? Time.unscaledTime - _searchStarted : 0f;
        public float SearchLimit => L.searchSeconds;

        /// <summary>« Duel classé » : cherche un adversaire, sans code. Au bout de searchSeconds sans personne : NoOpponent.</summary>
        public void FindMatch(DuelHello me)
        {
            if (Current != Phase.None) return;
            Reset(me);
            Ranked = true;
            _searchStarted = Time.unscaledTime;
            int op = ++_op;
            Current = Phase.Searching;
            Changed?.Invoke();
            _ = SearchNow(op);
        }

        /// <summary>
        /// Rejoint une partie classée qui attend, sinon ouvre la mienne. Dans la mienne, si personne ne vient avant un
        /// temps tiré au hasard, je la referme et je cherche à nouveau (Tick) : deux joueurs arrivés en même temps, chacun
        /// dans sa partie, finissent ainsi par se trouver.
        /// </summary>
        async System.Threading.Tasks.Task SearchNow(int op)
        {
            _isHost = false;
            _requeueAt = 0f;
            _guestSince = -1f;
            // Temps de recherche tiré au hasard : de deux joueurs arrivés ensemble, l'un ouvre sa partie avant l'autre,
            // qui la trouve en cherchant encore.
            string error = await _net.QuickMatchAsync(L.quickJoinSeconds * (0.6f + 1.4f * Mathf.Max(0f, Rand())));
            if (op != _op) return;
            if (error != null)
            {
                if (error == "duel_unavailable") { Fail(error); return; }
                // Serveur injoignable un instant : on réessaie tant que la recherche dure (Tick l'arrête à searchSeconds).
                await Delay(2000);
                if (op != _op || Current != Phase.Searching) return;
                await SearchNow(op);
                return;
            }
            if (Current != Phase.Searching) return;
            _isHost = _net.IsHost;
            if (_isHost)
            {
                _requeueAt = Time.unscaledTime + L.requeueMinSeconds + (L.requeueMaxSeconds - L.requeueMinSeconds) * Mathf.Max(0f, Rand());
                if (_net.Linked) OnLinked();
            }
            else
            {
                // Partie trouvée : on attend la liaison avec ce joueur, puis son départ.
                Current = Phase.Greeting;
                _lastHeard = Time.unscaledTime;
                Changed?.Invoke();
                if (_net.Linked && Them == null) OnLinked();
            }
        }

        /// <summary>
        /// J'attends dans ma partie ouverte. Si un autre joueur attend dans la sienne et qu'elle passe avant la mienne (même
        /// règle pour tous), je referme la mienne et je le rejoins ; sinon je reste, et lui viendra.
        /// </summary>
        async System.Threading.Tasks.Task CheckYield(int op)
        {
            bool yield = false;
            try { yield = await _net.ShouldYieldAsync(); }
            catch (Exception e) { Debug.LogWarning("[Bascule] Duel classé : " + e.Message); }
            if (op != _op || Current != Phase.Searching || !_isHost) return;
            if (_net.Linked || _net.HasGuest)
            {
                // Quelqu'un arrive : on regardera à nouveau s'il repart sans s'être relié.
                _requeueAt = Time.unscaledTime + L.requeueMaxSeconds;
                return;
            }
            if (yield) { int next = ++_op; await SearchNow(next); }
            else _requeueAt = Time.unscaledTime + L.requeueMinSeconds + (L.requeueMaxSeconds - L.requeueMinSeconds) * Mathf.Max(0f, Rand());
        }

        /// <summary>L'adversaire trouvé n'est finalement pas là (liaison jamais faite, ou perdue avant le départ) : on en cherche un autre.</summary>
        void SearchAgain()
        {
            Them = null;
            TheirName = "";
            _helloAt = -1f;
            int op = ++_op;
            Current = Phase.Searching;
            Changed?.Invoke();
            _ = SearchNow(op);
        }

        /// <summary>
        /// Duel classé contre un fantôme : la partie enregistrée d'un joueur, rejouée avec sa graine (mêmes poids et mêmes
        /// rafales pour moi, aux mêmes instants). Le fantôme tient le rôle de l'hôte : il se présente et donne le départ.
        /// </summary>
        public bool StartGhost(DuelHello me, GhostRecording ghost)
        {
            var link = _net as DuelLink;
            if (Current != Phase.None || ghost == null || link == null) return false;
            Reset(me);
            Ranked = true;
            VsGhost = true;
            ++_op;
            link.UseGhost(ghost, T.finalWindowSeconds);
            Current = Phase.Greeting;
            _lastHeard = Time.unscaledTime;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Ma partie classée est finie : l'enregistrement part (GameApp le publie s'il est complet).</summary>
        void EndRecording()
        {
            if (!_recorder.Active) return;
            float end = RunTime != null ? RunTime() : 0f;
            DuelSnapshot last = null;
            if (Capture != null) { Capture(_out); last = _out; }
            var rec = _recorder.End((uint)Mathf.Max(0f, end * 1000f), _myScore, _mySurvivedMs, last);
            if (rec != null) Recorded?.Invoke(rec);
        }

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

        /// <summary>
        /// Numéro de l'opération en cours (créer, rejoindre, recréer, quitter). Une réponse du réseau qui arrive pour une
        /// opération dépassée est ignorée : avant la 1.4.1, une ancienne tentative pouvait relancer ou fermer le duel suivant.
        /// La sortie demandée par l'opération suivante nettoie la partie de l'ancienne (les étapes réseau passent dans l'ordre).
        /// </summary>
        int _op;
        bool _isHost;
        /// <summary>Début de la phase « Connexion… » : au-delà de ConnectGuardSeconds, on abandonne avec un message.</summary>
        float _connectingSince, _joinStartedAt;
        /// <summary>Hôte : présentation de l'ami reçue (le départ est donné un instant après, le temps de voir son nom).</summary>
        float _helloAt = -1f;
        /// <summary>Dernier retour dans l'application (une coupure juste après vient de MON absence, pas de la sienne).</summary>
        float _resumedAt = -100f, _awayFor;

        /// <summary>« Connexion… » ne dure jamais plus que ça (relais injoignable, serveur qui ne répond pas).</summary>
        public const float ConnectGuardSeconds = 40f;
        /// <summary>L'invité réessaie pendant au plus ce temps (code ami pas encore publié, hôte qui recrée sa partie).</summary>
        public const float JoinBudgetSeconds = 45f;
        /// <summary>Délai entre la présentation de l'ami et le départ (on voit son nom et sa tête dans la salle).</summary>
        public const float StartDelaySeconds = 0.8f;

        /// <summary>Attente (ms) ; remplacée par une horloge virtuelle dans les tests de simulation (Tests/Bascule.Duel.Tests).</summary>
        public Func<int, System.Threading.Tasks.Task> Delay = ms => System.Threading.Tasks.Task.Delay(ms);

        void SetConnecting()
        {
            Current = Phase.Connecting;
            _connectingSince = Time.unscaledTime;
            Changed?.Invoke();
        }

        public async void Host(DuelHello me)
        {
            if (Current != Phase.None) return;
            Reset(me);
            _isHost = true;
            int op = ++_op;
            SetConnecting();
            string error = await _net.HostAsync();
            if (op != _op) return; // annulé ou remplacé entre-temps : la sortie déjà demandée nettoie cette partie
            if (error != null) { Fail(error); return; }
            if (Current == Phase.Connecting) Current = Phase.Hosting;
            Hosted?.Invoke(Code);
            Changed?.Invoke();
        }

        public void Join(DuelHello me, string code)
        {
            if (Current != Phase.None) return;
            code = DuelRules.NormalizeCode(code);
            if (string.IsNullOrEmpty(code)) { Notice?.Invoke("duel_invalid"); return; }
            Reset(me);
            _isHost = false;
            _joinInput = code;
            _joinTries = 0;
            _joinStartedAt = Time.unscaledTime;
            int op = ++_op;
            SetConnecting();
            _ = JoinNow(op);
        }

        /// <summary>Rejoint (ou rejoint à nouveau) la partie de l'ami : code ami → duel ouvert le plus récent.</summary>
        async System.Threading.Tasks.Task JoinNow(int op)
        {
            string session = _joinInput;
            if (ResolveCode != null)
            {
                try { var r = await ResolveCode(_joinInput); if (!string.IsNullOrEmpty(r)) session = r; }
                catch (Exception e) { Debug.LogWarning("[Bascule] Duel : code ami : " + e.Message); }
                if (op != _op) return;
            }
            if (Current != Phase.Connecting) return;
            string error = await _net.JoinAsync(session);
            // Seule l'opération en cours compte. Avant la 1.4.1, le départ de l'hôte, arrivé pendant que la connexion se
            // terminait (très rapide côte à côte sur le même Wi-Fi), faisait quitter la partie qui venait de commencer.
            if (op != _op) return;
            if (error != null)
            {
                // Erreur arrivée après le départ (la liaison marchait déjà) : la partie continue.
                if (InMatch || Current == Phase.Results) return;
                // L'hôte recrée peut-être sa partie (retour dans l'appli), ou les services se connectent encore : on réessaie.
                if ((error == "duel_join_failed" || error == "duel_offline") && TryRejoin()) return;
                Fail(error);
                return;
            }
            if (Current == Phase.Connecting)
            {
                // Partie rejointe : on attend la liaison avec l'hôte, puis son départ.
                Current = Phase.Greeting;
                _lastHeard = Time.unscaledTime;
                Changed?.Invoke();
            }
            if (_net.Linked && Them == null) OnLinked();
        }

        /// <summary>Invité : nouvelle tentative (au plus joinRetries, pendant JoinBudgetSeconds), en affichant « Connexion… ».</summary>
        bool TryRejoin()
        {
            if (_isHost || string.IsNullOrEmpty(_joinInput) || _joinTries >= Mathf.Max(0, T.joinRetries)) return false;
            if (Time.unscaledTime - _joinStartedAt > JoinBudgetSeconds) return false;
            _joinTries++;
            int op = ++_op;
            Them = null;
            TheirName = "";
            SetConnecting();
            _ = Rejoin(op, RetryDelayMs(_joinTries));
            return true;
        }

        /// <summary>Attente avant la tentative n (laisse à l'hôte le temps de recréer sa partie) : 2,5 s, 4 s, puis 6 s.</summary>
        public static int RetryDelayMs(int attempt) => attempt <= 1 ? 2500 : attempt == 2 ? 4000 : 6000;

        async System.Threading.Tasks.Task Rejoin(int op, int delayMs)
        {
            _rejoining = true;
            try
            {
                await _net.LeaveAsync();
                await Delay(delayMs);
            }
            finally { if (op == _op) _rejoining = false; }
            if (op != _op || Current != Phase.Connecting) return;
            await JoinNow(op);
        }

        /// <summary>
        /// Retour dans l'application. Pendant le duel : une courte absence (Centre de contrôle, notification) ne compte plus
        /// comme un abandon ; au-delà du délai de l'adversaire, il a déjà gagné et on le reconnaît. Dans la salle d'attente,
        /// après plus de quelques secondes ailleurs (envoyer le code par Snap ou Messages), le relais a pu oublier la
        /// partie : l'hôte la recrée (le même code ami y mène), l'invité qui n'était pas encore relié réessaie.
        /// </summary>
        public async void OnAppResumed(float pausedSeconds)
        {
            float now = Time.unscaledTime;
            _resumedAt = now;
            _awayFor = pausedSeconds;
            if (Current == Phase.None) return;
            if (InMatch)
            {
                if (!_myFinished && pausedSeconds >= Mathf.Max(1f, T.timeoutSeconds - 1f)) { Forfeit(); return; }
                _lastHeard = now; // le temps passé dehors ne fait pas croire que l'adversaire est parti
                return;
            }
            _lastHeard = now;
            if (Current == Phase.Results) return;
            if (Ranked && !VsGhost)
            {
                // Avant le départ d'un duel classé, sortir de l'appli annule la mise en relation (OnAppPaused a déjà refermé
                // ma partie) : on repart sur une recherche neuve, même si la liaison semblait encore là. Sinon un joueur
                // revenu trop tard jouait seul contre un adversaire qui avait déjà gagné « par abandon ».
                if (Current == Phase.Searching || Current == Phase.Greeting) { _searchStarted = now; SearchAgain(); }
                return;
            }
            if (_net.Linked) return;
            if (_isHost && Current == Phase.Hosting)
            {
                if (pausedSeconds < 3f) return;
                int op = _op;
                // Moins de 10 s : le relais garde souvent la partie. On regarde son état un instant plus tard.
                if (pausedSeconds <= 10f)
                {
                    await Delay(1500);
                    if (op != _op || Current != Phase.Hosting || _net.Linked || _net.RelayAlive) return;
                }
                await Rehost(0);
            }
            else if (!_isHost && Current == Phase.Greeting && Them == null && pausedSeconds >= 3f)
            {
                // Pas encore relié à l'hôte (une tentative en cours, « Connexion… », continue d'elle-même).
                _joinTries = 0;
                _joinStartedAt = now;
                TryRejoin();
            }
        }

        /// <summary>L'hôte recrée sa partie (nouveau code de session, publié sous son code ami et renvoyé à l'ami invité).</summary>
        async System.Threading.Tasks.Task Rehost(int attempt)
        {
            int op = ++_op;
            _rehosting = true;
            SetConnecting();
            string error;
            try { error = await _net.HostAsync(); }
            finally { if (op == _op) _rehosting = false; }
            if (op != _op) return;
            if (error != null)
            {
                if (attempt == 0)
                {
                    await Delay(2000);
                    if (op == _op && Current == Phase.Connecting) await Rehost(1);
                    return;
                }
                Fail(error);
                return;
            }
            if (Current == Phase.Connecting) Current = Phase.Hosting;
            Hosted?.Invoke(Code);
            Changed?.Invoke();
        }

        void Reset(DuelHello me)
        {
            Me = me ?? new DuelHello();
            Them = null;
            TheirName = "";
            _wantRematch = _theyWantRematch = false;
            _rejoining = _rehosting = false;
            _helloAt = -1f;
            _joinInput = ""; // jamais de nouvel essai vers le code d'un duel précédent
            Ranked = false;
            VsGhost = false;
            _isHost = false;
            _requeueAt = 0f;
            ResetMatch();
        }

        void ResetMatch()
        {
            _myFall = _theirFall = null;
            _myScore = _theirScore = 0;
            _mySurvivedMs = _theirSurvivedMs = 0;
            _myFinished = _theirFinished = _forfeit = _iForfeit = _finishAsked = false;
            TimeUp = false;
            _recorder.Cancel();
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
            _op++;
            Current = Phase.None;
            _rejoining = _rehosting = false;
            Changed?.Invoke();
            Notice?.Invoke(errorKey);
            _ = _net.LeaveAsync();
        }

        /// <summary>Quitte le duel (salle d'attente, résultat). Pendant une partie : abandon (défaite), sauf si j'avais fini.</summary>
        public void Leave()
        {
            if (Current == Phase.None) return;
            if (InMatch)
            {
                Forfeit();
                return;
            }
            _op++;
            if (_net.Linked) _net.Send(DuelProtocol.Simple(DuelMsg.Leave), true);
            Current = Phase.None;
            _rejoining = _rehosting = false;
            _view?.End();
            Changed?.Invoke();
            _ = _net.LeaveAsync();
        }

        /// <summary>
        /// Je quitte la partie en cours : défaite immédiate, l'adversaire est prévenu. Si ma partie était déjà finie
        /// (j'attendais son score), partir ne change pas le résultat : avant la 1.4.1, les deux joueurs perdaient.
        /// </summary>
        public void Forfeit()
        {
            if (!InMatch) return;
            _op++;
            if (_net.Linked) _net.Send(DuelProtocol.Simple(DuelMsg.Leave), true);
            if (_myFinished)
            {
                _theirFinished = true;
                Decide();
            }
            else
            {
                _iForfeit = true;
                _recorder.Cancel();
                _myFall = _myFall ?? (RunTime != null ? RunTime() : 0f);
                _myScore = Score != null ? Score() : _myScore;
                _theirFall = null;
                _forfeit = true;
                Decide();
            }
            _ = _net.LeaveAsync();
        }

        // ------------------------------------------------------------------ Réseau

        void OnLinked()
        {
            _lastHeard = Time.unscaledTime;
            if (Current == Phase.Hosting || (Current == Phase.Searching && _isHost)) { Current = Phase.Greeting; Changed?.Invoke(); }
            _net.Send(DuelProtocol.Hello(Me), true);
        }

        void OnPeerLost()
        {
            // Pendant qu'on recrée ou rejoint à nouveau la partie, l'ancienne session signale son départ : on l'ignore.
            if (Current == Phase.None || _rejoining || _rehosting) return;
            if (Current == Phase.Results) { _theyWantRematch = false; Changed?.Invoke(); return; }
            Gone();
        }

        /// <summary>Une coupure juste après mon retour dans l'appli vient de mon absence (le relais m'a oublié).</summary>
        bool JustCameBack => _awayFor >= 1f && Time.unscaledTime - _resumedAt < 3f;

        /// <summary>L'adversaire est parti (déconnexion, application quittée, temps dépassé).</summary>
        void Gone()
        {
            switch (Current)
            {
                case Phase.Searching:
                    // Recherche en cours : une ancienne partie qui se referme ne change rien.
                    break;
                case Phase.Hosting:
                    // L'hôte attend son ami : un essai raté de l'ami ne ferme pas la partie, il peut revenir.
                    if (_isHost) break;
                    Fail("duel_lost_link");
                    break;
                case Phase.Connecting:
                case Phase.Greeting:
                    // Duel classé : l'adversaire trouvé a disparu avant le départ, on en cherche un autre.
                    if (Ranked && !VsGhost && Current == Phase.Greeting) { SearchAgain(); break; }
                    if (_isHost)
                    {
                        // Création en cours : le garde-fou de « Connexion… » s'en occupe.
                        if (Current == Phase.Connecting) break;
                        // Liaison perdue avant le départ : l'hôte se remet à attendre (même partie, même code).
                        Them = null;
                        TheirName = "";
                        _helloAt = -1f;
                        Current = Phase.Hosting;
                        Changed?.Invoke();
                        break;
                    }
                    // Invité pas encore parti : l'hôte attend encore (ou recrée sa partie) ; on réessaie.
                    if (TryRejoin()) break;
                    Fail("duel_lost_link");
                    break;
                case Phase.Countdown:
                case Phase.Playing:
                case Phase.LastWeight:
                case Phase.Waiting:
                    // C'est moi qui suis parti (appli en arrière-plan, le relais m'a oublié) : l'adversaire gagne, pas moi.
                    if (JustCameBack && !_myFinished) { Forfeit(); break; }
                    // Il avait fini (son score est arrivé) : son départ ne change rien, on conclut normalement.
                    bool theyHadFinished = _theirFinished;
                    // Abandon : victoire, sauf si j'avais déjà lâché (le résultat ne change pas).
                    _forfeit = !theyHadFinished && _theirFall == null && _myFall == null;
                    if (_forfeit) _theirFall = RunTime != null ? RunTime() : 0f;
                    _theirFinished = true;
                    _op++;
                    if (_forfeit) Notice?.Invoke("duel_they_left");
                    if (_myFinished) Decide();
                    // Ma partie s'arrête là (bras levés) ; si elle n'a pas encore commencé, on conclut tout de suite.
                    else if (_myFall == null && (FinishRun == null || !FinishRun())) { _myFinished = true; _myScore = Score != null ? Score() : _myScore; Decide(); }
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
                    TheirName = DuelRules.CleanName(Them.Name, Loc.Get(Ranked ? "ranked_opponent" : "duel_friend"));
                    // Hôte : la présentation de l'ami suffit à passer aux présentations, même si l'annonce de la liaison
                    // s'est perdue (ami reconnecté) ; le départ est donné un instant après (Tick).
                    if (_isHost && (Current == Phase.Hosting || Current == Phase.Greeting || Current == Phase.Searching))
                    {
                        Current = Phase.Greeting;
                        if (_helloAt < 0f) _helloAt = Time.unscaledTime;
                    }
                    Changed?.Invoke();
                    break;
                case DuelMsg.Start:
                    if (_isHost || InMatch) break;
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
                    // Il a fini sans lâcher alors que je joue encore, à la fin des 3 minutes : son temps est écoulé
                    // (les deux horloges ont quelques centièmes d'écart). Le mien aussi.
                    if (_theirFall == null && _myFall == null && Current == Phase.Playing && HasTimeLimit && TimeLeft <= 1.5f) EndByTime();
                    if (_myFinished) Decide();
                    break;
                case DuelMsg.Rematch:
                    if (Ranked) break; // pas de revanche en classé : un autre adversaire à chaque duel
                    _theyWantRematch = true;
                    if (Current == Phase.Results && _wantRematch && _isHost) StartMatch(GameRng.RandomSeed());
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
            _helloAt = -1f;
            // Ma présentation d'abord (l'ami ne l'a peut-être pas reçue s'il s'est reconnecté), puis le départ.
            _net.Send(DuelProtocol.Hello(Me), true);
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
            // Duel classé : ma partie est enregistrée (elle deviendra peut-être mon fantôme).
            if (Ranked) _recorder.Begin(seed, countdown, Me, L.ghostSnapshotsPerSecond);
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
            if (_recorder.Active)
            {
                DuelSnapshot at = null;
                if (Capture != null) { Capture(_out); at = _out; }
                _recorder.Fell((uint)Mathf.Max(0f, _myFall.Value * 1000f), _myScore, at);
            }
        }

        /// <summary>Ma partie est terminée (chute finie, ou bras levés après le dernier poids).</summary>
        public void OnLocalRunEnded(RunResult r)
        {
            if (!InMatch || _myFinished) return;
            _myFinished = true;
            _myScore = r.Score;
            _mySurvivedMs = (uint)Mathf.Max(0f, ((_myFall ?? r.Seconds) - _goRunTime) * 1000f);
            _net.Send(DuelProtocol.Finished(_myScore, _mySurvivedMs), true);
            EndRecording();
            if (_theirFinished) { Decide(); return; }
            Current = Phase.Waiting;
            _waitTimer = 0f;
            Changed?.Invoke();
        }

        /// <summary>
        /// Temps écoulé sans que j'aie lâché : ma partie s'arrête (bras levés) et j'envoie mon score. Le résultat se
        /// décide au score dès que celui de l'adversaire arrive (DuelRules.Decide sans chute).
        /// </summary>
        void EndByTime()
        {
            if (TimeUp || _myFall != null || _myFinished) return;
            TimeUp = true;
            TimeUpEvent?.Invoke();
            if (FinishRun != null && FinishRun()) return; // OnLocalRunEnded envoie le score
            _myFinished = true;
            _myScore = Score != null ? Score() : _myScore;
            _mySurvivedMs = (uint)Mathf.Max(0f, Elapsed * 1000f);
            _net.Send(DuelProtocol.Finished(_myScore, _mySurvivedMs), true);
            EndRecording();
            if (_theirFinished) { Decide(); return; }
            Current = Phase.Waiting;
            _waitTimer = 0f;
            Changed?.Invoke();
        }

        void Decide()
        {
            if (Current == Phase.Results) return;
            _recorder.Cancel(); // partie conclue autrement (abandon, adversaire parti) : pas de fantôme
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
            if (Current != Phase.Results || _wantRematch || Ranked) return;
            if (!_net.Linked) { Notice?.Invoke("duel_they_left"); return; }
            _wantRematch = true;
            _net.Send(DuelProtocol.Simple(DuelMsg.Rematch), true);
            if (_theyWantRematch && _isHost) StartMatch(GameRng.RandomSeed());
            else Changed?.Invoke();
        }

        // ------------------------------------------------------------------ Chaque image

        public void Tick(float dt)
        {
            _net.Tick();
            if (Current == Phase.None) return;
            float now = Time.unscaledTime;

            // « Connexion… » bloquée (serveur qui ne répond pas) : on le dit au lieu d'attendre sans fin.
            if (Current == Phase.Connecting && now - _connectingSince > ConnectGuardSeconds)
            {
                Fail(_isHost ? "duel_create_failed" : "duel_join_failed");
                return;
            }
            // Duel classé : recherche d'un adversaire.
            if (Current == Phase.Searching)
            {
                // Quelqu'un vient d'entrer dans ma partie : on lui laisse le temps de se relier (pas de fermeture ni
                // d'abandon de la recherche pendant ce temps).
                bool guest = _isHost && _net.HasGuest;
                if (!guest) _guestSince = -1f;
                else if (_guestSince < 0f) _guestSince = now;
                bool arriving = guest && now - _guestSince < L.linkSeconds;
                // Entré dans ma partie mais jamais relié : il occupe la place pour rien, on repart sur une partie neuve.
                if (guest && !arriving && !_net.Linked && now - _searchStarted <= L.searchSeconds) { SearchAgain(); return; }
                if (now - _searchStarted > L.searchSeconds && !arriving)
                {
                    // Personne : on arrête, GameApp propose un fantôme.
                    _op++;
                    Current = Phase.None;
                    _ = _net.LeaveAsync();
                    Changed?.Invoke();
                    NoOpponent?.Invoke();
                    return;
                }
                if (_isHost && _requeueAt > 0f && now >= _requeueAt && !_net.Linked && !guest)
                {
                    // Personne n'est venu dans ma partie : un autre joueur attend-il dans la sienne ?
                    _requeueAt = 0f;
                    _ = CheckYield(_op);
                }
                return;
            }
            // Hôte : l'ami s'est présenté, départ un instant après.
            if (_isHost && Current == Phase.Greeting && Them != null)
            {
                if (_helloAt < 0f) _helloAt = now;
                else if (now - _helloAt >= StartDelaySeconds) StartMatch(GameRng.RandomSeed());
            }

            // Plus de nouvelles : l'adversaire est parti.
            bool linkedPhase = Current != Phase.Connecting && Current != Phase.Hosting && !(Current == Phase.Results);
            // Tant que l'ami ne s'est pas présenté, la liaison par le relais peut prendre plusieurs secondes (4G).
            float limit = Current == Phase.Greeting && Them == null ? (Ranked ? L.linkSeconds : Mathf.Max(T.timeoutSeconds, T.connectSeconds)) : T.timeoutSeconds;
            if (linkedPhase && now - _lastHeard > limit) { _lastHeard = now; Gone(); }

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
                case Phase.Playing:
                    if (_myFall == null && DuelRules.TimeUp(Elapsed, T)) EndByTime();
                    break;
                case Phase.LastWeight:
                    LastWeightLeft -= dt;
                    // Une seule demande de fin (bras levés) : avant la 1.4.1, elle était refaite à l'image suivante, qui
                    // concluait sans attendre la fin de ma partie.
                    if (LastWeightLeft <= 0f && _myFall == null && !_finishAsked)
                    {
                        LastWeightLeft = 0f;
                        _finishAsked = true;
                        if (FinishRun == null || !FinishRun())
                        {
                            // Ma partie ne peut pas lever les bras (déjà arrêtée) : j'envoie quand même mon score, sinon
                            // l'adversaire attendrait jusqu'à son délai.
                            _myFinished = true;
                            _myScore = Score != null ? Score() : _myScore;
                            _mySurvivedMs = (uint)Mathf.Max(0f, Elapsed * 1000f);
                            _net.Send(DuelProtocol.Finished(_myScore, _mySurvivedMs), true);
                            Decide();
                        }
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
                    if (_recorder.Active && !_myFinished) _recorder.Add(_out);
                }
            }

            // Vue de l'adversaire, lissée.
            if (_view != null && (InMatch || Current == Phase.Results))
            {
                var s = _buffer.Sample(now);
                if (s != null)
                {
                    Opponent = s;
                    // Son score en direct (le dernier reçu) ; figé dès qu'il a lâché ou fini (score exact reçu).
                    if (_theirFall == null && !_theirFinished && Current != Phase.Results) _theirScore = s.Score;
                    _view.Apply(s, dt);
                }
            }
        }

        /// <summary>
        /// L'application passe en arrière-plan pendant un duel. Plus d'abandon immédiat (1.4.1) : tirer le Centre de
        /// contrôle ou toucher une notification ne fait plus perdre. Ma partie est figée, celle de l'adversaire continue ;
        /// si je reste parti plus que son délai (timeoutSeconds), il gagne et je le reconnais au retour (OnAppResumed).
        /// </summary>
        public void OnAppPaused()
        {
            _lastHeard = Time.unscaledTime;
            // Duel classé pas encore parti : ma partie ouverte quitte la file tout de suite (sinon un autre joueur la
            // rejoindrait et attendrait pour rien). La recherche repart au retour (OnAppResumed).
            if (Ranked && !VsGhost && (Current == Phase.Searching || Current == Phase.Greeting))
            {
                _op++;
                _requeueAt = 0f;
                _ = _net.LeaveAsync();
            }
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
