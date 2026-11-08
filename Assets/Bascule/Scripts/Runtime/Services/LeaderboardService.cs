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

    public sealed class LeaderboardPage
    {
        public bool Online;
        public List<LeaderboardEntry> Entries = new List<LeaderboardEntry>();
        public LeaderboardEntry Me;
    }

    public static class Boards
    {
        /// <summary>Identifiants à créer à l'identique dans le tableau de bord Unity Gaming Services.</summary>
        public const string World = "bascule_monde";
        /// <summary>Classement du défi du jour : à configurer avec une remise à zéro quotidienne (00:00 UTC).</summary>
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
        Task<LeaderboardPage> GetTopAsync(string board, int limit);
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
        Task<string> PublishFriendCodeAsync();
        /// <summary>Retrouve un joueur par son code court ; null s'il est introuvable (ou ambigu).</summary>
        Task<string> FindPlayerByCodeAsync(string code);
    }

    /// <summary>Sans backend : rien n'est envoyé, les scores attendent dans le profil (pendingScores).</summary>
    public sealed class OfflineLeaderboardService : ILeaderboardService
    {
        public bool IsOnline => false;
        public Task<bool> InitializeAsync() => Task.FromResult(false);
        public Task<bool> SetPseudoAsync(string pseudo) => Task.FromResult(false);
        public Task<int> SubmitAsync(string board, int score, int head) => Task.FromResult(-1);
        public Task<LeaderboardPage> GetTopAsync(string board, int limit) => Task.FromResult(new LeaderboardPage { Online = false });
        public string PlayerId => "";
        public Task<LeaderboardPage> GetFriendsAsync(string board, IList<string> playerIds) => Task.FromResult(new LeaderboardPage { Online = false });
        public Task<bool> DeleteAccountAsync() => Task.FromResult(false);
        public Task<string> PublishFriendCodeAsync() => Task.FromResult<string>(null);
        public Task<string> FindPlayerByCodeAsync(string code) => Task.FromResult<string>(null);
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
