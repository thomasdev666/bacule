using System;
using System.IO;
using Bascule.Core;
using UnityEngine;

namespace Bascule.Services
{
    /// <summary>
    /// Sauvegarde locale en JSON dans Application.persistentDataPath :
    /// profile.json (collection, record, réglages) et run.json (partie en cours, pour reprendre).
    /// Écriture atomique (fichier temporaire puis remplacement) pour ne jamais corrompre la sauvegarde.
    /// </summary>
    public sealed class SaveService
    {
        readonly string _dir;
        string ProfilePath => Path.Combine(_dir, "profile.json");
        string RunPath => Path.Combine(_dir, "run.json");

        public SaveService(string directory = null) { _dir = directory ?? Application.persistentDataPath; }

        public Profile LoadProfile()
        {
            Profile p = null;
            try
            {
                if (File.Exists(ProfilePath)) p = JsonUtility.FromJson<Profile>(File.ReadAllText(ProfilePath));
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Profil illisible : " + e.Message); }
            // Application fermée au milieu d'une écriture (entre l'effacement de l'ancien fichier et le renommage du
            // nouveau), ou fichier abîmé : la copie temporaire, complète, sert de secours au lieu de tout perdre.
            if (p == null)
            {
                try
                {
                    string tmp = ProfilePath + ".tmp";
                    if (File.Exists(tmp)) p = JsonUtility.FromJson<Profile>(File.ReadAllText(tmp));
                }
                catch (Exception e) { Debug.LogWarning("[Bascule] Copie de secours illisible, on repart à zéro : " + e.Message); }
            }
            bool existed = p != null;
            p = p ?? new Profile();
            p.Sanitize();
            // Une nouvelle sauvegarde est marquée de la saison en cours ; une ancienne (1.2) est gardée telle quelle
            // (Core.Seasons.OnLoad : rien n'est remis à zéro depuis la 1.4).
            p = Seasons.OnLoad(p, existed, out bool changed, out bool wiped);
            SeasonReset = wiped;
            if (changed) SaveProfile(p);
            return p;
        }

        /// <summary>La sauvegarde vient d'être remise à zéro pour la nouvelle saison (la partie en cours est alors oubliée).</summary>
        public bool SeasonReset { get; private set; }

        public void SaveProfile(Profile p) => Write(ProfilePath, JsonUtility.ToJson(p));

        public bool HasRun => File.Exists(RunPath);

        public RunSnapshot LoadRun()
        {
            try
            {
                if (!File.Exists(RunPath)) return null;
                var s = JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(RunPath));
                return s != null && s.version == 1 ? s : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Bascule] Partie sauvegardée illisible : " + e.Message);
                return null;
            }
        }

        public void SaveRun(RunSnapshot s) => Write(RunPath, JsonUtility.ToJson(s));

        public void ClearRun()
        {
            try { if (File.Exists(RunPath)) File.Delete(RunPath); }
            catch (Exception e) { Debug.LogWarning("[Bascule] " + e.Message); }
        }

        public void DeleteAll()
        {
            ClearRun();
            try { if (File.Exists(ProfilePath)) File.Delete(ProfilePath); }
            catch (Exception e) { Debug.LogWarning("[Bascule] " + e.Message); }
        }

        // Les écritures se font sur un fil à part, dans l'ordre : le jeu ne attend jamais le disque (pas de freeze).
        // Le dernier contenu d'un même fichier remplace les précédents encore en attente.
        static readonly object Gate = new object();
        static readonly System.Collections.Generic.Dictionary<string, string> Pending = new System.Collections.Generic.Dictionary<string, string>();
        static readonly System.Collections.Generic.Queue<string> Order = new System.Collections.Generic.Queue<string>();
        static bool _worker;

#if UNITY_WEBGL && !UNITY_EDITOR
        // Sur le web, il n'y a pas de fils d'exécution : on écrit tout de suite, puis on enregistre dans le navigateur.
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void BasculeSyncFs();

        static void Write(string path, string content)
        {
            try
            {
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, content);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                BasculeSyncFs();
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Bascule] Sauvegarde impossible : " + e.Message); }
        }
#else
        static void Write(string path, string content)
        {
            lock (Gate)
            {
                if (!Pending.ContainsKey(path)) Order.Enqueue(path);
                Pending[path] = content;
                if (_worker) return;
                _worker = true;
            }
            System.Threading.ThreadPool.QueueUserWorkItem(_ => Drain());
        }
#endif

        static void Drain()
        {
            while (true)
            {
                string path, content;
                lock (Gate)
                {
                    if (Order.Count == 0) { _worker = false; System.Threading.Monitor.PulseAll(Gate); return; }
                    path = Order.Dequeue();
                    content = Pending[path];
                    Pending.Remove(path);
                }
                try
                {
                    var tmp = path + ".tmp";
                    File.WriteAllText(tmp, content);
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tmp, path);
                }
                catch (Exception e) { UnityEngine.Debug.LogWarning("[Bascule] Sauvegarde impossible : " + e.Message); }
            }
        }

        /// <summary>À la fermeture de l'appli : attend que les écritures en attente soient faites (au plus 2 s).</summary>
        public static void Flush()
        {
#if !(UNITY_WEBGL && !UNITY_EDITOR) // sur le web, chaque sauvegarde est déjà écrite tout de suite
            lock (Gate)
            {
                var end = DateTime.UtcNow.AddSeconds(2);
                while (_worker && DateTime.UtcNow < end) System.Threading.Monitor.Wait(Gate, 50);
            }
#endif
        }
    }

    /// <summary>Charge bascule_tuning.json. Dans l'éditeur, relit le fichier directement : on règle sans réimporter.</summary>
    public static class TuningLoader
    {
        public const string ResourceName = "bascule_tuning";

        public static Tuning Load()
        {
            var t = new Tuning();
            string json = null;
#if UNITY_EDITOR
            var path = Path.Combine(Application.dataPath, "Bascule/Resources/" + ResourceName + ".json");
            if (File.Exists(path)) json = File.ReadAllText(path);
#endif
            if (json == null)
            {
                var asset = Resources.Load<TextAsset>(ResourceName);
                if (asset != null) json = asset.text;
            }
            if (!string.IsNullOrEmpty(json))
            {
                try { JsonUtility.FromJsonOverwrite(json, t); }
                catch (Exception e) { Debug.LogError("[Bascule] bascule_tuning.json invalide, valeurs par défaut utilisées : " + e.Message); t = new Tuning(); }
            }
            else Debug.LogWarning("[Bascule] bascule_tuning.json introuvable, valeurs par défaut utilisées.");
            t.Sanitize();
            return t;
        }
    }
}
