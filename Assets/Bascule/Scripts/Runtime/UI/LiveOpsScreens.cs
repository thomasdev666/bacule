using System;
using Bascule.Core;
using Bascule.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>
    /// Rendez-vous de l'accueil (roue, événement de la semaine, quête du débutant) : un médaillon rond avec son dessin
    /// en couleur (petite roue qui tourne doucement, bloc-notes coché, calendrier étoilé), un anneau d'avancement doré,
    /// une étiquette sombre dessous et une pastille « ! » quand il y a quelque chose à prendre.
    /// Ancrés en bas, sur les côtés, sous la planche de Basile : ils restent à côté de ses jambes sur tous les écrans
    /// (sur un iPhone haut, la planche paraît plus large et plus haute ; plus bas, rien ne la touche).
    /// </summary>
    public sealed class SideWidget
    {
        // Compacts : ils signalent le rendez-vous sans voler la vedette à Basile ni au bouton Jouer.
        public const float Width = 156f, Height = 162f, Medal = 112f, Edge = 88f;
        /// <summary>Hauteurs (depuis le bas de l'écran) des deux rangées : au-dessus du dock, sous la planche.</summary>
        public const float Row1 = 555f, Row2 = 365f;

        public RectTransform Root;
        public Button Button;
        public Text Label;
        public GameObject Badge;
        public Image Ring;

        public static SideWidget Create(Transform parent, ArtCache art, Sprite picture, Color color, Action onTap, string name, float side, float y, bool ring, bool wheel = false)
        {
            var w = new SideWidget();
            w.Root = Ui.Rect(name, parent);
            Ui.Place(w.Root, side < 0 ? 0 : 1, 0, side < 0 ? Edge : -Edge, y, Width, Height);
            float my = -5f - Medal * 0.5f - (ring ? 6f : 0f); // centre du médaillon, depuis le haut
            if (ring)
            {
                // Anneau d'avancement (événement de la semaine, quête) autour du médaillon.
                var track = Ui.Image(w.Root, art.Ring, new Color(1f, 1f, 1f, 0.4f), "Piste");
                Ui.Place(track.rectTransform, 0.5f, 1, 0, my, Medal + 20, Medal + 20);
                w.Ring = Ui.Image(w.Root, art.Ring, Palette.Accent, "Anneau");
                Ui.Place(w.Ring.rectTransform, 0.5f, 1, 0, my, Medal + 20, Medal + 20);
                w.Ring.type = Image.Type.Filled; w.Ring.fillMethod = Image.FillMethod.Radial360; w.Ring.fillOrigin = 2; w.Ring.fillClockwise = true;
            }
            // Médaillon : contour sombre, disque de couleur, reflet clair en haut, dessin au centre.
            var rim = Ui.Image(w.Root, art.Circle, Palette.Outline, "Medaillon");
            Ui.Place(rim.rectTransform, 0.5f, 1, 0, my, Medal, Medal);
            rim.raycastTarget = true;
            var disc = Ui.Image(rim.transform, art.Circle, color, "Fond");
            Ui.Place(disc.rectTransform, 0.5f, 0.5f, 0, 0, Medal - 11, Medal - 11);
            var shine = Ui.Image(disc.transform, art.SoftDisc, new Color(1f, 1f, 1f, 0.3f), "Reflet");
            Ui.Place(shine.rectTransform, 0.5f, 1, 0, -4, Medal * 0.72f, Medal * 0.4f, 0.5f, 1);
            var pic = Ui.Icon(disc.transform, picture, "Dessin");
            Ui.Place(pic.rectTransform, 0.5f, 0.5f, 0, 0, wheel ? Medal - 26 : Medal - 20, wheel ? Medal - 26 : Medal - 20);
            if (wheel)
            {
                // Petite roue qui tourne doucement, avec son moyeu et son repère rouge (qui, eux, ne tournent pas).
                pic.gameObject.AddComponent<Spin>().Speed = -18f;
                var hub = Ui.Image(disc.transform, art.Circle, Palette.Outline, "Moyeu");
                Ui.Place(hub.rectTransform, 0.5f, 0.5f, 0, 0, 26, 26); // roue et moyeu bien au centre du médaillon
                var hubIn = Ui.Image(hub.transform, art.Circle, Palette.Accent, "Centre");
                Ui.Place(hubIn.rectTransform, 0.5f, 0.5f, 0, 0, 18, 18);
                var pointer = Ui.Icon(rim.transform, art.IconSprite(Icon.Play), "Repere");
                pointer.color = Palette.Primary;
                Ui.Place(pointer.rectTransform, 0.5f, 1, 0, 6, 38, 38, 0.5f, 0.5f);
                pointer.rectTransform.localRotation = Quaternion.Euler(0, 0, -90f);
            }
            w.Button = rim.gameObject.AddComponent<Button>();
            w.Button.transition = Selectable.Transition.None;
            w.Button.onClick.AddListener(() => onTap?.Invoke());
            rim.gameObject.AddComponent<PressScale>();

            // Étiquette sombre, lisible sur n'importe quel décor.
            var pill = Ui.Image(w.Root, art.Pill, new Color(0.12f, 0.07f, 0.28f, 0.88f), "Etiquette", true);
            Ui.Place(pill.rectTransform, 0.5f, 0, 0, 0, Width, 46, 0.5f, 0);
            w.Label = Ui.Title(pill.transform, "", 30, Color.white, "Legende");
            Ui.Stretch(w.Label.rectTransform, 10, 2, 10, 3);

            // Pastille « ! » sur le coin du médaillon.
            var badge = Ui.Image(rim.transform, art.Circle, Palette.Primary, "Pastille");
            Ui.Place(badge.rectTransform, 1, 1, -8, -8, 40, 40, 0.5f, 0.5f);
            var o = badge.gameObject.AddComponent<Outline>();
            o.effectColor = Color.white; o.effectDistance = new Vector2(3, -3);
            var t = Ui.Title(badge.transform, "!", 28);
            Ui.Stretch(t.rectTransform, 0, 0, 0, 3);
            badge.gameObject.AddComponent<Pulse>().Amount = 0.1f;
            w.Badge = badge.gameObject;
            Ui.Isolate(w.Root.gameObject);
            return w;
        }

        /// <summary>Change de rangée (l'événement monte à la place de la quête une fois celle-ci finie).</summary>
        public void MoveTo(float side, float y) =>
            Ui.Place(Root, side < 0 ? 0 : 1, 0, side < 0 ? Edge : -Edge, y, Width, Height);
    }

    // ===================================================================== Roue de la chance

    /// <summary>
    /// Roue de la chance : un tour gratuit toutes les 24 h, deux tours en plus par jour avec une vidéo. La roue
    /// accélère, tourne plusieurs fois, ralentit en cliquetant à chaque part (le repère bascule), s'arrête en
    /// rebondissant ; ampoules qui clignotent, puis le lot apparaît en grand (confettis pour le gros lot).
    /// Chances affichées (bouton « Chances »).
    /// </summary>
    public sealed class WheelScreen : UiScreen
    {
        const float Size = 860f, SpinSeconds = 4.8f;
        RectTransform _wheel, _pointer, _card, _odds;
        Text _status, _cardTitle, _cardDetail;
        Image _cardIcon, _cardGlow;
        Button _spin;
        Text _spinLabel;
        readonly Image[] _bulbs = new Image[16];
        float _angle, _from, _to, _t = -1f, _pointerKick, _clock, _blink;
        int _lastTick;
        Wheel.Result _pending;
        bool _pendingAd;

        static readonly Color Deep = ArtCache.Hex("#1E1240");

        protected override void Build()
        {
            BackButton(Close);
            var title = Ui.Title(Safe, Loc.Get("wheel_title"), 84, Palette.Accent);
            Ui.Place(title.rectTransform, 0.5f, 1, 70, -112, 760, 130);
            _status = Ui.Label(Safe, "", 40, ArtCache.Hex("#D8E9FF"));
            Ui.Place(_status.rectTransform, 0.5f, 1, 0, -228, 900, 70);

            // Halo et rayons derrière la roue.
            var holder = Ui.Rect("Roue", Safe);
            Ui.Place(holder, 0.5f, 0.5f, 0, 70, Size, Size);
            var glow = Ui.Image(holder, Art.SoftDisc, new Color(1f, 0.78f, 0.3f, 0.55f), "Halo");
            Ui.Place(glow.rectTransform, 0.5f, 0.5f, 0, 0, Size * 1.35f, Size * 1.35f);
            var rays = Ui.Image(holder, Art.Rays, new Color(1f, 0.85f, 0.4f, 0.3f), "Rayons");
            Ui.Place(rays.rectTransform, 0.5f, 0.5f, 0, 0, Size * 1.7f, Size * 1.7f);
            rays.gameObject.AddComponent<Spin>().Speed = 8f;
            Ui.Isolate(rays.gameObject);

            // La roue elle-même (tourne), ses lots et ses ampoules.
            _wheel = Ui.Rect("Plateau", holder);
            Ui.Place(_wheel, 0.5f, 0.5f, 0, 0, Size, Size);
            Ui.Isolate(_wheel.gameObject);
            var board = Ui.Image(_wheel, Art.WheelBoard, Color.white, "Plateau");
            Ui.Stretch(board.rectTransform);
            for (int i = 0; i < Wheel.Slots.Length; i++) SlotLabel(i);
            float bulbR = Size * 0.5f * 0.914f;
            for (int i = 0; i < _bulbs.Length; i++)
            {
                double a = i * Math.PI / 8 + Math.PI / 16;
                var b = Ui.Image(_wheel, Art.Circle, Color.white, "Ampoule");
                Ui.Place(b.rectTransform, 0.5f, 0.5f, (float)Math.Cos(a) * bulbR, (float)Math.Sin(a) * bulbR, 34, 34);
                _bulbs[i] = b;
            }

            // Moyeu : touche pour tourner.
            var hubRim = Ui.Image(holder, Art.Circle, Palette.Outline, "Moyeu");
            Ui.Place(hubRim.rectTransform, 0.5f, 0.5f, 0, 0, 200, 200);
            var hub = Ui.Image(hubRim.transform, Art.Circle, Palette.Accent, "Centre");
            Ui.Place(hub.rectTransform, 0.5f, 0.5f, 0, 0, 176, 176);
            hub.raycastTarget = true;
            var hubBtn = hub.gameObject.AddComponent<Button>();
            hubBtn.transition = Selectable.Transition.None;
            hubBtn.onClick.AddListener(() => { if (_t < 0) Spin(); });
            hub.gameObject.AddComponent<PressScale>();
            var go = Ui.Title(hub.transform, Loc.Get("wheel_go"), 56);
            Ui.Stretch(go.rectTransform, 0, 0, 0, 6);

            // Repère en haut : une flèche rouge qui bascule à chaque part.
            var pointer = Ui.Icon(holder, Art.IconSprite(Icon.Play), "Repere");
            _pointer = pointer.rectTransform;
            Ui.Place(_pointer, 0.5f, 0.5f, 0, Size * 0.5f + 6, 130, 130, 0.5f, 0.7f);
            _pointer.localRotation = Quaternion.Euler(0, 0, -90f);
            pointer.color = Palette.Primary;
            Ui.Isolate(pointer.gameObject);

            // Bouton principal : tour gratuit, ou tour en plus avec une vidéo.
            _spin = Ui.Button(Safe, Loc.Get("wheel_spin"), Palette.Play, Spin, Icon.Wheel, 60, "Tourner");
            Ui.Place((RectTransform)_spin.transform, 0.5f, 0, 0, 260, 820, 170);
            _spinLabel = Ui.ButtonLabel(_spin);
            _spin.gameObject.AddComponent<Pulse>().Amount = 0.02f;
            var odds = Ui.Button(Safe, Loc.Get("wheel_odds"), Palette.Muted, ToggleOdds, null, 40, "Chances");
            Ui.Place((RectTransform)odds.transform, 0.5f, 0, 0, 100, 420, 110);

            BuildOdds();
            BuildCard();
        }

        // Disposition d'une part, depuis le centre (en part de la taille de la roue) : le lot en image au milieu
        // de la part, son nom tout près du bord doré, là où la part est la plus large (les mots longs y tiennent).
        const float IconR = 0.238f, TextR = 0.37f;

        void SlotLabel(int i)
        {
            var slot = Wheel.Slots[i];
            var arm = Ui.Rect("Lot" + i, _wheel);
            Ui.Place(arm, 0.5f, 0.5f, 0, 0, 10, 10);
            arm.localRotation = Quaternion.Euler(0, 0, -i * 360f / Wheel.Slots.Length);
            float iconY = Size * IconR, textY = Size * TextR;
            switch (slot.Kind)
            {
                case WheelPrize.Coins:
                {
                    // Plus le lot est gros, plus il y a de pièces : 1 pièce, 2 pièces, puis un tas de 3.
                    float w = slot.Coins >= 200 ? 150f : slot.Coins >= 100 ? 134f : 116f;
                    var pile = Ui.Icon(arm, Art.CoinPile(slot.Coins), "Pieces");
                    Ui.Place(pile.rectTransform, 0.5f, 0.5f, 0, iconY + 4, w, w);
                    break;
                }
                case WheelPrize.Boost:
                {
                    // Pièces doublées : une pièce marquée « ×2 ».
                    var coin = Ui.Icon(arm, Art.CoinPile(1), "Piece");
                    Ui.Place(coin.rectTransform, 0.5f, 0.5f, 0, iconY + 4, 118, 118);
                    var x2 = Ui.Title(coin.transform, "×2", 56, Color.white);
                    Ui.Place(x2.rectTransform, 0.5f, 0.5f, 0, 6, 110, 70);
                    break;
                }
                case WheelPrize.Chest:
                {
                    var chest = MetaUi.MiniChest(arm, Art, 124);
                    Ui.Place(chest, 0.5f, 0.5f, 0, iconY, 124, 112);
                    break;
                }
                case WheelPrize.Jackpot when Wheel.LuckyHead != null:
                {
                    var face = Ui.Icon(arm, Art.Portrait(Wheel.LuckyHead), "Tete");
                    Ui.Place(face.rectTransform, 0.5f, 0.5f, 0, iconY, 136, 136);
                    break;
                }
                default:
                {
                    var ic = Ui.Icon(arm, Art.IconSprite(SlotIcon(slot)), "Icone");
                    ic.color = slot.Kind == WheelPrize.Shield ? ArtCache.Hex("#FFB347") : Color.white;
                    Ui.Place(ic.rectTransform, 0.5f, 0.5f, 0, iconY, 108, 108);
                    break;
                }
            }
            bool number = slot.Kind == WheelPrize.Coins;
            var color = slot.Kind == WheelPrize.Jackpot ? ArtCache.Hex("#FFD447") : Color.white;
            var t = Ui.Title(arm, SlotText(slot), number ? 54 : 40, color);
            Ui.Place(t.rectTransform, 0.5f, 0.5f, 0, textY, number ? 200 : 226, 62);
        }

        static Icon SlotIcon(WheelSlot s)
        {
            switch (s.Kind)
            {
                case WheelPrize.Ticket: return Icon.Gift;
                case WheelPrize.Shield: return Icon.Flame;
                case WheelPrize.Jackpot: return Icon.Crown;
                default: return Icon.Coin;
            }
        }

        static string SlotText(WheelSlot s)
        {
            switch (s.Kind)
            {
                case WheelPrize.Coins: return Loc.FormatScore(s.Coins);
                case WheelPrize.Chest: return Loc.Get("wheel_slot_chest");
                case WheelPrize.Ticket: return Loc.Get("wheel_slot_ticket");
                case WheelPrize.Shield: return Loc.Get("wheel_slot_shield");
                case WheelPrize.Boost: return Loc.Get("wheel_slot_boost");
                default: return Loc.Get("wheel_slot_jackpot");
            }
        }

        void BuildOdds()
        {
            var panel = Ui.Image(Safe, Art.Panel, Deep, "Chances", true);
            _odds = panel.rectTransform;
            Ui.Place(_odds, 0.5f, 0.5f, 0, 60, 880, 1060);
            panel.raycastTarget = true;
            panel.gameObject.AddComponent<PopIn>();
            var t = Ui.Title(panel.transform, Loc.Get("wheel_odds_title"), 60, Palette.Accent);
            Ui.Place(t.rectTransform, 0.5f, 1, 0, -80, 800, 90);
            for (int i = 0; i < Wheel.Slots.Length; i++)
            {
                var row = Ui.Glass(panel.transform, "Ligne");
                Ui.Place(row.rectTransform, 0.5f, 1, 0, -170 - i * 100, 780, 88, 0.5f, 1);
                var dot = Ui.Image(row.transform, Art.Circle, ArtCache.Hex(Wheel.Slots[i].Color), "Couleur");
                Ui.Place(dot.rectTransform, 0, 0.5f, 50, 0, 48, 48);
                var name = Ui.Title(row.transform, PrizeName(Wheel.Slots[i]), 38, Color.white);
                Ui.Stretch(name.rectTransform, 96, 4, 180, 4);
                name.alignment = TextAnchor.MiddleLeft;
                var pc = Ui.Title(row.transform, Loc.Format("wheel_chance", (Wheel.Chance(i) * 100f).ToString("0")), 40, Palette.Accent);
                Ui.Place(pc.rectTransform, 1, 0.5f, -30, 0, 150, 70, 1, 0.5f);
                pc.alignment = TextAnchor.MiddleRight;
            }
            var close = Ui.Button(panel.transform, Loc.Get("ok"), Palette.Secondary, ToggleOdds, null, 46, "Fermer");
            Ui.Place((RectTransform)close.transform, 0.5f, 0, 0, 70, 420, 110);
            panel.gameObject.SetActive(false);
        }

        static string PrizeName(WheelSlot s)
        {
            switch (s.Kind)
            {
                case WheelPrize.Coins: return Loc.Format("wheel_prize_coins", Loc.FormatScore(s.Coins));
                case WheelPrize.Chest: return Loc.Get("wheel_prize_chest");
                case WheelPrize.Ticket: return Loc.Get("wheel_prize_ticket");
                case WheelPrize.Shield: return Loc.Get("wheel_prize_shield");
                case WheelPrize.Boost: return Loc.Format("wheel_prize_boost", Wheel.BoostGames);
                default: return Loc.Get("wheel_prize_jackpot");
            }
        }

        void BuildCard()
        {
            // Le lot gagné, en grand, au-dessus de la roue.
            var card = Ui.Image(Safe, Art.Panel, Deep, "Lot", true);
            _card = card.rectTransform;
            Ui.Place(_card, 0.5f, 0.5f, 0, 60, 860, 820);
            card.raycastTarget = true;
            card.gameObject.AddComponent<PopIn>();
            card.gameObject.AddComponent<RectMask2D>();
            var rays = Ui.Image(card.transform, Art.Rays, new Color(1f, 0.85f, 0.4f, 0.4f), "Rayons");
            Ui.Place(rays.rectTransform, 0.5f, 1, 0, -300, 1100, 1100);
            rays.gameObject.AddComponent<Spin>().Speed = 20f;
            Ui.Isolate(rays.gameObject);
            var won = Ui.Title(card.transform, Loc.Get("wheel_won"), 60, Palette.Accent);
            Ui.Place(won.rectTransform, 0.5f, 1, 0, -80, 800, 90);
            _cardGlow = Ui.Image(card.transform, Art.SoftDisc, new Color(1f, 0.85f, 0.4f, 0.7f), "Halo");
            Ui.Place(_cardGlow.rectTransform, 0.5f, 1, 0, -300, 420, 420);
            _cardIcon = Ui.Icon(card.transform, null, "Lot");
            Ui.Place(_cardIcon.rectTransform, 0.5f, 1, 0, -300, 260, 260);
            _cardX2 = Ui.Title(_cardIcon.transform, "×2", 110);
            Ui.Place(_cardX2.rectTransform, 0.5f, 0.5f, 0, 14, 220, 130);
            _cardIcon.gameObject.AddComponent<Rock>().Angle = 5f;
            _cardTitle = Ui.Title(card.transform, "", 68, Color.white);
            Ui.Place(_cardTitle.rectTransform, 0.5f, 1, 0, -500, 800, 100);
            _cardDetail = Ui.Label(card.transform, "", 40, ArtCache.Hex("#D8E9FF"));
            Ui.Place(_cardDetail.rectTransform, 0.5f, 1, 0, -590, 780, 90);
            var ok = Ui.Button(card.transform, Loc.Get("chest_ok"), Palette.Play, CloseCard, Icon.Check, 58, "Super");
            Ui.Place((RectTransform)ok.transform, 0.5f, 0, 0, 80, 600, 150);
            card.gameObject.SetActive(false);

            // Pluie de pièces qui jaillit du lot (hors de la carte, pour qu'elles débordent) : plus le gain est gros, plus il y en a.
            _rain = Ui.Rect("PluieDePieces", Safe);
            Ui.Place(_rain, 0.5f, 0.5f, 0, 60 + 410 - 300, 10, 10);
            for (int i = 0; i < RainMax; i++)
            {
                var c = Ui.Icon(_rain, Art.CoinPile(1), "Piece");
                _rainCoins[i] = c.rectTransform;
                c.raycastTarget = false;
                c.gameObject.SetActive(false);
            }
            Ui.Isolate(_rain.gameObject);
            _rain.gameObject.SetActive(false);
        }

        const int RainMax = 28;
        const float RainLife = 1.5f;
        RectTransform _rain;
        Text _cardX2;
        readonly RectTransform[] _rainCoins = new RectTransform[RainMax];
        readonly Vector2[] _rainPos = new Vector2[RainMax], _rainVel = new Vector2[RainMax];
        readonly float[] _rainSpin = new float[RainMax], _rainDelay = new float[RainMax], _rainSize = new float[RainMax];
        int _rainCount;
        float _rainT = -1f;

        /// <summary>Nombre de pièces qui jaillissent : 6 pour 50, 10 pour 100, 18 pour 250, 28 pour le jackpot.</summary>
        static int RainFor(int coins) => coins <= 0 ? 0 : coins < 100 ? 6 : coins < 200 ? 10 : coins < 1000 ? 18 : RainMax;

        void StartRain(int coins)
        {
            _rainCount = App.Profile.settings.reducedMotion ? 0 : RainFor(coins);
            if (_rainCount == 0) return;
            for (int i = 0; i < RainMax; i++)
            {
                bool on = i < _rainCount;
                _rainCoins[i].gameObject.SetActive(on);
                if (!on) continue;
                float a = UnityEngine.Random.Range(25f, 155f) * Mathf.Deg2Rad;
                float speed = UnityEngine.Random.Range(950f, 1750f);
                _rainPos[i] = new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-30f, 30f));
                _rainVel[i] = new Vector2(Mathf.Cos(a) * speed, Mathf.Sin(a) * speed);
                _rainSpin[i] = UnityEngine.Random.Range(-420f, 420f);
                _rainDelay[i] = i * 0.025f;
                _rainSize[i] = UnityEngine.Random.Range(62f, 96f);
                _rainCoins[i].sizeDelta = new Vector2(_rainSize[i], _rainSize[i]);
                _rainCoins[i].anchoredPosition = _rainPos[i];
                _rainCoins[i].localScale = Vector3.zero;
            }
            _rain.gameObject.SetActive(true);
            _rain.SetAsLastSibling();
            _rainT = 0f;
        }

        void TickRain(float dt)
        {
            if (_rainT < 0f) return;
            _rainT += dt;
            bool any = false;
            for (int i = 0; i < _rainCount; i++)
            {
                float t = _rainT - _rainDelay[i];
                var rt = _rainCoins[i];
                if (t < 0f) { any = true; continue; }
                if (t > RainLife) { if (rt.gameObject.activeSelf) rt.gameObject.SetActive(false); continue; }
                any = true;
                _rainVel[i].y -= 3000f * dt;
                _rainPos[i] += _rainVel[i] * dt;
                rt.anchoredPosition = _rainPos[i];
                rt.localRotation = Quaternion.Euler(0, 0, _rainSpin[i] * t);
                float grow = Mathf.Clamp01(t / 0.12f), fade = 1f - Mathf.Clamp01((t - (RainLife - 0.4f)) / 0.4f);
                rt.localScale = Vector3.one * grow * Mathf.Max(0.01f, fade);
            }
            if (!any) { _rainT = -1f; _rain.gameObject.SetActive(false); }
        }

        public override void Refresh()
        {
            _odds.gameObject.SetActive(false);
            _card.gameObject.SetActive(_pending != null);
            if (_pending == null) { _rainT = -1f; _rain.gameObject.SetActive(false); }
            UpdateButtons();
        }

        void UpdateButtons()
        {
            var p = App.Profile;
            bool free = Wheel.FreeReady(p.wheel, App.Now);
            int ads = Wheel.AdSpinsLeft(p.wheel, App.LocalDay);
            bool spinning = _t >= 0f;
            if (free)
            {
                _spin.GetComponent<Image>().color = Palette.Play;
                _spinLabel.text = Loc.Get("wheel_spin");
                _spin.interactable = !spinning;
                _status.text = Loc.Get("wheel_free_ready");
            }
            else
            {
                string wait = FormatWait(Wheel.FreeIn(p.wheel, App.Now));
                _status.text = Loc.Format("wheel_free_in", wait);
                _spin.GetComponent<Image>().color = ads > 0 ? Palette.Purple : Palette.Muted;
                _spinLabel.text = ads > 0 ? Loc.Format("wheel_spin_ad", ads) : Loc.Get("wheel_come_back");
                _spin.interactable = ads > 0 && !spinning;
            }
        }

        static string FormatWait(double seconds)
        {
            var t = TimeSpan.FromSeconds(Math.Ceiling(seconds));
            return t.TotalHours >= 1 ? Loc.Format("daily_hours", (int)t.TotalHours, t.Minutes) : Loc.Format("daily_minutes", Math.Max(1, t.Minutes));
        }

        void ToggleOdds() => _odds.gameObject.SetActive(!_odds.gameObject.activeSelf);

        void Spin()
        {
            if (_t >= 0f || _pending != null) return;
            var p = App.Profile;
            if (Wheel.FreeReady(p.wheel, App.Now)) { Launch(false); return; }
            if (Wheel.AdSpinsLeft(p.wheel, App.LocalDay) <= 0) return;
            App.WatchRewarded(Placements.WheelSpin, () => Launch(true));
        }

        void Launch(bool viaAd)
        {
            var res = App.SpinWheel(viaAd);
            if (res == null) { UpdateButtons(); return; }
            _pending = res;
            _pendingAd = viaAd;
            // La part gagnée vient sous le repère après 5 tours (et un petit décalage au hasard dans la part).
            float seg = 360f / Wheel.Slots.Length;
            float jitter = UnityEngine.Random.Range(-seg * 0.32f, seg * 0.32f);
            _from = _angle;
            float target = res.Slot * seg + jitter;
            float k = Mathf.Floor((_from - target) / 360f);
            _to = target + 360f * (k - 5f);
            _t = 0f;
            _lastTick = Mathf.FloorToInt(_angle / seg);
            UpdateButtons();
        }

        public override void Tick(float dt)
        {
            _clock += dt;
            // Ampoules : elles clignotent doucement au repos, puis à toute vitesse pendant le tour.
            _blink += dt * (_t >= 0f ? 14f : 2.5f);
            for (int i = 0; i < _bulbs.Length; i++)
            {
                bool on = ((int)_blink + i) % 2 == 0;
                _bulbs[i].color = on ? new Color(1f, 0.97f, 0.75f) : new Color(1f, 0.62f, 0.2f, 0.85f);
            }
            if (_t >= 0f)
            {
                _t += dt;
                float x = Mathf.Clamp01(_t / SpinSeconds);
                // Démarrage franc, long ralentissement, petit rebond à l'arrêt.
                float e = 1f - Mathf.Pow(1f - x, 4f);
                float settle = x > 0.92f ? Mathf.Sin((x - 0.92f) / 0.08f * Mathf.PI) * 1.5f : 0f;
                _angle = Mathf.LerpUnclamped(_from, _to, e) + settle;
                float seg = 360f / Wheel.Slots.Length;
                int tick = Mathf.FloorToInt(_angle / seg);
                if (tick != _lastTick)
                {
                    _lastTick = tick;
                    _pointerKick = 1f;
                    App.PlaySfx(Sfx.Click, 2, 0.45f);
                    if (x > 0.6f) App.HapticLight();
                }
                if (x >= 1f)
                {
                    _t = -1f;
                    _angle = _to;
                    Reveal();
                }
                _wheel.localRotation = Quaternion.Euler(0, 0, _angle);
            }
            _pointerKick = Mathf.MoveTowards(_pointerKick, 0f, dt * 6f);
            _pointer.localRotation = Quaternion.Euler(0, 0, -90f + _pointerKick * 18f);
            if (_card.gameObject.activeSelf) _cardGlow.rectTransform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(_clock * 4f));
            TickRain(dt);
            if (!_card.gameObject.activeSelf && _t < 0f && ((int)(_clock * 2f) % 2 == 0)) UpdateButtons(); // compte à rebours
        }

        void Reveal()
        {
            var r = _pending;
            var slot = Wheel.Slots[r.Slot];
            _card.gameObject.SetActive(true);
            _card.SetAsLastSibling();
            _cardIcon.color = Color.white;
            _cardX2.gameObject.SetActive(r.Kind == WheelPrize.Boost);
            if (r.Head != null) _cardIcon.sprite = Art.Portrait(r.Head);
            else if (r.Kind == WheelPrize.Chest) _cardIcon.sprite = Art.ChestBase;
            else if (r.Kind == WheelPrize.Boost) _cardIcon.sprite = Art.CoinPile(1);
            else if (r.Coins > 0) _cardIcon.sprite = Art.CoinPile(r.Coins);
            else
            {
                _cardIcon.sprite = Art.IconSprite(SlotIcon(slot));
                if (r.Kind == WheelPrize.Shield) _cardIcon.color = ArtCache.Hex("#FFB347");
            }
            // Les pièces gagnées jaillissent du lot (le gros lot en fait pleuvoir le plus).
            if (r.Kind != WheelPrize.Boost) StartRain(r.Coins);
            _cardTitle.text = r.Head != null ? r.Head.Name(Loc.Language)
                : r.Coins > 0 && r.Kind != WheelPrize.Coins ? Loc.Format("wheel_prize_coins", Loc.FormatScore(r.Coins))
                : PrizeName(slot);
            _cardDetail.text = Loc.Get("wheel_detail_" + r.Kind);
            bool big = r.Kind == WheelPrize.Jackpot || r.Kind == WheelPrize.Chest || (r.Kind == WheelPrize.Coins && r.Coins >= 250);
            App.PlaySfx(Sfx.Chime, big ? 10 : 6, 0.9f);
            App.PlaySfx(Sfx.Lift, 0, big ? 0.9f : 0.5f);
            App.HapticMedium();
        }

        void CloseCard()
        {
            var r = _pending;
            _pending = null;
            _card.gameObject.SetActive(false);
            UpdateButtons();
            App.AfterWheelPrize(r, _pendingAd);
        }

        void Close()
        {
            if (_t >= 0f) return; // on ne quitte pas pendant que la roue tourne
            if (_pending != null) CloseCard();
            App.GoHome();
        }

        public void Back() => Close();
    }

    // ===================================================================== Quête du débutant

    /// <summary>Les trois étapes de la quête du débutant, avec leur récompense ; « Jouer » pour avancer.</summary>
    public sealed class QuestScreen : UiScreen
    {
        readonly (Image dot, Text num, Image check, Text text, Text reward)[] _rows = new (Image, Text, Image, Text, Text)[3];
        Text _sub;

        protected override void Build()
        {
            var card = Ui.Image(Safe, Art.Panel, ArtCache.Hex("#2B1B54"), "Fenetre", true);
            Ui.Place(card.rectTransform, 0.5f, 0.5f, 0, 20, 980, 1300);
            card.gameObject.AddComponent<PopIn>();
            card.gameObject.AddComponent<RectMask2D>();
            var rays = Ui.Image(card.transform, Art.Rays, new Color(1f, 0.85f, 0.4f, 0.25f), "Rayons");
            Ui.Place(rays.rectTransform, 0.5f, 1, 0, -260, 1300, 1300);
            rays.gameObject.AddComponent<Spin>().Speed = 10f;
            Ui.Isolate(rays.gameObject);
            var icon = Ui.Image(card.transform, Art.Circle, Palette.Success, "Pastille");
            Ui.Place(icon.rectTransform, 0.5f, 1, 0, -150, 170, 170);
            var ic = Ui.Icon(icon.transform, Art.QuestBadge, "BlocNotes");
            Ui.Stretch(ic.rectTransform, 16, 16, 16, 16);
            var title = Ui.Title(card.transform, Loc.Get("quest_title"), 76, Palette.Accent);
            Ui.Place(title.rectTransform, 0.5f, 1, 0, -300, 900, 110);
            _sub = Ui.Label(card.transform, "", 40, ArtCache.Hex("#D8E9FF"));
            Ui.Place(_sub.rectTransform, 0.5f, 1, 0, -380, 860, 70);
            for (int i = 0; i < 3; i++)
            {
                var row = Ui.Glass(card.transform, "Etape");
                Ui.Place(row.rectTransform, 0.5f, 1, 0, -450 - i * 170, 900, 150, 0.5f, 1);
                var dot = Ui.Image(row.transform, Art.Circle, Palette.Secondary, "Numero");
                Ui.Place(dot.rectTransform, 0, 0.5f, 70, 0, 96, 96);
                var num = Ui.Title(dot.transform, (i + 1).ToString(), 50);
                Ui.Stretch(num.rectTransform, 0, 0, 0, 4);
                var check = Ui.Icon(dot.transform, Art.IconSprite(Icon.Check), "Fait");
                Ui.Stretch(check.rectTransform, 16, 16, 16, 16);
                var text = Ui.Title(row.transform, Loc.Get(BeginnerQuest.Steps[i].Key), 42, Color.white);
                Ui.Place(text.rectTransform, 0, 1, 140, -46, 500, 60, 0, 0.5f);
                text.alignment = TextAnchor.MiddleLeft;
                var reward = Ui.Label(row.transform, "", 34, Palette.Accent, TextAnchor.MiddleLeft);
                Ui.Place(reward.rectTransform, 0, 0, 140, 42, 520, 50, 0, 0.5f);
                _rows[i] = (dot, num, check, text, reward);
            }
            var play = Ui.Button(card.transform, Loc.Get("play"), Palette.Play, () => { Hide(); App.Play(GameMode.Infinite); }, Icon.Play, 62, "Jouer");
            Ui.Place((RectTransform)play.transform, 0.5f, 0, 0, 220, 760, 166);
            play.gameObject.AddComponent<Pulse>().Amount = 0.025f;
            var back = Ui.Button(card.transform, Loc.Get("back"), Palette.Muted, Hide, null, 46, "Retour");
            Ui.Place((RectTransform)back.transform, 0.5f, 0, 0, 74, 460, 112);
        }

        public override void Refresh()
        {
            var p = App.Profile;
            _sub.text = Loc.Format("quest_sub", p.quest.done, BeginnerQuest.Steps.Length);
            for (int i = 0; i < 3; i++)
            {
                var s = BeginnerQuest.Steps[i];
                bool done = p.quest.done > i, current = p.quest.done == i;
                var (dot, num, check, text, reward) = _rows[i];
                dot.color = done ? Palette.Play : current ? Palette.Accent : new Color(1, 1, 1, 0.25f);
                num.gameObject.SetActive(!done);
                check.gameObject.SetActive(done);
                text.color = done ? new Color(1, 1, 1, 0.6f) : Color.white;
                string r = s.Reward == BeginnerQuest.Kind.Coins ? Loc.Format("quest_reward_coins", s.Coins)
                    : s.Reward == BeginnerQuest.Kind.Chest ? Loc.Get("quest_reward_chest") : Loc.Format("quest_reward_plank", PlankCatalog.Get(BeginnerQuest.BambooPlank).Name(Loc.Language));
                if (current && s.Index > 0) r += "  ·  " + Loc.Format("quest_progress", Mathf.Min(BeginnerQuest.Value(p, s), s.Target), s.Target);
                reward.text = done ? Loc.Get("quest_reward_done") : r;
            }
        }
    }

    // ===================================================================== Événement de la semaine

    /// <summary>Événement de la semaine : le monde à l'honneur, l'objectif commun, trois paliers et le temps restant.</summary>
    public sealed class EventScreen : UiScreen
    {
        Text _title, _goal, _count, _left;
        Image _banner;
        RectTransform _fill;
        readonly (Image disc, Image icon, Text label, Image check)[] _tiers = new (Image, Image, Text, Image)[3];
        float _clock;

        protected override void Build()
        {
            var card = Ui.Image(Safe, Art.Panel, ArtCache.Hex("#2B1B54"), "Fenetre", true);
            Ui.Place(card.rectTransform, 0.5f, 0.5f, 0, 20, 980, 1400);
            card.gameObject.AddComponent<PopIn>();
            card.gameObject.AddComponent<RectMask2D>();
            // Bandeau à la couleur du monde de la semaine.
            _banner = Ui.Image(card.transform, Art.Panel, Palette.Secondary, "Bandeau", true);
            Ui.Place(_banner.rectTransform, 0.5f, 1, 0, -30, 920, 300, 0.5f, 1);
            var rays = Ui.Image(_banner.transform, Art.Rays, new Color(1f, 1f, 1f, 0.22f), "Rayons");
            Ui.Place(rays.rectTransform, 0.5f, 0.5f, 0, 0, 1100, 1100);
            rays.gameObject.AddComponent<Spin>().Speed = 9f;
            _banner.gameObject.AddComponent<RectMask2D>();
            Ui.Isolate(rays.gameObject);
            var tag = Ui.Image(_banner.transform, Art.Pill, Palette.Primary, "Etiquette", true);
            Ui.Place(tag.rectTransform, 0.5f, 1, 0, -50, 420, 70);
            var tagText = Ui.Title(tag.transform, Loc.Get("event_tag"), 36, Color.white);
            Ui.Stretch(tagText.rectTransform, 12, 2, 12, 0);
            _title = Ui.Title(_banner.transform, "", 70, Color.white);
            Ui.Place(_title.rectTransform, 0.5f, 0.5f, 0, -10, 860, 110);
            _left = Ui.Title(_banner.transform, "", 36, ArtCache.Hex("#FFF1B8"));
            Ui.Place(_left.rectTransform, 0.5f, 0, 0, 40, 860, 56);

            _goal = Ui.Title(card.transform, "", 52, Palette.Accent);
            Ui.Place(_goal.rectTransform, 0.5f, 1, 0, -400, 900, 120);
            var bar = Ui.Rect("Jauge", card.transform);
            Ui.Place(bar, 0.5f, 1, 0, -500, 860, 56);
            _fill = MetaUi.Bar(bar, Art, Palette.Accent);
            Ui.Stretch((RectTransform)_fill.parent);
            _fill.parent.GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);
            _count = Ui.Title(card.transform, "", 44, Color.white);
            Ui.Place(_count.rectTransform, 0.5f, 1, 0, -572, 860, 64);

            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 300f;
                var box = Ui.Glass(card.transform, "Palier");
                Ui.Place(box.rectTransform, 0.5f, 1, x, -640, 280, 340, 0.5f, 1);
                // De haut en bas : « Palier N », le lot, puis son nom (sur deux lignes au besoin, sans rien chevaucher).
                var step = Ui.Title(box.transform, Loc.Format("event_tier", i + 1), 30, ArtCache.Hex("#BFE0FF"));
                Ui.Place(step.rectTransform, 0.5f, 1, 0, -32, 190, 44);
                var disc = Ui.Image(box.transform, Art.Circle, new Color(1, 1, 1, 0.15f), "Fond");
                Ui.Place(disc.rectTransform, 0.5f, 1, 0, -136, 156, 156);
                var icon = Ui.Icon(box.transform, null, "Lot");
                Ui.Place(icon.rectTransform, 0.5f, 1, 0, -136, 136, 136);
                var label = Ui.Title(box.transform, "", 32, Color.white);
                Ui.Place(label.rectTransform, 0.5f, 0, 0, 62, 256, 96);
                var check = Ui.Image(box.transform, Art.Circle, Palette.Play, "Fait");
                Ui.Place(check.rectTransform, 1, 1, -14, -14, 58, 58, 1, 1);
                var ci = Ui.Icon(check.transform, Art.IconSprite(Icon.Check), "Coche");
                Ui.Stretch(ci.rectTransform, 10, 10, 10, 10);
                _tiers[i] = (disc, icon, label, check);
            }

            var play = Ui.Button(card.transform, Loc.Get("play"), Palette.Play, () => { Hide(); App.Play(GameMode.Infinite); }, Icon.Play, 62, "Jouer");
            Ui.Place((RectTransform)play.transform, 0.5f, 0, 0, 220, 760, 166);
            play.gameObject.AddComponent<Pulse>().Amount = 0.025f;
            var back = Ui.Button(card.transform, Loc.Get("back"), Palette.Muted, Hide, null, 46, "Retour");
            Ui.Place((RectTransform)back.transform, 0.5f, 0, 0, 74, 460, 112);
        }

        public override void Refresh()
        {
            var p = App.Profile;
            var d = App.CurrentEvent;
            WeeklyEvent.Roll(p.week, d.Week);
            var theme = Theme.All[Mathf.Clamp(d.Theme, 0, Theme.All.Length - 1)];
            _banner.color = new Color(theme.SkyTop.r / 255f, theme.SkyTop.g / 255f, theme.SkyTop.b / 255f);
            _title.text = Loc.Get("event_name_" + d.Theme);
            _goal.text = GoalText(d);
            _count.text = Loc.Format("event_count", ValueText(d, p.week.progress), ValueText(d, d.Target));
            Canvas.ForceUpdateCanvases();
            MetaUi.SetFill(_fill, d.Target > 0 ? p.week.progress / (float)d.Target : 0f);
            for (int i = 0; i < 3; i++)
            {
                var (disc, icon, label, check) = _tiers[i];
                bool done = p.week.tiers > i;
                check.gameObject.SetActive(done);
                icon.color = Color.white;
                if (i == 0) { icon.sprite = Art.CoinPile(WeeklyEvent.TierCoins); icon.color = Color.white; label.text = Loc.Format("quest_reward_coins", WeeklyEvent.TierCoins); }
                else if (i == 1) { icon.sprite = Art.IconSprite(Icon.Gift); label.text = Loc.Get("event_reward_ticket"); }
                else if (d.Head != null && !p.unlockedHeads.Contains(d.Head.Id) || (done && d.Head != null))
                {
                    icon.sprite = Art.Portrait(d.Head);
                    label.text = Loc.Format("event_reward_head", d.Head.Name(Loc.Language));
                }
                else { icon.sprite = Art.CoinPile(WeeklyEvent.OwnedHeadCoins); icon.color = Color.white; label.text = Loc.Format("quest_reward_coins", WeeklyEvent.OwnedHeadCoins); }
                disc.color = done ? new Color(0.36f, 0.82f, 0.43f, 0.45f) : new Color(1, 1, 1, 0.15f);
            }
            UpdateLeft();
        }

        public static string GoalText(WeeklyEvent.Def d) => Loc.Format("event_goal_" + d.Kind, ValueText(d, d.Target));

        public static string ValueText(WeeklyEvent.Def d, int v) => d.Kind == WeeklyEvent.Kind.HoldSeconds ? Loc.FormatDuration(v) : Loc.FormatScore(v);

        void UpdateLeft()
        {
            var t = WeeklyEvent.Left(DateTime.UtcNow);
            _left.text = t.TotalDays >= 1 ? Loc.Format("event_left_days", (int)t.TotalDays, t.Hours) : Loc.Format("daily_hours", (int)t.TotalHours, t.Minutes);
        }

        public override void Tick(float dt)
        {
            _clock += dt;
            if (_clock >= 30f) { _clock = 0f; UpdateLeft(); }
        }
    }
}
