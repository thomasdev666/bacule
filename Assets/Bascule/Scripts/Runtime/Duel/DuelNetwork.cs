using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Bascule.Duel
{
    /// <summary>
    /// Lien réseau d'un duel : créer une partie (un code), la rejoindre avec ce code, échanger des messages.
    /// Deux sortes d'envoi : fiable (événements : début, chute, fin, revanche) et non fiable (photos 15/s :
    /// une photo perdue est remplacée par la suivante, inutile de la renvoyer).
    /// </summary>
    public interface IDuelNetwork
    {
        /// <summary>Le paquet réseau est installé (sinon le duel affiche « bientôt »).</summary>
        bool Available { get; }
        bool IsHost { get; }
        /// <summary>Code à donner à l'ami (hôte), une fois la partie créée.</summary>
        string Code { get; }
        /// <summary>Les deux téléphones sont reliés.</summary>
        bool Linked { get; }
        /// <summary>La liaison avec le relais est encore valable (après un passage en arrière-plan, elle peut avoir expiré).</summary>
        bool RelayAlive { get; }
        /// <summary>Un autre joueur est entré dans ma partie (la liaison directe peut encore être en train de se faire).</summary>
        bool HasGuest { get; }
        event Action PeerLinked;
        event Action PeerLost;
        event Action<byte[]> Received;
        /// <summary>Crée une partie privée pour deux ; renvoie null si c'est réussi, sinon la clé du message d'erreur.</summary>
        Task<string> HostAsync();
        /// <summary>Rejoint la partie d'un ami ; renvoie null si c'est réussi, sinon la clé du message d'erreur.</summary>
        Task<string> JoinAsync(string code);
        /// <summary>
        /// Duel classé : rejoint une partie ouverte qui attend un adversaire (cherchée pendant <paramref name="joinSeconds"/>),
        /// sinon ouvre la sienne. IsHost dit ensuite lequel des deux. Null si c'est réussi, sinon la clé du message d'erreur.
        /// </summary>
        Task<string> QuickMatchAsync(float joinSeconds);
        /// <summary>
        /// J'attends dans ma partie classée ouverte : une AUTRE partie ouverte passe-t-elle avant la mienne ? Si oui, je
        /// referme la mienne et je la rejoins. La règle est la même pour tous (ordre des identifiants) : de deux joueurs
        /// qui attendent chacun dans sa partie, un seul cède, et ils se trouvent.
        /// </summary>
        Task<bool> ShouldYieldAsync();
        void Send(byte[] data, bool reliable);
        /// <summary>À appeler à chaque image (réception, connexions).</summary>
        void Tick();
        Task LeaveAsync();
    }

    /// <summary>Sans le paquet Multiplayer Services : le duel en temps réel n'est pas disponible.</summary>
    public sealed class OfflineDuelNetwork : IDuelNetwork
    {
        public bool Available => false;
        public bool IsHost => false;
        public string Code => "";
        public bool Linked => false;
        public bool RelayAlive => false;
        public bool HasGuest => false;
        public event Action PeerLinked { add { } remove { } }
        public event Action PeerLost { add { } remove { } }
        public event Action<byte[]> Received { add { } remove { } }
        public Task<string> HostAsync() => Task.FromResult("duel_unavailable");
        public Task<string> JoinAsync(string code) => Task.FromResult("duel_unavailable");
        public Task<string> QuickMatchAsync(float joinSeconds) => Task.FromResult("duel_unavailable");
        public Task<bool> ShouldYieldAsync() => Task.FromResult(false);
        public void Send(byte[] data, bool reliable) { }
        public void Tick() { }
        public Task LeaveAsync() => Task.CompletedTask;
    }

    /// <summary>
    /// Réseau du duel vu par le contrôleur : le vrai (relais de Unity) ou, pour un duel contre un fantôme, la partie
    /// enregistrée d'un joueur rejouée comme s'il était là (Core.GhostPlayback). Le contrôleur ne voit pas la différence :
    /// mêmes messages, mêmes règles.
    /// </summary>
    public sealed class DuelLink : IDuelNetwork
    {
        readonly IDuelNetwork _real;
        Bascule.Core.GhostPlayback _ghost;
        readonly System.Collections.Generic.List<byte[]> _out = new System.Collections.Generic.List<byte[]>();
        float _lastTick, _lastRun;
        bool _ghostLinked, _mineOver;

        /// <summary>
        /// Temps de MA partie (GameSession.RunTime). Le fantôme avance avec lui, pas avec l'horloge du téléphone : le temps
        /// de partie ne tourne pas pendant l'arrivée de Basile, ralentit pendant un sauvetage et s'arrête si l'appli passe
        /// en arrière-plan. Calé dessus, le fantôme est toujours au même instant de partie que moi : mêmes poids, mêmes
        /// rafales, et sa chute tombe au bon moment. Une fois ma partie finie (temps figé), il continue au temps réel.
        /// </summary>
        public Func<float> RunTime;

        public DuelLink(IDuelNetwork real)
        {
            _real = real;
            _real.PeerLinked += () => { if (_ghost == null) PeerLinked?.Invoke(); };
            _real.PeerLost += () => { if (_ghost == null) PeerLost?.Invoke(); };
            _real.Received += d => { if (_ghost == null) Received?.Invoke(d); };
        }

        /// <summary>Un duel contre un fantôme est en cours.</summary>
        public bool Ghost => _ghost != null;
        public bool Available => _real.Available;
        public bool IsHost => _ghost == null && _real.IsHost;
        public string Code => _ghost == null ? _real.Code : "";
        public bool Linked => _ghost != null ? _ghostLinked : _real.Linked;
        public bool RelayAlive => _ghost != null || _real.RelayAlive;
        public bool HasGuest => _ghost == null && _real.HasGuest;
        public event Action PeerLinked;
        public event Action PeerLost;
        public event Action<byte[]> Received;

        /// <summary>Commence un duel contre ce fantôme (la liaison « arrive » à la prochaine image).</summary>
        public void UseGhost(Bascule.Core.GhostRecording recording, float finalWindowSeconds)
        {
            _ghost = new Bascule.Core.GhostPlayback(recording, finalWindowSeconds);
            _ghostLinked = false;
            _mineOver = false;
            _lastTick = Time.unscaledTime;
            _lastRun = RunTime != null ? RunTime() : 0f;
        }

        public Task<string> HostAsync() { _ghost = null; return _real.HostAsync(); }
        public Task<string> JoinAsync(string code) { _ghost = null; return _real.JoinAsync(code); }
        public Task<string> QuickMatchAsync(float joinSeconds) { _ghost = null; return _real.QuickMatchAsync(joinSeconds); }
        public Task<bool> ShouldYieldAsync() => _ghost != null ? Task.FromResult(false) : _real.ShouldYieldAsync();

        public void Send(byte[] data, bool reliable)
        {
            if (_ghost == null) { _real.Send(data, reliable); return; }
            // Le fantôme n'écoute qu'une chose : ma chute (il lève alors les bras après la fenêtre du dernier poids).
            var kind = Bascule.Core.DuelProtocol.Kind(data);
            if (kind == Bascule.Core.DuelMsg.Fell) _ghost.PlayerFell();
            // Ma partie est finie : son temps ne tourne plus, le fantôme continue au temps réel.
            if (kind == Bascule.Core.DuelMsg.Fell || kind == Bascule.Core.DuelMsg.Finished) _mineOver = true;
        }

        public void Tick()
        {
            if (_ghost == null) { _real.Tick(); return; }
            float now = Time.unscaledTime;
            float real = Mathf.Max(0f, now - _lastTick);
            _lastTick = now;
            float run = RunTime != null ? RunTime() : 0f;
            // Tant que je joue : le fantôme avance du temps de MA partie (0 si elle n'a pas avancé, ou si elle vient de
            // repartir de zéro). Ensuite : temps réel, sans saut (retour d'arrière-plan).
            float dt = _mineOver || RunTime == null ? Mathf.Min(0.25f, real) : Mathf.Max(0f, run - _lastRun);
            _lastRun = run;
            if (!_ghostLinked) { _ghostLinked = true; PeerLinked?.Invoke(); return; }
            _out.Clear();
            var ghost = _ghost;
            ghost.Advance(dt, _out);
            foreach (var packet in _out)
            {
                if (_ghost != ghost) break; // duel quitté pendant la remise des messages
                Received?.Invoke(packet);
            }
        }

        public Task LeaveAsync()
        {
            if (_ghost != null) { _ghost = null; _ghostLinked = false; return Task.CompletedTask; }
            return _real.LeaveAsync();
        }
    }

#if BASCULE_MULTIPLAYER
    /// <summary>
    /// Duel par Unity Gaming Services : une « session » privée pour 2 (Multiplayer Services) donne le code ; le relais
    /// de Unity relie les deux téléphones à travers internet (pas besoin d'être sur le même Wi-Fi) ; nos propres
    /// messages passent par Unity Transport (pas de Netcode : on n'envoie que ce dont on a besoin, très léger).
    /// Mise en place : docs/INTEGRATIONS.md, section « Duel ».
    /// </summary>
    public sealed class UgsDuelNetwork : IDuelNetwork
    {
        Unity.Services.Multiplayer.ISession _session;
        UtpLink _link;

        public bool Available => true;
        public bool IsHost { get; private set; }
        public string Code { get; private set; } = "";
        public bool Linked => _link != null && _link.Linked;
        public bool RelayAlive => _link != null && _link.RelayAlive;
        public bool HasGuest => Linked || (_session != null && _session.PlayerCount > 1);
        public event Action PeerLinked;

        /// <summary>
        /// Créer, rejoindre et quitter passent UN PAR UN, dans l'ordre : avant la 1.4.1, une sortie encore en cours (lien
        /// d'invitation touché depuis la salle d'attente) détruisait la liaison de la partie suivante, qui n'arrivait jamais.
        /// </summary>
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
        public event Action PeerLost;
        public event Action<byte[]> Received;

        /// <summary>
        /// Connexion aux services de Unity. Au lancement par un lien d'invitation, le classement se connecte en même temps :
        /// on attend qu'il ait fini au lieu d'échouer (« Connecte-toi à internet » alors qu'on l'est).
        /// </summary>
        static async Task<string> SignInAsync()
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized)
                        await Unity.Services.Core.UnityServices.InitializeAsync();
                    var auth = Unity.Services.Authentication.AuthenticationService.Instance;
                    if (!auth.IsSignedIn) await auth.SignInAnonymouslyAsync();
                    return null;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Bascule] Duel : connexion (essai " + (attempt + 1) + ") : " + e.Message);
                    for (int k = 0; k < 16; k++)
                    {
                        await Task.Delay(250);
                        if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized
                            && Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn) return null;
                    }
                }
            }
            return "duel_offline";
        }

        UtpLink NewLink()
        {
            var link = new UtpLink();
            link.OnLinked += () => PeerLinked?.Invoke();
            link.OnLost += () => PeerLost?.Invoke();
            link.OnReceived += d => Received?.Invoke(d);
            return link;
        }

        public Task<string> HostAsync() => Serial(HostNow);

        async Task<string> HostNow()
        {
            await LeaveNow();
            var err = await SignInAsync();
            if (err != null) return err;
            try
            {
                _link = NewLink();
                Unity.Services.Multiplayer.ISession session = null;
                // 1.4.1 : liaison WebSocket sécurisée (WSS, port 443) d'abord : elle passe sur tous les réseaux, y compris
                // la 4G des opérateurs (souvent en IPv6 seulement, où l'UDP vers une adresse IPv4 échoue) et les Wi-Fi qui
                // filtrent l'UDP. Un joueur en Wi-Fi et l'autre en 4G : ça ne marchait pas en 1.4. Puis repli.
                var tries = new (Unity.Services.Multiplayer.RelayProtocol protocol, string region)[]
                {
                    (Unity.Services.Multiplayer.RelayProtocol.WSS, RelayRegion),
                    (Unity.Services.Multiplayer.RelayProtocol.WSS, null),
                    (Unity.Services.Multiplayer.RelayProtocol.Default, null),
                };
                Exception last = null;
                foreach (var (protocol, region) in tries)
                {
                    try { session = await Create(protocol, region); break; }
                    catch (Exception e)
                    {
                        last = e;
                        Debug.LogWarning("[Bascule] Duel : relais " + protocol + " " + (region ?? "auto") + " indisponible : " + e.Message);
                        _link.Dispose();
                        _link = NewLink();
                    }
                }
                if (session == null) throw last ?? new Exception("relais indisponible");
                Watch(session);
                IsHost = true;
                Code = session.Code ?? "";
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Bascule] Duel : création impossible : " + e.Message);
                await LeaveNow();
                return "duel_create_failed";
            }
        }

        /// <summary>
        /// Région du relais : le jeu est publié en France, les deux joueurs sont presque toujours en Europe. La donner
        /// d'office évite la mesure de toutes les régions du monde (QoS) à chaque duel : le code arrive plus vite.
        /// </summary>
        const string RelayRegion = "europe-west4";

        async Task<Unity.Services.Multiplayer.ISession> Create(Unity.Services.Multiplayer.RelayProtocol protocol, string region)
        {
            var options = new Unity.Services.Multiplayer.SessionOptions { MaxPlayers = 2, IsPrivate = true, Name = "Bascule duel" };
            var relay = region == null
                ? new Unity.Services.Multiplayer.RelayNetworkOptions(protocol)
                : new Unity.Services.Multiplayer.RelayNetworkOptions(protocol, region);
            options = Unity.Services.Multiplayer.SessionOptionsExtensions.WithRelayNetwork(options, relay);
            options = Unity.Services.Multiplayer.SessionOptionsExtensions.WithNetworkHandler(options, _link);
            return await Unity.Services.Multiplayer.MultiplayerService.Instance.CreateSessionAsync(options);
        }

        public Task<string> JoinAsync(string code) => Serial(() => JoinNow(code));

        async Task<string> JoinNow(string code)
        {
            await LeaveNow();
            IsHost = false;
            var err = await SignInAsync();
            if (err != null) return err;
            try
            {
                _link = NewLink();
                var options = Unity.Services.Multiplayer.SessionOptionsExtensions.WithNetworkHandler(new Unity.Services.Multiplayer.JoinSessionOptions(), _link);
                var session = await Unity.Services.Multiplayer.MultiplayerService.Instance.JoinSessionByCodeAsync(code, options);
                Watch(session);
                IsHost = false;
                Code = code;
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Bascule] Duel : impossible de rejoindre : " + e.Message);
                // La liaison est déjà établie (erreur arrivée après coup) : la partie continue.
                if (_link != null && _link.Linked) { IsHost = false; Code = code; return null; }
                await LeaveNow();
                // Code faux, partie pleine ou terminée : on le dit simplement.
                return "duel_join_failed";
            }
        }

        /// <summary>
        /// Étiquette des parties classées ouvertes (propriété indexée de la session). Elle porte la version du protocole :
        /// deux versions du jeu qui ne se comprendraient pas ne sont jamais mises ensemble.
        /// </summary>
        public static readonly string RankedTag = "classe-p" + Bascule.Core.DuelProtocol.Version;

        public Task<string> QuickMatchAsync(float joinSeconds) => Serial(() => QuickMatchNow(joinSeconds));
        bool _quickRegionFailed;

        async Task<string> QuickMatchNow(float joinSeconds)
        {
            await LeaveNow();
            IsHost = false;
            var err = await SignInAsync();
            if (err != null) return err;
            try
            {
                _link = NewLink();
                // Partie publique pour deux : trouvée par les autres joueurs qui cherchent (sans code), sinon créée.
                var options = new Unity.Services.Multiplayer.SessionOptions
                {
                    MaxPlayers = 2, IsPrivate = false, Name = "Bascule classe",
                    SessionProperties = new System.Collections.Generic.Dictionary<string, Unity.Services.Multiplayer.SessionProperty>
                    {
                        { "mode", new Unity.Services.Multiplayer.SessionProperty(RankedTag, Unity.Services.Multiplayer.VisibilityPropertyOptions.Public, Unity.Services.Multiplayer.PropertyIndex.String1) },
                    },
                };
                // Relais de la région habituelle ; si elle est indisponible (second essai), la région choisie par Unity.
                var relay = _quickRegionFailed
                    ? new Unity.Services.Multiplayer.RelayNetworkOptions(Unity.Services.Multiplayer.RelayProtocol.WSS)
                    : new Unity.Services.Multiplayer.RelayNetworkOptions(Unity.Services.Multiplayer.RelayProtocol.WSS, RelayRegion);
                options = Unity.Services.Multiplayer.SessionOptionsExtensions.WithRelayNetwork(options, relay);
                options = Unity.Services.Multiplayer.SessionOptionsExtensions.WithNetworkHandler(options, _link);
                var quick = new Unity.Services.Multiplayer.QuickJoinOptions
                {
                    CreateSession = true,
                    Timeout = TimeSpan.FromSeconds(Math.Max(1f, joinSeconds)),
                    Filters = new System.Collections.Generic.List<Unity.Services.Multiplayer.FilterOption>
                    {
                        new Unity.Services.Multiplayer.FilterOption(Unity.Services.Multiplayer.FilterField.StringIndex1, RankedTag, Unity.Services.Multiplayer.FilterOperation.Equal),
                    },
                };
                var session = await Unity.Services.Multiplayer.MultiplayerService.Instance.MatchmakeSessionAsync(quick, options);
                Watch(session);
                IsHost = session.IsHost;
                Code = session.Code ?? "";
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Bascule] Duel classé : recherche impossible : " + e.Message);
                // La liaison est déjà établie (erreur arrivée après coup) : la partie continue.
                if (_link != null && _link.Linked) { IsHost = false; return null; }
                // Le contrôleur réessaie : la prochaine fois, sans imposer la région.
                _quickRegionFailed = !_quickRegionFailed;
                await LeaveNow();
                return "duel_search_failed";
            }
        }

        public async Task<bool> ShouldYieldAsync()
        {
            var mine = _session;
            if (mine == null || !mine.IsHost || mine.PlayerCount > 1) return false;
            try
            {
                var query = new Unity.Services.Multiplayer.QuerySessionsOptions
                {
                    Count = 20,
                    FilterOptions = new System.Collections.Generic.List<Unity.Services.Multiplayer.FilterOption>
                    {
                        new Unity.Services.Multiplayer.FilterOption(Unity.Services.Multiplayer.FilterField.StringIndex1, RankedTag, Unity.Services.Multiplayer.FilterOperation.Equal),
                        new Unity.Services.Multiplayer.FilterOption(Unity.Services.Multiplayer.FilterField.AvailableSlots, "0", Unity.Services.Multiplayer.FilterOperation.Greater),
                    },
                };
                var found = await Unity.Services.Multiplayer.MultiplayerService.Instance.QuerySessionsAsync(query);
                if (_session != mine || found == null || found.Sessions == null) return false;
                foreach (var other in found.Sessions)
                    if (other != null && !string.IsNullOrEmpty(other.Id) && other.Id != mine.Id && string.CompareOrdinal(other.Id, mine.Id) < 0) return true;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Duel classé : liste des parties : " + e.Message); }
            return false;
        }

        void Watch(Unity.Services.Multiplayer.ISession session)
        {
            _session = session;
            // Seulement pour la partie en cours : une ancienne session (quittée ou recréée) ne coupe plus le duel.
            // Départ d'un joueur annoncé par la salle : seulement si la liaison directe ne répond plus (un ancien essai de
            // l'ami, annoncé en retard, ne coupe pas la partie qui vient de commencer).
            session.PlayerHasLeft += _ => { if (_session == session && !Linked) PeerLost?.Invoke(); };
            session.Deleted += () => { if (_session == session) PeerLost?.Invoke(); };
            session.RemovedFromSession += () => { if (_session == session) PeerLost?.Invoke(); };
        }

        public void Send(byte[] data, bool reliable) => _link?.Send(data, reliable);
        public void Tick() => _link?.Tick();

        public Task LeaveAsync() => Serial(async () => { await LeaveNow(); return true; });

        async Task LeaveNow()
        {
            // Tout est détaché AVANT d'attendre le serveur : rien de la partie suivante ne peut être touché ensuite.
            var s = _session;
            var link = _link;
            _session = null;
            _link = null;
            Code = "";
            link?.Dispose();
            if (s != null)
            {
                try
                {
                    // L'hôte supprime la partie (son code ne marche plus) ; l'invité la quitte.
                    if (s.IsHost) await s.AsHost().DeleteAsync();
                    else await s.LeaveAsync();
                }
                catch (Exception e) { Debug.LogWarning("[Bascule] Duel : sortie : " + e.Message); }
            }
        }

        /// <summary>
        /// Gestionnaire réseau branché sur la session : le relais de Unity donne l'adresse, Unity Transport fait le reste.
        /// L'hôte écoute ; l'invité se connecte. Deux canaux : fiable et ordonné (événements), non fiable mais
        /// ordonné (photos : une photo plus ancienne qu'une autre déjà reçue est jetée).
        /// </summary>
        sealed class UtpLink : Unity.Services.Multiplayer.INetworkHandler, IDisposable
        {
            Unity.Networking.Transport.NetworkDriver _driver;
            Unity.Networking.Transport.NetworkPipeline _reliable, _unreliable;
            Unity.Networking.Transport.NetworkConnection _conn;
            bool _host, _started, _disposed;
            public bool Linked { get; private set; }
            public bool RelayAlive => _started && _driver.IsCreated
                && Unity.Networking.Transport.Relay.RelayDriverExtensions.GetRelayConnectionStatus(_driver) == Unity.Networking.Transport.Relay.RelayConnectionStatus.Established;
            public event Action OnLinked;
            public event Action OnLost;
            public event Action<byte[]> OnReceived;

            public Task StartAsync(Unity.Services.Multiplayer.NetworkConfiguration configuration)
            {
                // Partie déjà quittée pendant sa création : pas de pilote orphelin (jamais mis à jour ni libéré).
                if (_disposed) return Task.CompletedTask;
                var relay = configuration.RelayServerData;
                var settings = new Unity.Networking.Transport.NetworkSettings();
                Unity.Networking.Transport.Relay.RelayParameterExtensions.WithRelayParameters(ref settings, ref relay);
                // Le relais donne le protocole de CE joueur (WSS ou UDP/DTLS) : le pilote doit utiliser le même.
                // IsWebSocket est un octet dans Unity Transport 2.x (1 = WebSocket), pas un booléen.
                _driver = relay.IsWebSocket != 0
                    ? Unity.Networking.Transport.NetworkDriver.Create(new Unity.Networking.Transport.WebSocketNetworkInterface(), settings)
                    : Unity.Networking.Transport.NetworkDriver.Create(settings);
                _reliable = _driver.CreatePipeline(typeof(Unity.Networking.Transport.ReliableSequencedPipelineStage));
                _unreliable = _driver.CreatePipeline(typeof(Unity.Networking.Transport.UnreliableSequencedPipelineStage));
                _host = configuration.Role != Unity.Services.Multiplayer.NetworkRole.Client;
                if (_driver.Bind(Unity.Networking.Transport.NetworkEndpoint.AnyIpv4) != 0)
                    Debug.LogWarning("[Bascule] Duel : liaison au relais impossible");
                if (_host) _driver.Listen();
                else _conn = _driver.Connect(relay.Endpoint);
                _started = true;
                return Task.CompletedTask;
            }

            public Task StopAsync() { Dispose(); return Task.CompletedTask; }

            public void Tick()
            {
                if (!_started || !_driver.IsCreated) return;
                _driver.ScheduleUpdate().Complete();
                if (_host)
                {
                    Unity.Networking.Transport.NetworkConnection c;
                    while ((c = _driver.Accept()) != default)
                    {
                        // Un seul adversaire : une seconde connexion (l'ami qui se reconnecte) remplace la première, et la
                        // liaison est annoncée à nouveau pour que les présentations recommencent.
                        if (_conn.IsCreated) _driver.Disconnect(_conn);
                        _conn = c;
                        Linked = false;
                        SetLinked(true);
                    }
                }
                if (!_conn.IsCreated) return;
                Unity.Networking.Transport.NetworkEvent.Type ev;
                while (_started && _driver.IsCreated && _conn.IsCreated
                       && (ev = _driver.PopEventForConnection(_conn, out var reader)) != Unity.Networking.Transport.NetworkEvent.Type.Empty)
                {
                    if (ev == Unity.Networking.Transport.NetworkEvent.Type.Connect) SetLinked(true);
                    else if (ev == Unity.Networking.Transport.NetworkEvent.Type.Data)
                    {
                        var data = new byte[reader.Length];
                        for (int i = 0; i < data.Length; i++) data[i] = reader.ReadByte();
                        OnReceived?.Invoke(data);
                    }
                    else if (ev == Unity.Networking.Transport.NetworkEvent.Type.Disconnect)
                    {
                        _conn = default;
                        SetLinked(false);
                        return;
                    }
                }
            }

            void SetLinked(bool on)
            {
                if (Linked == on) return;
                Linked = on;
                if (on) OnLinked?.Invoke(); else OnLost?.Invoke();
            }

            public void Send(byte[] data, bool reliable)
            {
                if (!_started || !_driver.IsCreated || !_conn.IsCreated || !Linked || data == null) return;
                if (_driver.BeginSend(reliable ? _reliable : _unreliable, _conn, out var writer) != 0) return;
                for (int i = 0; i < data.Length; i++) writer.WriteByte(data[i]);
                _driver.EndSend(writer);
            }

            public void Dispose()
            {
                _disposed = true;
                Linked = false;
                if (_started && _driver.IsCreated)
                {
                    if (_conn.IsCreated) _driver.Disconnect(_conn);
                    _driver.ScheduleUpdate().Complete();
                    _driver.Dispose();
                }
                _started = false;
                _conn = default;
            }
        }
    }
#endif
}
