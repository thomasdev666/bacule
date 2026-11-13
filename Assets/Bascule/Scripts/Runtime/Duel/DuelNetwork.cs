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
        event Action PeerLinked;
        event Action PeerLost;
        event Action<byte[]> Received;
        /// <summary>Crée une partie privée pour deux ; renvoie null si c'est réussi, sinon la clé du message d'erreur.</summary>
        Task<string> HostAsync();
        /// <summary>Rejoint la partie d'un ami ; renvoie null si c'est réussi, sinon la clé du message d'erreur.</summary>
        Task<string> JoinAsync(string code);
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
        public event Action PeerLinked { add { } remove { } }
        public event Action PeerLost { add { } remove { } }
        public event Action<byte[]> Received { add { } remove { } }
        public Task<string> HostAsync() => Task.FromResult("duel_unavailable");
        public Task<string> JoinAsync(string code) => Task.FromResult("duel_unavailable");
        public void Send(byte[] data, bool reliable) { }
        public void Tick() { }
        public Task LeaveAsync() => Task.CompletedTask;
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
        public event Action PeerLinked;
        public event Action PeerLost;
        public event Action<byte[]> Received;

        static async Task<string> SignInAsync()
        {
            try
            {
                if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Uninitialized)
                    await Unity.Services.Core.UnityServices.InitializeAsync();
                var auth = Unity.Services.Authentication.AuthenticationService.Instance;
                if (!auth.IsSignedIn) await auth.SignInAnonymouslyAsync();
                return null;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Duel : connexion impossible : " + e.Message); return "duel_offline"; }
        }

        UtpLink NewLink()
        {
            var link = new UtpLink();
            link.OnLinked += () => PeerLinked?.Invoke();
            link.OnLost += () => PeerLost?.Invoke();
            link.OnReceived += d => Received?.Invoke(d);
            return link;
        }

        public async Task<string> HostAsync()
        {
            await LeaveAsync();
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
                await LeaveAsync();
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

        public async Task<string> JoinAsync(string code)
        {
            await LeaveAsync();
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
                await LeaveAsync();
                // Code faux, partie pleine ou terminée : on le dit simplement.
                return "duel_join_failed";
            }
        }

        void Watch(Unity.Services.Multiplayer.ISession session)
        {
            _session = session;
            // Seulement pour la partie en cours : une ancienne session (quittée ou recréée) ne coupe plus le duel.
            session.PlayerHasLeft += _ => { if (_session == session) PeerLost?.Invoke(); };
            session.Deleted += () => { if (_session == session) PeerLost?.Invoke(); };
            session.RemovedFromSession += () => { if (_session == session) PeerLost?.Invoke(); };
        }

        public void Send(byte[] data, bool reliable) => _link?.Send(data, reliable);
        public void Tick() => _link?.Tick();

        public async Task LeaveAsync()
        {
            var s = _session;
            _session = null;
            Code = "";
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
            _link?.Dispose();
            _link = null;
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
            bool _host, _started;
            public bool Linked { get; private set; }
            public event Action OnLinked;
            public event Action OnLost;
            public event Action<byte[]> OnReceived;

            public Task StartAsync(Unity.Services.Multiplayer.NetworkConfiguration configuration)
            {
                var relay = configuration.RelayServerData;
                var settings = new Unity.Networking.Transport.NetworkSettings();
                Unity.Networking.Transport.Relay.RelayParameterExtensions.WithRelayParameters(ref settings, ref relay);
                // Le relais donne le protocole de CE joueur (WSS ou UDP/DTLS) : le pilote doit utiliser le même.
                _driver = relay.IsWebSocket
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
                        // Un seul adversaire : une seconde connexion (rare) remplace la première.
                        if (_conn.IsCreated) _driver.Disconnect(_conn);
                        _conn = c;
                        SetLinked(true);
                    }
                }
                if (!_conn.IsCreated) return;
                Unity.Networking.Transport.NetworkEvent.Type ev;
                while ((ev = _driver.PopEventForConnection(_conn, out var reader)) != Unity.Networking.Transport.NetworkEvent.Type.Empty)
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
