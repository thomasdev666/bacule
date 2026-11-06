using System;
using System.Collections.Generic;
using Bascule.Core;
using UnityEngine;

namespace Bascule.Services
{
    public enum Sfx { Thud, Pop, Lift, Fall, Chime, Phew, Lost, Click }

    /// <summary>Sons du jeu, générés au lancement par Bascule.Core.Synth (aucun fichier audio).</summary>
    public sealed class AudioService
    {
        readonly AudioSource[] _sources;
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        public bool Enabled = true;
        /// <summary>Volume général des bruitages (sous la musique, pour que l'ensemble reste agréable).</summary>
        public float Master = 0.6f;
        /// <summary>Appelé pour les gros bruitages : la musique se baisse un instant (GameApp le relie à MusicPlayer.Duck).</summary>
        public Action<float> Duck;

        int _frame = -1, _startedThisFrame, _shot;
        readonly Queue<float> _recent = new Queue<float>();
#if UNITY_WEBGL && !UNITY_EDITOR
        const int Burst = 3, BurstImportant = 4;
#else
        const int Burst = 6, BurstImportant = 8;
#endif

        /// <summary>
        /// Tampon audio plus grand (1024 échantillons, environ 21 ms) : pendant un combo, le téléphone calcule beaucoup
        /// (fusions, confettis, textes) et, avec le petit tampon par défaut, le son n'était plus fourni à temps
        /// (craquements, coupures). À appeler au tout début, avant de créer le moindre son.
        /// </summary>
        public static void UseSafeBuffer()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                var cfg = AudioSettings.GetConfiguration();
                if (cfg.dspBufferSize >= 1024) return;
                cfg.dspBufferSize = 1024;
                AudioSettings.Reset(cfg);
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Tampon audio : " + e.Message); }
#endif
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        const int MaxPerFrame = 2;
#else
        const int MaxPerFrame = 4;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        public AudioService(GameObject host, int voices = 8)
#else
        public AudioService(GameObject host, int voices = 12)
#endif
        {
            _sources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                var s = host.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                // Priorité basse : si le téléphone manque de voix, c'est un bruitage qui saute, jamais la musique.
                s.priority = 200;
                _sources[i] = s;
            }
            foreach (var v in WeightCatalog.Values)
            {
                Add("thud" + v, Synth.Thud(v));
                Add("pop" + v, Synth.Pop(v));
            }
            for (int i = 1; i <= 8; i++) Add("chime" + i, Synth.Chime(i));
            Add("lift", Synth.Lift());
            Add("fall", Synth.Fall());
            Add("phew", Synth.Phew());
            Add("lost", Synth.Lost());
            Add("click", Synth.Click());
        }

        void Add(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Synth.SampleRate, false);
            clip.SetData(data, 0);
            _clips[name] = clip;
        }

        public void Play(Sfx sfx, int value = 1, float volume = 1f)
        {
            if (!Enabled) return;
            string key;
            switch (sfx)
            {
                case Sfx.Thud: key = "thud" + WeightCatalog.Normalize(value); break;
                case Sfx.Pop: key = "pop" + WeightCatalog.Normalize(value); break;
                case Sfx.Chime: key = "chime" + Mathf.Clamp(value, 1, 8); break;
                case Sfx.Lift: key = "lift"; break;
                case Sfx.Fall: key = "fall"; break;
                case Sfx.Phew: key = "phew"; break;
                case Sfx.Lost: key = "lost"; break;
                default: key = "click"; break;
            }
            if (!_clips.TryGetValue(key, out var clip)) return;
            // Pas deux fois le même son en moins de 45 ms (quand on enchaîne les poids, ils s'empilaient par dizaines).
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(key, out float last) && now - last < 0.045f) return;
            // Combos : une cascade de fusions lance des dizaines de sons dans la même image. Au-delà de 4 nouveaux sons
            // par image, les sons secondaires sont ignorés (on ne les entendrait pas, et ils faisaient saturer).
            bool important = sfx == Sfx.Chime || sfx == Sfx.Lift || sfx == Sfx.Fall || sfx == Sfx.Lost || sfx == Sfx.Phew;
            if (Time.frameCount != _frame) { _frame = Time.frameCount; _startedThisFrame = 0; }
            if (_startedThisFrame >= MaxPerFrame && !important) return;
            // Fenêtre glissante : au plus N sons lancés en 0,25 s (combos en cascade). Les sons importants passent,
            // mais eux aussi sont limités sur le web, où le navigateur sature plus vite.
            while (_recent.Count > 0 && now - _recent.Peek() > 0.25f) _recent.Dequeue();
            if (_recent.Count >= (important ? BurstImportant : Burst)) return;
            _recent.Enqueue(now);
            _startedThisFrame++;
            _lastPlayed[key] = now;
            // PlayOneShot : le son se superpose aux autres sans jamais en couper un. Avant, on arrêtait une source
            // (Stop) pour la réutiliser : un son coupé en pleine vibration fait un clic, c'était le craquement des combos.
            // Plusieurs sons en même temps : chacun un peu moins fort, pour que leur somme ne sature pas.
            _shot = (_shot + 1) % _sources.Length;
            _sources[_shot].PlayOneShot(clip, volume * Master * (1f / (1f + 0.12f * _startedThisFrame)));
            if (sfx == Sfx.Lift || sfx == Sfx.Fall) Duck?.Invoke(0.55f);
            else if (sfx == Sfx.Lost || (sfx == Sfx.Pop && value >= 32)) Duck?.Invoke(0.3f);
        }
    }

    /// <summary>Vibrations courtes. Android : durée et intensité réglables. iOS : seulement la vibration forte (sans plugin).</summary>
    public sealed class HapticsService
    {
        public bool Enabled = true;
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject _vibrator;
        int _sdk;
        bool _init;
#endif

        public void Light() => Vibrate(12, 60, false);
        public void Medium() => Vibrate(25, 140, false);
        public void Heavy() => Vibrate(60, 255, true);

        void Vibrate(long ms, int amplitude, bool strong)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (!_init)
                {
                    _init = true;
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION")) _sdk = version.GetStatic<int>("SDK_INT");
#if UNITY_2023_1_OR_NEWER
                    var activity = UnityEngine.Android.AndroidApplication.currentActivity;
#else
                    var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
#endif
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                if (_vibrator == null) return;
                if (_sdk >= 26)
                {
                    using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var e = effect.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude))
                        _vibrator.Call("vibrate", e);
                }
                else _vibrator.Call("vibrate", ms);
            }
            catch (System.Exception)
            {
                if (strong) Handheld.Vibrate();
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (strong) Handheld.Vibrate();
#endif
        }
    }
}

namespace Bascule.Services
{
    /// <summary>
    /// Musique adaptative. La couche douce joue tout le temps ; la couche tendue monte avec le danger ; le tempo
    /// accélère un peu ; près de la chute, un battement de cœur se cale exactement sur les temps de la musique
    /// (un temps sur deux, puis chaque temps), donc il accélère avec elle.
    /// Le morceau (Core/Music) est calculé en arrière-plan au lancement.
    /// Pour utiliser une vraie musique : déposer deux fichiers audio de même longueur et même tempo dans
    /// Assets/Bascule/Resources/Musique/ nommés « calme » et « tension » (et régler BpmPerso) ; ils remplacent la musique générée.
    /// </summary>
    public sealed class MusicPlayer
    {
        /// <summary>Tempo des fichiers déposés dans Resources/Musique (pour caler le battement de cœur).</summary>
        public static float BpmPerso = 96f;
        // Niveaux choisis pour que musique + cœur + bruitages restent sous la saturation, même au plus fort.
        const float CalmLevel = 0.42f, TenseLevel = 0.44f, HeartLevel = 0.6f;
#if UNITY_WEBGL && !UNITY_EDITOR
        static readonly bool Web = true;
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void BasculeAudioInit();
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int BasculeAudioRunning();
        static bool AudioRunning() => BasculeAudioRunning() != 0;
#else
        static readonly bool Web = false;
        static void BasculeAudioInit() { }
        static bool AudioRunning() => true;
#endif
        float _pitch = 1f;
        int _stalls;
        float _cooldown;

        readonly AudioSource _calm, _tense;
        // Deux sources pour le cœur : programmer le battement suivant ne coupe pas celui qui sonne encore.
        readonly AudioSource[] _hearts = new AudioSource[2];
        int _heartIndex;
        AudioClip _heartClip;
        float _intensity, _target, _volume, _targetVolume = 1f, _duck = 1f, _bpm = Music.Bpm;
        bool _enabled = true, _started;
        double _nextHeartDsp;
        System.Threading.Tasks.Task<float[][]> _job;

        public MusicPlayer(GameObject host)
        {
            _calm = Make(host);
            _tense = Make(host);
            for (int h = 0; h < 2; h++)
            {
                _hearts[h] = host.AddComponent<AudioSource>();
                _hearts[h].playOnAwake = false;
                _hearts[h].spatialBlend = 0f;
                _hearts[h].priority = 10;
            }

            var custom = Resources.Load<AudioClip>("Musique/calme");
            var customTense = Resources.Load<AudioClip>("Musique/tension");
            _heartClip = Clip("coeur", Music.Heartbeat(), Music.SampleRate);
            if (custom != null)
            {
                _bpm = BpmPerso;
                Start(custom, customTense);
                return;
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            // Pas de fils d'exécution sur le web : calcul direct (environ une seconde au lancement).
            Start(Clip("musique_douce", Music.CalmLoop(), Music.SampleRate), Clip("musique_tendue", Music.TenseLoop(), Music.SampleRate));
#else
            // Calcul du morceau hors du fil principal : le jeu se lance sans attendre.
            _job = System.Threading.Tasks.Task.Run(() => new[] { Music.CalmLoop(), Music.TenseLoop() });
#endif
        }

        static AudioClip Clip(string name, float[] data, int rate)
        {
            var c = AudioClip.Create(name, data.Length, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioSource Make(GameObject host)
        {
            var s = host.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            s.priority = 0; // jamais coupée au profit d'un bruitage
            return s;
        }

        void Start(AudioClip calm, AudioClip tense)
        {
            // Web (Safari surtout) : une seule couche de musique, sans changement de vitesse ; le cœur bat quand même (WebHeart).
            // Deux pistes décodées qui jouent ensemble, plus les bruitages, faisaient saturer le son du navigateur.
            if (Web) { BasculeAudioInit(); tense = null; }
            _calm.clip = calm;
            _tense.clip = tense;
            // Les deux couches démarrent au même instant pour rester synchronisées.
            PlayBoth(0);
            _started = true;
            _volume = 0f; // fondu d'entrée
        }

        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; if (!value) { _calm.volume = _tense.volume = 0f; } }
        }

        /// <summary>intensity 0 = tout va bien, 1 = sur le point de tomber. volume : 1 en partie, plus bas dans les menus.</summary>
        public void Set(float intensity, float volume)
        {
            _target = Mathf.Clamp01(intensity);
            _targetVolume = Mathf.Clamp01(volume);
        }

        /// <summary>Baisse la musique un instant sous un gros bruitage (fusion importante, chute, 128).</summary>
        public void Duck(float amount = 0.45f) => _duck = Mathf.Min(_duck, 1f - amount);

        public void Tick(float dt)
        {
            if (!_started && _job != null && _job.IsCompleted)
            {
                if (_job.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                    Start(Clip("musique_douce", _job.Result[0], Music.SampleRate), Clip("musique_tendue", _job.Result[1], Music.SampleRate));
                else Debug.LogWarning("[Bascule] Musique : " + _job.Exception?.GetBaseException().Message);
                _job = null;
            }
            if (!_started) return;
            Watchdog(dt);

            _intensity = Mathf.MoveTowards(_intensity, _target, dt * (_target > _intensity ? 1.2f : 0.5f));
            _volume = Mathf.MoveTowards(_volume, _targetVolume, dt * 0.7f);
            _duck = Mathf.MoveTowards(_duck, 1f, dt * 1.6f);
            if (!_enabled) return;
            float i = _intensity, v = _volume * _duck;
            _calm.volume = CalmLevel * v * (1f - 0.4f * i);
            _tense.volume = TenseLevel * v * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.9f, i));
            // Le tempo monte doucement avec le danger (les deux couches ensemble : toujours synchrones).
            // Changement de vitesse seulement s'il se remarque : moins de recalculs du lecteur (et pas de « zip » audible).
            if (Web) { WebHeart(i, dt); return; }
            float pitch = 1f + 0.14f * i * i;
            if (Mathf.Abs(pitch - _pitch) > 0.004f || (pitch == 1f && _pitch != 1f)) { _pitch = pitch; _calm.pitch = _tense.pitch = pitch; }
            HeartOnBeat(i, _pitch);
        }

        float _check;
        bool _waitedForAudio;
        int _lastCalmPos = -1, _lastTensePos = -1;

        /// <summary>
        /// Garde-fou : si la musique s'est arrêtée (son coupé par le système, appel, navigateur qui bloquait le son
        /// avant le premier toucher), on relance les deux couches ensemble, au même instant.
        /// </summary>
        void Watchdog(float dt)
        {
            _cooldown -= dt;
            _check -= dt;
            if (_check > 0) return;
            _check = 1f;
            if (!_enabled || _calm.clip == null) { _stalls = 0; return; }
            // Son du navigateur suspendu : relancer maintenant ne ferait que des « tic ». On attend qu'il reparte
            // (au prochain toucher), puis on relance la musique une seule fois.
            if (!AudioRunning()) { _stalls = 0; _waitedForAudio = true; return; }
            if (_waitedForAudio)
            {
                _waitedForAudio = false;
                _calm.Stop(); _tense.Stop();
                PlayBoth(0);
                _cooldown = 5f;
                return;
            }
            // Web : la boucle est gérée par le navigateur et « isPlaying » peut y tomber à faux au moment où le morceau
            // revient au début. Relancer à ce moment-là faisait un accroc à chaque fin de boucle (vers 1 min 17).
            // Sur le web, on ne relance donc qu'après une vraie coupure du son du navigateur (cas traité plus haut).
            if (Web) { _stalls = 0; return; }
            // Téléphone : arrêté seulement si la lecture n'avance plus. Au passage de la boucle, la position revient au
            // début : elle a changé, ce n'est pas un arrêt.
            int calmPos = _calm.timeSamples, tensePos = _tense.clip != null ? _tense.timeSamples : 0;
            bool moving = calmPos != _lastCalmPos || (_tense.clip != null && tensePos != _lastTensePos);
            _lastCalmPos = calmPos; _lastTensePos = tensePos;
            if ((_calm.isPlaying && (_tense.clip == null || _tense.isPlaying)) || moving) { _stalls = 0; return; }
            // Deux constats de suite (pas une simple mesure au mauvais moment), et au plus une relance toutes les 5 s :
            // avant, une relance chaque seconde pouvait faire grésiller la musique au lieu de la réparer.
            if (++_stalls < 2 || _cooldown > 0) return;
            _stalls = 0;
            _cooldown = 5f;
            int pos = _calm.isPlaying ? _calm.timeSamples : 0;
            _calm.Stop(); _tense.Stop();
            PlayBoth(pos);
        }

        /// <summary>Lance les deux couches au même instant (sur le web, Play tout simple : la programmation y est imprécise).</summary>
        void PlayBoth(int pos)
        {
            if (Web)
            {
                _calm.Play();
                if (_tense.clip != null) _tense.Play();
                return;
            }
            double at = AudioSettings.dspTime + 0.12;
            if (pos > 0 && pos < _calm.clip.samples) _calm.timeSamples = pos;
            _calm.PlayScheduled(at);
            if (_tense.clip != null)
            {
                if (pos > 0) _tense.timeSamples = Mathf.Min(pos, _tense.clip.samples - 1);
                _tense.PlayScheduled(at);
            }
        }

        /// <summary>
        /// Retour dans l'application après une coupure (appel, verrouillage, Siri) : sur téléphone, le système audio
        /// revient parfois abîmé (son qui grésille ou muet). On relance les deux couches proprement, au même endroit.
        /// </summary>
        public void OnResume()
        {
            if (!_started || Web) return;
            int pos = _calm.isPlaying ? _calm.timeSamples : 0;
            _calm.Stop(); _tense.Stop();
            _pitch = -1f; // réappliqué au prochain Tick
            PlayBoth(pos);
            _nextHeartDsp = 0;
            _stalls = 0; _cooldown = 2f;
        }

        float _webHeart;

        /// <summary>
        /// Web : le battement de cœur du danger, joué simplement (Play) au rythme de la musique, sans programmation
        /// précise ni changement de vitesse (ce qui faisait grésiller Safari). Un temps sur deux, puis chaque temps.
        /// </summary>
        void WebHeart(float i, float dt)
        {
            if (i < 0.35f || _volume < 0.3f || _heartClip == null) { _webHeart = 0.15f; return; }
            _webHeart -= dt;
            if (_webHeart > 0) return;
            float beat = 60f / _bpm;
            _webHeart = i > 0.7f ? beat : beat * 2f;
            float k = Mathf.InverseLerp(0.35f, 1f, i);
            var src = _hearts[_heartIndex];
            _heartIndex = 1 - _heartIndex;
            src.clip = _heartClip;
            src.volume = HeartLevel * Mathf.Lerp(0.35f, 1f, k) * _volume;
            src.pitch = 1f;
            src.Play();
        }

        /// <summary>Programme le prochain battement de cœur exactement sur un temps de la musique.</summary>
        void HeartOnBeat(float i, float pitch)
        {
            if (i < 0.35f || _volume < 0.3f || _calm.clip == null) { _nextHeartDsp = 0; return; }
            double now = AudioSettings.dspTime;
            if (_nextHeartDsp > now + 0.02) return; // déjà programmé
            int every = i > 0.7f ? 1 : 2; // un temps sur deux, puis chaque temps
            float beatSamples = _calm.clip.frequency * 60f / _bpm;
            float pos = _calm.timeSamples / beatSamples;
            float next = (Mathf.Floor(pos / every) + 1) * every;
            double wait = (next - pos) * beatSamples / (_calm.clip.frequency * pitch);
            if (wait < 0.03) wait += every * beatSamples / (_calm.clip.frequency * pitch);
            _nextHeartDsp = now + wait;
            float k = Mathf.InverseLerp(0.35f, 1f, i);
            var src = _hearts[_heartIndex];
            _heartIndex = 1 - _heartIndex;
            src.clip = _heartClip;
            src.volume = HeartLevel * Mathf.Lerp(0.35f, 1f, k) * _volume;
            src.pitch = 1f + 0.08f * k;
            src.PlayScheduled(_nextHeartDsp);
        }
    }

    /// <summary>
    /// Limiteur doux posé sur l'écouteur : il traite le mélange final (musique + bruitages). Au-delà de 0,7 le son
    /// est arrondi au lieu d'être écrêté, ce qui supprime le grésillement quand beaucoup de sons se superposent.
    /// Sans allocation (appelé sur le fil audio). Le web n'appelle pas cette fonction : les niveaux plus bas y suffisent.
    /// </summary>
    public sealed class SoftLimiter : MonoBehaviour
    {
        const float Knee = 0.7f, Room = 0.3f;

        void OnAudioFilterRead(float[] data, int channels)
        {
            for (int i = 0; i < data.Length; i++)
            {
                float x = data[i];
                float a = x < 0 ? -x : x;
                if (a <= Knee) continue;
                float y = Knee + Room * (float)System.Math.Tanh((a - Knee) / Room);
                data[i] = x < 0 ? -y : y;
            }
        }
    }
}
