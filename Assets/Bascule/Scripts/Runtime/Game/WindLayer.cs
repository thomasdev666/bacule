using System.Collections.Generic;
using Bascule.Core;
using UnityEngine;

namespace Bascule
{
    /// <summary>
    /// Le vent à l'écran : traits d'air qui filent d'un bord à l'autre et feuilles qui tourbillonnent.
    /// Pendant l'annonce, quelques traits fins et rares (l'air se met en mouvement) ; pendant le souffle,
    /// des traits plus longs et nombreux, et des feuilles. Tout est recyclé (aucune création pendant la rafale)
    /// et rien n'est affiché quand il n'y a pas de vent.
    /// </summary>
    public sealed class WindLayer : MonoBehaviour
    {
        static readonly Color[] LeafColors =
        {
            ArtCache.Hex("#6BBF59"), ArtCache.Hex("#9BD770"), ArtCache.Hex("#E8C547"), ArtCache.Hex("#E0913A"),
        };

        sealed class Mote
        {
            public SpriteRenderer Sr;
            public bool Leaf, Alive;
            public float X, Y, Speed, Life, Total, Phase, Amp, Freq, Spin, Angle, Alpha;
        }

        ArtCache _art;
        Camera _cam;
        readonly List<Mote> _motes = new List<Mote>();
        float _warning, _strength, _streakDebt, _leafDebt;
        int _dir = 1;
        bool _reduced;
        const int MaxStreaks = 46, MaxLeaves = 10;
        int _streaks, _leaves;

        public void Init(ArtCache art, Camera cam) { _art = art; _cam = cam; }

        /// <summary>
        /// État du vent pour cette image : <paramref name="warning"/> (0..1) pendant l'annonce,
        /// <paramref name="strength"/> (0..1) pendant le souffle, <paramref name="dir"/> +1 vers la droite.
        /// </summary>
        public void Set(float warning, float strength, int dir, bool reducedMotion)
        {
            _warning = warning; _strength = strength; _reduced = reducedMotion;
            if (warning > 0f || strength > 0f) _dir = dir >= 0 ? 1 : -1;
        }

        public void Clear()
        {
            foreach (var m in _motes) { m.Alive = false; m.Sr.gameObject.SetActive(false); }
            _streaks = _leaves = 0;
            _streakDebt = _leafDebt = 0f;
        }

        Mote Take(bool leaf)
        {
            foreach (var m in _motes)
                if (!m.Alive && m.Leaf == leaf) return m;
            var sr = SpriteUtil.Make(leaf ? "Feuille" : "Souffle", transform, leaf ? _art.Confetti : _art.Capsule, Color.white, leaf ? Layers.Effects - 5 : Layers.Effects - 10);
            var mote = new Mote { Sr = sr, Leaf = leaf };
            _motes.Add(mote);
            return mote;
        }

        void Spawn(bool leaf, float hw, float bottom, float top)
        {
            var m = Take(leaf);
            m.Alive = true;
            m.Sr.gameObject.SetActive(true);
            float edge = hw + (leaf ? 0.5f : 2.5f);
            m.X = -_dir * edge + Random.Range(-0.5f, 0.5f) * _dir;
            m.Y = Random.Range(bottom, top);
            m.Phase = Random.Range(0f, 6.28f);
            if (leaf)
            {
                _leaves++;
                m.Speed = Random.Range(5.5f, 8.5f);
                m.Amp = Random.Range(0.25f, 0.6f);
                m.Freq = Random.Range(2.5f, 4.5f);
                m.Spin = Random.Range(220f, 520f) * (Random.value < 0.5f ? -1f : 1f);
                m.Angle = Random.Range(0f, 360f);
                m.Alpha = 1f;
                m.Sr.color = LeafColors[Random.Range(0, LeafColors.Length)];
                // Le confetti mesure 0,18 unité : une feuille fait 0,3 à 0,4 unité, aplatie en ovale.
                float s = Random.Range(1.7f, 2.3f);
                m.Sr.transform.localScale = new Vector3(s, s * 0.55f, 1f);
            }
            else
            {
                _streaks++;
                bool gust = _strength > 0.05f;
                m.Speed = Random.Range(13f, 20f) * (gust ? 1f : 0.75f);
                m.Amp = Random.Range(0.03f, 0.1f);
                m.Freq = Random.Range(2f, 3.5f);
                m.Alpha = gust ? Random.Range(0.32f, 0.6f) : Random.Range(0.18f, 0.3f);
                float len = gust ? Random.Range(1.4f, 3f) : Random.Range(0.8f, 1.6f);
                float thick = gust ? Random.Range(0.045f, 0.085f) : Random.Range(0.035f, 0.055f);
                // Capsule de 4 × 1 unités (bouts ronds) : étirée en trait.
                m.Sr.drawMode = SpriteDrawMode.Simple;
                m.Sr.transform.localScale = new Vector3(len / 4f, thick, 1f);
                m.Sr.color = new Color(1f, 1f, 1f, 0f);
            }
            m.Total = m.Life = (2f * edge) / m.Speed;
        }

        void Update()
        {
            if (_cam == null) return;
            float dt = Time.deltaTime;
            float hw = WorldLayout.HalfWidth(_cam);
            float size = _cam.orthographicSize;
            float camY = _cam.transform.position.y;
            float bottom = camY - size + 2.6f, top = camY + size - 3.2f;

            // Débit : quelques traits pendant l'annonce, beaucoup pendant le souffle ; trois fois moins en « animations réduites ».
            float k = _reduced ? 0.33f : 1f;
            float streakRate = (_warning * 7f + _strength * 38f) * k;
            float leafRate = _strength * 4.5f * k;
            _streakDebt += streakRate * dt;
            _leafDebt += leafRate * dt;
            while (_streakDebt >= 1f) { _streakDebt -= 1f; if (_streaks < MaxStreaks) Spawn(false, hw, bottom, top); }
            while (_leafDebt >= 1f) { _leafDebt -= 1f; if (_leaves < MaxLeaves) Spawn(true, hw, bottom, top); }

            foreach (var m in _motes)
            {
                if (!m.Alive) continue;
                m.Life -= dt;
                // Les traits déjà lancés ralentissent quand le vent retombe (ils ne s'arrêtent pas net).
                float push = 0.55f + 0.45f * Mathf.Max(_strength, _warning * 0.6f);
                m.X += _dir * m.Speed * push * dt;
                m.Phase += m.Freq * dt;
                float y = m.Y + Mathf.Sin(m.Phase) * m.Amp;
                m.Sr.transform.position = new Vector3(m.X, y, 0f);
                float t = 1f - Mathf.Clamp01(m.Life / m.Total);
                if (m.Leaf)
                {
                    m.Angle += m.Spin * dt;
                    m.Sr.transform.rotation = Quaternion.Euler(0, 0, m.Angle);
                }
                else
                {
                    // Apparition et disparition en fondu, inclinaison légère qui suit l'ondulation.
                    float a = m.Alpha * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
                    var c = m.Sr.color; c.a = a; m.Sr.color = c;
                    m.Sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Cos(m.Phase) * m.Amp * 40f * _dir);
                }
                if (m.Life <= 0f || Mathf.Abs(m.X) > hw + 4f)
                {
                    m.Alive = false;
                    m.Sr.gameObject.SetActive(false);
                    if (m.Leaf) _leaves--; else _streaks--;
                }
            }
        }
    }
}
