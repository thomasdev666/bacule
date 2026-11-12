using System;
using System.Collections.Generic;
using Bascule.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>
    /// Premier lancement, juste après l'écran de bienvenue : trois petites étapes, une par fenêtre.
    /// 1. Le pseudo (visible dans les classements et les duels), avec une idée au hasard ; « Plus tard » possible.
    /// 2. L'année de naissance, demandée de façon neutre (une grille d'années, rien de présélectionné).
    /// 3. Les pubs (15 ans ou plus seulement) : « Accepter » et « Refuser » ont la même taille et la même place ;
    ///    le jeu est identique dans les deux cas. Sur iPhone, l'accord est suivi de la demande d'iOS.
    /// Un joueur déjà installé ne voit que les étapes qui lui manquent.
    /// </summary>
    public sealed class OnboardScreen : UiScreen
    {
        enum Step { Pseudo, Age, Ads }

        RectTransform _pseudoRt, _ageRt, _adsRt, _dots;
        Text _title, _sub, _pseudoError, _yearPicked;
        InputField _input;
        Button _ageOk;
        RectTransform _yearGrid;
        readonly List<(int year, Image bg, Text label)> _years = new List<(int, Image, Text)>();
        readonly List<Step> _steps = new List<Step>();
        int _stepIndex, _pickedYear;
        Action _done;

        static readonly string[] IdeaStart = { "Super", "Mega", "Turbo", "Hyper", "Ultra", "Roi", "Reine", "Mini", "Maxi", "Capitaine" };
        static readonly string[] IdeaEnd = { "Basile", "Planche", "Bascule", "Panda", "Fusée", "Comète", "Tornade", "Pingouin", "Dragon", "Équilibre" };

        static readonly Color Deep = ArtCache.Hex("#2B1B54"), Soft = ArtCache.Hex("#D8E9FF"), Sky = ArtCache.Hex("#BFE0FF");
        Text _nameTag;

        protected override void Build()
        {
            // Même style que le duel et l'invitation : fenêtre violet nuit, rayons qui tournent, verre et pastilles.
            var card = Ui.Image(Safe, Art.Panel, Deep, "Fenetre", true);
            Ui.Place(card.rectTransform, 0.5f, 0.5f, 0, 20, 980, 1560);
            card.gameObject.AddComponent<PopIn>();
            card.gameObject.AddComponent<RectMask2D>();
            var rays = Ui.Image(card.transform, Art.Rays, new Color(1f, 0.85f, 0.4f, 0.26f), "Rayons");
            Ui.Place(rays.rectTransform, 0.5f, 1, 0, -500, 1400, 1400);
            rays.gameObject.AddComponent<Spin>().Speed = 10f;
            Ui.Isolate(rays.gameObject);

            // Points d'étape (1, 2, 3) en haut de la fenêtre.
            _dots = Ui.Rect("Etapes", card.transform);
            Ui.Place(_dots, 0.5f, 1, 0, -62, 300, 34);
            var row = _dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 22; row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = false; row.childForceExpandWidth = row.childForceExpandHeight = false;

            _title = Ui.Title(card.transform, "", 76, Palette.Accent);
            Ui.Place(_title.rectTransform, 0.5f, 1, 0, -146, 920, 110);
            _sub = Ui.Label(card.transform, "", 40, Soft);
            Ui.Place(_sub.rectTransform, 0.5f, 1, 0, -250, 880, 100);

            BuildPseudo(card.transform);
            BuildAge(card.transform);
            BuildAds(card.transform);
        }

        // ------------------------------------------------------------------ 1. Pseudo

        void BuildPseudo(Transform card)
        {
            _pseudoRt = Ui.Stretch(Ui.Rect("Pseudo", card));
            // La tête du joueur sur un halo, et son pseudo en étiquette dessous, mis à jour en direct.
            var glow = Ui.Image(_pseudoRt, Art.SoftDisc, new Color(1f, 0.78f, 0.35f, 0.5f), "Halo");
            Ui.Place(glow.rectTransform, 0.5f, 1, 0, -500, 460, 460);
            var face = DuelUi.Portrait(_pseudoRt, Palette.Secondary, 270, "Tete");
            face.sprite = Art.Portrait(HeadCatalog.Get(App.Profile.selectedHead));
            Ui.Place((RectTransform)face.transform.parent, 0.5f, 1, 0, -500, 270, 270);
            face.transform.parent.gameObject.AddComponent<Rock>().Angle = 3f;
            Ui.Isolate(face.transform.parent.gameObject);
            var tag = Ui.Image(_pseudoRt, Art.Pill, Palette.Primary, "Etiquette", true);
            Ui.Place(tag.rectTransform, 0.5f, 1, 0, -682, 460, 84);
            _nameTag = Ui.Title(tag.transform, "", 42, Color.white);
            Ui.Stretch(_nameTag.rectTransform, 22, 2, 22, 0);

            // Champ de saisie blanc bien visible, et le dé (idée au hasard) à droite.
            var rim = Ui.Image(_pseudoRt, Art.Pill, Palette.Outline, "Bord", true);
            Ui.Place(rim.rectTransform, 0.5f, 1, -84, -862, 720, 146);
            var field = Ui.Image(rim.transform, Art.Pill, Color.white, "Champ", true);
            Ui.Stretch(field.rectTransform, 7, 7, 7, 7);
            field.raycastTarget = true;
            var text = Ui.Label(field.transform, "", 58, Palette.Ink, TextAnchor.MiddleCenter);
            Ui.Stretch(text.rectTransform, 40, 8, 40, 8);
            var placeholder = Ui.Label(field.transform, Loc.Get("onboard_pseudo_placeholder"), 50, Palette.Muted, TextAnchor.MiddleCenter);
            Ui.Stretch(placeholder.rectTransform, 40, 8, 40, 8);
            _input = field.gameObject.AddComponent<InputField>();
            _input.textComponent = text;
            _input.placeholder = placeholder;
            _input.characterLimit = 16;
            _input.lineType = InputField.LineType.SingleLine;
            _input.shouldHideMobileInput = true;
            _input.customCaretColor = true;
            _input.caretColor = Palette.Ink;
            _input.caretWidth = 4;
            _input.selectionColor = new Color(0.29f, 0.61f, 1f, 0.35f);
            _input.onValueChanged.AddListener(v => { _pseudoError.text = ""; ShowTag(v); });

            var dice = Ui.IconButton(_pseudoRt, Icon.Replay, Palette.Purple, () => { _input.text = Idea(); }, "Idee");
            Ui.Place((RectTransform)dice.transform, 0.5f, 1, 386, -862, 150, 150);

            _pseudoError = Ui.Title(_pseudoRt, "", 38, ArtCache.Hex("#FF8A8E"));
            Ui.Place(_pseudoError.rectTransform, 0.5f, 1, 0, -980, 880, 64);
            var rules = Ui.Label(_pseudoRt, Loc.Get("onboard_pseudo_rules"), 36, Sky);
            Ui.Place(rules.rectTransform, 0.5f, 1, 0, -1062, 860, 96);

            var ok = Ui.Button(_pseudoRt, Loc.Get("onboard_next"), Palette.Play, ValidatePseudo, Icon.Check, 62, "Valider");
            Ui.Place((RectTransform)ok.transform, 0.5f, 0, 0, 210, 780, 166);
            var later = Ui.Button(_pseudoRt, Loc.Get("onboard_later"), Palette.Muted, () => { App.Profile.pseudoAsked = true; Next(); }, null, 44, "PlusTard");
            Ui.Place((RectTransform)later.transform, 0.5f, 0, 0, 66, 460, 112);
        }

        void ShowTag(string raw)
        {
            raw = (raw ?? "").Trim();
            _nameTag.text = raw.Length > 0 ? raw : Loc.Get("onboard_pseudo_placeholder");
        }

        static string Idea()
        {
            var r = new System.Random();
            return IdeaStart[r.Next(IdeaStart.Length)] + IdeaEnd[r.Next(IdeaEnd.Length)] + r.Next(10, 100);
        }

        void ValidatePseudo()
        {
            string raw = _input.text;
            if (string.IsNullOrWhiteSpace(raw)) { _pseudoError.text = Loc.Get("onboard_pseudo_empty"); return; }
            string name = PseudoFilter.Clean(raw);
            if (name == null) { _pseudoError.text = Loc.Get("pseudo_refused"); return; }
            App.SetPseudo(name);
            App.Profile.pseudoAsked = true;
            Next();
        }

        // ------------------------------------------------------------------ 2. Année de naissance

        Image _pickedPill;

        void BuildAge(Transform card)
        {
            _ageRt = Ui.Stretch(Ui.Rect("Age", card));
            // Grille d'années sur un panneau de verre, rien de présélectionné (question neutre).
            var area = Ui.Glass(_ageRt, "Annees");
            Ui.Place(area.rectTransform, 0.5f, 1, 0, -330, 900, 770, 0.5f, 1);
            var inner = Ui.Rect("Zone", area.transform);
            Ui.Stretch(inner, 22, 22, 22, 22);
            _yearGrid = Ui.Scroll(inner, out _);
            var grid = _yearGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(196, 104);
            grid.spacing = new Vector2(14, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(4, 4, 6, 6);

            _pickedPill = Ui.Image(_ageRt, Art.Pill, Palette.Purple, "Choix", true);
            Ui.Place(_pickedPill.rectTransform, 0.5f, 0, 0, 392, 520, 84);
            _yearPicked = Ui.Title(_pickedPill.transform, "", 44, Color.white);
            Ui.Stretch(_yearPicked.rectTransform, 20, 2, 20, 0);
            var note = Ui.Label(_ageRt, Loc.Get("onboard_age_note"), 34, Sky);
            Ui.Place(note.rectTransform, 0.5f, 0, 0, 300, 860, 76);
            _ageOk = Ui.Button(_ageRt, Loc.Get("onboard_next"), Palette.Play, ValidateAge, Icon.Check, 62, "Valider");
            Ui.Place((RectTransform)_ageOk.transform, 0.5f, 0, 0, 120, 780, 166);
        }

        void FillYears()
        {
            if (_years.Count > 0) return;
            foreach (int y in PrivacyRules.Years(GameApp.CurrentYear))
            {
                int year = y;
                var cell = Ui.Image(_yearGrid, Art.Pill, Color.white, "Annee" + year, true);
                cell.raycastTarget = true;
                var label = Ui.Plain(Ui.Label(cell.transform, year.ToString(), 46, Palette.Ink));
                Ui.Stretch(label.rectTransform, 6, 4, 6, 4);
                var b = cell.gameObject.AddComponent<Button>();
                var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Ui.Click?.Invoke(); PickYear(year); });
                cell.gameObject.AddComponent<PressScale>();
                _years.Add((year, cell, label));
            }
        }

        void PickYear(int year)
        {
            _pickedYear = year;
            foreach (var (y, bg, label) in _years)
            {
                bool on = y == year;
                bg.color = on ? Palette.Secondary : Color.white;
                label.color = on ? Color.white : Palette.Ink;
            }
            _ageOk.interactable = true;
            _pickedPill.gameObject.SetActive(true);
            _yearPicked.text = Loc.Format("onboard_age_picked", year);
        }

        void ValidateAge()
        {
            if (_pickedYear <= 0) return;
            PrivacyRules.SetBirthYear(App.Profile, _pickedYear, GameApp.CurrentYear);
            App.SaveProfileNow();
            // Moins de 15 ans : pas de question sur les pubs (elles restent non personnalisées).
            if (PrivacyRules.IsMinor(App.Profile, GameApp.CurrentYear)) _steps.Remove(Step.Ads);
            else if (!_steps.Contains(Step.Ads)) _steps.Add(Step.Ads);
            Next();
        }

        // ------------------------------------------------------------------ 3. Pubs

        void BuildAds(Transform card)
        {
            _adsRt = Ui.Stretch(Ui.Rect("Pubs", card));
            var glow = Ui.Image(_adsRt, Art.SoftDisc, new Color(0.75f, 0.5f, 1f, 0.55f), "Halo");
            Ui.Place(glow.rectTransform, 0.5f, 1, 0, -460, 480, 400);
            var disc = Ui.Image(_adsRt, Art.Circle, Palette.Purple, "Pastille");
            Ui.Place(disc.rectTransform, 0.5f, 1, 0, -460, 230, 230);
            var film = Ui.Icon(disc.transform, Art.IconSprite(Icon.Film), "Film");
            Ui.Place(film.rectTransform, 0.5f, 0.5f, 0, 0, 132, 132);
            disc.gameObject.AddComponent<Pulse>().Amount = 0.03f;
            Ui.Isolate(disc.gameObject);
            // Petites icônes autour : pièce, cadeau, cœur (le jeu reste gratuit grâce aux pubs).
            Floating(_adsRt, Icon.Coin, Palette.Accent, -250, -400, 96, 8f);
            Floating(_adsRt, Icon.Gift, Palette.Play, 250, -410, 96, -7f);
            Floating(_adsRt, Icon.Heart, Palette.Primary, 200, -560, 72, 6f);

            Point(_adsRt, 0, Icon.Heart, Palette.Primary, Loc.Get("onboard_ads_p1"));
            Point(_adsRt, 1, Icon.Gift, Palette.Play, Loc.Get("onboard_ads_p2"));
            Point(_adsRt, 2, Icon.Gear, Palette.Secondary, Loc.Get("onboard_ads_p3"));

            // Accepter et refuser : même taille, même importance (CNIL : refuser doit être aussi simple qu'accepter).
            var yes = Ui.Button(_adsRt, Loc.Get("onboard_ads_yes"), Palette.Play, () => ChooseAds(true), Icon.Check, 52, "Accepter");
            Ui.Place((RectTransform)yes.transform, 0.5f, 0, 222, 196, 424, 156);
            var no = Ui.Button(_adsRt, Loc.Get("onboard_ads_no"), Palette.Secondary, () => ChooseAds(false), Icon.Close, 52, "Refuser");
            Ui.Place((RectTransform)no.transform, 0.5f, 0, -222, 196, 424, 156);
            var more = Ui.Button(_adsRt, Loc.Get("onboard_ads_more"), Palette.Muted, App.OpenPrivacy, null, 40, "EnSavoirPlus");
            Ui.Place((RectTransform)more.transform, 0.5f, 0, 0, 62, 460, 100);
        }

        void Floating(RectTransform parent, Icon icon, Color tint, float x, float y, float size, float angle)
        {
            var ic = Ui.Icon(parent, Art.IconSprite(icon), "Flottant");
            ic.color = tint;
            Ui.Place(ic.rectTransform, 0.5f, 1, x, y, size, size);
            ic.gameObject.AddComponent<Rock>().Angle = angle;
            Ui.Isolate(ic.gameObject);
        }

        void Point(RectTransform parent, int i, Icon icon, Color tint, string text)
        {
            var rowImg = Ui.Glass(parent, "Point");
            Ui.Place(rowImg.rectTransform, 0.5f, 1, 0, -640 - i * 156, 900, 140, 0.5f, 1);
            var dot = Ui.Image(rowImg.transform, Art.Circle, tint, "Pastille");
            Ui.Place(dot.rectTransform, 0, 0.5f, 66, 0, 88, 88);
            var ic = Ui.Icon(dot.transform, Art.IconSprite(icon), "Icone");
            Ui.Stretch(ic.rectTransform, 16, 16, 16, 16);
            var label = Ui.Label(rowImg.transform, text, 36, Color.white, TextAnchor.MiddleLeft);
            Ui.Stretch(label.rectTransform, 130, 8, 30, 8);
        }

        void ChooseAds(bool yes)
        {
            App.SetAdChoice(yes ? AdChoice.Personalized : AdChoice.Standard, () => Next());
        }

        // ------------------------------------------------------------------ Enchaînement

        /// <summary>Ouvre les étapes qui manquent à ce joueur ; <paramref name="done"/> à la fin (ou tout de suite s'il n'y a rien).</summary>
        public void Open(Action done)
        {
            _done = done;
            _steps.Clear();
            var p = App.Profile;
            if (string.IsNullOrEmpty(p.pseudo) && !p.pseudoAsked) _steps.Add(Step.Pseudo);
            if (!PrivacyRules.AgeKnown(p)) _steps.Add(Step.Age);
            if (PrivacyRules.NeedsAsking(p, GameApp.CurrentYear) && (!PrivacyRules.AgeKnown(p) || !PrivacyRules.IsMinor(p, GameApp.CurrentYear)))
                _steps.Add(Step.Ads);
            if (_steps.Count == 0) { done?.Invoke(); return; }
            _stepIndex = -1;
            Show();
            Next();
        }

        void Next()
        {
            App.SaveProfileNow();
            _stepIndex++;
            if (_stepIndex >= _steps.Count)
            {
                Hide();
                var d = _done; _done = null;
                d?.Invoke();
                return;
            }
            var step = _steps[_stepIndex];
            _pseudoRt.gameObject.SetActive(step == Step.Pseudo);
            _ageRt.gameObject.SetActive(step == Step.Age);
            _adsRt.gameObject.SetActive(step == Step.Ads);
            switch (step)
            {
                case Step.Pseudo:
                    _title.text = Loc.Get("onboard_pseudo_title");
                    _sub.text = Loc.Get("onboard_pseudo_sub");
                    _input.text = "";
                    _pseudoError.text = "";
                    ShowTag("");
                    break;
                case Step.Age:
                    _title.text = Loc.Get("onboard_age_title");
                    _sub.text = Loc.Get("onboard_age_sub");
                    FillYears();
                    _pickedYear = 0;
                    foreach (var (_, bg, label) in _years) { bg.color = Color.white; label.color = Palette.Ink; }
                    _yearPicked.text = "";
                    _pickedPill.gameObject.SetActive(false);
                    _ageOk.interactable = false;
                    break;
                case Step.Ads:
                    _title.text = Loc.Get("onboard_ads_title");
                    _sub.text = Loc.Get("onboard_ads_sub");
                    break;
            }
            // Points d'étape.
            Ui.Clear(_dots);
            for (int i = 0; i < _steps.Count; i++)
            {
                var d = Ui.Image(_dots, Art.Circle, i == _stepIndex ? Palette.Accent : new Color(1f, 1f, 1f, 0.25f), "Point");
                d.rectTransform.sizeDelta = new Vector2(i == _stepIndex ? 34 : 24, i == _stepIndex ? 34 : 24);
            }
            _dots.gameObject.SetActive(_steps.Count > 1);
        }
    }
}
