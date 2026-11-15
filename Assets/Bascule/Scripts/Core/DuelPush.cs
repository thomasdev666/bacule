using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bascule.Core
{
    /// <summary>
    /// Notification « Léo te défie en duel ! » (1.5, docs/NOTIFICATIONS.md) : règles partagées entre le jeu et le module
    /// serveur Server/BasculePush, qui compile ce fichier tel quel. Aucun réseau ici.
    ///
    /// Le jeu du destinataire publie, dans ses données Cloud Save PRIVÉES (lui seul et le serveur les lisent), son jeton
    /// de notification, ses amis confirmés et son fuseau horaire. Quand un ami l'invite, le jeu de l'invitant appelle le
    /// module, qui vérifie tout (invitation réelle et récente, amitié acceptée par le destinataire, heure, cadence) avant
    /// de demander à Apple d'afficher la notification.
    /// </summary>
    public static class DuelPush
    {
        /// <summary>Module Cloud Code et fonction appelés par le jeu de l'invitant.</summary>
        public const string Module = "BasculePush", Function = "DuelInvite", TargetParam = "targetId";
        /// <summary>
        /// Auto-test de la liaison avec Apple (Tools/deploy_push_module.py, Tools/verif_publication.py) : envoi à un faux
        /// jeton, au plus une fois toutes les 5 minutes pour tout le jeu. « ok » = clé, équipe, app et réseau bons.
        /// </summary>
        public const string CheckFunction = "DuelPushCheck";
        public const long SelfTestGapSeconds = 300;

        /// <summary>
        /// Invitation, dans les données PUBLIQUES de l'invitant (écrites par son jeu : UgsLeaderboardService.SendDuelInviteAsync,
        /// lues par le jeu de l'invité et par le module).
        /// </summary>
        public const string DuelToKey = "duel_to", DuelCodeKey = "duel_code", DuelAtKey = "duel_at", DuelNameKey = "duel_name";

        /// <summary>Données du destinataire (accès par défaut : privées), écrites par son jeu.</summary>
        public const string TokenKey = "push_token", FriendsKey = "push_friends", TzKey = "push_tz";

        /// <summary>
        /// Données protégées (le joueur les lit, seul le serveur les écrit) : envois de l'invitant, réceptions du
        /// destinataire (heures Unix sur 24 h) et résultat du dernier envoi (diagnostic, visible dans le tableau de bord).
        /// </summary>
        public const string SentLogKey = "push_log", ReceivedLogKey = "push_in", StatusKey = "push_status";

        /// <summary>Taille maximale de la liste d'amis (= FriendCode.MaxFriends, vérifié par un test).</summary>
        public const int MaxFriends = 50;

        /// <summary>Une invitation plus vieille n'envoie rien : l'invitant a eu le temps de partir.</summary>
        public const long InviteMaxAgeSeconds = 120;
        /// <summary>Horloge du téléphone de l'invitant un peu en avance : tolérée jusque-là.</summary>
        public const long ClockSkewSeconds = 30;

        /// <summary>Cadence de l'invitant : 10 s entre deux envois, 2 min avant de relancer le même ami, 20 par jour, 5 au même ami.</summary>
        public const long MinGapSeconds = 10, SameTargetGapSeconds = 120;
        public const int MaxSentPerDay = 20, MaxSameTargetPerDay = 5;
        /// <summary>Le destinataire reçoit 10 notifications de duel par jour au plus, tous amis confondus.</summary>
        public const int MaxReceivedPerDay = 10;

        /// <summary>Jamais la nuit chez le destinataire : rien entre 21 h et 8 h (beaucoup de joueurs sont jeunes).</summary>
        public const int QuietFromHour = 21, QuietToHour = 8;
        /// <summary>Fuseau inconnu ou abîmé : heure de Paris en hiver (le jeu est publié en France).</summary>
        public const int DefaultTzMinutes = 60;

        /// <summary>Apple garde la notification 5 minutes si le téléphone est injoignable : après, le duel est fini.</summary>
        public const long ExpirationSeconds = 300;

        /// <summary>Identifiant de joueur Unity : lettres et chiffres ASCII, 64 caractères au plus.</summary>
        public static bool IsPlayerId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64) return false;
            foreach (char c in id)
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) return false;
            return true;
        }

        /// <summary>Jeton de notification donné par iOS : hexadécimal, 64 caractères aujourd'hui (jusqu'à 200 acceptés).</summary>
        public static bool IsDeviceToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length < 64 || token.Length > 200 || token.Length % 2 != 0) return false;
            foreach (char c in token)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false;
            return true;
        }

        /// <summary>
        /// Amis qui peuvent m'envoyer une notification : mes amis confirmés, sans doublon ni identifiant abîmé, dans
        /// l'ordre, au plus <see cref="MaxFriends"/> (un joueur bloqué n'est plus dans mes amis).
        /// </summary>
        public static string FriendsCsv(IEnumerable<string> confirmedFriends)
        {
            var seen = new HashSet<string>();
            var sb = new StringBuilder();
            if (confirmedFriends != null)
                foreach (var id in confirmedFriends)
                {
                    if (!IsPlayerId(id) || !seen.Add(id)) continue;
                    if (seen.Count > MaxFriends) break;
                    if (sb.Length > 0) sb.Append(',');
                    sb.Append(id);
                }
            return sb.ToString();
        }

        /// <summary>L'invitant fait partie des amis publiés par le destinataire.</summary>
        public static bool IsFriend(string friendsCsv, string inviterId)
        {
            if (string.IsNullOrEmpty(friendsCsv) || !IsPlayerId(inviterId)) return false;
            foreach (var id in friendsCsv.Split(','))
                if (id.Trim() == inviterId) return true;
            return false;
        }

        /// <summary>
        /// Invitation publique de l'invitant (clés duel_to et duel_code écrites par son jeu) : adressée à ce joueur, avec
        /// un code de duel, écrite il y a moins de 2 minutes. <paramref name="invitedAtUnix"/> : heure d'écriture donnée par
        /// le serveur (Cloud Save), pas par le téléphone de l'invitant, dont l'horloge peut être fausse ; -1 si inconnue.
        /// </summary>
        public static bool IsFreshInvite(string duelTo, string duelCode, long invitedAtUnix, string targetId, long nowUnix)
        {
            if (string.IsNullOrEmpty(targetId) || duelTo != targetId) return false;
            if (string.IsNullOrWhiteSpace(duelCode) || duelCode == "-") return false;
            if (invitedAtUnix < 0) return false;
            return nowUnix - invitedAtUnix <= InviteMaxAgeSeconds && invitedAtUnix - nowUnix <= ClockSkewSeconds;
        }

        /// <summary>Heure Unix écrite en texte (« 1800000000 ») ; -1 si elle est absente ou abîmée.</summary>
        public static long ParseUnix(string s) =>
            long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long t) && t >= 0 ? t : -1;

        /// <summary>Décalage horaire du téléphone en minutes (« 60 » à Paris l'hiver), borné à ±14 h.</summary>
        public static int ParseTz(string s)
        {
            if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tz) || tz < -14 * 60 || tz > 14 * 60)
                return DefaultTzMinutes;
            return tz;
        }

        /// <summary>Texte du fuseau publié par le jeu (minutes, culture invariante).</summary>
        public static string FormatTz(TimeSpan offset)
        {
            int minutes = (int)Math.Round(offset.TotalMinutes);
            if (minutes < -14 * 60 || minutes > 14 * 60) minutes = DefaultTzMinutes;
            return minutes.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Heure de nuit chez le destinataire (entre 21 h et 8 h, heure de son téléphone).</summary>
        public static bool IsQuietHours(long nowUnix, int tzMinutes)
        {
            long local = nowUnix + tzMinutes * 60L;
            long secondsOfDay = ((local % 86400) + 86400) % 86400;
            int hour = (int)(secondsOfDay / 3600);
            return hour >= QuietFromHour || hour < QuietToHour;
        }

        /// <summary>
        /// Cadence de l'invitant. <paramref name="log"/> : « heure:ami,heure:ami… » (envois des dernières 24 h).
        /// Renvoie true si l'envoi est permis ; <paramref name="newLog"/> est alors le journal avec cet envoi (à écrire
        /// AVANT d'envoyer : même un envoi raté compte, sinon on pourrait insister sans fin).
        /// </summary>
        public static bool AllowSend(string log, string targetId, long nowUnix, out string newLog)
        {
            var entries = new List<(long t, string to)>();
            if (!string.IsNullOrEmpty(log))
                foreach (var part in log.Split(','))
                {
                    int colon = part.IndexOf(':');
                    if (colon <= 0) continue;
                    if (!long.TryParse(part.Substring(0, colon), NumberStyles.Integer, CultureInfo.InvariantCulture, out long t)) continue;
                    if (nowUnix - t >= 86400 || t - nowUnix > ClockSkewSeconds) continue;
                    entries.Add((t, part.Substring(colon + 1)));
                }
            entries.Sort((a, b) => a.t.CompareTo(b.t));
            newLog = SentLog(entries);
            if (entries.Count >= MaxSentPerDay) return false;
            if (entries.Count > 0 && nowUnix - entries[entries.Count - 1].t < MinGapSeconds) return false;
            int sameTarget = 0;
            foreach (var e in entries)
            {
                if (e.to != targetId) continue;
                sameTarget++;
                if (nowUnix - e.t < SameTargetGapSeconds) return false;
            }
            if (sameTarget >= MaxSameTargetPerDay) return false;
            entries.Add((nowUnix, targetId));
            newLog = SentLog(entries);
            return true;
        }

        static string SentLog(List<(long t, string to)> entries)
        {
            var sb = new StringBuilder();
            foreach (var e in entries)
            {
                if (sb.Length > 0) sb.Append(',');
                sb.Append(e.t.ToString(CultureInfo.InvariantCulture)).Append(':').Append(e.to);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Réceptions du destinataire. <paramref name="log"/> : « heure,heure… » (dernières 24 h). Renvoie true s'il peut
        /// en recevoir une de plus ; <paramref name="newLog"/> la compte alors.
        /// </summary>
        public static bool AllowReceive(string log, long nowUnix, out string newLog)
        {
            var times = new List<long>();
            if (!string.IsNullOrEmpty(log))
                foreach (var part in log.Split(','))
                    if (long.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out long t)
                        && nowUnix - t < 86400 && t - nowUnix <= ClockSkewSeconds)
                        times.Add(t);
            times.Sort();
            newLog = Join(times);
            if (times.Count >= MaxReceivedPerDay) return false;
            times.Add(nowUnix);
            newLog = Join(times);
            return true;
        }

        static string Join(List<long> times)
        {
            var sb = new StringBuilder();
            foreach (var t in times)
            {
                if (sb.Length > 0) sb.Append(',');
                sb.Append(t.ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Pseudo de l'invitant tel qu'il apparaît dans la notification (écran verrouillé) : sans le suffixe « #1234 », puis
        /// le même nettoyage qu'à la saisie du pseudo (<see cref="PseudoFilter.Clean"/> : lettres, chiffres, _ et -,
        /// 16 caractères, grossièretés refusées). Rien d'autre ne passe : ni lien (« bit.ly/x »), ni caractère invisible
        /// qui retourne le texte. Refusé : <paramref name="fallback"/>.
        /// </summary>
        public static string DisplayName(string raw, string fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            string s = raw.Trim();
            int hash = s.IndexOf('#');
            if (hash > 0) s = s.Substring(0, hash).Trim();
            return PseudoFilter.Clean(s) ?? fallback;
        }
    }
}
