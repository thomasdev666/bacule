using System;
using System.Collections.Generic;
using Bascule.Core;
using Bascule.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>Petits morceaux d'interface partagés par les écrans de progression.</summary>
    public static class MetaUi
    {
        /// <summary>Icône propre à chaque type de mission (au lieu d'une cible pour toutes).</summary>
        public static Icon MissionIcon(MissionKind k)
        {
            switch (k)
            {
                case MissionKind.PlayGames: return Icon.Play;
                case MissionKind.ScoreInGame: case MissionKind.NoHeartLost: return Icon.Star;
                case MissionKind.TotalScore: return Icon.Trophy;
                case MissionKind.Merges: return Icon.Plus;
                case MissionKind.MakeValue: return Icon.Crown;
                case MissionKind.Balloon: return Icon.Gift;
                case MissionKind.Combo: return Icon.Flame;
                case MissionKind.NearMiss: return Icon.Heart;
                case MissionKind.PlayDaily: return Icon.Calendar;
                case MissionKind.SurviveGusts: return Icon.Swords;
                case MissionKind.HoldSeconds: return Icon.Hand;
                case MissionKind.CleanScore: return Icon.Check;
                case MissionKind.PlayDuels: case MissionKind.WinDuels: return Icon.Swords;
                default: return Icon.Target;
            }
        }

        public static Color MissionColor(MissionKind k)
        {
            switch (k)
            {
                case MissionKind.PlayGames: return Palette.Play;
                case MissionKind.ScoreInGame: case MissionKind.NoHeartLost: return Palette.Accent;
                case MissionKind.TotalScore: return Palette.Secondary;
                case MissionKind.Merges: return Palette.Success;
                case MissionKind.MakeValue: return Palette.Purple;
                case MissionKind.Combo: return Palette.Primary;
                case MissionKind.NearMiss: return Palette.Primary;
                case MissionKind.PlayDaily: return Palette.Secondary;
                case MissionKind.SurviveGusts: return Palette.Secondary;
                case MissionKind.HoldSeconds: return Palette.Success;
                case MissionKind.CleanScore: return Palette.Accent;
                case MissionKind.PlayDuels: return Palette.Primary;
                case MissionKind.WinDuels: return Palette.Purple;
                default: return Palette.Purple;
            }
        }

        /// <summary>Pastille « pièces » : icône dorée + montant.</summary>
        public static Text CoinPill(Transform parent, ArtCache art, out RectTransform rt, int fontSize = 52, Action onClick = null)
        {
            var pill = Ui.Glass(parent, "Pieces");
            rt = pill.rectTransform;
            var row = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter; row.spacing = 10; row.padding = new RectOffset(18, 26, 4, 4);
            row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = row.childForceExpandHeight = false;
            var ic = Ui.Icon(pill.transform, art.IconSprite(Icon.Coin), "Piece");
            ic.color = Palette.Accent;
            Ui.Size(ic, fontSize * 1.25f, fontSize * 1.25f);
            var t = Ui.Title(pill.transform, "0", fontSize, Color.white, "Montant");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (onClick != null)
            {
                pill.raycastTarget = true;
                var b = pill.gameObject.AddComponent<Button>();
                b.onClick.AddListener(() => { Ui.Click?.Invoke(); onClick(); });
                pill.gameObject.AddComponent<PressScale>();
                var plus = Ui.Icon(pill.transform, art.IconSprite(Icon.Plus), "Plus");
                plus.color = Palette.Play;
                Ui.Size(plus, fontSize * 0.9f, fontSize * 0.9f);
            }
            return t;
        }

        /// <summary>Barre de progression arrondie ; renvoie le remplissage (largeur à régler avec SetFill).</summary>
        public static RectTransform Bar(Transform parent, ArtCache art, Color fill, string name = "Barre")
        {
            var track = Ui.Image(parent, art.Pill, new Color(0.17f, 0.18f, 0.26f, 0.22f), name, true);
            CapsuleSlice.Attach(track);
            var fi = Ui.Image(track.transform, art.Pill, fill, "Remplissage", true);
            CapsuleSlice.Attach(fi);
            var f = fi.rectTransform;
            f.anchorMin = new Vector2(0, 0); f.anchorMax = new Vector2(0, 1); f.pivot = new Vector2(0, 0.5f);
            f.anchoredPosition = Vector2.zero;
            Shine(fi);
            return f;
        }

        static readonly Dictionary<int, Sprite> PlankPreviews = new Dictionary<int, Sprite>();

        /// <summary>Aperçu d'une planche avec ses rebords (dessiné une fois, à la demande ; partagé par la boutique et le cadeau).</summary>
        public static Sprite PlankPreview(ArtCache art, PlankSkin s)
        {
            if (PlankPreviews.TryGetValue(s.Id, out var sp) && sp != null) return sp;
            var raster = SceneArtist.PlankPreview(s, 4.4f, 0.62f, 96f, 0.2f, 0.36f);
            sp = ArtCache.ToSprite(raster, 96f, Vector4.zero);
            PlankPreviews[s.Id] = sp;
            return sp;
        }

        /// <summary>Petit coffre fermé (corps + couvercle) dans une zone de <paramref name="w"/> × 0,9 w.</summary>
        public static RectTransform MiniChest(Transform parent, ArtCache art, float w)
        {
            var holder = Ui.Rect("Coffre", parent);
            holder.sizeDelta = new Vector2(w, w * 0.9f);
            var body = Ui.Image(holder, art.ChestBase, Color.white, "Corps");
            Ui.Place(body.rectTransform, 0.5f, 0, 0, 0, w, w * 0.66f, 0.5f, 0);
            var lid = Ui.Image(holder, art.ChestLid, Color.white, "Couvercle");
            Ui.Place(lid.rectTransform, 0.5f, 0, 0, w * 0.53f, w, w * 0.42f, 0.5f, 0.03f / 0.42f);
            return holder;
        }

        /// <summary>Reflet clair sur le haut d'un remplissage de barre : l'effet « bonbon » des boutons.</summary>
        public static void Shine(Image fill)
        {
            var sh = Ui.Image(fill.transform, Ui.Art.Pill, new Color(1, 1, 1, 0.38f), "Reflet", true);
            var rt = sh.rectTransform;
            rt.anchorMin = new Vector2(0, 0.52f); rt.anchorMax = new Vector2(1, 0.86f);
            rt.offsetMin = new Vector2(7, 0); rt.offsetMax = new Vector2(-7, 0);
            CapsuleSlice.Attach(sh);
        }

        public static void SetFill(RectTransform fill, float t)
        {
            var parent = (RectTransform)fill.parent;
            float w = parent.rect.width > 1 ? parent.rect.width : parent.sizeDelta.x;
            fill.sizeDelta = new Vector2(Mathf.Max(parent.rect.height > 1 ? parent.rect.height : 30f, w * Mathf.Clamp01(t)), 0);
        }

        /// <summary>Pastille rouge (nombre de choses à faire) dans le coin d'un bouton.</summary>
        public static Text Badge(Transform parent, ArtCache art, out GameObject root)
        {
            var b = Ui.Image(parent, art.Pill, Palette.Primary, "Pastille", true);
            root = b.gameObject;
            Ui.Place(b.rectTransform, 1, 1, 10, 10, 76, 76, 1, 1);
            var t = Ui.Title(b.transform, "", 44);
            Ui.Stretch(t.rectTransform, 0, -2, 0, 4);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            b.gameObject.AddComponent<Pulse>().Amount = 0.06f;
            return t;
        }
    }

    // ===================================================================== Fenêtre générique

    /// <summary>Question avec une image, un texte et un ou deux boutons (achat de tête, offre, rappels, statistiques).</summary>
    public sealed class ConfirmScreen : UiScreen
    {
        Text _title, _body;
        Image _image;
        Button _ok, _cancel;
        Action _onOk, _onCancel;

        protected override void Build()
        {
            var card = Ui.Card(Safe, "Fenetre");
            Ui.Place(card.rectTransform, 0.5f, 0.5f, 0, 40, 920, 1140);
            card.gameObject.AddComponent<PopIn>();
            _title = Ui.Title(card.transform, "", 70, Palette.Accent);
            Ui.Place(_title.rectTransform, 0.5f, 1, 0, -100, 840, 140);
            _title.resizeTextForBestFit = true; _title.resizeTextMinSize = 48; _title.resizeTextMaxSize = _title.fontSize;
            _image = Ui.Icon(card.transform, null, "Image");
            Ui.Place(_image.rectTransform, 0.5f, 1, 0, -330, 280, 280);
            _body = Ui.Label(card.transform, "", 46, Palette.Ink, TextAnchor.UpperCenter);
            Ui.Stretch(_body.rectTransform, 60, 500, 60, 300);
            // Libellé provisoire : un bouton créé sans texte n'a pas de Label à modifier ensuite.
            _ok = Ui.Button(card.transform, "OK", Palette.Play, () => { var a = _onOk; Hide(); a?.Invoke(); }, null, 56, "Oui");
            _cancel = Ui.Button(card.transform, "OK", Palette.Muted, () => { var a = _onCancel; Hide(); a?.Invoke(); }, null, 48, "Non");
        }

        public void Ask(string title, string body, Sprite image, string okLabel, Action onOk, string cancelLabel, Action onCancel)
        {
            Show();
            _title.text = title;
            _body.text = body ?? "";
            _image.sprite = image;
            _image.gameObject.SetActive(image != null);
            _onOk = onOk;
            _onCancel = onCancel;
            Ui.ButtonLabel(_ok).text = okLabel;
            bool two = !string.IsNullOrEmpty(cancelLabel);
            _cancel.gameObject.SetActive(two);
            if (two)
            {
                Ui.ButtonLabel(_cancel).text = cancelLabel;
                Ui.Place((RectTransform)_ok.transform, 0.5f, 0, 0, 260, 760, 160);
                Ui.Place((RectTransform)_cancel.transform, 0.5f, 0, 0, 100, 560, 120);
            }
            else Ui.Place((RectTransform)_ok.transform, 0.5f, 0, 0, 150, 640, 160);
        }
    }

    // ===================================================================== Défi du jour

    /// <summary>
    /// Défi du jour : le défi en grand sur une carte violette, la récompense en pastille dorée (ou « réussi »), le meilleur
    /// score du jour et la série, la règle en une ligne, et un compte à rebours jusqu'au prochain défi.
    /// </summary>
    public sealed class DailyScreen : UiScreen
    {
        Text _sub, _challenge, _pillText, _best, _streak;
        Image _pill, _pillIcon;
        Button _play;
        float _clock;

        protected override void Build()
        {
            var card = Ui.Card(Safe, "Fenetre");
            Ui.Place(card.rectTransform, 0.5f, 0.5f, 0, 20, 960, 1400);
            card.gameObject.AddComponent<PopIn>();
            var title = Ui.Title(card.transform, Loc.Get("daily"), 84, Palette.Accent);
            Ui.Place(title.rectTransform, 0.5f, 1, 0, -92, 880, 120);
            _sub = Ui.Label(card.transform, "", 38, Palette.Muted);
            Ui.Place(_sub.rectTransform, 0.5f, 1, 0, -172, 880, 56);

            // Carte du défi (violette), avec halo et cible.
            var glow = Ui.Image(card.transform, Art.SoftDisc, new Color(0.65f, 0.42f, 1f, 0.35f), "Halo");
            Ui.Place(glow.rectTransform, 0.5f, 1, 0, -440, 1000, 520);
            var panel = Ui.Image(card.transform, Art.Panel, Palette.Purple, "Defi", true);
            Ui.Place(panel.rectTransform, 0.5f, 1, 0, -220, 860, 440, 0.5f, 1);
            var target = Ui.Icon(panel.transform, Art.IconSprite(Icon.Target), "Cible");
            target.color = new Color(1, 1, 1, 0.9f);
            Ui.Place(target.rectTransform, 0.5f, 1, 0, -70, 96, 96);
            var label = Ui.Label(panel.transform, Loc.Get("daily_your_challenge"), 36, new Color(1, 1, 1, 0.85f));
            Ui.Place(label.rectTransform, 0.5f, 1, 0, -146, 760, 50);
            _challenge = Ui.Title(panel.transform, "", 60, Color.white);
            Ui.Place(_challenge.rectTransform, 0.5f, 1, 0, -248, 780, 150);
            _challenge.resizeTextForBestFit = true; _challenge.resizeTextMinSize = 40; _challenge.resizeTextMaxSize = 60;
            // Pastille de récompense.
            _pill = Ui.Image(panel.transform, Art.Pill, Palette.Accent, "Recompense", true);
            Ui.Place(_pill.rectTransform, 0.5f, 0, 0, 34, 470, 92, 0.5f, 0);
            _pillIcon = Ui.Icon(_pill.transform, Art.IconSprite(Icon.Coin), "Piece");
            Ui.Place(_pillIcon.rectTransform, 0, 0.5f, 52, 0, 66, 66);
            _pillText = Ui.Title(_pill.transform, "", 44, Color.white);
            Ui.Stretch(_pillText.rectTransform, 90, 4, 24, 0);
            _pill.gameObject.AddComponent<Pulse>().Amount = 0.04f;

            // Meilleur score du jour et série.
            _best = StatTile(card.transform, -0.5f, Icon.Trophy, Palette.Accent, Loc.Get("daily_best_label"));
            _streak = StatTile(card.transform, 0.5f, Icon.Flame, Palette.Primary, Loc.Get("daily_streak_label"));

            var rule = Ui.Label(card.transform, Loc.Get("daily_rule_short"), 34, Palette.Muted);
            Ui.Place(rule.rectTransform, 0.5f, 1, 0, -1000, 840, 100);

            _play = Ui.Button(card.transform, Loc.Get("play"), Palette.Play, () => { Hide(); App.Play(GameMode.Daily); }, Icon.Play, 60, "Jouer");
            Ui.Place((RectTransform)_play.transform, 0.5f, 0, 0, 236, 760, 160);
            var back = Ui.Button(card.transform, Loc.Get("back"), Palette.Muted, Hide, null, 46, "Retour");
            Ui.Place((RectTransform)back.transform, 0.5f, 0, 0, 86, 520, 116);
        }

        Text StatTile(Transform parent, float side, Icon icon, Color tint, string caption)
        {
            var tile = Ui.Image(parent, Art.Pill, new Color(1f, 1f, 1f, 0.75f), "Tuile", true);
            Ui.Place(tile.rectTransform, 0.5f, 1, side * 430f, -700, 410, 200, 0.5f, 1);
            var ic = Ui.Icon(tile.transform, Art.IconSprite(icon), "Icone");
            ic.color = tint;
            Ui.Place(ic.rectTransform, 0, 0.5f, 70, 18, 76, 76);
            // Texte simple, sans contour : un titre foncé cerné de foncé devient une tache illisible.
            var value = Ui.Label(tile.transform, "0", 60, Palette.Ink);
            Ui.Place(value.rectTransform, 0.5f, 0.5f, 40, 26, 270, 84);
            var cap = Ui.Label(tile.transform, caption, 32, Palette.Muted);
            Ui.Place(cap.rectTransform, 0.5f, 0, 0, 22, 370, 54, 0.5f, 0);
            return value;
        }

        public override void Refresh()
        {
            var p = App.Profile;
            string today = App.TodayKey;
            var obj = DailyChallenge.For(today);
            _challenge.text = Loc.DailyText(obj);
            bool done = DailyChallenge.DoneToday(p, today);
            _pill.color = done ? Palette.Success : Palette.Accent;
            _pillIcon.gameObject.SetActive(!done);
            _pillText.text = done ? Loc.Get("daily_done_pill") : Loc.Format("daily_reward_pill", obj.Reward);
            ((RectTransform)_pillText.transform).offsetMin = new Vector2(done ? 24 : 90, 4);
            int best = p.daily.bestDate == today ? p.daily.bestScoreToday : 0;
            _best.text = Loc.FormatScore(best);
            int streak = Daily.CurrentStreak(p.daily, today);
            _streak.text = streak.ToString();
            Ui.ButtonLabel(_play).text = Loc.Get(Daily.PlayedToday(p.daily, today) ? "daily_again" : "play");
            UpdateClock();
        }

        public override void Tick(float dt)
        {
            _clock -= dt;
            if (_clock <= 0f) UpdateClock();
        }

        void UpdateClock()
        {
            _clock = 1f;
            // Le défi change à minuit UTC (comme App.TodayKey).
            var left = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
            string time = left.TotalHours >= 1 ? Loc.Format("daily_hours", (int)left.TotalHours, left.Minutes) : Loc.Format("daily_minutes", Math.Max(1, left.Minutes));
            _sub.text = Loc.Format("daily_sub", Brag.DayNumber(App.TodayKey), time);
        }
    }

    // ===================================================================== Bienvenue (lancement)

    /// <summary>
    /// Écran de bienvenue du lancement, montré une fois à tout le monde : Basile sur fond de rayons, merci d'avoir
    /// téléchargé le jeu, ce qui attend le joueur en quatre lignes, et les classements tout neufs.
    /// </summary>
    public sealed class WelcomeScreen : UiScreen
    {
        protected override void Build()
        {
            var card = Ui.Card(Safe, "Fenetre");
            Ui.Place(card.rectTransform, 0.5f, 0.5f, 0, 20, 960, 1480);
            card.gameObject.AddComponent<PopIn>();
            var title = Ui.Title(card.transform, Loc.Get("welcome_title"), 100, Palette.Accent);
            Ui.Place(title.rectTransform, 0.5f, 1, 0, -96, 880, 130);
            var sub = Ui.Label(card.transform, Loc.Get("welcome_sub"), 44, Palette.Ink);
            Ui.Place(sub.rectTransform, 0.5f, 1, 0, -184, 880, 60);

            // Bandeau violet : rayons qui tournent, Basile, étiquette « C'est le lancement ».
            var hero = Ui.Image(card.transform, Art.Panel, Palette.Purple, "Bandeau", true);
            Ui.Place(hero.rectTransform, 0.5f, 1, 0, -236, 860, 380, 0.5f, 1);
            hero.gameObject.AddComponent<RectMask2D>();
            var rays = Ui.Image(hero.transform, Art.Rays, new Color(1f, 0.9f, 0.5f, 0.35f), "Rayons");
            Ui.Place(rays.rectTransform, 0.5f, 0.5f, 0, -10, 900, 900);
            rays.gameObject.AddComponent<Spin>().Speed = 12f;
            var glow = Ui.Image(hero.transform, Art.SoftDisc, new Color(1f, 0.85f, 0.4f, 0.55f), "Halo");
            Ui.Place(glow.rectTransform, 0.5f, 0.5f, 0, 10, 420, 420);
            var basile = Ui.Image(hero.transform, Art.Portrait(HeadCatalog.Get(0)), Color.white, "Basile");
            Ui.Place(basile.rectTransform, 0.5f, 0.5f, 0, 18, 270, 270);
            basile.preserveAspect = true;
            basile.gameObject.AddComponent<Rock>().Angle = 4f;
            var tag = Ui.Image(hero.transform, Art.Pill, Palette.Primary, "Lancement", true);
            Ui.Place(tag.rectTransform, 0.5f, 0, 0, 22, 520, 74, 0.5f, 0);
            var tagText = Ui.Title(tag.transform, Loc.Get("welcome_launch"), 38, Color.white);
            Ui.Stretch(tagText.rectTransform, 20, 2, 20, 0);
            tag.gameObject.AddComponent<Pulse>().Amount = 0.04f;

            // Ce qui attend le joueur.
            Feature(card.transform, 0, Icon.Star, Palette.Secondary, Loc.Get("welcome_f1"));
            Feature(card.transform, 1, Icon.Face, Palette.Purple, Loc.Get("welcome_f2"));
            Feature(card.transform, 2, Icon.Calendar, Palette.Accent, Loc.Get("welcome_f3"));
            // Le duel n'est annoncé que s'il fonctionne dans cette version (jamais de fonction promise mais absente).
            if (App.DuelAvailable) Feature(card.transform, 3, Icon.Swords, Palette.Primary, Loc.Get("welcome_f4"));
            else Feature(card.transform, 3, Icon.Trophy, Palette.Primary, Loc.Get("welcome_f4_solo"));

            var note = Ui.Label(card.transform, Loc.Get("welcome_note"), 36, Palette.Muted);
            Ui.Place(note.rectTransform, 0.5f, 1, 0, -1178, 840, 100);

            var go = Ui.Button(card.transform, Loc.Get("welcome_ok"), Palette.Play, Hide, Icon.Play, 62, "Partir");
            Ui.Place((RectTransform)go.transform, 0.5f, 0, 0, 120, 760, 160);
            go.gameObject.AddComponent<Pulse>().Amount = 0.025f;
        }

        void Feature(Transform parent, int i, Icon icon, Color tint, string text)
        {
            var row = Ui.Image(parent, Art.Pill, new Color(1f, 1f, 1f, 0.75f), "Atout", true);
            Ui.Place(row.rectTransform, 0.5f, 1, 0, -650 - i * 120, 860, 104, 0.5f, 1);
            var dot = Ui.Image(row.transform, Art.Circle, tint, "Pastille");
            Ui.Place(dot.rectTransform, 0, 0.5f, 62, 0, 80, 80);
            var ic = Ui.Icon(dot.transform, Art.IconSprite(icon), "Icone");
            ic.color = Color.white;
            Ui.Stretch(ic.rectTransform, 14, 14, 14, 14);
            var label = Ui.Label(row.transform, text, 38, Palette.Ink, TextAnchor.MiddleLeft);
            Ui.Stretch(label.rectTransform, 124, 6, 30, 6);
        }
    }

    // ===================================================================== Cadeau du jour

    public sealed class GiftScreen : UiScreen
    {
        // Sept jours en tuiles (3 + 3, puis le grand cadeau du jour 7 en large) : chaque tuile montre ce qu'on y gagne
        // (pièces, coffre, surprise gratuite, planche). Les jours passés sont cochés, celui d'aujourd'hui brille,
        // celui de demain est annoncé : on a envie de revenir.
        RectTransform _days;
        Text _sub;
        Button _claim, _double;
        const float Tile = 262f, TileGap = 18f;

        protected override void Build()
        {
            var card = Ui.Card(Safe, "Fenetre");
            Ui.Place(card.rectTransform, 0.5f, 0.5f, 0, 10, 960, 1430);
            card.gameObject.AddComponent<PopIn>();
            var title = Ui.Title(card.transform, Loc.Get("gift_title"), 84, Palette.Accent);
            Ui.Place(title.rectTransform, 0.5f, 1, 0, -92, 880, 120);
            _sub = Ui.Label(card.transform, "", 40, Palette.Muted);
            Ui.Place(_sub.rectTransform, 0.5f, 1, 0, -176, 860, 56);
            _days = Ui.Rect("Jours", card.transform);
            Ui.Place(_days, 0.5f, 1, 0, -220, 880, 840, 0.5f, 1);
            _claim = Ui.Button(card.transform, "OK", Palette.Play, () => App.ClaimGift(false), Icon.Gift, 60, "Recuperer");
            Ui.Place((RectTransform)_claim.transform, 0.5f, 0, 0, 244, 760, 160);
            _double = Ui.Button(card.transform, Loc.Get("gift_double"), Palette.Purple, () => App.ClaimGift(true), Icon.Film, 52, "Doubler");
            Ui.Place((RectTransform)_double.transform, 0.5f, 0, 0, 96, 600, 136);
        }

        public override void Refresh()
        {
            var t = App.Tuning.economy;
            var p = App.Profile;
            string today = App.LocalDay;
            int cycle = DailyGift.Cycle(t);
            int todayIndex = DailyGift.NextDay(p.gift, today, cycle, p.streakShields);
            _sub.text = Loc.Format("gift_sub", todayIndex);
            Ui.Clear(_days);
            for (int d = 1; d <= cycle; d++)
            {
                bool big = d == cycle;
                int row = (d - 1) / 3, col = (d - 1) % 3;
                float w = big ? 880f : Tile, h = big ? 250f : Tile;
                float x = big ? 0f : (col - 1) * (Tile + TileGap);
                float y = -(row * (Tile + TileGap)) - (big ? 2 * (Tile + TileGap) - row * (Tile + TileGap) : 0f);
                DayTile(d, todayIndex, t, p, x, y, w, h, big);
            }
            var item = DailyGift.ItemOf(t, todayIndex);
            int coins = DailyGift.Amount(t, todayIndex);
            Ui.ButtonLabel(_claim).text = Loc.Get("gift_claim_now");
            // La vidéo double les pièces : seulement les jours où il y en a (pièces, coffre, planche).
            _double.gameObject.SetActive(coins > 0 || item == GiftItem.Chest);
        }

        void DayTile(int d, int todayIndex, EconomyTuning t, Profile p, float x, float y, float w, float h, bool big)
        {
            bool done = d < todayIndex, now = d == todayIndex, next = d == todayIndex + 1;
            var holder = Ui.Rect("Jour" + d, _days);
            Ui.Place(holder, 0.5f, 1, x, y, w, h, 0.5f, 1);
            if (now)
            {
                var glow = Ui.Image(holder, Art.SoftDisc, new Color(1f, 0.82f, 0.3f, 0.75f), "Lueur");
                Ui.Place(glow.rectTransform, 0.5f, 0.5f, 0, 0, w * 1.35f, h * 1.5f);
            }
            var tile = Ui.Card(holder, "Tuile");
            Ui.Stretch(tile.rectTransform);
            tile.color = done ? new Color(0.78f, 0.93f, 0.82f) : now ? new Color(1f, 0.93f, 0.66f) : Color.white;
            // En haut de la tuile : « Jour N », ou une étiquette « Aujourd'hui » / « Demain » à sa place (dans la tuile :
            // elle ne déborde jamais sur la voisine).
            if ((now || next) && !big)
            {
                var tag = Ui.Image(tile.transform, Art.Pill, now ? Palette.Primary : Palette.Secondary, "Etiquette", true);
                Ui.Place(tag.rectTransform, 0.5f, 1, 0, -40, w - 40, 54);
                var tt = Ui.Title(tag.transform, Loc.Get(now ? "gift_today" : "gift_tomorrow"), 32);
                Ui.Stretch(tt.rectTransform, 8, 0, 8, 3);
            }
            else
            {
                string text = big ? Loc.Get("gift_big") + (now ? " · " + Loc.Get("gift_today") : next ? " · " + Loc.Get("gift_tomorrow") : "") : Loc.Format("gift_day", d);
                var label = Ui.Title(tile.transform, text, big ? 44 : 40, now ? Palette.Primary : next ? Palette.Secondary : Palette.Ink);
                Ui.Plain(label);
                if (big) { label.alignment = TextAnchor.MiddleLeft; Ui.Place(label.rectTransform, 0, 1, 36, -46, 800, 56, 0, 0.5f); }
                else Ui.Place(label.rectTransform, 0.5f, 1, 0, -40, w - 20, 52);
            }

            var item = DailyGift.ItemOf(t, d);
            int coins = DailyGift.Amount(t, d);
            RectTransform visual;
            string caption;
            switch (item)
            {
                case GiftItem.Chest:
                    visual = MetaUi.MiniChest(tile.transform, Art, 150);
                    caption = Loc.Get(big ? "gift_item_chest" : "gift_tile_chest");
                    break;
                case GiftItem.Ticket:
                    var disc = Ui.Image(tile.transform, Art.Circle, Palette.Purple, "Surprise");
                    visual = disc.rectTransform;
                    var q = Ui.Title(disc.transform, "?", 96, Palette.Accent);
                    Ui.Stretch(q.rectTransform, 0, 0, 0, 8);
                    caption = Loc.Get(big ? "gift_item_ticket" : "gift_tile_ticket");
                    break;
                case GiftItem.Plank:
                    var skin = DailyGift.NextPlank(p);
                    var plank = Ui.Image(tile.transform, skin != null ? MetaUi.PlankPreview(Art, skin) : Art.IconSprite(Icon.Coin), Color.white, "Planche");
                    plank.preserveAspect = true;
                    if (skin == null) plank.color = Palette.Accent;
                    visual = plank.rectTransform;
                    caption = skin != null ? Loc.Format("gift_item_plank", skin.Name(Loc.Language)) : Loc.FormatScore(coins + DailyGift.PlankFallbackCoins);
                    break;
                default:
                    var coin = Ui.Icon(tile.transform, Art.IconSprite(Icon.Coin), "Pieces");
                    coin.color = Palette.Accent;
                    visual = coin.rectTransform;
                    caption = Loc.FormatScore(coins);
                    break;
            }
            if (big)
            {
                Ui.Place(visual, 0, 0.5f, 40, -16, 420, 130, 0, 0.5f);
                var cap = Ui.Title(tile.transform, caption, 44, Palette.Purple);
                Ui.Plain(cap);
                cap.alignment = TextAnchor.MiddleLeft;
                Ui.Place(cap.rectTransform, 0, 0.5f, 490, 20, 360, 60, 0, 0.5f);
                if (coins > 0 && item == GiftItem.Plank && DailyGift.NextPlank(p) != null)
                {
                    var plus = Ui.Title(tile.transform, "+" + Loc.FormatScore(coins), 52, Palette.Accent);
                    plus.alignment = TextAnchor.MiddleLeft;
                    Ui.Place(plus.rectTransform, 0, 0.5f, 560, -48, 260, 66, 0, 0.5f);
                    var pc = Ui.Icon(tile.transform, Art.IconSprite(Icon.Coin), "Piece");
                    pc.color = Palette.Accent;
                    Ui.Place(pc.rectTransform, 0, 0.5f, 492, -48, 56, 56, 0, 0.5f);
                }
            }
            else
            {
                float vs = item == GiftItem.Chest ? 124f : item == GiftItem.Ticket ? 104f : 100f;
                Ui.Place(visual, 0.5f, 0.5f, 0, 14, vs, item == GiftItem.Chest ? vs * 0.9f : vs);
                if (done)
                {
                    // Déjà pris : une coche verte à la place du texte.
                    var ok = Ui.Image(tile.transform, Art.Circle, Palette.Success, "Fait");
                    Ui.Place(ok.rectTransform, 0.5f, 0, 0, 44, 64, 64);
                    var ic = Ui.Icon(ok.transform, Art.IconSprite(Icon.Check));
                    Ui.Place(ic.rectTransform, 0.5f, 0.5f, 0, 0, 42, 42);
                }
                else
                {
                    var cap = Ui.Title(tile.transform, caption, item == GiftItem.Coins ? 50 : 38, item == GiftItem.Coins ? Palette.Accent : Palette.Ink);
                    if (item != GiftItem.Coins) Ui.Plain(cap);
                    Ui.Place(cap.rectTransform, 0.5f, 0, 0, 40, w - 24, 52);
                }
            }
            if (big && done)
            {
                var ok = Ui.Image(tile.transform, Art.Circle, Palette.Success, "Fait");
                Ui.Place(ok.rectTransform, 1, 1, -18, -18, 64, 64, 1, 1);
                var ic = Ui.Icon(ok.transform, Art.IconSprite(Icon.Check));
                Ui.Place(ic.rectTransform, 0.5f, 0.5f, 0, 0, 42, 42);
            }
            if (now) holder.gameObject.AddComponent<Pulse>().Amount = 0.025f;
        }
    }

    // ===================================================================== Missions

    public sealed class MissionsScreen : UiScreen
    {
        RectTransform _list;
        Text _bonus, _timer;
        RectTransform _bonusFill;

        protected override void Build()
        {
            BackButton(App.GoHome);
            Header(Loc.Get("missions_title"));
            var area = Ui.Rect("Liste", Safe);
            Ui.Stretch(area, 40, 260, 40, 420);
            _list = Ui.Rect("Missions", area);
            Ui.Stretch(_list);
            Ui.Column(_list, 26);

            var bonusCard = Ui.Card(Safe, "Bonus");
            Ui.Place(bonusCard.rectTransform, 0.5f, 0, 0, 250, 1000, 200);
            var chest = Ui.Icon(bonusCard.transform, Art.IconSprite(Icon.Gift), "Coffre");
            chest.color = Palette.Accent;
            Ui.Place(chest.rectTransform, 0, 0.5f, 40, 0, 130, 130, 0, 0.5f);
            _bonus = Ui.Label(bonusCard.transform, "", 46, Palette.Ink, TextAnchor.MiddleLeft);
            Ui.Place(_bonus.rectTransform, 0, 0.5f, 200, 32, 760, 70, 0, 0.5f);
            _bonusFill = MetaUi.Bar(bonusCard.transform, Art, Palette.Accent);
            Ui.Place((RectTransform)_bonusFill.parent, 0, 0.5f, 200, -40, 740, 40, 0, 0.5f);
            _timer = Ui.Title(Safe, "", 44, ArtCache.Hex("#BFE0FF"));
            Ui.Place(_timer.rectTransform, 0.5f, 0, 0, 90, 1000, 70);
        }

        public override void Refresh()
        {
            var p = App.Profile;
            Missions.Refresh(p, App.LocalDay);
            Ui.Clear(_list);
            for (int i = 0; i < p.missions.list.Count; i++) Row(i, p.missions.list[i], p.missions.rerolled);
            int done = p.missions.list.FindAll(m => m.done).Count;
            _bonus.text = p.missions.bonusClaimed ? Loc.Get("missions_bonus_done") : Loc.Format("missions_bonus", App.Tuning.economy.missionsBonus);
            MetaUi.SetFill(_bonusFill, done / (float)Mathf.Max(1, p.missions.list.Count));
            Tick(0);
        }

        void Row(int index, Mission m, bool rerolled)
        {
            var row = Ui.Card(_list, "Mission");
            row.color = m.done ? ArtCache.Hex("#D8F7E3") : Color.white;
            Ui.Size(row, 250);
            // Pastille de couleur avec l'icône du type de mission ; un « fabrique un 64 » montre le vrai poids.
            var disc = Ui.Image(row.transform, Art.Circle, m.done ? Palette.Success : MetaUi.MissionColor(m.kind), "Pastille");
            Ui.Place(disc.rectTransform, 0, 0.5f, 26, 0, 128, 128, 0, 0.5f);
            if (!m.done && m.kind == MissionKind.MakeValue)
            {
                var wimg = Ui.Icon(disc.transform, Art.Weight(m.target));
                Ui.Place(wimg.rectTransform, 0.5f, 0.5f, 0, 4, 104, 104);
                var num = Ui.Label(wimg.transform, m.target.ToString(), 44, Palette.Ink);
                Ui.Place(num.rectTransform, 0.5f, 0.5f, 0, (WeightArtist.PlateCenterY - 0.5f) * 104, 80, 50);
                num.resizeTextForBestFit = true; num.resizeTextMinSize = 20; num.resizeTextMaxSize = 40;
            }
            else
            {
                var icon = Ui.Icon(disc.transform, Art.IconSprite(m.done ? Icon.Check : MetaUi.MissionIcon(m.kind)));
                Ui.Place(icon.rectTransform, 0.5f, 0.5f, 0, 0, 84, 84);
            }
            var text = Ui.Label(row.transform, Loc.MissionText(m), 48, Palette.Ink, TextAnchor.MiddleLeft);
            Ui.Place(text.rectTransform, 0, 1, 170, -70, 560, 110, 0, 0.5f);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 38; text.resizeTextMaxSize = text.fontSize;
            var fill = MetaUi.Bar(row.transform, Art, m.done ? Palette.Success : Palette.Secondary);
            Ui.Place((RectTransform)fill.parent, 0, 0, 170, 60, 360, 40, 0, 0.5f);
            MetaUi.SetFill(fill, m.target > 0 ? m.progress / (float)m.target : 0);
            bool big = m.kind == MissionKind.ScoreInGame || m.kind == MissionKind.TotalScore || m.kind == MissionKind.NoHeartLost;
            var prog = Ui.Label(row.transform, big ? Loc.FormatScore(m.progress) : $"{m.progress}/{m.target}", 44, Palette.Muted, TextAnchor.MiddleLeft);
            Ui.Place(prog.rectTransform, 0, 0, 548, 60, 160, 56, 0, 0.5f);
            prog.horizontalOverflow = HorizontalWrapMode.Overflow;

            var reward = Ui.Rect("Recompense", row.transform);
            Ui.Place(reward, 1, 1, -30, -64, 200, 80, 1, 0.5f);
            var coin = Ui.Icon(reward, Art.IconSprite(Icon.Coin));
            coin.color = Palette.Accent;
            Ui.Place(coin.rectTransform, 0, 0.5f, 0, 0, 70, 70, 0, 0.5f);
            var amount = Ui.Title(reward, "+" + m.reward, 50, Palette.Accent);
            Ui.Place(amount.rectTransform, 0, 0.5f, 80, 0, 130, 70, 0, 0.5f);
            amount.alignment = TextAnchor.MiddleLeft;

            if (!m.done && !rerolled)
            {
                int i = index;
                var swap = Ui.Button(row.transform, Loc.Get("mission_reroll"), Palette.Purple, () => App.RerollMission(i), Icon.Film, 44, "Changer");
                Ui.Place((RectTransform)swap.transform, 1, 0, -24, 26, 260, 110, 1, 0);
            }
        }

        public override void Tick(float dt)
        {
            var tomorrow = DateTime.Now.Date.AddDays(1);
            _timer.text = Loc.Format("missions_new_in", Loc.Duration((tomorrow - DateTime.Now).TotalSeconds));
        }
    }

    // ===================================================================== Boutique

    public sealed class ShopScreen : UiScreen
    {
        RectTransform _col, _coinsRow;
        ScrollRect _scroll;
        Text _coins, _starterTimer, _freeLeft;

        /// <summary>Aperçu d'une planche (dessiné une fois, à la demande).</summary>
        public Sprite PlankSprite(PlankSkin s) => MetaUi.PlankPreview(Art, s); // plus court et plus épais que la vraie planche

        /// <summary>Fait défiler jusqu'aux paquets de pièces (quand il en manque).</summary>
        public void ScrollToCoins()
        {
            if (_coinsRow == null || _scroll == null) return;
            Canvas.ForceUpdateCanvases();
            var view = (RectTransform)_scroll.viewport;
            float viewH = view != null ? view.rect.height : 1000f;
            float y = -_coinsRow.anchoredPosition.y - 120f;
            _col.anchoredPosition = new Vector2(_col.anchoredPosition.x, Mathf.Clamp(y, 0f, Mathf.Max(0f, _col.rect.height - viewH)));
        }

        protected override void Build()
        {
            BackButton(App.GoHome);
            // Titre entre le bouton retour et le compteur de pièces, avec de l'air des deux côtés.
            var header = Header(Loc.Get("shop"));
            // Centré sur l'écran, comme les autres titres : le bouton retour et les pièces sont loin de chaque côté.
            header.rectTransform.anchoredPosition = new Vector2(0, header.rectTransform.anchoredPosition.y);
            header.rectTransform.sizeDelta = new Vector2(460, header.rectTransform.sizeDelta.y);
            header.fontSize = Mathf.RoundToInt(76 * Ui.TextScale);
            _coins = MetaUi.CoinPill(Safe, Art, out var rt);
            Ui.Place(rt, 1, 1, -36, -60, 270, 104, 1, 1);
            var area = Ui.Rect("Zone", Safe);
            Ui.Stretch(area, 36, 230, 36, 20);
            _col = Ui.Scroll(area, out _scroll);
            Ui.Column(_col, 24);
            _col.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 10, 60);
        }

        public override void Refresh()
        {
            var p = App.Profile;
            _coins.text = Loc.FormatScore(p.coins);
            Ui.Clear(_col);
            _starterTimer = null;
            var purchases = App.Purchases;

            if (Offers.StarterActive(p, App.Now))
            {
                var def = Shop.Get(Shop.Starter);
                HeadDef golden = null;
                foreach (var hd in HeadCatalog.All) if (hd.Product == Shop.Starter) golden = hd;
                var card = Offer(def, 420, Palette.Accent, golden);
                _starterTimer = Ui.Title(card.transform, "", 44, Palette.Primary);
                Ui.Place(_starterTimer.rectTransform, 0, 0, 40, 96, 600, 60, 0, 0.5f);
                _starterTimer.alignment = TextAnchor.MiddleLeft;
                card.gameObject.AddComponent<Pulse>().Amount = 0.012f;
            }
            // Ce que l'on fait de ses pièces : surprises, planches, protège-série. Rien ne change le score.
            SurpriseCard();
            PlankSection();
            ShieldCard();

            // Pièces gratuites en vidéo.
            int left = AdPolicy.FreeCoinsLeft(App.Tuning.ads, p.ads, App.LocalDay);
            var free = Ui.Card(_col, "Gratuit");
            Ui.Size(free, 230);
            var fi = Ui.Icon(free.transform, Art.IconSprite(Icon.Film));
            fi.color = Palette.Play;
            Ui.Place(fi.rectTransform, 0, 0.5f, 34, 0, 130, 130, 0, 0.5f);
            var ft = Ui.Label(free.transform, Loc.Format("free_coins", App.Tuning.ads.freeCoinsAmount), 52, Palette.Ink, TextAnchor.MiddleLeft);
            Ui.Place(ft.rectTransform, 0, 0.5f, 190, 34, 480, 80, 0, 0.5f);
            _freeLeft = Ui.Label(free.transform, left > 0 ? Loc.Format("free_coins_left", left) : Loc.Get("free_coins_none"), 44, Palette.Muted, TextAnchor.MiddleLeft);
            Ui.Place(_freeLeft.rectTransform, 0, 0.5f, 190, -40, 480, 60, 0, 0.5f);
            var fb = Ui.Button(free.transform, "", left > 0 ? Palette.Play : Palette.Muted, App.WatchFreeCoins, Icon.Play, 50, "Voir");
            Ui.Place((RectTransform)fb.transform, 1, 0.5f, -30, 0, 220, 150, 1, 0.5f);
            fb.interactable = left > 0;

            // Paquets de pièces côte à côte.
            var coinsTitle = Ui.Title(_col, Loc.Get("coins_title"), 52, Palette.Accent);
            coinsTitle.alignment = TextAnchor.LowerLeft;
            Ui.Size(coinsTitle, 84);
            _coinsRow = coinsTitle.rectTransform;
            var row = Ui.Rect("Pieces", _col);
            Ui.Size(row, 470);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = true;
            foreach (var id in new[] { Shop.CoinsSmall, Shop.CoinsMedium, Shop.CoinsLarge }) CoinPack(row, Shop.Get(id), purchases.Price(id));

            if (!p.adsRemoved) Offer(Shop.Get(Shop.RemoveAds), 300, Palette.Purple, null, Icon.Film);

            if (!Offers.Owns(p, Shop.Legends))
            {
                var legends = Offer(Shop.Get(Shop.Legends), 430, Palette.Secondary, null, Icon.Crown);
                var heads = Ui.Rect("Tetes", legends.transform);
                Ui.Place(heads, 0, 0, 40, 30, 620, 150, 0, 0);
                var hl = heads.gameObject.AddComponent<HorizontalLayoutGroup>();
                hl.spacing = 6; hl.childControlWidth = hl.childControlHeight = false; hl.childForceExpandWidth = false; hl.childAlignment = TextAnchor.MiddleLeft;
                foreach (var hd in HeadCatalog.All)
                    if (hd.Product == Shop.Legends)
                    {
                        var ic = Ui.Icon(heads, Art.Portrait(hd));
                        ic.rectTransform.sizeDelta = new Vector2(118, 118);
                    }
            }

            var restore = Ui.Button(_col, Loc.Get("restore"), Palette.Dark, App.RestorePurchases, Icon.Replay, 46);
            Ui.Size(restore, 130);
            Tick(0);
        }

        Image Offer(ProductDef def, float height, Color color, HeadDef head, Icon icon = Icon.Star)
        {
            var p = App.Profile;
            var card = Ui.Card(_col, def.Id);
            Ui.Size(card, height);
            bool owned = def.Kind == ProductKind.NonConsumable && Offers.Owns(p, def.Id);
            var band = Ui.Image(card.transform, Art.Pill, color, "Bandeau", true);
            Ui.Place(band.rectTransform, 0, 1, 24, -22, 300, 12, 0, 1);
            if (head != null)
            {
                var portrait = Ui.Icon(card.transform, Art.Portrait(head));
                Ui.Place(portrait.rectTransform, 0, 1, 20, -40, 260, 260, 0, 1);
            }
            else
            {
                var ic = Ui.Icon(card.transform, Art.IconSprite(icon));
                ic.color = color;
                Ui.Place(ic.rectTransform, 0, 1, 40, -50, 150, 150, 0, 1);
            }
            float x = head != null ? 300 : 220;
            var title = Ui.Title(card.transform, Loc.Get(def.TitleKey), 64, color);
            Ui.Place(title.rectTransform, 0, 1, x, -80, 660 - x + 300, 90, 0, 0.5f);
            title.alignment = TextAnchor.MiddleLeft;
            var desc = Ui.Label(card.transform, Loc.Get(def.DescKey), 44, Palette.Ink, TextAnchor.UpperLeft);
            Ui.Place(desc.rectTransform, 0, 1, x, -130, 960 - x - 30, 150, 0, 1);
            // La carte grandit avec le texte : il ne passe jamais sous le bouton du prix.
            float descH = Mathf.Max(60f, desc.preferredHeight);
            desc.rectTransform.sizeDelta = new Vector2(desc.rectTransform.sizeDelta.x, descH);
            Ui.Size(card, Mathf.Max(height, 130 + descH + 200));
            if (def.BadgeKey != null)
            {
                var badge = Ui.Image(card.transform, Art.Pill, Palette.Primary, "Badge", true);
                Ui.Place(badge.rectTransform, 1, 1, -20, -20, 190, 84, 1, 1);
                var bt = Ui.Title(badge.transform, Loc.Get(def.BadgeKey), 46);
                Ui.Stretch(bt.rectTransform, 0, -2, 0, 4);
                badge.transform.localRotation = Quaternion.Euler(0, 0, -8);
            }
            var buy = Ui.Button(card.transform, owned ? Loc.Get("owned") : App.Purchases.Price(def.Id), owned ? Palette.Muted : Palette.Play, () => App.BuyProduct(def.Id), null, 56, "Prix");
            Ui.Place((RectTransform)buy.transform, 1, 0, -28, 26, 300, 140, 1, 0);
            buy.interactable = !owned;
            return card;
        }

        /// <summary>Bouton de prix en pièces (pièce dorée + montant), vert si on peut payer.</summary>
        Button CoinButton(Transform parent, int price, Action onClick)
        {
            bool can = App.Profile.coins >= price;
            var b = Ui.Button(parent, Loc.FormatScore(price), can ? Palette.Play : Palette.Muted, onClick, Icon.Coin, 50, "PrixPieces");
            var ic = b.transform.Find("Contenu/Icone");
            if (ic != null) ic.GetComponent<Image>().color = Palette.Accent;
            return b;
        }

        void SurpriseCard()
        {
            var p = App.Profile;
            var odds = SurpriseBox.Odds(p);
            var card = Ui.Card(_col, "Surprise");
            Ui.Size(card, 420);
            var band = Ui.Image(card.transform, Art.Pill, Palette.Purple, "Bandeau", true);
            Ui.Place(band.rectTransform, 0, 1, 24, -22, 300, 12, 0, 1);
            // Gros point d'interrogation doré sur une pastille violette, avec deux étincelles : la surprise.
            var disc = Ui.Image(card.transform, Art.Circle, Palette.Purple, "Pastille");
            Ui.Place(disc.rectTransform, 0, 0.5f, 46, 0, 220, 220, 0, 0.5f);
            disc.gameObject.AddComponent<Pulse>().Amount = 0.04f;
            var ring = Ui.Image(disc.transform, Art.Circle, new Color(1, 1, 1, 0.18f), "Reflet");
            Ui.Place(ring.rectTransform, 0.5f, 0.5f, -38, 44, 90, 60);
            var q = Ui.Title(disc.transform, "?", 150, Palette.Accent, "Question");
            Ui.Stretch(q.rectTransform, 0, 0, 0, 10);
            for (int i = 0; i < 2; i++)
            {
                var sp = Ui.Icon(card.transform, Art.IconSprite(Icon.Star), "Etincelle");
                sp.color = Palette.Accent;
                Ui.Place(sp.rectTransform, 0, 0.5f, i == 0 ? 244 : 70, i == 0 ? 92 : -92, i == 0 ? 58 : 42, i == 0 ? 58 : 42, 0.5f, 0.5f);
                sp.rectTransform.localRotation = Quaternion.Euler(0, 0, i == 0 ? 14 : -10);
            }
            var title = Ui.Title(card.transform, Loc.Get("surprise_title"), 60, Palette.Purple);
            Ui.Place(title.rectTransform, 0, 1, 310, -78, 670, 84, 0, 0.5f);
            title.alignment = TextAnchor.MiddleLeft;
            var desc = Ui.Label(card.transform, odds.Count > 0 ? Loc.Get("surprise_desc") : Loc.Get("surprise_none"), 42, Palette.Ink, TextAnchor.UpperLeft);
            Ui.Place(desc.rectTransform, 0, 1, 310, -124, 670, 100, 0, 1);
            if (odds.Count > 0)
            {
                // Petite étiquette violette sous la description, à gauche du bouton (jamais par-dessus).
                var chip = Ui.Image(card.transform, Art.Pill, new Color(0.65f, 0.42f, 1f, 0.16f), "Reste", true);
                Ui.Place(chip.rectTransform, 0, 0, 310, 82, 330, 64, 0, 0.5f);
                var left = Ui.Label(chip.transform, Loc.Format("surprise_left", odds.Count), 36, Palette.Purple);
                if (p.surpriseTickets > 0) left.text = Loc.Format("surprise_tickets", p.surpriseTickets);
                Ui.Stretch(left.rectTransform, 16, 0, 16, 2);
                if (p.surpriseTickets > 0)
                {
                    // Ticket du cadeau du jour : ouverture gratuite.
                    var fb = Ui.Button(card.transform, Loc.Get("surprise_free_button"), Palette.Purple, App.OpenSurprise, Icon.Gift, 44, "Gratuit");
                    Ui.Place((RectTransform)fb.transform, 1, 0, -32, 26, 300, 132, 1, 0);
                    fb.gameObject.AddComponent<Pulse>().Amount = 0.04f;
                }
                else
                {
                    var b = CoinButton(card.transform, SurpriseBox.Price, App.OpenSurprise);
                    Ui.Place((RectTransform)b.transform, 1, 0, -32, 30, 260, 112, 1, 0);
                }
            }
        }

        void PlankSection()
        {
            var p = App.Profile;
            var title = Ui.Title(_col, Loc.Get("planks_title"), 52, Palette.Accent);
            title.alignment = TextAnchor.LowerLeft;
            Ui.Size(title, 84);
            var sub = Ui.Label(_col, Loc.Get("planks_sub"), 40, ArtCache.Hex("#D8E9FF"), TextAnchor.UpperLeft);
            Ui.Size(sub, 56);

            var gridRt = Ui.Rect("Planches", _col);
            int n = PlankCatalog.All.Count, rows = (n + 1) / 2;
            Ui.Size(gridRt, rows * 318 - 18);
            var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(495, 300);
            grid.spacing = new Vector2(18, 18);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;
            foreach (var s in PlankCatalog.All)
            {
                bool owned = Cosmetics.OwnsPlank(p, s.Id), worn = p.selectedPlank == s.Id;
                var cell = Ui.Card(gridRt, "Planche" + s.Id);
                if (worn) cell.color = ArtCache.Hex("#FFF1C2");
                var img = Ui.Icon(cell.transform, PlankSprite(s), "Apercu");
                Ui.Place(img.rectTransform, 0.5f, 1, 0, -60, 420, 92, 0.5f, 0.5f);
                img.preserveAspect = true;
                img.rectTransform.localRotation = Quaternion.Euler(0, 0, 5f);
                if (s.Pattern == PlankPattern.Gold) img.gameObject.AddComponent<Pulse>().Amount = 0.03f;
                var name = Ui.Label(cell.transform, s.Name(Loc.Language), 46, Palette.Ink);
                name.fontStyle = FontStyle.Bold;
                Ui.Place(name.rectTransform, 0.5f, 1, 0, -140, 440, 64);
                Button b;
                if (worn)
                {
                    b = Ui.Button(cell.transform, Loc.Get("plank_equipped"), Palette.Muted, null, Icon.Check, 44, "Equipee");
                    b.interactable = false;
                }
                else if (owned) b = Ui.Button(cell.transform, Loc.Get("plank_choose"), Palette.Secondary, () => App.TapPlank(s), null, 46, "Choisir");
                else b = CoinButton(cell.transform, s.Price, () => App.TapPlank(s));
                Ui.Place((RectTransform)b.transform, 0.5f, 0, 0, 22, 380, 116, 0.5f, 0);
            }
        }

        void ShieldCard()
        {
            var p = App.Profile;
            var card = Ui.Card(_col, "ProtegeSerie");
            Ui.Size(card, 250);
            var ic = Ui.Icon(card.transform, Art.IconSprite(Icon.Calendar));
            ic.color = Palette.Accent;
            Ui.Place(ic.rectTransform, 0, 0.5f, 34, 0, 130, 130, 0, 0.5f);
            var t = Ui.Title(card.transform, Loc.Get("shield_title"), 54, Palette.Accent);
            Ui.Place(t.rectTransform, 0, 1, 190, -62, 460, 76, 0, 0.5f);
            t.alignment = TextAnchor.MiddleLeft;
            var stock = Ui.Label(card.transform, Loc.Format("shield_stock", p.streakShields, StreakShield.Max), 40, Palette.Muted, TextAnchor.MiddleLeft);
            Ui.Place(stock.rectTransform, 0, 1, 190, -122, 460, 56, 0, 0.5f);
            var d = Ui.Label(card.transform, Loc.Get("shield_desc"), 38, Palette.Ink, TextAnchor.UpperLeft);
            Ui.Place(d.rectTransform, 0, 1, 190, -156, 440, 76, 0, 1);
            if (p.streakShields < StreakShield.Max)
            {
                var b = CoinButton(card.transform, StreakShield.Price, App.BuyShield);
                Ui.Place((RectTransform)b.transform, 1, 0.5f, -28, 0, 270, 130, 1, 0.5f);
            }
            else
            {
                var full = Ui.Button(card.transform, Loc.Get("shield_full_short"), Palette.Muted, null, Icon.Check, 44, "Plein");
                full.interactable = false;
                Ui.Place((RectTransform)full.transform, 1, 0.5f, -28, 0, 270, 130, 1, 0.5f);
            }
        }

        void CoinPack(Transform parent, ProductDef def, string price)
        {
            var card = Ui.Card(parent, def.Id);
            var ic = Ui.Icon(card.transform, Art.IconSprite(Icon.Coin));
            ic.color = Palette.Accent;
            float k = def.Id == Shop.CoinsSmall ? 0.75f : def.Id == Shop.CoinsMedium ? 0.9f : 1.05f;
            Ui.Place(ic.rectTransform, 0.5f, 1, 0, -40, 150 * k, 150 * k, 0.5f, 1);
            var amount = Ui.Title(card.transform, Loc.FormatScore(def.Coins), 58, Palette.Accent);
            Ui.Place(amount.rectTransform, 0.5f, 1, 0, -230, 300, 80);
            if (def.BadgeKey != null)
            {
                var badge = Ui.Image(card.transform, Art.Pill, Palette.Primary, "Badge", true);
                Ui.Place(badge.rectTransform, 1, 1, 6, 6, 150, 70, 1, 1);
                var bt = Ui.Title(badge.transform, Loc.Get(def.BadgeKey), 44);
                Ui.Stretch(bt.rectTransform, 0, -2, 0, 4);
                badge.transform.localRotation = Quaternion.Euler(0, 0, -8);
            }
            var buy = Ui.Button(card.transform, price, Palette.Play, () => App.BuyProduct(def.Id), null, 50, "Prix");
            Ui.Place((RectTransform)buy.transform, 0.5f, 0, 0, 24, 280, 130, 0.5f, 0);
        }

        public override void Tick(float dt)
        {
            _coins.text = Loc.FormatScore(App.Profile.coins);
            if (_starterTimer != null)
                _starterTimer.text = Loc.Format("offer_ends", Loc.Duration(Offers.StarterSecondsLeft(App.Profile, App.Now)));
        }
    }
}

namespace Bascule.UI
{
    /// <summary>
    /// Coffre de fin de partie (toutes les 3 parties) : un vrai coffre posé devant le joueur. Il sautille ; on le
    /// touche, il tremble, le couvercle s'envole, une lumière d'or jaillit avec des pièces, et son contenu apparaît
    /// (pièces, parfois une tête). Les autres récompenses de la partie (objectif, missions, niveau) sont listées
    /// dessous, toutes ensemble : plus de file de notifications l'une après l'autre.
    /// </summary>
    public sealed class ChestScreen : UiScreen
    {
        RectTransform _chest, _lid, _rays, _glow, _loot, _list, _hintRt;
        Image _lidImg, _glowImg, _raysImg;
        Text _title, _hint, _amount, _headName;
        Image _headPortrait;
        Button _ok;
        Action _after;
        float _t, _openAt = -1f;
        bool _closing;
        readonly List<(RectTransform rt, Vector2 v, float age)> _coins = new List<(RectTransform, Vector2, float)>();
        const float ChestW = 700f;
        static readonly Vector2[] SparkPos = { new Vector2(-390, 420), new Vector2(380, 470), new Vector2(-300, 80), new Vector2(330, 120), new Vector2(0, 640), new Vector2(-420, 260), new Vector2(430, 300) };
        readonly Image[] _sparks = new Image[7];
        Image _leakImg, _flashImg;
        int _taps;
        int _lastBurst = -1;
        float _kick;
        // Effets : ondes de choc, confettis, compteur de pièces, faisceau de lumière.
        Image _catcher, _beamImg;
        readonly List<(Image img, float age, float life, float size)> _rings = new List<(Image, float, float, float)>();
        readonly List<(RectTransform rt, Image img, Vector2 v, float spin, float age)> _confetti = new List<(RectTransform, Image, Vector2, float, float)>();
        int _coinsTarget, _coinsShown;
        float _countStart = -1f;
        /// <summary>Couleur de la lumière : elle change à chaque coup (or, orange, rose), puis explose en violet à l'ouverture.</summary>
        static readonly Color[] TapColors = { new Color(1f, 0.85f, 0.35f), new Color(1f, 0.6f, 0.25f), new Color(1f, 0.38f, 0.62f), new Color(0.72f, 0.45f, 1f) };
        static readonly Color[] RingColors = { new Color(1f, 0.83f, 0.28f), new Color(1f, 0.42f, 0.55f), new Color(0.45f, 0.72f, 1f), new Color(0.6f, 1f, 0.6f), new Color(0.78f, 0.5f, 1f) };
        /// <summary>Comme dans les jeux de coffres : on le tapote plusieurs fois, il s'ouvre au dernier coup.</summary>
        const int TapsToOpen = 3;

        protected override void Build()
        {
            // Tout bouge sur cet écran : dans son propre sous-canvas, le bilan affiché derrière n'est pas recalculé.
            Ui.Isolate(Safe.gameObject);
            // Tout l'écran se touche : pas besoin de viser le coffre pour l'ouvrir.
            _catcher = Ui.Image(Safe, null, new Color(0, 0, 0, 0), "ToucherPartout");
            Ui.Stretch(_catcher.rectTransform, -400, -400, -400, -400);
            _catcher.raycastTarget = true;
            _catcher.gameObject.AddComponent<Button>().onClick.AddListener(Tap);

            _title = Ui.Title(Safe, Loc.Get("chest_won"), 84, Palette.Accent);
            Ui.Place(_title.rectTransform, 0.5f, 1, 0, -150, 980, 130);

            // Faisceau de lumière qui jaillit du coffre à l'ouverture.
            _beamImg = Ui.Image(Safe, Art.SoftDisc, new Color(1f, 0.95f, 0.75f, 0f), "Faisceau");
            Ui.Place(_beamImg.rectTransform, 0.5f, 0.5f, 0, 620, 460, 1500);

            // Lumière et rayons derrière le coffre (apparaissent à l'ouverture).
            _glowImg = Ui.Image(Safe, Art.SoftDisc, new Color(1f, 0.85f, 0.35f, 0f), "Lueur");
            _glow = _glowImg.rectTransform;
            Ui.Place(_glow, 0.5f, 0.5f, 0, 330, 900, 900);
            _raysImg = Ui.Image(Safe, Art.Rays, new Color(1f, 0.92f, 0.6f, 0f), "Rayons");
            _rays = _raysImg.rectTransform;
            Ui.Place(_rays, 0.5f, 0.5f, 0, 330, 1500, 1500);
            _rays.gameObject.AddComponent<Spin>().Speed = 14f;

            // Contenu du coffre : il monte au-dessus du coffre une fois ouvert.
            _loot = Ui.Rect("Contenu", Safe);
            Ui.Place(_loot, 0.5f, 0.5f, 0, 560, 900, 330);
            _headPortrait = Ui.Icon(_loot, null, "Tete");
            Ui.Place(_headPortrait.rectTransform, 0.5f, 0.5f, -170, 30, 250, 250);
            _headName = Ui.Title(_loot, "", 44, Color.white);
            Ui.Place(_headName.rectTransform, 0.5f, 0.5f, -170, -125, 420, 60);
            var coin = Ui.Icon(_loot, Art.IconSprite(Icon.Coin), "Piece");
            coin.color = Palette.Accent;
            Ui.Place(coin.rectTransform, 0.5f, 0.5f, -95, 30, 130, 130);
            coin.name = "Piece";
            _amount = Ui.Title(_loot, "", 110, Palette.Accent);
            Ui.Place(_amount.rectTransform, 0.5f, 0.5f, 90, 30, 360, 140);
            _amount.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Le coffre : corps + couvercle (pivot sur la charnière). Le toucher l'ouvre.
            _chest = Ui.Rect("Coffre", Safe);
            Ui.Place(_chest, 0.5f, 0.5f, 0, 180, ChestW, ChestW * 0.66f);
            var body = Ui.Image(_chest, Art.ChestBase, Color.white, "Corps");
            Ui.Stretch(body.rectTransform);
            body.raycastTarget = true;
            body.gameObject.AddComponent<Button>().onClick.AddListener(Tap);
            _lidImg = Ui.Image(_chest, Art.ChestLid, Color.white, "Couvercle");
            _lid = _lidImg.rectTransform;
            float lidH = ChestW * 0.42f;
            Ui.Place(_lid, 0.5f, 0, 0, ChestW * 0.53f, ChestW, lidH, 0.5f, 0.03f / 0.42f);
            _lidImg.raycastTarget = true;
            _lidImg.gameObject.AddComponent<Button>().onClick.AddListener(Tap);

            // Filet de lumière entre le corps et le couvercle quand le coffre « bouge tout seul ».
            _leakImg = Ui.Image(_chest, Art.SoftDisc, new Color(1f, 0.9f, 0.5f, 0f), "Fuite");
            Ui.Place(_leakImg.rectTransform, 0.5f, 0, 0, ChestW * 0.55f, ChestW * 0.95f, ChestW * 0.16f);
            _lid.SetAsLastSibling();
            // Étincelles qui scintillent autour du coffre.
            for (int i = 0; i < SparkPos.Length; i++)
            {
                var sp = Ui.Image(Safe, Art.Spark, new Color(1f, 0.95f, 0.7f, 0f), "Etincelle");
                Ui.Place(sp.rectTransform, 0.5f, 0.5f, SparkPos[i].x, SparkPos[i].y, 64, 64);
                _sparks[i] = sp;
            }
            _hint = Ui.Title(Safe, Loc.Get("chest_tap"), 52, Color.white);
            _hintRt = _hint.rectTransform;
            Ui.Place(_hintRt, 0.5f, 0.5f, 0, -115, 900, 80);

            // Les autres récompenses de la partie, en lignes compactes.
            _list = Ui.Rect("Recompenses", Safe);
            Ui.Place(_list, 0.5f, 0.5f, 0, -110, 900, 600, 0.5f, 1f);
            var col = _list.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 18; col.childAlignment = TextAnchor.UpperCenter;
            col.childControlWidth = col.childControlHeight = false; col.childForceExpandWidth = col.childForceExpandHeight = false;

            _ok = Ui.Button(Safe, Loc.Get("chest_ok"), Palette.Play, Close, null, 64, "Super");
            Ui.Place((RectTransform)_ok.transform, 0.5f, 0, 0, 170, 700, 160);
            _ok.gameObject.AddComponent<PopIn>().Delay = 0.9f;

            // Éclair blanc à l'ouverture.
            _flashImg = Ui.Image(Safe, null, new Color(1f, 0.97f, 0.85f, 0f), "Eclair");
            Ui.Stretch(_flashImg.rectTransform, -300, -300, -300, -300);
        }

        /// <summary>Coffre sans autres récompenses (cadeau du jour) : pièces, parfois une tête.</summary>
        public void PresentLoot(int coins, HeadDef head, string title, Action after)
        {
            _after = after;
            Show();
            Reset(coins, head, title);
            Ui.Clear(_list);
            _list.gameObject.SetActive(false);
        }

        void Reset(int coins, HeadDef head, string title)
        {
            _t = 0; _openAt = -1f; _closing = false; _taps = 0; _kick = 0f; _lastBurst = -1;
            _title.text = title;
            _amount.text = "+" + Loc.FormatScore(coins);
            bool hasHead = head != null;
            _headPortrait.gameObject.SetActive(hasHead);
            _headName.gameObject.SetActive(hasHead);
            var coinRt = _loot.Find("Piece") as RectTransform;
            float shift = hasHead ? 150f : 0f;
            if (coinRt != null) coinRt.anchoredPosition = new Vector2(-95 + shift, 30);
            _amount.rectTransform.anchoredPosition = new Vector2(90 + shift, 30);
            if (hasHead)
            {
                _headPortrait.sprite = Art.Portrait(head);
                _headName.text = head.Name(Loc.Language);
            }
            _loot.gameObject.SetActive(false);
            _lid.anchoredPosition = new Vector2(0, ChestW * 0.53f);
            _chest.anchoredPosition = new Vector2(0, 180f);
            _chest.localEulerAngles = Vector3.zero;
            _chest.localScale = Vector3.one;
            _lid.localEulerAngles = Vector3.zero;
            _lid.localScale = Vector3.one;
            _lidImg.color = Color.white;
            _glowImg.color = new Color(1f, 0.85f, 0.35f, 0f);
            _raysImg.color = new Color(1f, 0.92f, 0.6f, 0f);
            _flashImg.color = new Color(1f, 0.97f, 0.85f, 0f);
            _hint.text = Loc.Get("chest_tap");
            _hint.gameObject.SetActive(true);
            _ok.gameObject.SetActive(false);
            foreach (var c in _coins) if (c.rt != null) UnityEngine.Object.Destroy(c.rt.gameObject);
            _coins.Clear();
            foreach (var r in _rings) if (r.img != null) UnityEngine.Object.Destroy(r.img.gameObject);
            _rings.Clear();
            foreach (var c in _confetti) if (c.rt != null) UnityEngine.Object.Destroy(c.rt.gameObject);
            _confetti.Clear();
            _coinsTarget = coins; _coinsShown = 0; _countStart = -1f;
            _amount.text = "+0";
            _catcher.gameObject.SetActive(true);
            _beamImg.color = new Color(1f, 0.95f, 0.75f, 0f);
        }

        /// <summary>Présente le coffre de la partie (et les autres récompenses). <paramref name="after"/> : à la fermeture.</summary>
        public void Present(EconomyGain gain, int goal, Action after)
        {
            _after = after;
            Show();
            var loot = gain.Chest;
            Reset(loot?.Coins ?? 0, loot?.Head, Loc.Get("chest_won"));

            // Le coffre ne montre que ce qu'il contient : les autres gains (niveau, missions, objectif) sont déjà
            // affichés dans le bilan de la partie, les répéter ici embrouillait (« Niveau 6 +120 » sous le coffre).
            Ui.Clear(_list);
            _list.gameObject.SetActive(false);
        }

        void Row(int index, Icon icon, Color color, string title, string detail, int coins)
        {
            // Étiquette crème bien ronde (comme les bannières de récompense), cerclée de sombre, avec une ombre :
            // pastille de couleur à gauche, titre et détail sur deux lignes, pièces gagnées à droite.
            const float W = 880f, H = 128f;
            var holder = Ui.Rect("Ligne", _list);
            holder.sizeDelta = new Vector2(W, H + 8);
            var shadow = Ui.Image(holder, Art.Pill, new Color(0.05f, 0.03f, 0.12f, 0.35f), "Ombre", true);
            Ui.Place(shadow.rectTransform, 0.5f, 0.5f, 0, -8, W, H);
            var rim = Ui.Image(holder, Art.Pill, Palette.Outline, "Bord", true);
            Ui.Place(rim.rectTransform, 0.5f, 0.5f, 0, 0, W, H);
            var row = Ui.Image(rim.transform, Art.Pill, Palette.Paper, "Fond", true);
            Ui.Stretch(row.rectTransform, 6, 6, 6, 6);
            var shine = Ui.Image(row.transform, Art.Pill, new Color(1, 1, 1, 0.55f), "Reflet", true);
            var srt = shine.rectTransform;
            srt.anchorMin = new Vector2(0, 0.6f); srt.anchorMax = new Vector2(1, 0.92f);
            srt.offsetMin = new Vector2(60, 0); srt.offsetMax = new Vector2(-60, 0);
            var disc = Ui.Image(row.transform, Art.Circle, Palette.Outline, "Pastille");
            Ui.Place(disc.rectTransform, 0, 0.5f, 10, 0, 96, 96, 0, 0.5f);
            var discIn = Ui.Image(disc.transform, Art.Circle, color, "Couleur");
            Ui.Place(discIn.rectTransform, 0.5f, 0.5f, 0, 0, 84, 84);
            var ic = Ui.Icon(discIn.transform, Art.IconSprite(icon));
            Ui.Place(ic.rectTransform, 0.5f, 0.5f, 0, 0, 54, 54);
            float right = coins > 0 ? 236f : 36f;
            bool two = !string.IsNullOrEmpty(detail);
            var t = Ui.Label(row.transform, title, 44, Palette.Ink, TextAnchor.MiddleLeft);
            t.rectTransform.anchorMin = new Vector2(0, two ? 0.5f : 0f); t.rectTransform.anchorMax = new Vector2(1, 1);
            t.rectTransform.offsetMin = new Vector2(126, two ? -4 : 0); t.rectTransform.offsetMax = new Vector2(-right, two ? -6 : 0);
            if (two)
            {
                var dt = Ui.Label(row.transform, detail, 34, Palette.Muted, TextAnchor.MiddleLeft);
                dt.rectTransform.anchorMin = new Vector2(0, 0); dt.rectTransform.anchorMax = new Vector2(1, 0.5f);
                dt.rectTransform.offsetMin = new Vector2(126, 10); dt.rectTransform.offsetMax = new Vector2(-right, 2);
            }
            if (coins > 0)
            {
                var c = Ui.Title(row.transform, "+" + Loc.FormatScore(coins), 48, Palette.Accent);
                Ui.Place(c.rectTransform, 1, 0.5f, -30, 0, 150, 70, 1, 0.5f);
                c.alignment = TextAnchor.MiddleRight;
                c.horizontalOverflow = HorizontalWrapMode.Overflow;
                // La pièce juste devant le nombre, quelle que soit sa longueur.
                float textW = Mathf.Min(150f, c.preferredWidth);
                var coin = Ui.Icon(row.transform, Art.IconSprite(Icon.Coin), "Piece");
                coin.color = Palette.Accent;
                Ui.Place(coin.rectTransform, 1, 0.5f, -30 - textW - 38, 0, 58, 58, 0.5f, 0.5f);
            }
            var row0 = holder;
            row0.gameObject.AddComponent<PopIn>().Delay = 0.35f + index * 0.12f;
        }

        /// <summary>Un coup sur le coffre : il saute et tremble, la lumière fuit davantage ; au troisième, il s'ouvre.</summary>
        void Tap()
        {
            if (_openAt >= 0) return;
            _taps++;
            _kick = 1f;
            App.PlaySfx(Sfx.Pop, _taps == 1 ? 4 : _taps == 2 ? 16 : 64, 0.85f);
            App.HapticMedium();
            if (!App.Profile.settings.reducedMotion)
            {
                // Onde de choc de la couleur du coup, et quelques étincelles qui jaillissent.
                var col = TapColors[Mathf.Clamp(_taps - 1, 0, TapColors.Length - 1)];
                Ring(col, 0.5f, 900f + 200f * _taps, new Vector2(0, 330));
                Confetti(8 + 4 * _taps, new Vector2(0, 330), 900f, false, col);
            }
            if (_taps >= TapsToOpen) { Open(); return; }
            _hint.text = Loc.Get(TapsToOpen - _taps == 1 ? "chest_tap_last" : "chest_tap_more");
        }

        void Open()
        {
            if (_openAt >= 0) return;
            _openAt = _t + (App.Profile.settings.reducedMotion ? 0f : 0.25f); // dernier tremblement, puis il s'ouvre
            _hint.gameObject.SetActive(false);
            _catcher.gameObject.SetActive(false);
        }

        void Close()
        {
            if (_closing) return;
            _closing = true;
            Hide();
            var next = _after;
            _after = null;
            next?.Invoke();
        }

        /// <summary>Bouton retour (Android) : ouvre le coffre s'il est fermé, sinon ferme l'écran.</summary>
        public void Back() { if (_openAt < 0) Tap(); else if (_ok.gameObject.activeSelf) Close(); }

        public override void Tick(float dt)
        {
            _t += dt;
            bool still = App.Profile.settings.reducedMotion;
            Twinkle(still);
            if (_openAt < 0)
            {
                // En attendant : lumière douce derrière, et le coffre tremble par à-coups, couvercle qui se soulève
                // un peu avec un éclat de lumière, comme si quelque chose bougeait dedans.
                float charge = _taps / (float)TapsToOpen; // la lumière monte à chaque coup
                _kick = Mathf.Max(0f, _kick - dt * 3.5f);
                var tc = TapColors[Mathf.Clamp(_taps, 0, TapColors.Length - 1)];
                _glowImg.color = new Color(tc.r, tc.g, tc.b, 0.32f + 0.3f * charge + 0.1f * Mathf.Sin(_t * 2.4f) + 0.2f * _kick);
                _raysImg.color = new Color(Mathf.Lerp(1f, tc.r, 0.5f), Mathf.Lerp(0.92f, tc.g, 0.5f), Mathf.Lerp(0.6f, tc.b, 0.5f), 0.16f + 0.18f * charge);
                TickEffects(dt);
                _hint.color = new Color(1, 1, 1, 0.75f + 0.25f * Mathf.Sin(_t * 4f));
                _hintRt.localScale = Vector3.one * (1f + 0.15f * _kick);
                if (still) { _chest.localScale = Vector3.one * (1f + 0.04f * _taps); return; }
                // Secousses de plus en plus rapprochées à chaque coup : le coffre a vraiment envie de s'ouvrir.
                float period = Mathf.Lerp(1.05f, 0.55f, charge);
                float c = Mathf.Repeat(_t, period);
                int burst = Mathf.FloorToInt(_t / period);
                if (burst != _lastBurst)
                {
                    _lastBurst = burst;
                    // Petites étincelles qui s'échappent du couvercle à chaque secousse.
                    Confetti(3 + _taps, new Vector2(0, 360), 520f, false, new Color(1f, 0.85f, 0.35f));
                    // Vibration seulement pendant les premières secousses : un téléphone posé sur cet écran ne vibre pas sans fin.
                    if (burst < 8) App.HapticLight();
                }
                float env = Mathf.Max(c < period * 0.55f ? Mathf.Sin(c / (period * 0.55f) * Mathf.PI) : 0f, _kick);
                float shake = 9f + 4f * charge + 8f * _kick;
                float breathe = 0.035f * Mathf.Sin(_t * 3.2f);
                _chest.localEulerAngles = new Vector3(0, 0, Mathf.Sin(_t * (42f + 30f * _kick)) * shake * env);
                _chest.anchoredPosition = new Vector2(Mathf.Sin(_t * 55f) * 10f * env, 180f + Mathf.Abs(Mathf.Sin(c / period * Mathf.PI)) * 46f * env + 40f * _kick * _kick);
                _chest.localScale = new Vector3(1f + breathe + 0.05f * _taps + 0.1f * _kick + 0.04f * env, 1f + breathe * 0.6f + 0.05f * _taps - 0.06f * _kick - 0.03f * env, 1f);
                float peek = Mathf.Max(env * Mathf.Abs(Mathf.Sin(_t * 17f)), _kick);
                _lid.anchoredPosition = new Vector2(0, ChestW * 0.53f + peek * (16f + 22f * charge));
                _lid.localEulerAngles = new Vector3(0, 0, -peek * (3f + 4f * charge));
                _leakImg.color = new Color(1f, 0.9f, 0.5f, Mathf.Clamp01(peek * (0.9f + charge)));
                return;
            }
            _leakImg.color = new Color(1f, 0.9f, 0.5f, 0f);
            _chest.anchoredPosition = new Vector2(0, 180f);
            float s = _t - _openAt;
            if (s < 0)
            {
                // Il tremble de plus en plus.
                float a = 1f + s / 0.45f;
                _chest.localEulerAngles = new Vector3(0, 0, Mathf.Sin(_t * 60f) * 4f * a);
                _chest.localScale = Vector3.one * (1f + 0.06f * a);
                return;
            }
            _chest.localEulerAngles = Vector3.zero;
            _chest.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, Mathf.Clamp01(s * 4f));
            _flashImg.color = new Color(1f, 0.97f, 0.85f, still ? 0f : 0.85f * (1f - Mathf.Clamp01(s / 0.35f)));
            // La tête (s'il y en a une) sort après les pièces : deux surprises au lieu d'une.
            float hp = Mathf.Clamp01((s - 0.6f) / 0.3f);
            float headPop = hp <= 0f ? 0.01f : hp < 1f ? Mathf.Lerp(0.2f, 1.2f, hp) : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((s - 0.9f) / 0.15f));
            _headPortrait.rectTransform.localScale = Vector3.one * headPop;
            // La tête arrive en tournant sur elle-même.
            _headPortrait.rectTransform.localEulerAngles = new Vector3(0, still ? 0f : (1f - Mathf.Clamp01(hp)) * 360f, 0);
            _headName.rectTransform.localScale = Vector3.one * headPop;
            if (_headPortrait.gameObject.activeSelf && hp > 0f && hp - dt / 0.3f <= 0f) { App.PlaySfx(Sfx.Chime, 8, 0.8f); if (!still) Burst(); }
            if (!_loot.gameObject.activeSelf)
            {
                _loot.gameObject.SetActive(true);
                _loot.localScale = Vector3.one * 0.2f;
                _list.gameObject.SetActive(true);
                _ok.gameObject.SetActive(true);
                App.PlaySfx(Sfx.Chime, 6, 0.9f);
                App.PlaySfx(Sfx.Lift, 0, 0.8f);
                App.HapticMedium();
                _countStart = _t + 0.15f;
                if (!still)
                {
                    Burst();
                    // Grande ouverture : trois ondes de choc arc-en-ciel, une pluie de confettis.
                    for (int k = 0; k < 3; k++) Ring(RingColors[k], 0.7f + 0.15f * k, 1700f + 500f * k, new Vector2(0, 330), -0.08f * k);
                    Confetti(70, new Vector2(0, 360), 1700f, true, Color.white);
                }
            }
            // Couvercle : il saute, bascule en arrière et disparaît.
            float l = Mathf.Clamp01(s / 0.55f);
            _lid.anchoredPosition = new Vector2(still ? 0 : -l * 60f, ChestW * 0.53f + (still ? 0 : l * 380f - l * l * 120f));
            _lid.localEulerAngles = new Vector3(0, 0, still ? 0 : l * 28f);
            _lid.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.45f, l), 1f);
            _lidImg.color = new Color(1, 1, 1, 1f - Mathf.Clamp01((s - 0.25f) / 0.3f));
            // Faisceau : un trait de lumière qui monte du coffre puis s'adoucit.
            float beam = still ? 0f : Mathf.Clamp01(s * 5f) * Mathf.Lerp(0.85f, 0.35f, Mathf.Clamp01((s - 0.3f) / 1.2f));
            _beamImg.color = new Color(1f, 0.95f, 0.75f, beam);
            _beamImg.rectTransform.localScale = new Vector3(0.6f + 0.4f * Mathf.Clamp01(s * 3f) + 0.05f * Mathf.Sin(_t * 9f), 1f, 1f);
            TickEffects(dt);
            // Les pièces comptent jusqu'au total (petit tintement régulier), puis le nombre rebondit.
            if (_countStart >= 0f && _coinsShown < _coinsTarget && _t >= _countStart)
            {
                float k = Mathf.Clamp01((_t - _countStart) / 0.9f);
                int v = Mathf.RoundToInt(_coinsTarget * (1f - (1f - k) * (1f - k)));
                if (v != _coinsShown)
                {
                    if (v / Mathf.Max(1, _coinsTarget / 8) != _coinsShown / Mathf.Max(1, _coinsTarget / 8)) App.PlaySfx(Sfx.Click, 2, 0.35f);
                    _coinsShown = v;
                    _amount.text = "+" + Loc.FormatScore(v);
                    if (v >= _coinsTarget) { _amount.rectTransform.localScale = Vector3.one * 1.3f; App.PlaySfx(Sfx.Chime, 10, 0.7f); App.HapticMedium(); }
                }
            }
            _amount.rectTransform.localScale = Vector3.Lerp(_amount.rectTransform.localScale, Vector3.one, 1f - Mathf.Exp(-dt * 8f));
            // Lumière, rayons, contenu.
            _glowImg.color = new Color(1f, 0.85f, 0.35f, 0.75f * Mathf.Clamp01(s * 3f));
            _raysImg.color = new Color(1f, 0.92f, 0.6f, 0.4f * Mathf.Clamp01(s * 2f));
            float p = Mathf.Clamp01(s / 0.35f);
            float pop = p < 1f ? Mathf.Lerp(0.2f, 1.15f, p) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((s - 0.35f) / 0.15f));
            _loot.localScale = Vector3.one * pop;
            for (int i = _coins.Count - 1; i >= 0; i--)
            {
                var c = _coins[i];
                c.age += dt;
                c.v.y -= 2600f * dt;
                if (c.rt == null) { _coins.RemoveAt(i); continue; }
                c.rt.anchoredPosition += c.v * dt;
                c.rt.localEulerAngles = new Vector3(0, 0, c.age * 400f);
                if (c.age > 1.2f) { UnityEngine.Object.Destroy(c.rt.gameObject); _coins.RemoveAt(i); continue; }
                _coins[i] = c;
            }
        }

        void Twinkle(bool still)
        {
            for (int i = 0; i < _sparks.Length; i++)
            {
                float ph = Mathf.Repeat(_t * 0.8f + i * 0.37f, 1f);
                float k = still ? 0.5f : Mathf.Max(0f, Mathf.Sin(ph * Mathf.PI));
                _sparks[i].color = new Color(1f, 0.95f, 0.7f, k * 0.9f);
                _sparks[i].rectTransform.localScale = Vector3.one * (0.5f + 0.7f * k);
                _sparks[i].rectTransform.localEulerAngles = new Vector3(0, 0, _t * 40f + i * 30f);
            }
        }

        /// <summary>Onde de choc : un anneau de couleur qui grandit et s'efface.</summary>
        void Ring(Color c, float life, float size, Vector2 at, float delay = 0f)
        {
            var img = Ui.Image(Safe, Art.Ring, new Color(c.r, c.g, c.b, 0f), "Onde");
            Ui.Place(img.rectTransform, 0.5f, 0.5f, at.x, at.y, 100, 100);
            img.transform.SetSiblingIndex(_chest.GetSiblingIndex());
            _rings.Add((img, delay, life, size));
        }

        /// <summary>Confettis de couleurs : petits rectangles qui jaillissent, tournent et retombent.</summary>
        void Confetti(int n, Vector2 at, float speed, bool rainbow, Color tint)
        {
            for (int i = 0; i < n; i++)
            {
                var col = rainbow ? RingColors[UnityEngine.Random.Range(0, RingColors.Length)] : Color.Lerp(tint, Color.white, UnityEngine.Random.Range(0f, 0.4f));
                var img = Ui.Image(Safe, Art.Pixel, col, "Confetti");
                var rt = img.rectTransform;
                Ui.Place(rt, 0.5f, 0.5f, at.x + UnityEngine.Random.Range(-60f, 60f), at.y, UnityEngine.Random.Range(14f, 26f), UnityEngine.Random.Range(8f, 14f));
                float a = UnityEngine.Random.Range(0.1f, 0.9f) * Mathf.PI;
                float sp = UnityEngine.Random.Range(0.45f, 1f) * speed;
                _confetti.Add((rt, img, new Vector2(Mathf.Cos(a) * sp, Mathf.Sin(a) * sp), UnityEngine.Random.Range(-720f, 720f), 0f));
            }
        }

        void TickEffects(float dt)
        {
            for (int i = _rings.Count - 1; i >= 0; i--)
            {
                var r = _rings[i];
                r.age += dt;
                if (r.img == null) { _rings.RemoveAt(i); continue; }
                float k = r.age / r.life;
                if (k >= 1f) { UnityEngine.Object.Destroy(r.img.gameObject); _rings.RemoveAt(i); continue; }
                if (k > 0f)
                {
                    float e = 1f - (1f - k) * (1f - k);
                    r.img.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(120f, r.size, e);
                    var c = r.img.color; c.a = 0.9f * (1f - k); r.img.color = c;
                }
                _rings[i] = r;
            }
            for (int i = _confetti.Count - 1; i >= 0; i--)
            {
                var c = _confetti[i];
                c.age += dt;
                if (c.rt == null) { _confetti.RemoveAt(i); continue; }
                c.v.y -= 2200f * dt;
                c.v *= 1f - 0.8f * dt;
                c.rt.anchoredPosition += c.v * dt;
                c.rt.localEulerAngles = new Vector3(0, 0, c.rt.localEulerAngles.z + c.spin * dt);
                // Effet « papier qui tourne » : il s'aplatit et revient.
                c.rt.localScale = new Vector3(1f, Mathf.Abs(Mathf.Cos(c.age * 9f + i)), 1f);
                if (c.age > 1.8f) { var col = c.img.color; col.a = Mathf.Clamp01(1f - (c.age - 1.8f) / 0.4f); c.img.color = col; }
                if (c.age > 2.2f) { UnityEngine.Object.Destroy(c.rt.gameObject); _confetti.RemoveAt(i); continue; }
                _confetti[i] = c;
            }
        }

        void Burst()
        {
            for (int i = 0; i < 22; i++)
            {
                var img = Ui.Icon(Safe, Art.IconSprite(Icon.Coin), "PieceVolante");
                img.color = Palette.Accent;
                var rt = img.rectTransform;
                Ui.Place(rt, 0.5f, 0.5f, UnityEngine.Random.Range(-120f, 120f), 330, 70, 70);
                float a = UnityEngine.Random.Range(0.25f, 0.75f) * Mathf.PI;
                float sp = UnityEngine.Random.Range(900f, 1500f);
                _coins.Add((rt, new Vector2(Mathf.Cos(a) * sp, Mathf.Sin(a) * sp), 0f));
            }
            _chest.SetAsLastSibling();
            _hintRt.SetAsLastSibling();
            _flashImg.rectTransform.SetAsLastSibling();
        }
    }
}

namespace Bascule.UI
{
    /// <summary>
    /// Machine à surprises. D'abord l'offre : tous les lots possibles en cartes (têtes en silhouette, planches,
    /// pièces et jackpot), chacun avec sa chance en gros. Puis, une fois payé (ou avec un ticket gratuit du cadeau du
    /// jour), un carrousel : les lots défilent à l'horizontale, ralentissent, la flèche s'arrête sur celui qu'on gagne.
    /// Le lot est tiré AVANT l'animation (SurpriseBox.Open) : le carrousel ne fait que le montrer.
    /// </summary>
    public sealed class SurpriseScreen : UiScreen
    {
        RectTransform _offer, _grid, _reel, _strip, _reveal, _rays, _glow;
        Image _raysImg, _glowImg;
        Text _coins, _title, _wonName, _wonRarity;
        Image _wonChip;
        Button _open, _wear, _later, _ok;
        readonly List<RectTransform> _cells = new List<RectTransform>();
        Prize _won;
        float _t, _spinTime, _from, _to;
        int _lastTick = int.MinValue;
        bool _spinning, _revealed;
        const float Cell = 250f, Gap = 24f, Step = Cell + Gap;
        /// <summary>Largeur du cadre doré : la case gagnée grossie (250 × 1,12 = 280) plus 22 px d'or de chaque côté.</summary>
        const float FrameW = 324f;
        const int Count = 34, Target = 28;
        RectTransform _frameRt;

        protected override void Build()
        {
            _coins = MetaUi.CoinPill(Safe, Art, out var crt, 46);
            Ui.Place(crt, 1, 1, -36, -40, 270, 104, 1, 1);
            _title = Ui.Title(Safe, Loc.Get("surprise_title"), 80, Palette.Accent);
            Ui.Place(_title.rectTransform, 0.5f, 1, 0, -200, 980, 120);

            _glowImg = Ui.Image(Safe, Art.SoftDisc, new Color(1f, 0.85f, 0.4f, 0f), "Lueur");
            _glow = _glowImg.rectTransform;
            Ui.Place(_glow, 0.5f, 0.5f, 0, 120, 900, 900);
            _raysImg = Ui.Image(Safe, Art.Rays, new Color(1f, 0.92f, 0.6f, 0f), "Rayons");
            _rays = _raysImg.rectTransform;
            Ui.Place(_rays, 0.5f, 0.5f, 0, 120, 1500, 1500);
            _rays.gameObject.AddComponent<Spin>().Speed = 12f;

            // --- Offre ---
            _offer = Ui.Rect("Offre", Safe);
            Ui.Stretch(_offer);
            var sub = Ui.Label(_offer, Loc.Get("surprise_desc"), 40, Color.white);
            Ui.Place(sub.rectTransform, 0.5f, 1, 0, -298, 960, 100);
            Ui.Outlined(sub, 3f);
            var chances = Ui.Title(_offer, Loc.Get("surprise_chances"), 40, ArtCache.Hex("#BFE0FF"));
            Ui.Place(chances.rectTransform, 0.5f, 1, 0, -392, 900, 56);
            var area = Ui.Rect("Zone", _offer);
            Ui.Stretch(area, 40, 430, 40, 430);
            _grid = Ui.Scroll(area, out _);
            var g = _grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(232, 290);
            g.spacing = new Vector2(14, 16);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 4;
            g.childAlignment = TextAnchor.UpperCenter;
            _open = Ui.Button(_offer, "Ouvrir", Palette.Play, () => App.ConfirmSurprise(), Icon.Coin, 60, "Ouvrir");
            Ui.Place((RectTransform)_open.transform, 0.5f, 0, 0, 250, 780, 160);
            var cancel = Ui.Button(_offer, Loc.Get("cancel"), Palette.Dark, Hide, null, 50, "Annuler");
            Ui.Place((RectTransform)cancel.transform, 0.5f, 0, 0, 90, 520, 132);

            // --- Carrousel ---
            _reel = Ui.Rect("Carrousel", Safe);
            Ui.Place(_reel, 0.5f, 0.5f, 0, 120, 1080, 420);
            var frame = Ui.Image(_reel, Art.Pill, new Color(0.05f, 0.03f, 0.14f, 0.55f), "Bande", true);
            Ui.Place(frame.rectTransform, 0.5f, 0.5f, 0, 0, 1080, 330);
            // Cadre doré fixe au centre, derrière les lots : celui qui s'y arrête en ressort encadré d'or.
            // Assez large pour laisser une bordure dorée bien visible autour de la case gagnée (même grossie à 1,12).
            var sel = Ui.Image(_reel, Art.Pill, ArtCache.Hex("#FFC93C"), "Cadre", true);
            _frameRt = sel.rectTransform;
            Ui.Place(_frameRt, 0.5f, 0.5f, 0, 0, FrameW, 344);
            var view = Ui.Rect("Vue", _reel);
            Ui.Place(view, 0.5f, 0.5f, 0, 0, 1080, 330);
            view.gameObject.AddComponent<RectMask2D>();
            _strip = Ui.Rect("Lots", view);
            Ui.Place(_strip, 0.5f, 0.5f, 0, 0, 10, 300);
            foreach (int side in new[] { 1, -1 })
            {
                var arrow = Ui.Icon(_reel, Art.IconSprite(Icon.Back), "Fleche");
                arrow.color = ArtCache.Hex("#FFC93C");
                Ui.Place(arrow.rectTransform, 0.5f, 0.5f, 0, side * 200, 80, 80);
                arrow.rectTransform.localEulerAngles = new Vector3(0, 0, side > 0 ? 90 : -90);
            }

            // --- Révélation ---
            _reveal = Ui.Rect("Revelation", Safe);
            Ui.Stretch(_reveal);
            _wonName = Ui.Title(_reveal, "", 70, Color.white);
            Ui.Place(_wonName.rectTransform, 0.5f, 0.5f, 0, -170, 980, 100);
            _wonChip = Ui.Image(_reveal, Art.Pill, Color.white, "Etiquette", true);
            Ui.Place(_wonChip.rectTransform, 0.5f, 0.5f, 0, -262, 300, 66);
            _wonRarity = Ui.Title(_wonChip.transform, "", 40);
            Ui.Stretch(_wonRarity.rectTransform, 12, 0, 12, 4);
            _wear = Ui.Button(_reveal, Loc.Get("wear"), Palette.Play, () => { var w = _won; Hide(); App.WearPrize(w); }, Icon.Face, 60, "Porter");
            Ui.Place((RectTransform)_wear.transform, 0.5f, 0, 0, 250, 780, 160);
            _wear.gameObject.AddComponent<PopIn>().Delay = 0.2f;
            _ok = Ui.Button(_reveal, Loc.Get("chest_ok"), Palette.Play, Hide, null, 64, "Super");
            Ui.Place((RectTransform)_ok.transform, 0.5f, 0, 0, 250, 780, 160);
            _ok.gameObject.AddComponent<PopIn>().Delay = 0.2f;
            _later = Ui.Button(_reveal, Loc.Get("later"), Palette.Dark, Hide, null, 50, "PlusTard");
            Ui.Place((RectTransform)_later.transform, 0.5f, 0, 0, 90, 520, 132);
        }

        /// <summary>Montre l'offre : tous les lots possibles et leurs chances.</summary>
        public void Present()
        {
            Show();
            _spinning = _revealed = false;
            _offer.gameObject.SetActive(true);
            _reel.gameObject.SetActive(false);
            _reveal.gameObject.SetActive(false);
            _title.text = Loc.Get("surprise_title");
            _coins.text = Loc.FormatScore(App.Profile.coins);
            _glowImg.color = new Color(1f, 0.85f, 0.4f, 0f);
            _raysImg.color = new Color(1f, 0.92f, 0.6f, 0f);
            int tickets = App.Profile.surpriseTickets;
            Ui.ButtonLabel(_open).text = tickets > 0 ? Loc.Get("surprise_open_free") : Loc.Format("surprise_open", Loc.FormatScore(SurpriseBox.Price));
            Ui.Clear(_grid);
            var odds = SurpriseBox.Odds(App.Profile);
            odds.Sort((a, b) => b.Chance.CompareTo(a.Chance));
            foreach (var o in odds) OddsCard(o);
        }

        static Color PrizeColor(Prize p) =>
            p.Kind == PrizeKind.Head ? CollectionScreen.RarityColor(HeadCatalog.RarityOf(p.Head))
            : p.Kind == PrizeKind.Plank ? Palette.Secondary : p.Jackpot ? ArtCache.Hex("#FFB627") : Palette.Success;

        static string PrizeTag(Prize p) =>
            p.Kind == PrizeKind.Head ? Loc.Get("rarity_" + HeadCatalog.RarityOf(p.Head))
            : p.Kind == PrizeKind.Plank ? Loc.Get("prize_plank") : p.Jackpot ? Loc.Get("prize_jackpot") : Loc.Get("coins_word");

        /// <summary>L'image d'un lot : silhouette de tête (ou la tête si <paramref name="revealHead"/>), planche, pièces.</summary>
        RectTransform PrizeVisual(Transform parent, Prize p, bool revealHead, float size)
        {
            if (p.Kind == PrizeKind.Head)
            {
                var face = Ui.Icon(parent, revealHead ? Art.Portrait(p.Head) : Art.Locked(p.Head), "Tete");
                face.rectTransform.sizeDelta = new Vector2(size, size);
                if (!revealHead)
                {
                    var q = Ui.Title(face.transform, "?", Mathf.RoundToInt(size * 0.42f), Color.white);
                    Ui.Stretch(q.rectTransform);
                }
                return face.rectTransform;
            }
            if (p.Kind == PrizeKind.Plank)
            {
                var img = Ui.Image(parent, MetaUi.PlankPreview(Art, p.Plank), Color.white, "Planche");
                img.preserveAspect = true;
                img.rectTransform.sizeDelta = new Vector2(size * 1.25f, size * 0.6f);
                return img.rectTransform;
            }
            var holder = Ui.Rect("Pieces", parent);
            holder.sizeDelta = new Vector2(size, size);
            int n = p.Jackpot ? 3 : p.Coins >= 1000 ? 2 : 1;
            for (int i = 0; i < n; i++)
            {
                var c = Ui.Icon(holder, Art.IconSprite(Icon.Coin), "Piece");
                c.color = Palette.Accent;
                float cs = size * (n == 1 ? 0.62f : 0.5f);
                Ui.Place(c.rectTransform, 0.5f, 0.5f, (i - (n - 1) * 0.5f) * size * 0.26f, 24 - i * 8 + (n == 1 ? 0 : 0), cs, cs);
            }
            var amount = Ui.Title(holder, Loc.FormatScore(p.Coins), Mathf.RoundToInt(size * 0.26f), Palette.Accent);
            Ui.Place(amount.rectTransform, 0.5f, 0, 0, 2, size * 1.2f, size * 0.32f, 0.5f, 0);
            return holder;
        }

        void OddsCard(Prize p)
        {
            var rc = PrizeColor(p);
            var card = Ui.Card(_grid, "Carte");
            var band = Ui.Image(card.transform, Art.Pill, rc, "Bande", true);
            Ui.Place(band.rectTransform, 0.5f, 1, 0, -14, 170, 14);
            var halo = Ui.Image(card.transform, Art.SoftDisc, new Color(rc.r, rc.g, rc.b, 0.4f), "Halo");
            Ui.Place(halo.rectTransform, 0.5f, 1, 0, -98, 190, 190);
            var vis = PrizeVisual(card.transform, p, false, 150);
            vis.anchorMin = vis.anchorMax = new Vector2(0.5f, 1); vis.pivot = new Vector2(0.5f, 0.5f);
            vis.anchoredPosition = new Vector2(0, -98);
            float pct = p.Chance * 100f;
            var chip = Ui.Image(card.transform, Art.Pill, rc, "Chance", true);
            Ui.Place(chip.rectTransform, 0.5f, 0, 0, 48, 170, 60);
            var pt = Ui.Title(chip.transform, pct < 1f ? "< 1 %" : Mathf.RoundToInt(pct) + " %", 42);
            Ui.Stretch(pt.rectTransform, 8, 0, 8, 4);
            var r = Ui.Label(card.transform, PrizeTag(p), 30, rc);
            Ui.Place(r.rectTransform, 0.5f, 0, 0, 100, 220, 38);
        }

        /// <summary>Le lot est gagné (déjà payé et donné) : on lance le carrousel qui s'arrête sur lui.</summary>
        public void Spin(Prize won, List<Prize> pool)
        {
            _won = won;
            _offer.gameObject.SetActive(false);
            _reel.gameObject.SetActive(true);
            _reveal.gameObject.SetActive(false);
            _title.text = Loc.Get("surprise_spinning");
            _coins.text = Loc.FormatScore(App.Profile.coins);
            Ui.Clear(_strip);
            _cells.Clear();
            if (pool == null || pool.Count == 0) pool = new List<Prize> { won };
            for (int i = 0; i < Count; i++)
            {
                // Les lots défilent à peu près selon leurs chances (on voit plus souvent ce qui sort souvent).
                Prize p = i == Target ? won : Pick(pool, i);
                if (pool.Count > 1 && (i == Target - 1 || i == Target + 1)) { int guard = 0; while (Same(p, won) && guard++ < 8) p = Pick(pool, i + guard * 13); }
                var cell = Ui.Card(_strip, "Case");
                var rt = cell.rectTransform;
                Ui.Place(rt, 0.5f, 0.5f, i * Step, 0, Cell, 290);
                var rc = PrizeColor(p);
                var halo = Ui.Image(rt, Art.SoftDisc, new Color(rc.r, rc.g, rc.b, 0.45f), "Halo");
                Ui.Place(halo.rectTransform, 0.5f, 0.5f, 0, 8, 240, 240);
                var vis = PrizeVisual(rt, p, true, 200);
                vis.anchoredPosition = new Vector2(0, 12);
                var band = Ui.Image(rt, Art.Pill, rc, "Bande", true);
                Ui.Place(band.rectTransform, 0.5f, 0, 0, 20, 170, 16);
                _cells.Add(rt);
            }
            bool still = App.Profile.settings.reducedMotion;
            // Le lot gagné s'arrête pile sous le cadre doré (pas de décalage).
            _from = 0f;
            _to = Target * Step;
            _spinTime = still ? 0.6f : 5.5f;
            _t = 0;
            _spinning = true;
            _lastTick = int.MinValue;
            _strip.anchoredPosition = new Vector2(-_from, 0);
        }

        /// <summary>
        /// Fait glisser la bande pour que le centre de la case gagnée soit sur le centre du cadre doré, mesuré dans le
        /// repère de l'écran (et non calculé) : quelle que soit la taille de l'écran ou un arrondi, c'est exact.
        /// </summary>
        void AlignWinner(float amount)
        {
            if (_frameRt == null || _cells.Count <= Target) return;
            var win = _cells[Target];
            float scale = _strip.lossyScale.x;
            if (Mathf.Abs(scale) < 1e-5f) return;
            float worldDelta = _frameRt.position.x - win.position.x;
            float local = worldDelta / scale;
            if (Mathf.Abs(local) < 0.01f) return;
            var p = _strip.anchoredPosition;
            _strip.anchoredPosition = new Vector2(p.x + local * Mathf.Clamp01(amount), p.y);
        }

        static bool Same(Prize a, Prize b) => a.Kind == b.Kind && a.Head == b.Head && a.Plank == b.Plank && a.Coins == b.Coins;

        static Prize Pick(List<Prize> pool, int salt)
        {
            float r = Mathf.Repeat(Mathf.Sin(salt * 12.9898f + 4.1f) * 43758.5453f, 1f), acc = 0f, total = 0f;
            foreach (var p in pool) total += p.Chance;
            r *= total;
            foreach (var p in pool) { acc += p.Chance; if (r < acc) return p; }
            return pool[pool.Count - 1];
        }

        public bool Busy => _spinning;

        public override void Tick(float dt)
        {
            _coins.text = Loc.FormatScore(App.Profile.coins);
            if (!_spinning && !_revealed) return;
            _t += dt;
            if (_spinning)
            {
                float x = Mathf.Clamp01(_t / _spinTime);
                // Ça démarre vite et ralentit doucement jusqu'à s'arrêter sur le lot gagné. Pas de fausse fin.
                float e = 1f - Mathf.Pow(1f - x, 4f);
                float pos = Mathf.Lerp(_from, _to, e);
                _strip.anchoredPosition = new Vector2(-pos, 0);
                // Recalage : sur la fin, on mesure où sont vraiment le cadre et la case gagnée à l'écran, et on aligne
                // leurs centres (rien ne bouge si le calcul est déjà juste). La case finit pile dans le cadre.
                if (x > 0.8f) AlignWinner(Mathf.SmoothStep(0f, 1f, (x - 0.8f) / 0.2f));
                if (x >= 1f) { AlignWinner(1f); Reveal(); }
                int idx = Mathf.RoundToInt(pos / Step);
                if (idx != _lastTick) { _lastTick = idx; App.PlaySfx(Sfx.Click, 1, 0.5f); }
                for (int i = 0; i < _cells.Count; i++)
                {
                    float d = Mathf.Abs(i * Step - pos) / Step;
                    _cells[i].localScale = Vector3.one * Mathf.Lerp(1.08f, 0.82f, Mathf.Clamp01(d));
                }
                return;
            }
            float r = _t;
            _glowImg.color = new Color(1f, 0.85f, 0.4f, Mathf.Clamp01(r * 3f) * 0.7f);
            _raysImg.color = new Color(1f, 0.92f, 0.6f, Mathf.Clamp01(r * 2f) * (_won.Jackpot ? 0.55f : 0.35f));
            var win = _cells[Target];
            AlignWinner(1f); // reste centrée dans le cadre
            // Elle grossit sans sortir du carrousel (1,12 × 290 px < 330 px de haut).
            float pop = r < 0.25f ? Mathf.Lerp(1.08f, 1.12f, r / 0.25f) : 1.1f + 0.02f * Mathf.Sin(r * 4f);
            win.localScale = Vector3.one * pop;
        }

        void Reveal()
        {
            _spinning = false;
            _revealed = true;
            _t = 0;
            var rc = PrizeColor(_won);
            switch (_won.Kind)
            {
                case PrizeKind.Head:
                    _title.text = Loc.Get("surprise_new");
                    _wonName.text = _won.Head.Name(Loc.Language);
                    break;
                case PrizeKind.Plank:
                    _title.text = Loc.Get("surprise_new_plank");
                    _wonName.text = Loc.Format("gift_item_plank", _won.Plank.Name(Loc.Language));
                    break;
                default:
                    _title.text = Loc.Get(_won.Jackpot ? "prize_jackpot" : "surprise_coins");
                    _wonName.text = "+" + Loc.FormatScore(_won.Coins) + " " + Loc.Get("coins_word");
                    break;
            }
            _wonChip.color = rc;
            _wonRarity.text = PrizeTag(_won);
            _reveal.gameObject.SetActive(true);
            bool wearable = _won.Kind != PrizeKind.Coins;
            _wear.gameObject.SetActive(wearable);
            _later.gameObject.SetActive(wearable);
            _ok.gameObject.SetActive(!wearable);
            _cells[Target].SetAsLastSibling();
            App.PlaySfx(Sfx.Chime, _won.Jackpot ? 8 : 7, 0.9f);
            App.HapticMedium();
        }

        public void Back() { if (!_spinning) Hide(); }

        public override void Hide()
        {
            base.Hide();
            if (_revealed || _spinning) App.OnSurpriseClosed();
            _spinning = _revealed = false;
        }
    }
}
