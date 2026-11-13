using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bascule.Core;
using UnityEngine;

namespace Bascule.Services
{
    public sealed class LeaderboardEntry
    {
        public int Rank;
        /// <summary>Identifiant du joueur (pour le bloquer ou le signaler). Vide hors ligne.</summary>
        public string PlayerId;
        public string Name;
        public int Score;
        public int Head;
        public bool IsMe;
    }

    /// <summary>Invitation à un duel reçue d'un ami (Cloud Save, données publiques de l'ami).</summary>
    public sealed class DuelInvite
    {
        public string FromId, FromName, Code;
        /// <summary>Tête portée par l'ami (portrait de l'invitation).</summary>
        public int Head;
        /// <summary>Heure d'envoi (secondes Unix, horloge de l'ami).</summary>
        public long At;
    }

    public sealed class LeaderboardPage
    {
        public bool Online;
        public List<LeaderboardEntry> Entries = new List<LeaderboardEntry>();
        public LeaderboardEntry Me;
    }

    public static class Boards
    {
        /// <summary>
        /// Identifiants du tableau de bord Unity Gaming Services (ceux de la 1.4). La version publiée avant la 1.4 (export
        /// « 1.3 », affichée 1.2) écrivait dans bascule_monde_s2 / bascule_jour_s2 : ses records ont été copiés ici le
        /// 13/11/2026 (Tools/copy_leaderboards.py, à relancer tant que des joueurs ont l'ancienne version). Ne plus
        /// changer ces noms sans saison annoncée.
        /// </summary>
        public const string World = "bascule_monde";
        /// <summary>Classement du défi du jour : remise à zéro quotidienne (00:00 UTC).</summary>
        public const string Daily = "bascule_jour";
    }

    /// <summary>Classement mondial et du jour. Connexion anonyme, pas de compte obligatoire.</summary>
    public interface ILeaderboardService
    {
        bool IsOnline { get; }
        Task<bool> InitializeAsync();
        Task<bool> SetPseudoAsync(string pseudo);
        /// <summary>Envoie un score. Renvoie le rang obtenu (1 = premier), 0 si inconnu, -1 en cas d'échec.</summary>
        Task<int> SubmitAsync(string board, int score, int head);
        /// <summary>Scores à partir de la place <paramref name="offset"/> + 1 (0 = les premiers), au plus <paramref name="limit"/>.</summary>
        Task<LeaderboardPage> GetTopAsync(string board, int limit, int offset = 0);
        /// <summary>Les joueurs juste devant et juste derrière soi (<paramref name="range"/> de chaque côté), soi compris.</summary>
        Task<List<LeaderboardEntry>> GetAroundMeAsync(string board, int range);
        /// <summary>Identifiant de joueur en ligne (sert de code ami). Vide hors ligne.</summary>
        string PlayerId { get; }
        /// <summary>Scores de ces joueurs (et du joueur lui-même), classés entre eux.</summary>
        Task<LeaderboardPage> GetFriendsAsync(string board, IList<string> playerIds);
        /// <summary>Efface le compte en ligne (pseudo, scores). Droit à l'effacement (RGPD) et règle de l'App Store.</summary>
        Task<bool> DeleteAccountAsync();
        /// <summary>
        /// Publie le code ami court du joueur (Cloud Save, donnée publique indexée) pour qu'on puisse le retrouver.
        /// Renvoie le code, ou null si les codes courts ne sont pas disponibles (paquet Cloud Save absent, hors ligne).
        /// </summary>
        Task<string> PublishFriendCodeAsync(string pseudo = "", int head = 0);
        /// <summary>Pseudo public d'un joueur (publié avec son code ami) ; vide s'il est inconnu.</summary>
        Task<string> GetPlayerNameAsync(string playerId);
        /// <summary>Pseudo et tête publics d'un joueur (publiés avec son code ami) ; ("", 0) s'ils sont inconnus.</summary>
        Task<(string name, int head)> GetPublicProfileAsync(string playerId);
        /// <summary>Retrouve un joueur par son code court ; null s'il est introuvable (ou ambigu).</summary>
        Task<string> FindPlayerByCodeAsync(string code);
        /// <summary>
        /// Amis réciproques : publie « j'ai ajouté ce joueur » (donnée publique indexée, case <paramref name="slot"/> de 1 à
        /// FriendLinkSlots). L'autre joueur le retrouve avec FindPlayersWhoAddedMeAsync et nous ajoute à son tour.
        /// </summary>
        Task<bool> PublishFriendLinkAsync(string friendId, int slot);
        /// <summary>
        /// Republie d'un coup les derniers amis ajoutés (cases 1 à FriendLinkSlots ; les cases en trop sont vidées).
        /// Au lancement : un ajout fait hors ligne, ou par une ancienne version du jeu, finit ainsi par arriver.
        /// </summary>
        Task<bool> PublishFriendLinksAsync(IList<string> friendIds);
        /// <summary>Joueurs qui nous ont ajouté en ami (récemment).</summary>
        Task<List<string>> FindPlayersWhoAddedMeAsync();
        /// <summary>Invite un ami au duel <paramref name="code"/> (il le voit sur son écran d'accueil).</summary>
        Task<bool> SendDuelInviteAsync(string friendId, string code, string myName, int myHead);
        /// <summary>Retire l'invitation en cours (duel commencé ou annulé).</summary>
        Task<bool> ClearDuelInviteAsync();
        /// <summary>
        /// Duel ouvert (code de session Unity) publié sous mon code ami, pour qu'un ami le rejoigne avec mon code ami même si
        /// la session a été recréée entre-temps (retour dans l'appli). Code vide : plus de duel ouvert.
        /// </summary>
        Task<bool> PublishOpenDuelAsync(string sessionCode);
        /// <summary>Code de session du duel ouvert par le joueur qui a ce code ami ; null s'il n'y en a pas (ou trop ancien).</summary>
        Task<string> FindOpenDuelAsync(string friendShortCode);
        /// <summary>Invitations au duel adressées à ce joueur.</summary>
        Task<List<DuelInvite>> FindDuelInvitesAsync();
    }

    /// <summary>Sans backend : rien n'est envoyé, les scores attendent dans le profil (pendingScores).</summary>
    public sealed class OfflineLeaderboardService : ILeaderboardService
    {
        public bool IsOnline => false;
        public Task<bool> InitializeAsync() => Task.FromResult(false);
        public Task<bool> SetPseudoAsync(string pseudo) => Task.FromResult(false);
        public Task<int> SubmitAsync(string board, int score, int head) => Task.FromResult(-1);
        public Task<LeaderboardPage> GetTopAsync(string board, int limit, int offset = 0) => Task.FromResult(new LeaderboardPage { Online = false });
        public Task<List<LeaderboardEntry>> GetAroundMeAsync(string board, int range) => Task.FromResult(new List<LeaderboardEntry>());
        public string PlayerId => "";
        public Task<LeaderboardPage> GetFriendsAsync(string board, IList<string> playerIds) => Task.FromResult(new LeaderboardPage { Online = false });
        public Task<bool> DeleteAccountAsync() => Task.FromResult(false);
        public Task<string> PublishFriendCodeAsync(string pseudo = "", int head = 0) => Task.FromResult<string>(null);
        public Task<string> GetPlayerNameAsync(string playerId) => Task.FromResult("");
        public Task<(string name, int head)> GetPublicProfileAsync(string playerId) => Task.FromResult(("", 0));
        public Task<string> FindPlayerByCodeAsync(string code) => Task.FromResult<string>(null);
        public Task<bool> PublishFriendLinkAsync(string friendId, int slot) => Task.FromResult(false);
        public Task<bool> PublishFriendLinksAsync(IList<string> friendIds) => Task.FromResult(false);
        public Task<List<string>> FindPlayersWhoAddedMeAsync() => Task.FromResult(new List<string>());
        public Task<bool> SendDuelInviteAsync(string friendId, string code, string myName, int myHead) => Task.FromResult(false);
        public Task<bool> ClearDuelInviteAsync() => Task.FromResult(false);
        public Task<bool> PublishOpenDuelAsync(string sessionCode) => Task.FromResult(false);
        public Task<string> FindOpenDuelAsync(string friendShortCode) => Task.FromResult<string>(null);
        public Task<List<DuelInvite>> FindDuelInvitesAsync() => Task.FromResult(new List<DuelInvite>());
    }

    /// <summary>
    /// File d'attente hors ligne : chaque score est d'abord rangé dans le profil, puis envoyé dès que possible.
    /// Le jeu reste jouable sans connexion (section 12).
    /// </summary>
    public sealed class LeaderboardSync
    {
        readonly ILeaderboardService _service;
        readonly Profile _profile;
        readonly Action _save;
        bool _busy;

        /// <summary>Appelé quand un rang mondial est connu (pour la tête « Légende »).</summary>
        public event Action<string, int> RankKnown;

        public LeaderboardSync(ILeaderboardService service, Profile profile, Action save)
        {
            _service = service;
            _profile = profile;
            _save = save;
        }

        public ILeaderboardService Service => _service;

        public void Enqueue(string board, int score, int head, string dayKey)
        {
            if (score <= 0) return;
            // On ne garde que le meilleur score en attente par classement (et par jour pour le défi).
            var existing = _profile.pendingScores.Find(p => p.board == board && p.date == dayKey);
            if (existing != null)
            {
                if (score <= existing.score) return;
                existing.score = score; existing.head = head;
            }
            else _profile.pendingScores.Add(new PendingScore { board = board, score = score, head = head, date = dayKey });
            _save();
        }

        public async void Flush(string todayKey) => await FlushAsync(todayKey);

        /// <summary>Envoie les scores en attente ; si un envoi est déjà en cours, attend qu'il se termine.</summary>
        public async System.Threading.Tasks.Task FlushAsync(string todayKey)
        {
            for (int i = 0; _busy && i < 50; i++) await System.Threading.Tasks.Task.Delay(100);
            if (_busy || !_service.IsOnline || _profile.pendingScores.Count == 0) return;
            _busy = true;
            try
            {
                // Un score du défi d'un autre jour n'a plus de sens dans le classement du jour.
                // Scores mis en attente par l'ancienne version sous les noms _s2 : envoyés aux classements actuels.
                foreach (var p in _profile.pendingScores)
                {
                    if (p.board == "bascule_monde_s2") p.board = Boards.World;
                    else if (p.board == "bascule_jour_s2") p.board = Boards.Daily;
                }
                _profile.pendingScores.RemoveAll(p => p.board == Boards.Daily && p.date != todayKey);
                foreach (var p in _profile.pendingScores.ToArray())
                {
                    int rank = await _service.SubmitAsync(p.board, p.score, p.head);
                    if (rank < 0) break;
                    _profile.pendingScores.Remove(p);
                    if (rank > 0) RankKnown?.Invoke(p.board, rank);
                }
                _save();
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Synchronisation du classement : " + e.Message); }
            finally { _busy = false; }
        }
    }
}
