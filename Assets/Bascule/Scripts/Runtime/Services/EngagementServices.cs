using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Bascule.Services
{
    // =====================================================================================================
    // Notifications locales
    // =====================================================================================================

    /// <summary>
    /// Rappels locaux (aucun serveur) : cadeau du jour prêt, série de défi à ne pas perdre, retour après quelques jours.
    /// Au plus un rappel par jour, jamais la nuit. Avec le paquet com.unity.mobile.notifications ; sinon rien.
    /// </summary>
    public interface INotificationService
    {
        bool IsSupported { get; }
        void RequestPermission(Action<bool> onDone);
        void CancelAll();
        void Schedule(string id, string title, string body, DateTime localTime);
    }

    public sealed class NoNotificationService : INotificationService
    {
        public bool IsSupported => false;
        public void RequestPermission(Action<bool> onDone) => onDone?.Invoke(false);
        public void CancelAll() { }
        public void Schedule(string id, string title, string body, DateTime localTime) { }
    }

#if BASCULE_NOTIFICATIONS && (UNITY_ANDROID || UNITY_IOS)
    public sealed class MobileNotificationService : INotificationService
    {
        const string Channel = "bascule_rappels";
        readonly MonoBehaviour _host;

        public MobileNotificationService(MonoBehaviour host)
        {
            _host = host;
#if UNITY_ANDROID
            Unity.Notifications.Android.AndroidNotificationCenter.RegisterNotificationChannel(
                new Unity.Notifications.Android.AndroidNotificationChannel(Channel, "Rappels", "Cadeau du jour et défi", Unity.Notifications.Android.Importance.Default));
#endif
        }

        public bool IsSupported => true;

        public void RequestPermission(Action<bool> onDone) => _host.StartCoroutine(Request(onDone));

        System.Collections.IEnumerator Request(Action<bool> onDone)
        {
#if UNITY_ANDROID
            var req = new Unity.Notifications.Android.PermissionRequest();
            while (req.Status == Unity.Notifications.Android.PermissionStatus.RequestPending) yield return null;
            onDone?.Invoke(req.Status == Unity.Notifications.Android.PermissionStatus.Allowed);
#else
            using (var req = new Unity.Notifications.iOS.AuthorizationRequest(Unity.Notifications.iOS.AuthorizationOption.Alert | Unity.Notifications.iOS.AuthorizationOption.Badge | Unity.Notifications.iOS.AuthorizationOption.Sound, false))
            {
                while (!req.IsFinished) yield return null;
                onDone?.Invoke(req.Granted);
            }
#endif
        }

        public void CancelAll()
        {
#if UNITY_ANDROID
            Unity.Notifications.Android.AndroidNotificationCenter.CancelAllScheduledNotifications();
#else
            Unity.Notifications.iOS.iOSNotificationCenter.RemoveAllScheduledNotifications();
            Unity.Notifications.iOS.iOSNotificationCenter.ApplicationBadge = 0;
#endif
        }

        public void Schedule(string id, string title, string body, DateTime localTime)
        {
            if (localTime <= DateTime.Now.AddMinutes(1)) return;
#if UNITY_ANDROID
            var n = new Unity.Notifications.Android.AndroidNotification { Title = title, Text = body, FireTime = localTime };
            Unity.Notifications.Android.AndroidNotificationCenter.SendNotification(n, Channel);
#else
            var n = new Unity.Notifications.iOS.iOSNotification
            {
                Identifier = id, Title = title, Body = body, ShowInForeground = false,
                Trigger = new Unity.Notifications.iOS.iOSNotificationTimeIntervalTrigger { TimeInterval = localTime - DateTime.Now, Repeats = false },
            };
            Unity.Notifications.iOS.iOSNotificationCenter.ScheduleNotification(n);
#endif
        }
    }
#endif

    // =====================================================================================================
    // Statistiques d'usage
    // =====================================================================================================

    /// <summary>
    /// Journal d'événements anonymes (parties, pubs, achats, missions…) pour mesurer la rétention et régler le jeu
    /// pendant le lancement test. Stocké sur le téléphone (analytics.jsonl, 2 Mo au plus) ; un service d'analyse
    /// (Unity Analytics, Firebase, GameAnalytics) se branche en implémentant IAnalyticsSink. Aucune donnée personnelle.
    /// Liste des événements et de ce qu'ils permettent de mesurer : docs/RAPPORT.md, section « Mesurer ».
    /// </summary>
    public interface IAnalyticsSink
    {
        void Send(string name, IReadOnlyDictionary<string, object> data);
    }

    public sealed class Analytics
    {
        readonly string _path;
        readonly List<IAnalyticsSink> _sinks = new List<IAnalyticsSink>();
        readonly string _session = Guid.NewGuid().ToString("N").Substring(0, 8);
        const long MaxBytes = 2 * 1024 * 1024;
        public bool Enabled = true;

        public Analytics()
        {
            try { _path = Path.Combine(Application.persistentDataPath, "analytics.jsonl"); }
            catch (Exception) { _path = null; }
        }

        public void AddSink(IAnalyticsSink sink) { if (sink != null) _sinks.Add(sink); }

        public void Event(string name, params (string key, object value)[] data)
        {
            if (!Enabled) return;
            var dict = new Dictionary<string, object>();
            foreach (var (k, v) in data) dict[k] = v;
            foreach (var s in _sinks)
            {
                try { s.Send(name, dict); }
                catch (Exception e) { Debug.LogWarning("[Bascule] Statistiques : " + e.Message); }
            }
            Write(name, dict);
        }

        // Écritures sur un fil à part (le jeu ne attend jamais le disque).
        readonly object _gate = new object();
        bool _busy;
        readonly System.Collections.Generic.Queue<string> _lines = new System.Collections.Generic.Queue<string>();

        void Enqueue(string line)
        {
            lock (_gate)
            {
                _lines.Enqueue(line);
                if (_busy) return;
                _busy = true;
            }
            System.Threading.ThreadPool.QueueUserWorkItem(_ => Drain());
        }

        void Drain()
        {
            while (true)
            {
                string line;
                lock (_gate)
                {
                    if (_lines.Count == 0) { _busy = false; return; }
                    line = _lines.Dequeue();
                }
                AppendLine(line);
            }
        }

        void Write(string name, Dictionary<string, object> data)
        {
            if (_path == null) return;
            try { Enqueue(Format(name, data)); }
            catch (Exception) { /* jamais bloquant */ }
        }

        string Format(string name, Dictionary<string, object> data)
        {
            var sb = new StringBuilder(128);
            sb.Append("{\"t\":").Append(((long)Core.Dates.UnixSeconds(DateTime.UtcNow)).ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(",\"s\":\"").Append(_session).Append("\",\"e\":\"").Append(name).Append('"');
            foreach (var kv in data)
            {
                sb.Append(",\"").Append(kv.Key).Append("\":");
                switch (kv.Value)
                {
                    case null: sb.Append("null"); break;
                    case bool b: sb.Append(b ? "true" : "false"); break;
                    case string str: sb.Append('"').Append(str.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"'); break;
                    case float f: sb.Append(f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)); break;
                    case double d: sb.Append(d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)); break;
                    default: sb.Append(Convert.ToString(kv.Value, System.Globalization.CultureInfo.InvariantCulture)); break;
                }
            }
            sb.Append("}\n");
            return sb.ToString();
        }

        void AppendLine(string line)
        {
            try
            {
                var info = new FileInfo(_path);
                if (info.Exists && info.Length > MaxBytes)
                {
                    var old = _path + ".1";
                    if (File.Exists(old)) File.Delete(old);
                    File.Move(_path, old);
                }
                File.AppendAllText(_path, line);
            }
            catch (Exception) { /* jamais bloquant */ }
        }
    }
}
