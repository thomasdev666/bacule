using System;
using System.Collections.Generic;
using Bascule.Core;
using Bascule.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>Habillage commun des ligues : couleur, icône et nom de chaque division, écusson, durée avant le lundi.</summary>
    static class LeagueUi
    {
        public static Color Tint(Division d) =>
            d == Division.Legend ? ArtCache.Hex("#FFC93C") : d == Division.Elite ? ArtCache.Hex("#B07BFF")
            : d == Division.Challenger ? ArtCache.Hex("#4AA8FF") : ArtCache.Hex("#8FA3BF");

        /// <summary>Couleur foncée assortie (bandeaux, fond de l'écusson).</summary>
        public static Color Deep(Division d) =>
            d == Division.Legend ? ArtCache.Hex("#B8720A") : d == Division.Elite ? ArtCache.Hex("#5B2DB5")
            : d == Division.Challenger ? ArtCache.Hex("#1B5FB8") : ArtCache.Hex("#4A5A75");

        public static Icon Symbol(Division d) =>
            d == Division.Legend ? Icon.Crown : d == Division.Elite ? Icon.Star : d == Division.Challenger ? Icon.Swords : Icon.Flame;

        public static string Name(Division d) => Loc.Get("league_div_" + (int)d);

        /// <summary>« 3 j 4 h » au-delà d'un jour, sinon « 5 h 03 » ou « 12 min 04 ».</summary>
        public static string TimeLeft(double seconds)
        {
            int s = (int)Math.Max(0, seconds);
            return s >= 86400 ? Loc.Format("league_days_hours", s / 86400, (s % 86400) / 3600) : Loc.Duration(s);
        }

        /// <summary>Blason d'une division (dessin UiArtist.LeagueCrest) sur un halo de sa couleur.</summary>
        public sealed class Badge
        {
            public RectTransform Root;
            public Image Glow, Crest;

            public void Set(Division d)
            {
                var tint = Tint(d);
                Glow.color = new Color(tint.r, tint.g, tint.b, 0.5f);
                Crest.sprite = Ui.Art.LeagueCrest((int)d);
            }
        }

        public static Badge MakeBadge(Transform parent, float size, string name = "Blason")
        {
            var b = new Badge { Root = Ui.Rect(name, parent) };
            b.Root.sizeDelta = new Vector2(size, size);
            b.Glow = Ui.Image(b.Root, Ui.Art.SoftDisc, Color.white, "Halo");
            Ui.Place(b.Glow.rectTransform, 0.5f, 0.5f, 0, 0, size * 1.35f, size * 1.35f);
            b.Crest = Ui.Icon(b.Root, null, "Dessin");
            // Le dessin a un peu de marge autour de l'écu : on le pose légèrement plus grand que son cadre.
            Ui.Place(b.Crest.rectTransform, 0.5f, 0.5f, 0, 0, size * 1.16f, size * 1.16f);
            return b;
        }
    }

    /// <summary>
    /// Classement de la ligue de la semaine : ma division en grand, le temps avant le lundi, les récompenses, puis les
    /// joueurs rangés par division (Légende, Élite, Challenger, Espoir), chacune sous son bandeau de couleur.
    /// </summary>
    public sealed class LeagueScreen : UiScreen
    {
        RectTransform _list;
        Text _status, _division, _line, _reset, _rule;
        LeagueUi.Badge _badge;
        readonly List<(Image image, int head)> _pending = new List<(Image, int)>();
        int _request;
        float _clock;

        static readonly Color Gold = ArtCache.Hex("#FFC93C"), Silver = ArtCache.Hex("#C9D3E3"), Bronze = ArtCache.Hex("#E08A4F");

        protected override void Build()
        {
            BackButton(App.CloseLeague);
            Header(Loc.Get("league_title"));

            // ---- Ma division.
            var mine = Ui.Image(Safe, Art.Panel, ArtCache.Hex("#2B1B54"), "MaLigue", true);
            Ui.Place(mine.rectTransform, 0.5f, 1, 0, -250, 1000, 250, 0.5f, 1);
            var rays = Ui.Image(mine.transform, Art.Rays, new Color(1f, 0.85f, 0.4f, 0.18f), "Rayons");
            Ui.Place(rays.rectTransform, 0, 0.5f, 150, 0, 520, 520);
            rays.gameObject.AddComponent<Spin>().Speed = 10f;
            Ui.Isolate(rays.gameObject);
            mine.gameObject.AddComponent<RectMask2D>();
            _badge = LeagueUi.MakeBadge(mine.transform, 180);
            Ui.Place(_badge.Root, 0, 0.5f, 150, 0, 180, 180);
            _division = Ui.Title(mine.transform, "", 64, Color.white, "Division");
            Ui.Place(_division.rectTransform, 0, 0.5f, 280, 62, 690, 80, 0, 0.5f);
            _division.alignment = TextAnchor.MiddleLeft;
            _line = Ui.Title(mine.transform, "", 44, ArtCache.Hex("#FFE9A8"), "Place");
            Ui.Place(_line.rectTransform, 0, 0.5f, 280, -4, 690, 60, 0, 0.5f);
            _line.alignment = TextAnchor.MiddleLeft;
            _reset = Ui.Title(mine.transform, "", 34, ArtCache.Hex("#BFE0FF"), "Fin");
            Ui.Place(_reset.rectTransform, 0, 0.5f, 280, -66, 690, 48, 0, 0.5f);
            _reset.alignment = TextAnchor.MiddleLeft;

            // ---- Récompenses du lundi, une pastille par division.
            var t = App.Tuning.league;
            Reward(0, Division.Legend, 1, t.legendPlaces);
            Reward(1, Division.Elite, t.legendPlaces + 1, t.legendPlaces + t.elitePlaces);
            Reward(2, Division.Challenger, t.legendPlaces + t.elitePlaces + 1, League.LastPlace(Division.Challenger, t));

            _status = Ui.Title(Safe, "", 42);
            Ui.Place(_status.rectTransform, 0.5f, 1, 0, -900, 900, 200);

            var area = Ui.Rect("Liste", Safe);
            Ui.Stretch(area, 36, 720, 36, 150);
            _list = Ui.Scroll(area, out _);
            Ui.Column(_list, 12);

            _rule = Ui.Label(Safe, "", 30, ArtCache.Hex("#D8E9FF"));
            Ui.Place(_rule.rectTransform, 0.5f, 0, 0, 84, 1000, 90);
        }

        void Reward(int i, Division d, int from, int to)
        {
            var chip = Ui.Image(Safe, Art.Panel, LeagueUi.Deep(d), "Recompense", true);
            Ui.Place(chip.rectTransform, 0.5f, 1, (i - 1) * 336, -522, 322, 176, 0.5f, 1);
            var top = Ui.Image(chip.transform, Art.Pill, LeagueUi.Tint(d), "Nom", true);
            Ui.Place(top.rectTransform, 0.5f, 1, 0, -12, 290, 52, 0.5f, 1);
            var name = Ui.Title(top.transform, LeagueUi.Name(d), 34, Palette.Ink);
            Ui.Plain(name);
            Ui.Stretch(name.rectTransform, 12, 0, 12, 0);
            var places = Ui.Label(chip.transform, Loc.Format("league_places", from, to), 28, new Color(1, 1, 1, 0.85f));
            Ui.Place(places.rectTransform, 0.5f, 1, 0, -72, 300, 36, 0.5f, 1);
            var coin = Ui.Icon(chip.transform, Art.IconSprite(Icon.Coin), "Piece");
            coin.color = Palette.Accent;
            Ui.Place(coin.rectTransform, 0.5f, 0, -66, 36, 46, 46);
            var amount = Ui.Title(chip.transform, Loc.FormatScore(League.Reward(d, App.Tuning.league)), 44, Palette.Accent);
            Ui.Place(amount.rectTransform, 0.5f, 0, 44, 36, 150, 52);
            amount.alignment = TextAnchor.MiddleLeft;
        }

        public override void Tick(float dt)
        {
            _clock -= dt;
            if (_clock <= 0f) { _clock = 1f; UpdateReset(); }
            int budget = 2;
            for (int i = 0; i < _pending.Count && budget > 0;)
            {
                var (image, headId) = _pending[i];
                if (image == null) { _pending.RemoveAt(i); continue; }
                var head = HeadCatalog.Get(headId);
                bool ready = Art.PortraitReady(head, false);
                if (!ready && Art.Preparing) { i++; continue; }
                image.sprite = Art.Portrait(head);
                image.color = Color.white;
                _pending.RemoveAt(i);
                budget -= ready ? 1 : 2;
            }
        }

        void UpdateReset()
        {
            var now = DateTime.UtcNow;
            _reset.text = Loc.Format("league_reset", LeagueUi.TimeLeft((League.NextReset(now) - now).TotalSeconds));
        }

        void SetMine(int rank, int points)
        {
            var t = App.Tuning.league;
            var d = League.DivisionOf(points > 0 ? rank : 0, t);
            _badge.Set(d);
            _division.text = Loc.Format("league_division", LeagueUi.Name(d));
            _division.color = LeagueUi.Tint(d);
            _line.text = points <= 0 ? Loc.Get("league_no_points")
                : rank > 0 ? Loc.Format("league_my_line", Loc.Rank(rank), Loc.FormatScore(points)) : Loc.Format("league_points_long", Loc.FormatScore(points));
        }

        public override async void Refresh()
        {
            var t = App.Tuning.league;
            var s = App.Profile.league;
            League.Roll(s, League.WeekKey(DateTime.UtcNow));
            SetMine(s.rank, s.points);
            UpdateReset();
            _rule.text = Loc.Format("league_rule", t.winPoints, t.drawPoints, t.losePoints, t.ghostWinPoints, t.ghostDrawPoints, t.ghostLosePoints);
            Ui.Clear(_list);
            _pending.Clear();
            int request = ++_request;
            var service = App.Leaderboard.Service;
            if (!service.IsOnline) { _status.text = Loc.Get("lb_offline"); return; }
            _status.text = Loc.Get("lb_loading");
            await App.Leaderboard.FlushAsync(App.TodayKey);
            if (request != _request || !Visible) return;
            var page = await service.GetTopAsync(League.Board, 100);
            if (request != _request || !Visible) return;
            if (!page.Online) { _status.text = Loc.Get("league_soon"); return; }

            var shown = new List<LeaderboardEntry>();
            foreach (var e in page.Entries)
            {
                if (!e.IsMe && Moderation.IsBlocked(App.Profile, e.PlayerId)) continue;
                if (!e.IsMe) e.Name = PseudoFilter.Display(e.Name, Loc.Get("lb_player"));
                shown.Add(e);
            }
            var me = shown.Find(e => e.IsMe) ?? page.Me;
            if (me != null && me.Rank > 0)
            {
                App.AdoptLeaguePoints(me.Score);
                App.SetLeagueRank(me.Rank);
                SetMine(me.Rank, Mathf.Max(s.points, me.Score));
            }
            _status.text = shown.Count == 0 ? Loc.Get("league_empty") : "";

            // Une section par division. Les places libres des divisions récompensées se voient : elles donnent envie.
            int index = 0;
            foreach (Division d in new[] { Division.Legend, Division.Elite, Division.Challenger, Division.Hopeful })
            {
                int last = League.LastPlace(d, t);
                int first = d == Division.Legend ? 1 : League.LastPlace(d - 1, t) + 1;
                var rows = new List<LeaderboardEntry>();
                while (index < shown.Count && (last == 0 || shown[index].Rank <= last)) rows.Add(shown[index++]);
                if (d == Division.Hopeful && rows.Count == 0) continue;
                Section(d, first, last);
                foreach (var e in rows) Row(d, e);
                if (last > 0 && rows.Count < last - first + 1 && shown.Count > 0) FreePlace(d, first + rows.Count);
            }
        }

        void Section(Division d, int first, int last)
        {
            var band = Ui.Image(_list, Art.Pill, LeagueUi.Deep(d), "Division", true);
            Ui.Size(band, 92);
            var badge = LeagueUi.MakeBadge(band.transform, 72);
            Ui.Place(badge.Root, 0, 0.5f, 56, 0, 72, 72);
            badge.Glow.gameObject.SetActive(false);
            badge.Set(d);
            var name = Ui.Title(band.transform, LeagueUi.Name(d), 46, LeagueUi.Tint(d));
            Ui.Place(name.rectTransform, 0, 0.5f, 110, 0, 340, 64, 0, 0.5f);
            name.alignment = TextAnchor.MiddleLeft;
            int coins = League.Reward(d, App.Tuning.league);
            string right = last > 0 ? Loc.Format("league_places", first, last) + (coins > 0 ? " · " + Loc.Format("league_reward", Loc.FormatScore(coins)) : "")
                : Loc.Format("league_places_after", first);
            var info = Ui.Label(band.transform, right, 30, new Color(1, 1, 1, 0.9f), TextAnchor.MiddleRight);
            Ui.Place(info.rectTransform, 1, 0.5f, -30, 0, 520, 60, 1, 0.5f);
        }

        void Row(Division d, LeaderboardEntry e)
        {
            var row = Ui.Card(_list, "Ligne");
            row.color = e.IsMe ? ArtCache.Hex("#FFE9A8") : Color.white;
            Ui.Size(row, d == Division.Legend ? 156 : 136);
            // Liseré de la couleur de la division, à gauche.
            var edge = Ui.Image(row.transform, Art.Pill, LeagueUi.Tint(d), "Lisere", true);
            Ui.Place(edge.rectTransform, 0, 0.5f, 12, 0, 14, d == Division.Legend ? 112 : 92, 0, 0.5f);
            if (e.Rank >= 1 && e.Rank <= 3)
            {
                var disc = Ui.Image(row.transform, Art.Circle, new[] { Gold, Silver, Bronze }[e.Rank - 1], "Medaille");
                Ui.Place(disc.rectTransform, 0, 0.5f, 44, 0, 92, 92, 0, 0.5f);
                var num = Ui.Title(disc.transform, e.Rank.ToString(), 50);
                Ui.Stretch(num.rectTransform, 0, 0, 0, 4);
            }
            else
            {
                var r = Ui.Title(row.transform, e.Rank.ToString(), 50, e.IsMe ? Palette.Primary : ArtCache.Hex("#AEB6C8"));
                Ui.Place(r.rectTransform, 0, 0.5f, 34, 0, 112, 100, 0, 0.5f);
            }
            float face = d == Division.Legend ? 128 : 112;
            var icon = Ui.Icon(row.transform, Art.PortraitIfLoaded(HeadCatalog.Get(e.Head), false));
            if (icon.sprite == null) { icon.sprite = Art.SoftDisc; icon.color = new Color(0.17f, 0.18f, 0.26f, 0.12f); _pending.Add((icon, e.Head)); }
            Ui.Place(icon.rectTransform, 0, 0.5f, 154, 0, face, face, 0, 0.5f);
            if (e.Rank == 1)
            {
                var crown = Ui.Icon(row.transform, Art.IconSprite(Icon.Crown), "Couronne");
                crown.color = Gold;
                Ui.Place(crown.rectTransform, 0, 0.5f, 154 + face * 0.5f - 34, face * 0.5f - 6, 68, 56, 0, 0.5f);
                crown.rectTransform.localRotation = Quaternion.Euler(0, 0, 12f);
            }
            string shownName = e.IsMe && string.IsNullOrEmpty(e.Name) ? Loc.Get("lb_you") : string.IsNullOrEmpty(e.Name) ? "-" : e.Name;
            var n = Ui.Plain(Ui.Label(row.transform, shownName, 46, Palette.Ink, TextAnchor.MiddleLeft));
            Ui.Place(n.rectTransform, 0, 0.5f, 300, 0, 400, 90, 0, 0.5f);
            var pts = Ui.Plain(Ui.Label(row.transform, Loc.Format("league_points", Loc.FormatScore(e.Score)), 48, Palette.Ink, TextAnchor.MiddleRight));
            Ui.Place(pts.rectTransform, 1, 0.5f, -34, 0, 250, 90, 1, 0.5f);
            if (!e.IsMe && !string.IsNullOrEmpty(e.PlayerId))
            {
                string id = e.PlayerId, who = e.Name;
                int head = e.Head;
                row.raycastTarget = true;
                var open = row.gameObject.AddComponent<Button>();
                open.transition = Selectable.Transition.None;
                open.onClick.AddListener(() => { Ui.Click?.Invoke(); App.ShowPlayerProfile(id, who, head, 0, 0); });
            }
        }

        void FreePlace(Division d, int rank)
        {
            var row = Ui.Image(_list, Art.Panel, new Color(1, 1, 1, 0.16f), "PlaceLibre", true);
            Ui.Size(row, 104);
            var r = Ui.Title(row.transform, rank.ToString(), 46, LeagueUi.Tint(d));
            Ui.Place(r.rectTransform, 0, 0.5f, 34, 0, 112, 80, 0, 0.5f);
            var txt = Ui.Label(row.transform, Loc.Get("league_free_place"), 38, Color.white, TextAnchor.MiddleLeft);
            Ui.Place(txt.rectTransform, 0, 0.5f, 160, 0, 800, 70, 0, 0.5f);
        }
    }

    /// <summary>Lundi : la semaine finie, ma place, ma division et les pièces gagnées.</summary>
    public sealed class LeagueRewardScreen : UiScreen
    {
        Text _rank, _division, _coins;
        LeagueUi.Badge _badge;
        Image _coinIcon;

        protected override void Build()
        {
            var card = Ui.Image(Safe, Art.Panel, ArtCache.Hex("#2B1B54"), "Fenetre", true);
            Ui.Place(card.rectTransform, 0.5f, 0.52f, 0, 0, 960, 1200);
            card.gameObject.AddComponent<PopIn>();
            card.gameObject.AddComponent<RectMask2D>();
            var rays = Ui.Image(card.transform, Art.Rays, new Color(1f, 0.85f, 0.4f, 0.3f), "Rayons");
            Ui.Place(rays.rectTransform, 0.5f, 1, 0, -420, 1300, 1300);
            rays.gameObject.AddComponent<Spin>().Speed = 14f;
            Ui.Isolate(rays.gameObject);
            var title = Ui.Title(card.transform, Loc.Get("league_end_title"), 78, Palette.Accent);
            Ui.Place(title.rectTransform, 0.5f, 1, 0, -110, 880, 110);
            _badge = LeagueUi.MakeBadge(card.transform, 300);
            Ui.Place(_badge.Root, 0.5f, 1, 0, -420, 300, 300);
            _badge.Root.gameObject.AddComponent<Pulse>().Amount = 0.04f;
            _division = Ui.Title(card.transform, "", 66, Color.white, "Division");
            Ui.Place(_division.rectTransform, 0.5f, 1, 0, -660, 880, 90);
            _rank = Ui.Title(card.transform, "", 46, ArtCache.Hex("#D8E9FF"), "Place");
            Ui.Place(_rank.rectTransform, 0.5f, 1, 0, -750, 880, 110);
            _coinIcon = Ui.Icon(card.transform, Art.CoinPile(3), "Pieces");
            Ui.Place(_coinIcon.rectTransform, 0.5f, 1, -250, -880, 130, 130);
            _coins = Ui.Title(card.transform, "", 76, Palette.Accent, "Gain");
            Ui.Place(_coins.rectTransform, 0.5f, 1, 70, -880, 480, 110);
            var next = Ui.Label(card.transform, Loc.Get("league_end_next"), 36, ArtCache.Hex("#BFE0FF"));
            Ui.Place(next.rectTransform, 0.5f, 0, 0, 250, 860, 60);
            var ok = Ui.Button(card.transform, Loc.Get("league_end_ok"), Palette.Play, Hide, Icon.Check, 58, "Ok");
            Ui.Place((RectTransform)ok.transform, 0.5f, 0, 0, 110, 700, 160);
        }

        public void Open(int rank, Division division, int coins)
        {
            Show();
            _badge.Set(division);
            _division.text = Loc.Format("league_division", LeagueUi.Name(division));
            _division.color = LeagueUi.Tint(division);
            _rank.text = rank > 0 ? Loc.Format("league_end_rank", Loc.Rank(rank)) : Loc.Get("league_end_unranked");
            bool gain = coins > 0;
            _coinIcon.gameObject.SetActive(gain);
            _coins.gameObject.SetActive(gain);
            if (gain) { _coins.text = Loc.Format("league_end_coins", Loc.FormatScore(coins)); _coinIcon.sprite = Art.CoinPile(coins); }
        }
    }
}
