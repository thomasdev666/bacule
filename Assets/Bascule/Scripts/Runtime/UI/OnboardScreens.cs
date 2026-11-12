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

        protected override void Build()
        {
            var card = Ui.Card(Safe, "Fenetre");
            Ui.Place(card.rectTransform, 0.5f, 0.5f, 0, 20, 960, 1520);
            card.gameObject.AddComponent<PopIn>();

            // Points d'étape (1, 2, 3) en haut de la fenêtre.
            _dots = Ui.Rect("Etapes", card.transform);
            Ui.Place(_dots, 0.5f, 1, 0, -64, 300, 30);
            var row = _dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 22; row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = false; row.childForceExpandWidth = row.childForceExpandHeight = false;

            _title = Ui.Title(card.transform, "", 80, Palette.Accent);
            Ui.Place(_title.rectTransform, 0.5f, 1, 0, -150, 880, 120);
            _sub = Ui.Label(card.transform, "", 42, Palette.Ink);
            Ui.Place(_sub.rectTransform, 0.5f, 1, 0, -262, 860, 110);

            BuildPseudo(card.transform);
            BuildAge(card.transform);
            BuildAds(card.transform);
        }

        // ------------------------------------------------------------------ 1. Pseudo

        void BuildPseudo(Transform card)
        {
            _pseudoRt = Ui.Stretch(Ui.Rect("Pseudo", card));
            // Basile sur un disque à rayons.
            var halo = Ui.Image(_pseudoRt, Art.Rays, new Color(1f, 0.82f, 0.35f, 0.45f), "Rayons");
            Ui.Place(halo.rectTransform, 0.5f, 1, 0, -560, 560, 560);
            halo.gameObject.AddComponent<Spin>().Speed = 10f;
            var disc = Ui.Image(_pseudoRt, Art.Circle, Palette.Purple, "Disque");
            Ui.Place(disc.rectTransform, 0.5f, 1, 0, -560, 330, 330);
            var basile = Ui.Image(disc.transform, Art.Portrait(HeadCatalog.Get(App.Profile.selectedHead)), Color.white, "Basile");
            Ui.Place(basile.rectTransform, 0.5f, 0.5f, 0, 6, 290, 290);
            basile.preserveAspect = true;
            basile.gameObject.AddComponent<Rock>().Angle = 4f;

            // Champ de saisie, bien visible, avec le bouton « idée au hasard » à droite.
            var rim = Ui.Image(_pseudoRt, Art.Pill, Palette.Outline, "Bord", true);
            Ui.Place(rim.rectTransform, 0.5f, 1, -70, -850, 700, 150);
            var field = Ui.Image(rim.transform, Art.Pill, Color.white, "Champ", true);
            Ui.Stretch(field.rectTransform, 7, 7, 7, 7);
            field.raycastTarget = true;
            var text = Ui.Label(field.transform, "", 60, Palette.Ink, TextAnchor.MiddleCenter);
            Ui.Stretch(text.rectTransform, 40, 8, 40, 8);
            var placeholder = Ui.Label(field.transform, Loc.Get("onboard_pseudo_placeholder"), 52, Palette.Muted, TextAnchor.MiddleCenter);
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
            _input.onValueChanged.AddListener(_ => _pseudoError.text = "");

            var dice = Ui.IconButton(_pseudoRt, Icon.Replay, Palette.Secondary, () => { _input.text = Idea(); }, "Idee");
            Ui.Place((RectTransform)dice.transform, 0.5f, 1, 380, -850, 150, 150);

            _pseudoError = Ui.Label(_pseudoRt, "", 38, Palette.Primary);
            Ui.Place(_pseudoError.rectTransform, 0.5f, 1, 0, -975, 860, 70);
            var rules = Ui.Label(_pseudoRt, Loc.Get("onboard_pseudo_rules"), 36, Palette.Muted);
            Ui.Place(rules.rectTransform, 0.5f, 1, 0, -1060, 860, 100);

            var ok = Ui.Button(_pseudoRt, Loc.Get("onboard_next"), Palette.Play, ValidatePseudo, Icon.Check, 62, "Valider");
            Ui.Place((RectTransform)ok.transform, 0.5f, 0, 0, 200, 760, 160);
            ok.gameObject.AddComponent<Pulse>().Amount = 0.02f;
            var later = Ui.Button(_pseudoRt, Loc.Get("onboard_later"), Palette.Muted, () => { App.Profile.pseudoAsked = true; Next(); }, null, 44, "PlusTard");
            Ui.Place((RectTransform)later.transform, 0.5f, 0, 0, 62, 460, 110);
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

        void BuildAge(Transform card)
        {
            _ageRt = Ui.Stretch(Ui.Rect("Age", card));
            var area = Ui.Rect("Annees", _ageRt);
            Ui.Place(area, 0.5f, 1, 0, -360, 880, 760, 0.5f, 1);
            var bg = Ui.Image(area, Art.Panel, new Color(0.93f, 0.9f, 0.98f), "Fond", true);
            Ui.Stretch(bg.rectTransform);
            var inner = Ui.Rect("Zone", area);
            Ui.Stretch(inner, 18, 18, 18, 18);
            _yearGrid = Ui.Scroll(inner, out _);
            var grid = _yearGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(196, 104);
            grid.spacing = new Vector2(14, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(4, 4, 6, 6);

            _yearPicked = Ui.Title(_ageRt, "", 48, Color.white);
            Ui.Place(_yearPicked.rectTransform, 0.5f, 0, 0, 380, 860, 70);
            var note = Ui.Label(_ageRt, Loc.Get("onboard_age_note"), 34, Palette.Muted);
            Ui.Place(note.rectTransform, 0.5f, 0, 0, 300, 860, 80);
            _ageOk = Ui.Button(_ageRt, Loc.Get("onboard_next"), Palette.Play, ValidateAge, Icon.Check, 62, "Valider");
            Ui.Place((RectTransform)_ageOk.transform, 0.5f, 0, 0, 120, 760, 160);
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
            var halo = Ui.Image(_adsRt, Art.SoftDisc, new Color(0.65f, 0.42f, 1f, 0.35f), "Halo");
            Ui.Place(halo.rectTransform, 0.5f, 1, 0, -500, 520, 380);
            var tv = Ui.Image(_adsRt, Art.Circle, Palette.Purple, "Pastille");
            Ui.Place(tv.rectTransform, 0.5f, 1, 0, -500, 220, 220);
            var film = Ui.Icon(tv.transform, Art.IconSprite(Icon.Film), "Film");
            Ui.Place(film.rectTransform, 0.5f, 0.5f, 0, 0, 130, 130);
            tv.gameObject.AddComponent<Pulse>().Amount = 0.03f;

            Point(_adsRt, 0, Icon.Heart, Palette.Primary, Loc.Get("onboard_ads_p1"));
            Point(_adsRt, 1, Icon.Gift, Palette.Play, Loc.Get("onboard_ads_p2"));
            Point(_adsRt, 2, Icon.Gear, Palette.Secondary, Loc.Get("onboard_ads_p3"));

            // Accepter et refuser : même taille, même importance (CNIL : refuser doit être aussi simple qu'accepter).
            var yes = Ui.Button(_adsRt, Loc.Get("onboard_ads_yes"), Palette.Play, () => ChooseAds(true), null, 52, "Accepter");
            Ui.Place((RectTransform)yes.transform, 0.5f, 0, 215, 190, 410, 150);
            var no = Ui.Button(_adsRt, Loc.Get("onboard_ads_no"), Palette.Secondary, () => ChooseAds(false), null, 52, "Refuser");
            Ui.Place((RectTransform)no.transform, 0.5f, 0, -215, 190, 410, 150);
            var more = Ui.Button(_adsRt, Loc.Get("onboard_ads_more"), Palette.Muted, App.OpenPrivacy, null, 40, "EnSavoirPlus");
            Ui.Place((RectTransform)more.transform, 0.5f, 0, 0, 62, 460, 100);
        }

        void Point(RectTransform parent, int i, Icon icon, Color tint, string text)
        {
            var rowImg = Ui.Image(parent, Art.Pill, new Color(1f, 1f, 1f, 0.78f), "Point", true);
            Ui.Place(rowImg.rectTransform, 0.5f, 1, 0, -690 - i * 150, 860, 132, 0.5f, 1);
            var dot = Ui.Image(rowImg.transform, Art.Circle, tint, "Pastille");
            Ui.Place(dot.rectTransform, 0, 0.5f, 62, 0, 84, 84);
            var ic = Ui.Icon(dot.transform, Art.IconSprite(icon), "Icone");
            Ui.Stretch(ic.rectTransform, 15, 15, 15, 15);
            var label = Ui.Label(rowImg.transform, text, 36, Palette.Ink, TextAnchor.MiddleLeft);
            Ui.Stretch(label.rectTransform, 124, 6, 26, 6);
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
                    break;
                case Step.Age:
                    _title.text = Loc.Get("onboard_age_title");
                    _sub.text = Loc.Get("onboard_age_sub");
                    FillYears();
                    _pickedYear = 0;
                    foreach (var (_, bg, label) in _years) { bg.color = Color.white; label.color = Palette.Ink; }
                    _yearPicked.text = "";
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
                var d = Ui.Image(_dots, Art.Circle, i == _stepIndex ? Palette.Accent : new Color(0.17f, 0.18f, 0.26f, 0.2f), "Point");
                d.rectTransform.sizeDelta = new Vector2(i == _stepIndex ? 34 : 24, i == _stepIndex ? 34 : 24);
            }
            _dots.gameObject.SetActive(_steps.Count > 1);
        }
    }
}
