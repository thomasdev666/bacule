using System;
using System.Collections.Generic;
using Bascule.Core;
using Bascule.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>
    /// Premier lancement, juste après l'écran de bienvenue : trois petites étapes, une par fenêtre.
    /// 1. Le pseudo (visible dans les classements et les duels), avec une idée au hasard ; « Plus tard » possible.
    /// 2. L'année de naissance, demandée de façon neutre : une roue d'années qu'on fait défiler vers le haut ou le bas
    ///    (comme le réglage de l'heure d'un téléphone), « Fais défiler » au centre au départ (rien de présélectionné).
    /// 3. Les pubs (15 ans ou plus seulement) : « Accepter » et « Refuser » ont la même taille et la même place ;
    ///    le jeu est identique dans les deux cas. Sur iPhone, l'accord est suivi de la demande d'iOS.
    /// Un joueur déjà installé ne voit que les étapes qui lui manquent.
    /// </summary>
    public sealed class OnboardScreen : UiScreen
    {
        enum Step { Pseudo, Age, Ads }

        RectTransform _pseudoRt, _ageRt, _adsRt, _dots;
        Text _title, _sub, _pseudoError;
        InputField _input;
        Button _ageOk;
        WheelPicker _wheel;
        List<int> _yearValues;
        float _lastTick;
        readonly List<Step> _steps = new List<Step>();
        int _stepIndex;
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
            Ui.Place((RectTransform)ok.transform, 0.5f, 0, 0, 224, 780, 166);
            var later = Ui.Button(_pseudoRt, Loc.Get("onboard_later"), Palette.Muted, () => { App.Profile.pseudoAsked = true; Next(); }, null, 44, "PlusTard");
            Ui.Place((RectTransform)later.transform, 0.5f, 0, 0, 62, 460, 112);
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

        void BuildAge(Transform card)
        {
            _ageRt = Ui.Stretch(Ui.Rect("Age", card));
            // Roue des années sur un panneau de verre : une seule colonne qu'on fait défiler vers le haut ou le bas,
            // l'année au centre (dans la bande bleue) est celle choisie. Au départ : « Fais défiler » (question neutre).
            var area = Ui.Glass(_ageRt, "Annees");
            Ui.Place(area.rectTransform, 0.5f, 1, 0, -350, 640, 760, 0.5f, 1);
            area.raycastTarget = true;
            area.gameObject.AddComponent<RectMask2D>();
            var band = Ui.Image(area.transform, Art.Pill, Palette.Secondary, "Bande", true);
            Ui.Place(band.rectTransform, 0.5f, 0.5f, 0, 0, 590, 118);
            _wheel = WheelPicker.Create(area.rectTransform, 64, 110f);
            _wheel.Far = Sky;
            _wheel.Ticked = i =>
            {
                // Petit clic à chaque année qui passe (pas plus de 16 par seconde quand la roue file).
                if (Time.unscaledTime - _lastTick > 0.06f) { _lastTick = Time.unscaledTime; App.PlaySfx(Sfx.Click, 0, 0.35f); }
                _ageOk.interactable = i > 0;
            };
            _wheel.Settled = i => { _ageOk.interactable = i > 0; App.HapticLight(); };

            // Flèches au-dessus et au-dessous : une année de plus ou de moins (et elles montrent qu'on fait défiler).
            Arrow(_ageRt, -350, -1);
            Arrow(_ageRt, -1110, 1);

            var note = Ui.Label(_ageRt, Loc.Get("onboard_age_note"), 34, Sky);
            Ui.Place(note.rectTransform, 0.5f, 0, 0, 300, 860, 76);
            _ageOk = Ui.Button(_ageRt, Loc.Get("onboard_next"), Palette.Play, ValidateAge, Icon.Check, 62, "Valider");
            Ui.Place((RectTransform)_ageOk.transform, 0.5f, 0, 0, 120, 780, 166);
        }

        void Arrow(RectTransform parent, float y, int step)
        {
            var b = Ui.IconButton(parent, Icon.Back, Palette.Purple, () => _wheel.Step(step), step < 0 ? "Monter" : "Descendre");
            Ui.Place((RectTransform)b.transform, 0.5f, 1, 0, y, 104, 96);
            var icon = b.transform.Find("Contenu/Icone");
            // Flèche « retour » (vers la gauche) tournée vers le haut ou vers le bas.
            if (icon != null) icon.localRotation = Quaternion.Euler(0, 0, step < 0 ? -90f : 90f);
        }

        /// <summary>« Fais défiler » puis les années, de la plus récente à la plus ancienne.</summary>
        void FillYears()
        {
            _yearValues = PrivacyRules.Years(GameApp.CurrentYear);
            var items = new string[_yearValues.Count + 1];
            items[0] = Loc.Get("onboard_age_scroll");
            for (int i = 0; i < _yearValues.Count; i++) items[i + 1] = _yearValues[i].ToString();
            _wheel.SetItems(items, 0);
        }

        int PickedYear => _wheel != null && _yearValues != null && _wheel.Index > 0 ? _yearValues[_wheel.Index - 1] : 0;

        void ValidateAge()
        {
            int year = PickedYear;
            if (year <= 0) return;
            PrivacyRules.SetBirthYear(App.Profile, year, GameApp.CurrentYear);
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
            Ui.Place((RectTransform)yes.transform, 0.5f, 0, 222, 210, 424, 156);
            var no = Ui.Button(_adsRt, Loc.Get("onboard_ads_no"), Palette.Secondary, () => ChooseAds(false), Icon.Close, 52, "Refuser");
            Ui.Place((RectTransform)no.transform, 0.5f, 0, -222, 210, 424, 156);
            var more = Ui.Button(_adsRt, Loc.Get("onboard_ads_more"), Palette.Muted, App.OpenPrivacy, null, 40, "EnSavoirPlus");
            Ui.Place((RectTransform)more.transform, 0.5f, 0, 0, 58, 460, 100);
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
        /// <summary>
        /// Ouvre les étapes demandées qui manquent : le pseudo au premier lancement, l'âge et les pubs à la fin de la
        /// première partie (rapport 1.3 : le joueur a joué avant de répondre).
        /// </summary>
        public void Open(Action done, bool pseudo = true, bool privacy = true)
        {
            _done = done;
            _steps.Clear();
            var p = App.Profile;
            if (pseudo && string.IsNullOrEmpty(p.pseudo) && !p.pseudoAsked) _steps.Add(Step.Pseudo);
            if (privacy && !PrivacyRules.AgeKnown(p)) _steps.Add(Step.Age);
            if (privacy && PrivacyRules.NeedsAsking(p, GameApp.CurrentYear) && (!PrivacyRules.AgeKnown(p) || !PrivacyRules.IsMinor(p, GameApp.CurrentYear)))
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
