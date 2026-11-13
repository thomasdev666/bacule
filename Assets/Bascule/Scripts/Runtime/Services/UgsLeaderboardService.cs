#if BASCULE_UGS
// Compilé seulement quand le paquet com.unity.services.leaderboards est installé (define BASCULE_UGS,
// déclaré dans Bascule.Runtime.asmdef). Mise en place : docs/INTEGRATIONS.md, section « Classement ».
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Bascule.Core;
using UnityEngine;

namespace Bascule.Services
{
    public sealed class UgsLeaderboardService : ILeaderboardService
    {
        bool _ready;

        public bool IsOnline => _ready && Application.internetReachability != NetworkReachability.NotReachable;

        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                    await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                _ready = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Bascule] Unity Gaming Services indisponible (hors ligne ?) : " + e.Message);
                _ready = false;
            }
            return _ready;
        }

        public async Task<bool> SetPseudoAsync(string pseudo)
        {
            if (!_ready) return false;
            try
            {
                // UGS ajoute un suffixe #1234 pour rendre le nom unique ; on l'enlève à l'affichage.
                await AuthenticationService.Instance.UpdatePlayerNameAsync(Sanitize(pseudo));
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Pseudo refusé : " + e.Message); return false; }
        }

        public async Task<int> SubmitAsync(string board, int score, int head)
        {
            if (!IsOnline) return -1;
            try
            {
                var options = new AddPlayerScoreOptions { Metadata = new Dictionary<string, string> { { "head", head.ToString() } } };
                var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(board, score, options);
                return entry.Rank + 1;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Envoi du score : " + e.Message); return -1; }
        }

        public async Task<LeaderboardPage> GetTopAsync(string board, int limit, int offset = 0)
        {
            var page = new LeaderboardPage();
            if (!IsOnline) return page;
            try
            {
                var scores = await LeaderboardsService.Instance.GetScoresAsync(board, new GetScoresOptions { Offset = offset, Limit = limit, IncludeMetadata = true });
                string me = AuthenticationService.Instance.PlayerId;
                foreach (var r in scores.Results)
                {
                    page.Entries.Add(new LeaderboardEntry
                    {
                        Rank = r.Rank + 1,
                        PlayerId = r.PlayerId,
                        Name = StripSuffix(r.PlayerName),
                        Score = (int)r.Score,
                        Head = ParseHead(r.Metadata),
                        IsMe = r.PlayerId == me,
                    });
                }
                try
                {
                    var mine = await LeaderboardsService.Instance.GetPlayerScoreAsync(board);
                    page.Me = new LeaderboardEntry { Rank = mine.Rank + 1, Name = StripSuffix(mine.PlayerName), Score = (int)mine.Score, IsMe = true };
                }
                catch (Exception) { /* pas encore de score dans ce classement */ }
                page.Online = true;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Lecture du classement : " + e.Message); }
            return page;
        }

        public async Task<List<LeaderboardEntry>> GetAroundMeAsync(string board, int range)
        {
            var list = new List<LeaderboardEntry>();
            if (!IsOnline) return list;
            try
            {
                var scores = await LeaderboardsService.Instance.GetPlayerRangeAsync(board, new GetPlayerRangeOptions { RangeLimit = range, IncludeMetadata = true });
                string me = AuthenticationService.Instance.PlayerId;
                foreach (var r in scores.Results)
                    list.Add(new LeaderboardEntry
                    {
                        Rank = r.Rank + 1,
                        PlayerId = r.PlayerId,
                        Name = StripSuffix(r.PlayerName),
                        Score = (int)r.Score,
                        Head = ParseHead(r.Metadata),
                        IsMe = r.PlayerId == me,
                    });
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Joueurs autour de moi : " + e.Message); }
            return list;
        }

        public string PlayerId => _ready ? AuthenticationService.Instance.PlayerId ?? "" : "";

        public async Task<LeaderboardPage> GetFriendsAsync(string board, IList<string> playerIds)
        {
            var page = new LeaderboardPage();
            if (!IsOnline) return page;
            try
            {
                string me = AuthenticationService.Instance.PlayerId;
                var ids = new List<string>(playerIds);
                if (!ids.Contains(me)) ids.Add(me);
                var scores = await LeaderboardsService.Instance.GetScoresByPlayerIdsAsync(board, ids, new GetScoresByPlayerIdsOptions { IncludeMetadata = true });
                foreach (var r in scores.Results)
                    page.Entries.Add(new LeaderboardEntry
                    {
                        PlayerId = r.PlayerId,
                        Name = StripSuffix(r.PlayerName),
                        Score = (int)r.Score,
                        Head = ParseHead(r.Metadata),
                        IsMe = r.PlayerId == me,
                    });
#if BASCULE_CLOUDSAVE
                // Amis sans score (ils n'ont pas encore fini de partie en ligne) : affichés quand même, avec leur pseudo
                // public, sinon on croit que l'ajout n'a pas marché (retour 1.4).
                var known = new HashSet<string>();
                foreach (var e in page.Entries) known.Add(e.PlayerId);
                foreach (var id in playerIds)
                {
                    if (string.IsNullOrEmpty(id) || id == me || known.Contains(id)) continue;
                    known.Add(id);
                    var (name, headId) = await PublicProfile(id);
                    page.Entries.Add(new LeaderboardEntry { PlayerId = id, Name = name, Score = 0, Head = headId, IsMe = false });
                }
#endif
                // Rang entre amis (le rang mondial ne veut rien dire ici).
                page.Entries.Sort((a, b) => b.Score.CompareTo(a.Score));
                for (int i = 0; i < page.Entries.Count; i++) page.Entries[i].Rank = i + 1;
                page.Online = true;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Classement des amis : " + e.Message); }
            return page;
        }

        public async Task<bool> DeleteAccountAsync()
        {
            if (!_ready) return false;
            try
            {
#if BASCULE_CLOUDSAVE
                // Le fantôme (dernière partie classée) est effacé avant le compte : plus personne ne peut le rejouer.
                try { await SavePublic(new Dictionary<string, object> { { GhostKey, "" }, { GhostAtKey, "0" } }); }
                catch (Exception e) { Debug.LogWarning("[Bascule] Suppression du fantôme : " + e.Message); }
#endif
                await AuthenticationService.Instance.DeleteAccountAsync();
                _ready = false;
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Suppression du compte : " + e.Message); return false; }
        }

        /// <summary>Clé Cloud Save (données publiques du joueur) qui porte le code ami court. Indexée dans le tableau de bord.</summary>
        public const string FriendCodeKey = "friend_code";

        /// <summary>Pseudo et tête publiés avec le code ami : un ami sans score apparaît quand même dans « Amis » (1.4.1).</summary>
        public const string PseudoKey = "pseudo", HeadKey = "head";

        public async Task<string> PublishFriendCodeAsync(string pseudo = "", int head = 0)
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline) return null;
            try
            {
                string code = FriendCode.Short(AuthenticationService.Instance.PlayerId);
                await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.SaveAsync(
                    new Dictionary<string, object> { { FriendCodeKey, code }, { PseudoKey, Sanitize(pseudo ?? "") }, { HeadKey, head.ToString() } },
                    new Unity.Services.CloudSave.Models.Data.Player.SaveOptions(new Unity.Services.CloudSave.Models.Data.Player.PublicWriteAccessClassOptions()));
                return code;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Code ami court : " + e.Message); return null; }
#else
            await Task.CompletedTask;
            return null;
#endif
        }

        public async Task<string> FindPlayerByCodeAsync(string code)
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline || string.IsNullOrEmpty(code)) return null;
            try
            {
                var filters = new List<Unity.Services.CloudSave.Models.FieldFilter>
                {
                    new Unity.Services.CloudSave.Models.FieldFilter(FriendCodeKey, code, Unity.Services.CloudSave.Models.FieldFilter.OpOptions.EQ, true),
                };
                var query = new Unity.Services.CloudSave.Models.Query(filters, new HashSet<string> { FriendCodeKey }, 0, 2);
                var found = await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.QueryAsync(query, new Unity.Services.CloudSave.Models.Data.Player.QueryOptions());
                // Deux joueurs avec le même code (très rare) : on refuse plutôt que d'ajouter le mauvais.
                return found != null && found.Count == 1 ? found[0].Id : null;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Recherche du code ami : " + e.Message); return null; }
#else
            await Task.CompletedTask;
            return null;
#endif
        }

        // ------------------------------------------------------------------ Amis réciproques et invitations au duel
        // Données publiques du joueur dans Cloud Save. Les clés link1…link5 et duel_to doivent être INDEXÉES dans le
        // tableau de bord (Cloud Save → Indexes → Player, accès public, type texte), comme friend_code.

        public const int FriendLinkSlots = 5;
        public const string FriendLinkKey = "link", DuelToKey = "duel_to", DuelCodeKey = "duel_code", DuelAtKey = "duel_at", DuelNameKey = "duel_name", DuelHeadKey = "duel_head";

        public async Task<bool> PublishFriendLinkAsync(string friendId, int slot)
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline || string.IsNullOrEmpty(friendId)) return false;
            try
            {
                int s = 1 + Math.Abs(slot - 1) % FriendLinkSlots;
                await SavePublic(new Dictionary<string, object> { { FriendLinkKey + s, friendId } });
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Ami réciproque : " + e.Message); return false; }
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        public async Task<bool> PublishFriendLinksAsync(IList<string> friendIds)
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline || friendIds == null) return false;
            try
            {
                // Même case que lors de l'ajout : l'ami n° k (dans l'ordre d'ajout) va dans la case 1 + k % 5.
                var data = new Dictionary<string, object>();
                for (int s = 1; s <= FriendLinkSlots; s++) data[FriendLinkKey + s] = "-";
                int n = friendIds.Count;
                for (int k = Math.Max(0, n - FriendLinkSlots); k < n; k++)
                    if (!string.IsNullOrEmpty(friendIds[k])) data[FriendLinkKey + (1 + k % FriendLinkSlots)] = friendIds[k];
                await SavePublic(data);
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Amis réciproques (republication) : " + e.Message); return false; }
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        public async Task<List<string>> FindPlayersWhoAddedMeAsync()
        {
            var ids = new List<string>();
#if BASCULE_CLOUDSAVE
            if (!IsOnline) return ids;
            string me = AuthenticationService.Instance.PlayerId;
            for (int s = 1; s <= FriendLinkSlots; s++)
            {
                try
                {
                    foreach (var e in await QueryPublic(FriendLinkKey + s, me, 20))
                        if (!string.IsNullOrEmpty(e.Id) && e.Id != me && !ids.Contains(e.Id)) ids.Add(e.Id);
                }
                catch (Exception e) { Debug.LogWarning("[Bascule] Amis réciproques : " + e.Message); break; }
            }
#else
            await Task.CompletedTask;
#endif
            return ids;
        }

        public async Task<bool> SendDuelInviteAsync(string friendId, string code, string myName, int myHead)
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline || string.IsNullOrEmpty(friendId) || string.IsNullOrEmpty(code)) return false;
            try
            {
                await SavePublic(new Dictionary<string, object>
                {
                    { DuelToKey, friendId }, { DuelCodeKey, code }, { DuelNameKey, Sanitize(myName) }, { DuelHeadKey, myHead.ToString() },
                    { DuelAtKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
                });
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Invitation au duel : " + e.Message); return false; }
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        public async Task<bool> ClearDuelInviteAsync()
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline) return false;
            try { await SavePublic(new Dictionary<string, object> { { DuelToKey, "-" } }); return true; }
            catch (Exception e) { Debug.LogWarning("[Bascule] Invitation au duel : " + e.Message); return false; }
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        public const string OpenDuelKey = "duel_open", OpenDuelAtKey = "duel_open_at";
        /// <summary>Un duel ouvert depuis plus longtemps est oublié (l'hôte est parti sans le fermer).</summary>
        const long OpenDuelMaxAge = 1800;

        public async Task<bool> PublishOpenDuelAsync(string sessionCode)
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline) return false;
            try
            {
                await SavePublic(new Dictionary<string, object>
                {
                    { OpenDuelKey, string.IsNullOrEmpty(sessionCode) ? "-" : sessionCode },
                    { OpenDuelAtKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
                });
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Duel ouvert : " + e.Message); return false; }
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        public async Task<string> FindOpenDuelAsync(string friendShortCode)
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline || string.IsNullOrEmpty(friendShortCode)) return null;
            try
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                foreach (var e in await QueryPublic(FriendCodeKey, friendShortCode, 3, OpenDuelKey, OpenDuelAtKey))
                {
                    if (e.Data == null) continue;
                    string code = null; long at = 0;
                    foreach (var item in e.Data)
                    {
                        string v = item.Value?.GetAs<string>() ?? "";
                        if (item.Key == OpenDuelKey) code = v;
                        else if (item.Key == OpenDuelAtKey) long.TryParse(v, out at);
                    }
                    if (!string.IsNullOrEmpty(code) && code != "-" && Math.Abs(now - at) <= OpenDuelMaxAge) return code;
                }
            }
            catch (Exception ex) { Debug.LogWarning("[Bascule] Duel ouvert (recherche) : " + ex.Message); }
            return null;
#else
            await Task.CompletedTask;
            return null;
#endif
        }

        public async Task<List<DuelInvite>> FindDuelInvitesAsync()
        {
            var list = new List<DuelInvite>();
#if BASCULE_CLOUDSAVE
            if (!IsOnline) return list;
            try
            {
                string me = AuthenticationService.Instance.PlayerId;
                foreach (var e in await QueryPublic(DuelToKey, me, 10, DuelCodeKey, DuelNameKey, DuelAtKey, DuelHeadKey))
                {
                    var inv = new DuelInvite { FromId = e.Id };
                    if (e.Data != null)
                        foreach (var item in e.Data)
                        {
                            string v = item.Value?.GetAs<string>() ?? "";
                            if (item.Key == DuelCodeKey) inv.Code = v;
                            else if (item.Key == DuelNameKey) inv.FromName = v;
                            else if (item.Key == DuelAtKey) long.TryParse(v, out inv.At);
                            else if (item.Key == DuelHeadKey) int.TryParse(v, out inv.Head);
                        }
                    if (!string.IsNullOrEmpty(inv.Code)) list.Add(inv);
                }
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Invitations au duel : " + e.Message); }
#else
            await Task.CompletedTask;
#endif
            return list;
        }

        public async Task<string> GetPlayerNameAsync(string playerId)
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline || string.IsNullOrEmpty(playerId)) return "";
            return (await PublicProfile(playerId)).name;
#else
            await Task.CompletedTask;
            return "";
#endif
        }

        // ------------------------------------------------------------------ Ligue et fantômes (1.5)

        public async Task<int> GetArchivedRankAsync(string board, DateTime weekStartUtc)
        {
            if (!IsOnline) return -1;
            try
            {
                // Les dernières semaines archivées ; celle qui contient le milieu de la semaine demandée.
                var versions = await LeaderboardsService.Instance.GetVersionsAsync(board, new GetVersionsOptions { Limit = 8 });
                // L'archive de cette semaine est celle qui s'est fermée à sa fin (le lundi suivant, 00:00 UTC), quel que
                // soit le jour où le classement a été créé.
                var end = weekStartUtc.AddDays(7);
                string versionId = null;
                if (versions != null && versions.Results != null)
                    foreach (var v in versions.Results)
                        if (v != null && Math.Abs((v.End.ToUniversalTime() - end).TotalHours) < 12) { versionId = v.Id; break; }
                // Pas encore là (on ouvre le jeu juste après minuit, ou l'horloge avance un peu) : on redemandera.
                // Toujours absente deux jours après : la semaine n'a pas été archivée, on la solde sans pièces.
                if (versionId == null) return (DateTime.UtcNow - end).TotalDays < 2 ? -1 : 0;
                // Les 100 premiers suffisent : les divisions récompensées s'arrêtent bien avant.
                var page = await LeaderboardsService.Instance.GetVersionScoresAsync(board, versionId, new GetVersionScoresOptions { Limit = 100 });
                string me = AuthenticationService.Instance.PlayerId;
                if (page != null && page.Results != null)
                    foreach (var r in page.Results)
                        if (r.PlayerId == me) return r.Rank + 1;
                return 0;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Ligue : semaine finie : " + e.Message); return -1; }
        }

        /// <summary>Fantôme (données publiques du joueur, sans index) : la partie et l'heure de publication.</summary>
        public const string GhostKey = "ghost", GhostAtKey = "ghost_at";

        public async Task<bool> PublishGhostAsync(string data)
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline || string.IsNullOrEmpty(data)) return false;
            try
            {
                await SavePublic(new Dictionary<string, object> { { GhostKey, data }, { GhostAtKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() } });
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Fantôme : publication : " + e.Message); return false; }
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        static readonly System.Random _ghostRng = new System.Random();

        public async Task<GhostRecording> FindGhostAsync(ICollection<string> avoid, int maxAgeDays)
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline) return null;
            try
            {
                string me = AuthenticationService.Instance.PlayerId;
                long oldest = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Math.Max(1, maxAgeDays) * 86400L;
                // Les codes amis sont répartis au hasard : on part d'une lettre tirée au sort dans l'index friend_code (déjà
                // en place), puis du début si rien ne suit. Aucun réglage en plus dans le tableau de bord.
                string start = FriendCode.ShortAlphabet[_ghostRng.Next(FriendCode.ShortAlphabet.Length)].ToString();
                var candidates = new List<string>();
                var fallback = new List<string>();
                foreach (string from in new[] { start, "0" })
                {
                    var filters = new List<Unity.Services.CloudSave.Models.FieldFilter>
                    {
                        new Unity.Services.CloudSave.Models.FieldFilter(FriendCodeKey, from, Unity.Services.CloudSave.Models.FieldFilter.OpOptions.GE, true),
                    };
                    var query = new Unity.Services.CloudSave.Models.Query(filters, new HashSet<string> { FriendCodeKey, GhostAtKey }, 0, 100);
                    var found = await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.QueryAsync(query, new Unity.Services.CloudSave.Models.Data.Player.QueryOptions());
                    if (found != null)
                        foreach (var e in found)
                        {
                            if (e == null || string.IsNullOrEmpty(e.Id) || e.Id == me || e.Data == null) continue;
                            long at = 0;
                            foreach (var item in e.Data)
                                if (item.Key == GhostAtKey) long.TryParse(item.Value?.GetAs<string>() ?? "", out at);
                            if (at < oldest) continue;
                            if (avoid != null && avoid.Contains(e.Id)) { if (!fallback.Contains(e.Id)) fallback.Add(e.Id); }
                            else if (!candidates.Contains(e.Id)) candidates.Add(e.Id);
                        }
                    if (candidates.Count > 0) break;
                }
                // Tous déjà affrontés récemment : mieux vaut un fantôme déjà vu que pas de duel.
                if (candidates.Count == 0) candidates = fallback;
                // Quelques essais : un fantôme abîmé ou d'un autre format est passé.
                for (int tries = 0; tries < 3 && candidates.Count > 0; tries++)
                {
                    int k = _ghostRng.Next(candidates.Count);
                    string id = candidates[k];
                    candidates.RemoveAt(k);
                    var loaded = await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { GhostKey },
                        new Unity.Services.CloudSave.Models.Data.Player.LoadOptions(new Unity.Services.CloudSave.Models.Data.Player.PublicReadAccessClassOptions(id)));
                    if (loaded == null || !loaded.TryGetValue(GhostKey, out var value) || value == null) continue;
                    var ghost = GhostRecording.FromBase64(value.Value?.GetAs<string>() ?? "");
                    if (ghost == null) continue;
                    ghost.PlayerId = id;
                    return ghost;
                }
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Fantôme : recherche : " + e.Message); }
            return null;
#else
            await Task.CompletedTask;
            return null;
#endif
        }

        public async Task<(string name, int head)> GetPublicProfileAsync(string playerId)
        {
#if BASCULE_CLOUDSAVE
            if (!IsOnline || string.IsNullOrEmpty(playerId)) return ("", 0);
            return await PublicProfile(playerId);
#else
            await Task.CompletedTask;
            return ("", 0);
#endif
        }

#if BASCULE_CLOUDSAVE
        /// <summary>Pseudo et tête publics d'un joueur, retrouvés par son code ami (index friend_code).</summary>
        static async Task<(string name, int head)> PublicProfile(string playerId)
        {
            try
            {
                foreach (var e in await QueryPublic(FriendCodeKey, FriendCode.Short(playerId), 3, PseudoKey, HeadKey))
                {
                    if (e.Id != playerId || e.Data == null) continue;
                    string name = ""; int head = 0;
                    foreach (var item in e.Data)
                    {
                        string v = item.Value?.GetAs<string>() ?? "";
                        if (item.Key == PseudoKey) name = v;
                        else if (item.Key == HeadKey) int.TryParse(v, out head);
                    }
                    return (name, head);
                }
            }
            catch (Exception ex) { Debug.LogWarning("[Bascule] Profil public : " + ex.Message); }
            return ("", 0);
        }

        static Task SavePublic(Dictionary<string, object> data) =>
            Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.SaveAsync(data,
                new Unity.Services.CloudSave.Models.Data.Player.SaveOptions(new Unity.Services.CloudSave.Models.Data.Player.PublicWriteAccessClassOptions()));

        static Task<List<Unity.Services.CloudSave.Models.EntityData>> QueryPublic(string key, string value, int limit, params string[] returnKeys)
        {
            var filters = new List<Unity.Services.CloudSave.Models.FieldFilter>
            {
                new Unity.Services.CloudSave.Models.FieldFilter(key, value, Unity.Services.CloudSave.Models.FieldFilter.OpOptions.EQ, true),
            };
            var keys = new HashSet<string>(returnKeys) { key };
            var query = new Unity.Services.CloudSave.Models.Query(filters, keys, 0, limit);
            return Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.QueryAsync(query, new Unity.Services.CloudSave.Models.Data.Player.QueryOptions());
        }
#endif

        static string Sanitize(string s)
        {
            s = (s ?? "").Trim().Replace(" ", "_");
            if (s.Length > 20) s = s.Substring(0, 20);
            return string.IsNullOrEmpty(s) ? "Joueur" : s;
        }

        static string StripSuffix(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            int i = name.LastIndexOf('#');
            return i > 0 ? name.Substring(0, i) : name;
        }

        static int ParseHead(string metadata)
        {
            if (string.IsNullOrEmpty(metadata)) return 0;
            // Métadonnées au format {"head":"12"}.
            int i = metadata.IndexOf("\"head\"", StringComparison.Ordinal);
            if (i < 0) return 0;
            int start = metadata.IndexOfAny("0123456789".ToCharArray(), i);
            if (start < 0) return 0;
            int end = start;
            while (end < metadata.Length && char.IsDigit(metadata[end])) end++;
            return int.TryParse(metadata.Substring(start, end - start), out int h) ? h : 0;
        }
    }
}
#endif
