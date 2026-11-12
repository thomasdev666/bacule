using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>
    /// Sélecteur en roue, comme le réglage de l'heure d'un téléphone : une seule colonne de valeurs qu'on fait défiler
    /// vers le haut ou vers le bas avec le doigt (avec de l'élan), et qui se cale toute seule sur une valeur.
    /// La valeur au centre est celle choisie. Toucher une valeur au-dessus ou au-dessous la fait venir au centre ;
    /// la molette de la souris marche aussi (éditeur). En s'éloignant du centre, les valeurs rapetissent, pâlissent et
    /// se rapprochent (effet de tambour). Quelques lignes seulement existent (recyclées), même pour cent valeurs,
    /// dans leur propre sous-canvas (Ui.Isolate) : le reste de l'écran n'est pas redessiné quand la roue tourne.
    ///
    /// À poser sur une image qui capte le doigt (raycastTarget), de préférence découpée (RectMask2D).
    /// </summary>
    public sealed class WheelPicker : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IScrollHandler
    {
        /// <summary>La valeur au centre change (à chaque ligne qui passe) : petit clic.</summary>
        public Action<int> Ticked;
        /// <summary>La roue s'est arrêtée sur cette valeur.</summary>
        public Action<int> Settled;
        public Color Near = Color.white, Far = new Color(0.85f, 0.91f, 1f);

        const float Drum = 2.8f;          // rayon du tambour, en lignes
        const float Friction = 2.4f;      // freinage de l'élan (par seconde)
        const float MaxSpeed = 70f;       // lignes par seconde
        const float SnapSpeed = 5f;       // en dessous, la roue se cale sur la ligne la plus proche

        float _rowHeight = 110f;
        string[] _items = new string[0];
        Text[] _rows = new Text[0];
        int[] _rowItem = new int[0];
        RectTransform _content;
        float _pos, _vel, _target = float.NaN, _lastDragTime, _shownPos = float.NaN;
        bool _dragging, _dragged, _settled = true;
        int _centered = -1;

        /// <summary>Valeur au centre (celle qui serait choisie maintenant).</summary>
        public int Index => _items.Length == 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(_pos), 0, _items.Length - 1);
        public bool Moving => _dragging || !_settled;

        /// <summary>Crée la roue dans <paramref name="area"/> (qui doit capter le doigt).</summary>
        public static WheelPicker Create(RectTransform area, int fontSize, float rowHeight)
        {
            var w = area.gameObject.AddComponent<WheelPicker>();
            w._rowHeight = rowHeight;
            w._content = Ui.Stretch(Ui.Rect("Valeurs", area));
            int pool = Mathf.CeilToInt(area.rect.height > 0 ? area.rect.height / rowHeight : 6f) + 3;
            pool = Mathf.Max(pool, 7);
            w._rows = new Text[pool];
            w._rowItem = new int[pool];
            for (int i = 0; i < pool; i++)
            {
                var t = Ui.Title(w._content, "", fontSize, Color.white, "Valeur");
                Ui.Place(t.rectTransform, 0.5f, 0.5f, 0, 0, 560, rowHeight);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                w._rows[i] = t;
                w._rowItem[i] = int.MinValue;
            }
            Ui.Isolate(w._content.gameObject);
            return w;
        }

        /// <summary>Remplace les valeurs et se place sur <paramref name="index"/> sans animation.</summary>
        public void SetItems(string[] items, int index)
        {
            _items = items ?? new string[0];
            for (int i = 0; i < _rowItem.Length; i++) _rowItem[i] = int.MinValue;
            _pos = Mathf.Clamp(index, 0, Mathf.Max(0, _items.Length - 1));
            _vel = 0f;
            _target = float.NaN;
            _dragging = false;
            _settled = true;
            _centered = Index;
            _shownPos = float.NaN;
            Layout();
        }

        /// <summary>Fait venir une valeur au centre, en douceur (flèches, toucher).</summary>
        public void ScrollTo(int index)
        {
            if (_items.Length == 0) return;
            _vel = 0f;
            _target = Mathf.Clamp(index, 0, _items.Length - 1);
            _settled = false;
        }

        /// <summary>Une ligne vers le haut (-1) ou vers le bas (+1).</summary>
        public void Step(int delta) => ScrollTo((float.IsNaN(_target) ? Index : Mathf.RoundToInt(_target)) + delta);

        // ------------------------------------------------------------------ Doigt

        public void OnPointerDown(PointerEventData e)
        {
            // Toucher la roue qui tourne l'attrape.
            _vel = 0f;
            _dragged = false;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            _dragging = true;
            _dragged = true;
            _settled = false;
            _target = float.NaN;
            _vel = 0f;
            _lastDragTime = Time.unscaledTime;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragging) return;
            float dy = LocalDelta(e);
            float rows = dy / _rowHeight;
            // Au-delà des bouts : la roue résiste (élastique).
            if (_pos < 0f || _pos > _items.Length - 1) rows *= 0.35f;
            _pos += rows;
            float dt = Mathf.Max(0.004f, Time.unscaledTime - _lastDragTime);
            _vel = Mathf.Lerp(_vel, Mathf.Clamp(rows / dt, -MaxSpeed, MaxSpeed), 0.4f);
            _lastDragTime = Time.unscaledTime;
        }

        public void OnEndDrag(PointerEventData e)
        {
            _dragging = false;
            // Doigt resté immobile avant de lâcher : pas d'élan.
            if (Time.unscaledTime - _lastDragTime > 0.09f) _vel = 0f;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (_dragged || _items.Length == 0) return;
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local)) return;
            float y = local.y - rt.rect.center.y;
            // Position sur le tambour -> ligne touchée.
            float k = Mathf.Clamp(y / (_rowHeight * Drum), -0.98f, 0.98f);
            float rows = Mathf.Asin(k) * Drum;
            int index = Mathf.RoundToInt(_pos - rows);
            if (index != Index) { Ui.Click?.Invoke(); ScrollTo(index); }
        }

        public void OnScroll(PointerEventData e)
        {
            if (Mathf.Abs(e.scrollDelta.y) < 0.01f) return;
            Step(e.scrollDelta.y > 0 ? -1 : 1);
        }

        float LocalDelta(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            var cam = e.pressEventCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, cam, out var now) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position - e.delta, cam, out var before))
                return now.y - before.y;
            return e.delta.y;
        }

        // ------------------------------------------------------------------ Chaque image

        void Update()
        {
            int n = _items.Length;
            if (n == 0) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (!_dragging && !_settled)
            {
                if (float.IsNaN(_target) && Mathf.Abs(_vel) > SnapSpeed && _pos >= 0f && _pos <= n - 1)
                {
                    // Élan : la roue continue sur sa lancée et ralentit.
                    _pos = Mathf.Clamp(_pos + _vel * dt, -0.6f, n - 0.4f);
                    _vel *= Mathf.Exp(-Friction * dt);
                }
                else
                {
                    if (float.IsNaN(_target))
                    {
                        // Calage sur la ligne la plus proche (dans le sens du mouvement s'il en reste un peu).
                        float ahead = _pos + Mathf.Clamp(_vel * 0.08f, -0.45f, 0.45f);
                        _target = Mathf.Clamp(Mathf.Round(ahead), 0, n - 1);
                        _vel = 0f;
                    }
                    _pos = Mathf.Lerp(_pos, _target, 1f - Mathf.Exp(-13f * dt));
                    if (Mathf.Abs(_pos - _target) < 0.003f)
                    {
                        _pos = _target;
                        _target = float.NaN;
                        _settled = true;
                        Layout();
                        Settled?.Invoke(Index);
                        return;
                    }
                }
            }
            Layout();
        }

        void Layout()
        {
            if (_pos == _shownPos) return;
            _shownPos = _pos;
            int n = _items.Length;
            int idx = Index;
            if (idx != _centered)
            {
                _centered = idx;
                Ticked?.Invoke(idx);
            }
            int first = Mathf.FloorToInt(_pos) - (_rows.Length - 1) / 2;
            for (int k = 0; k < _rows.Length; k++)
            {
                int i = first + k;
                var t = _rows[k];
                bool on = i >= 0 && i < n;
                if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
                if (!on) continue;
                if (_rowItem[k] != i) { _rowItem[k] = i; t.text = _items[i]; }
                float d = _pos - i; // > 0 : au-dessus du centre
                // Tambour : les lignes se rapprochent et s'aplatissent en s'éloignant du centre.
                float angle = Mathf.Clamp(d / Drum, -1.45f, 1.45f);
                float y = Mathf.Sin(angle) * Drum * _rowHeight;
                float a = Mathf.Abs(d);
                float s = Mathf.Lerp(1f, 0.7f, Mathf.Clamp01(a / 2.6f));
                t.rectTransform.anchoredPosition = new Vector2(0f, y);
                t.rectTransform.localScale = new Vector3(s, s * Mathf.Lerp(1f, Mathf.Cos(angle), 0.7f), 1f);
                var col = Color.Lerp(Near, Far, Mathf.Clamp01(a * 1.6f));
                col.a = Mathf.Clamp01(1.1f - a * 0.34f);
                t.color = col;
            }
        }
    }
}
