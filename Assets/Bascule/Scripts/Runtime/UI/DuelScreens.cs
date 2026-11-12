using System.Collections.Generic;
using Bascule.Core;
using Bascule.Duel;
using Bascule.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    static class DuelUi
    {
        /// <summary>Durée lisible : « 1:07 ».</summary>
        public static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        /// <summary>
        /// Rectangle arrondi découpé en 9 : <paramref name="radius"/> = rayon des coins en unités d'interface.
        /// Le sprite « Rounded » a des coins de 25 unités à l'échelle 1 : on règle l'échelle pour obtenir le rayon voulu.
        /// Deux cadres concentriques (rayon R + marge, puis R) épousent exactement la même courbe.
        /// </summary>
        public static Image Rounded(Transform parent, Color color, float radius, string name)
        {
            var img = Ui.Image(parent, Ui.Art.Rounded, color, name, true);
            img.pixelsPerUnitMultiplier = 25f / Mathf.Max(4f, radius);
            return img;
        }

        /// <summary>Tête gagnée par les victoires en duel (la « Challenger »).</summary>
        public static HeadDef ChallengerHead()
        {
            foreach (var h in HeadCatalog.All) if (h.Unlock == UnlockKind.DuelWins) return h;
            return null;
        }

        /// <summary>Amis à afficher (pseudo et tête d'après le classement mondial), joueurs bloqués exclus.</summary>
        public static async System.Threading.Tasks.Task<System.Collections.Generic.List<(string id, string name, int head)>> LoadFriends(GameApp app)
        {
            var list = new System.Collections.Generic.List<(string, string, int)>();
            var friends = app.Profile.friends;
            var page = await app.Leaderboard.Service.GetFriendsAsync(Boards.World, friends);
            // Les plus récents d'abord : c'est souvent celui qu'on vient d'ajouter qu'on veut défier.
            for (int i = friends.Count - 1; i >= 0; i--)
            {
                var id = friends[i];
                if (Moderation.IsBlocked(app.Profile, id)) continue;
                var e = page?.Entries?.Find(x => x.PlayerId == id);
                string name = e != null ? PseudoFilter.Display(e.Name, Loc.Get("lb_player")) : Loc.Get("lb_player") + " " + FriendCode.Short(id);
                int head = e != null ? e.Head : 0;
                app.FriendHeads[id] = head;
                list.Add((id, name, head));
            }
            return list;
        }

        /// <summary>Ligne d'ami : portrait cerclé, pseudo, et un bouton d'action à droite.</summary>
        public static void FriendRow(GameApp app, RectTransform col, string id, string name, int head, string action, Color color, System.Action<Button> onTap)
        {
            var row = Ui.Image(col, Ui.Art.Pill, new Color(1f, 1f, 1f, 0.9f), "Ami", true);
            Ui.Size(row, 132);
            var face = Portrait(row.transform, Palette.Secondary, 108, "Tete");
            face.sprite = Ui.Art.Portrait(HeadCatalog.Get(head));
            Ui.Place((RectTransform)face.transform.parent, 0, 0.5f, 74, 0, 108, 108);
            var label = Ui.Plain(Ui.Label(row.transform, name, 44, Palette.Ink, TextAnchor.MiddleLeft));
            Ui.Stretch(label.rectTransform, 150, 8, 320, 8);
            Button btn = null;
            btn = Ui.Button(row.transform, action, color, () => onTap?.Invoke(btn), Icon.Swords, 40, "Action");
            Ui.Place((RectTransform)btn.transform, 1, 0.5f, -16, 0, 290, 108, 1, 0.5f);
        }

        /// <summary>Portrait rond cerclé de couleur.</summary>
        public static Image Portrait(Transform parent, Color ring, float size, string name = "Portrait")
        {
            var disc = Ui.Image(parent, Ui.Art.Circle, ring, name);
            disc.rectTransform.sizeDelta = new Vector2(size, size);
            var inner = Ui.Image(disc.transform, Ui.Art.Circle, Palette.Paper, "Fond");
            Ui.Place(inner.rectTransform, 0.5f, 0.5f, 0, 0, size - 14, size - 14);
            var face = Ui.Icon(disc.transform, null, "Tete");
            Ui.Place(face.rectTransform, 0.5f, 0.5f, 0, 2, size - 20, size - 20);
            return face;
        }
    }

    // ===================================================================== Salle d'attente

    /// <summary>Liste des amis à inviter au duel en cours (l'ami voit l'invitation sur son écran, en grand).</summary>
    public sealed class FriendPickerScreen : UiScreen
    {
        RectTransform _col;
        Text _status;
        int _request;

        protected override void Build()
        {
            var card = Ui.Card(Safe, "Fenetre");
            Ui.Place(card.rectTransform, 0.5f, 0.5f, 0, 20, 960, 1300);
            card.gameObject.AddComponent<PopIn>();
            var title = Ui.Title(card.transform, Loc.Get("duel_friends_title"), 72, Palette.Accent);
            Ui.Place(title.rectTransform, 0.5f, 1, 0, -90, 880, 110);
            _status = Ui.Label(card.transform, "", 40, Palette.Muted);
            Ui.Place(_status.rectTransform, 0.5f, 1, 0, -400, 840, 200);
            var area = Ui.Rect("Liste", card.transform);
            Ui.Place(area, 0.5f, 1, 0, -170, 880, 900, 0.5f, 1);
            _col = Ui.Scroll(area, out _);
            Ui.Column(_col, 14);
            var close = Ui.Button(card.transform, Loc.Get("back"), Palette.Muted, Hide, null, 46, "Fermer");
            Ui.Place((RectTransform)close.transform, 0.5f, 0, 0, 100, 520, 120);
        }

        public override async void Refresh()
        {
            Ui.Clear(_col);
            var friends = App.Profile.friends;
            if (friends.Count == 0) { _status.text = Loc.Get("duel_friends_none"); return; }
            if (!App.Leaderboard.Service.IsOnline) { _status.text = Loc.Get("lb_offline"); return; }
            _status.text = Loc.Get("lb_loading");
            int request = ++_request;
            var list = await DuelUi.LoadFriends(App);
            if (request != _request || !Visible) return;
            _status.text = "";
            foreach (var f in list)
                DuelUi.FriendRow(App, _col, f.id, f.name, f.head, Loc.Get("duel_friend_invite"), Palette.Play, async btn =>
                {
                    btn.interactable = false;
                    bool ok = await App.InviteFriendToDuel(f.id, f.name);
                    if (btn == null) return;
                    Ui.ButtonLabel(btn).text = Loc.Get(ok ? "duel_friend_sent" : "duel_friend_invite");
                    btn.interactable = !ok;
                    if (ok) Hide();
                });
        }
    }

    /// <summary>
    /// Entrée du duel, pensée pour donner envie : une bannière « VS » (toi à gauche en bleu, ton ami à droite en
    /// rouge, rayons qui tournent), les trois règles en pastilles, tes amis à défier en un geste, puis le code pour
    /// un ami pas encore ajouté. Ensuite, l'attente : le code en grandes tuiles, l'invitation, et l'ami qui arrive.
    /// </summary>
    public sealed class DuelLobbyScreen : UiScreen
    {
        RectTransform _choice, _hosting, _busy, _friendsCol, _codeTiles;
        Text _record, _wins, _busyTitle, _waitText, _theirName, _friendsStatus;
        Image _challenger, _theirFace, _theirRing, _myFace;
        RectTransform _winsFill, _theirSlot;
        GameObject _question, _invitedTag;
        Text _invitedText;
        int _request;

        protected override void Build()
        {
            BackButton(App.DuelCancel);
            Header(Loc.Get("duel_title"));

            // ---- Bannière VS (commune aux trois états)
            var banner = Ui.Image(Safe, Art.Panel, ArtCache.Hex("#2B1B54"), "Banniere", true);
            Ui.Place(banner.rectTransform, 0.5f, 1, 0, -250, 1000, 450, 0.5f, 1);
            // Bleu, rouge et rayons découpés aux coins arrondis du panneau (un RectMask2D les coupait en rectangle :
            // les coins de la carte étaient carrés).
            var clip = DuelUi.Rounded(banner.transform, Color.white, 52, "Decoupe");
            Ui.Stretch(clip.rectTransform, 10, 10, 10, 10);
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var blue = Ui.Image(clip.transform, null, new Color(0.29f, 0.61f, 1f, 0.55f), "Bleu");
            Ui.Place(blue.rectTransform, 0, 0.5f, -120, 0, 720, 900, 0, 0.5f);
            blue.rectTransform.localRotation = Quaternion.Euler(0, 0, -14f);
            var red = Ui.Image(clip.transform, null, new Color(1f, 0.35f, 0.37f, 0.55f), "Rouge");
            Ui.Place(red.rectTransform, 1, 0.5f, 120, 0, 720, 900, 1, 0.5f);
            red.rectTransform.localRotation = Quaternion.Euler(0, 0, -14f);
            var rays = Ui.Image(clip.transform, Art.Rays, new Color(1f, 0.85f, 0.4f, 0.35f), "Rayons");
            Ui.Place(rays.rectTransform, 0.5f, 0.5f, 0, 10, 900, 900);
            rays.gameObject.AddComponent<Spin>().Speed = 14f;
            Ui.Isolate(rays.gameObject);

            // Toi, à gauche.
            _myFace = DuelUi.Portrait(banner.transform, Palette.Secondary, 220, "Moi");
            Ui.Place((RectTransform)_myFace.transform.parent, 0.5f, 0.5f, -300, 56, 220, 220);
            var you = Ui.Title(banner.transform, Loc.Get("duel_you"), 46, ArtCache.Hex("#BFE0FF"));
            Ui.Place(you.rectTransform, 0.5f, 0.5f, -300, -92, 320, 60);
            // L'ami, à droite : point d'interrogation tant qu'il n'est pas là.
            _theirSlot = Ui.Rect("Ami", banner.transform);
            Ui.Place(_theirSlot, 0.5f, 0.5f, 300, 56, 220, 220);
            _theirRing = Ui.Image(_theirSlot, Art.Ring, Palette.Accent, "Attente");
            Ui.Place(_theirRing.rectTransform, 0.5f, 0.5f, 0, 0, 262, 262);
            _theirRing.type = Image.Type.Filled; _theirRing.fillMethod = Image.FillMethod.Radial360; _theirRing.fillAmount = 0.7f;
            _theirFace = DuelUi.Portrait(_theirSlot, Palette.Primary, 220, "Visage");
            Ui.Place((RectTransform)_theirFace.transform.parent, 0.5f, 0.5f, 0, 0, 220, 220);
            var q = Ui.Title(_theirFace.transform.parent, "?", 150, Color.white, "Question");
            Ui.Stretch(q.rectTransform, 0, 0, 0, 10);
            _question = q.gameObject;
            _theirName = Ui.Title(banner.transform, Loc.Get("duel_friend"), 46, ArtCache.Hex("#FFC2C4"));
            Ui.Place(_theirName.rectTransform, 0.5f, 0.5f, 300, -92, 340, 60);
            // VS au centre.
            var vsGlow = Ui.Image(banner.transform, Art.SoftDisc, new Color(1f, 0.8f, 0.3f, 0.6f), "Halo");
            Ui.Place(vsGlow.rectTransform, 0.5f, 0.5f, 0, 50, 300, 300);
            var vs = Ui.Title(banner.transform, "VS", 130, Palette.Accent, "VS");
            Ui.Place(vs.rectTransform, 0.5f, 0.5f, 0, 50, 260, 170);
            vs.gameObject.AddComponent<Pulse>().Amount = 0.06f;
            Ui.Isolate(vs.gameObject);
            var sub = Ui.Title(banner.transform, Loc.Get("duel_hero_sub"), 38, Color.white);
            Ui.Place(sub.rectTransform, 0.5f, 0, 0, 22, 940, 54, 0.5f, 0);

            BuildChoice();
            BuildHosting();
            BuildBusy();
        }

        // ------------------------------------------------------------------ Choix
        void BuildChoice()
        {
            _choice = Ui.Stretch(Ui.Rect("Choix", Safe));
            // De haut en bas (1.4) : la carte VS, la progression vers la tête Challenger juste dessous, les trois
            // règles, les amis à défier, puis le code tout en bas. La progression était collée aux boutons du code.

            // Victoires et tête Challenger, en bandeau compact sous la carte VS.
            var card = Ui.Glass(_choice, "Challenger");
            Ui.Place(card.rectTransform, 0.5f, 1, 0, -722, 1000, 140, 0.5f, 1);
            _challenger = DuelUi.Portrait(card.transform, Palette.Primary, 116);
            Ui.Place((RectTransform)_challenger.transform.parent, 0, 0.5f, 84, 0, 116, 116);
            _wins = Ui.Title(card.transform, "", 42, Color.white, "Victoires");
            Ui.Place(_wins.rectTransform, 0, 0.5f, 170, 36, 790, 54, 0, 0.5f);
            _wins.alignment = TextAnchor.MiddleLeft;
            var bar = Ui.Rect("Jauge", card.transform);
            Ui.Place(bar, 0, 0.5f, 170, -6, 780, 30, 0, 0.5f);
            _winsFill = MetaUi.Bar(bar, Art, Palette.Primary);
            Ui.Stretch((RectTransform)_winsFill.parent);
            _record = Ui.Title(card.transform, "", 32, ArtCache.Hex("#BFE0FF"), "Bilan");
            Ui.Place(_record.rectTransform, 0, 0.5f, 170, -44, 790, 44, 0, 0.5f);
            _record.alignment = TextAnchor.MiddleLeft;

            // Les trois règles.
            Rule(_choice, -1, Icon.Swords, Palette.Accent, Loc.Get("duel_rule_1"));
            Rule(_choice, 0, Icon.Film, Palette.Secondary, Loc.Get("duel_rule_2"));
            Rule(_choice, 1, Icon.Flag, Palette.Primary, Loc.Get("duel_rule_3"));

            // Amis à défier en un geste.
            var ft = Ui.Title(_choice, Loc.Get("duel_friends_section"), 52, Palette.Accent);
            Ui.Place(ft.rectTransform, 0, 1, 50, -1094, 700, 70, 0, 0.5f);
            ft.alignment = TextAnchor.MiddleLeft;
            var add = Ui.Button(_choice, "", Palette.Play, App.AskFriendCode, Icon.Plus, 44, "AjouterAmi");
            Ui.Place((RectTransform)add.transform, 1, 1, -40, -1094, 110, 110, 1, 0.5f);
            var area = Ui.Rect("Amis", _choice);
            Ui.Stretch(area, 40, 1158, 40, 400);
            _friendsCol = Ui.Scroll(area, out _);
            Ui.Column(_friendsCol, 14);
            _friendsStatus = Ui.Label(_choice, "", 40, ArtCache.Hex("#D8E9FF"));
            Ui.Stretch(_friendsStatus.rectTransform, 70, 1170, 70, 440);

            // Avec un code, tout en bas, avec de l'air autour.
            var or = Ui.Title(_choice, Loc.Get("duel_or_code"), 42, ArtCache.Hex("#BFE0FF"));
            Ui.Place(or.rectTransform, 0.5f, 0, 0, 342, 900, 60);
            var host = Ui.Button(_choice, Loc.Get("duel_new_code"), Palette.Purple, App.DuelHost, Icon.Plus, 50, "CreerCode");
            Ui.Place((RectTransform)host.transform, 0.5f, 0, -245, 200, 470, 150);
            var join = Ui.Button(_choice, Loc.Get("duel_enter_code"), Palette.Secondary, App.DuelJoinPrompt, Icon.Play, 50, "JaiUnCode");
            Ui.Place((RectTransform)join.transform, 0.5f, 0, 245, 200, 470, 150);
        }

        void Rule(RectTransform parent, int i, Icon icon, Color tint, string text)
        {
            var chip = Ui.Glass(parent, "Regle");
            Ui.Place(chip.rectTransform, 0.5f, 1, i * 336, -957, 322, 150);
            var dot = Ui.Image(chip.transform, Art.Circle, tint, "Pastille");
            Ui.Place(dot.rectTransform, 0.5f, 1, 0, -38, 64, 64);
            var ic = Ui.Icon(dot.transform, Art.IconSprite(icon), "Icone");
            Ui.Stretch(ic.rectTransform, 12, 12, 12, 12);
            var t = Ui.Title(chip.transform, text, 32, Color.white);
            Ui.Place(t.rectTransform, 0.5f, 0, 0, 12, 300, 72, 0.5f, 0);
        }

        // ------------------------------------------------------------------ Attente (hôte)
        void BuildHosting()
        {
            _hosting = Ui.Stretch(Ui.Rect("Attente", Safe));
            var title = Ui.Title(_hosting, Loc.Get("duel_ready_title"), 64, Palette.Accent);
            Ui.Place(title.rectTransform, 0.5f, 1, 0, -775, 960, 90);
            var lbl = Ui.Title(_hosting, Loc.Get("duel_code_label"), 42, ArtCache.Hex("#BFE0FF"));
            Ui.Place(lbl.rectTransform, 0.5f, 1, 0, -855, 900, 60);
            // Le code en six tuiles blanches, bien lisibles à voix haute.
            _codeTiles = Ui.Rect("Code", _hosting);
            Ui.Place(_codeTiles, 0.5f, 1, 0, -980, 960, 170);
            _codeTiles.gameObject.AddComponent<PopIn>();

            var invite = Ui.Button(_hosting, Loc.Get("duel_invite"), Palette.Play, App.DuelShareInvite, Icon.Share, 58, "Inviter");
            Ui.Place((RectTransform)invite.transform, 0.5f, 1, 0, -1165, 900, 170);
            invite.gameObject.AddComponent<Pulse>().Amount = 0.025f;
            var copy = Ui.Button(_hosting, Loc.Get("duel_copy"), Palette.Secondary, App.DuelCopyCode, Icon.Check, 44, "Copier");
            Ui.Place((RectTransform)copy.transform, 0.5f, 1, -228, -1340, 430, 140);
            var friends = Ui.Button(_hosting, Loc.Get("duel_friends"), Palette.Purple, App.OpenFriendPicker, Icon.Face, 44, "Amis");
            Ui.Place((RectTransform)friends.transform, 0.5f, 1, 228, -1340, 430, 140);

            // Ami invité : son nom dans une pastille ; sinon, l'astuce.
            var tag = Ui.Image(_hosting, Art.Pill, Palette.Purple, "Invite", true);
            Ui.Place(tag.rectTransform, 0.5f, 1, 0, -1475, 820, 86);
            _invitedText = Ui.Title(tag.transform, "", 40, Color.white);
            Ui.Stretch(_invitedText.rectTransform, 24, 2, 24, 0);
            _invitedTag = tag.gameObject;
            _waitText = Ui.Label(_hosting, Loc.Get("duel_ready_hint"), 38, ArtCache.Hex("#D8E9FF"));
            Ui.Place(_waitText.rectTransform, 0.5f, 1, 0, -1490, 900, 110);

            var cancel = Ui.Button(_hosting, Loc.Get("duel_cancel"), Palette.Muted, App.DuelCancel, Icon.Close, 46, "Annuler");
            Ui.Place((RectTransform)cancel.transform, 0.5f, 0, 0, 90, 520, 130, 0.5f, 0);
        }

        // ------------------------------------------------------------------ Connexion
        void BuildBusy()
        {
            _busy = Ui.Stretch(Ui.Rect("Connexion", Safe));
            _busyTitle = Ui.Title(_busy, Loc.Get("duel_connecting"), 64, Color.white);
            Ui.Place(_busyTitle.rectTransform, 0.5f, 1, 0, -830, 960, 100);
            var sub = Ui.Label(_busy, Loc.Get("duel_connecting_sub"), 40, ArtCache.Hex("#D8E9FF"));
            Ui.Place(sub.rectTransform, 0.5f, 1, 0, -920, 900, 70);
            var cancel = Ui.Button(_busy, Loc.Get("duel_cancel"), Palette.Muted, App.DuelCancel, Icon.Close, 46, "Annuler");
            Ui.Place((RectTransform)cancel.transform, 0.5f, 0, 0, 90, 520, 130, 0.5f, 0);
        }

        // ------------------------------------------------------------------ Mise à jour
        public override void Refresh()
        {
            var d = App.Duel;
            var phase = d.Current;
            bool choice = phase == DuelController.Phase.None, hosting = phase == DuelController.Phase.Hosting;
            bool busy = phase == DuelController.Phase.Connecting || phase == DuelController.Phase.Greeting;
            _choice.gameObject.SetActive(choice);
            _hosting.gameObject.SetActive(hosting);
            _busy.gameObject.SetActive(busy);

            var p = App.Profile;
            _myFace.sprite = Art.Portrait(HeadCatalog.Get(p.selectedHead));
            // L'ami : connu quand il est arrivé (ou invité) ; sinon un point d'interrogation.
            bool arrived = phase == DuelController.Phase.Greeting && !string.IsNullOrEmpty(d.TheirName);
            bool invited = hosting && !string.IsNullOrEmpty(App.InvitedName);
            _question.SetActive(!arrived && !invited);
            _theirFace.sprite = arrived ? Art.Portrait(HeadCatalog.Get(d.TheirHead)) : invited ? Art.Portrait(HeadCatalog.Get(App.InvitedHead)) : null;
            _theirFace.color = _theirFace.sprite != null ? Color.white : new Color(1, 1, 1, 0);
            _theirName.text = arrived ? d.TheirName : invited ? App.InvitedName : Loc.Get("duel_friend");
            _theirRing.gameObject.SetActive(!choice && !arrived);
            _busyTitle.text = arrived ? Loc.Format("duel_found", d.TheirName) : Loc.Get("duel_connecting");

            if (hosting)
            {
                SetCode(d.Code);
                _invitedTag.SetActive(invited);
                _invitedText.text = invited ? Loc.Format("duel_invited", App.InvitedName) : "";
                _waitText.gameObject.SetActive(!invited);
            }

            if (choice)
            {
                var head = DuelUi.ChallengerHead();
                int need = head != null ? head.UnlockValue : App.Tuning.duel.challengerWins;
                bool done = head != null && p.unlockedHeads.Contains(head.Id);
                if (head != null) _challenger.sprite = done ? Art.Portrait(head) : Art.Locked(head);
                _wins.text = done ? Loc.Get("duel_challenger_done") : Loc.Format("duel_challenger", Mathf.Min(p.duelWins, need), need);
                _record.text = Loc.Format("duel_record", p.duelWins, p.duelsPlayed);
                Canvas.ForceUpdateCanvases();
                MetaUi.SetFill(_winsFill, need > 0 ? p.duelWins / (float)need : 1f);
                LoadFriends();
            }
        }

        async void LoadFriends()
        {
            Ui.Clear(_friendsCol);
            var friends = App.Profile.friends;
            if (friends.Count == 0) { _friendsStatus.text = Loc.Get("duel_no_friends"); return; }
            if (!App.Leaderboard.Service.IsOnline) { _friendsStatus.text = Loc.Get("friend_offline"); return; }
            _friendsStatus.text = Loc.Get("lb_loading");
            int request = ++_request;
            var list = await DuelUi.LoadFriends(App);
            if (request != _request || !Visible || App.Duel.Current != DuelController.Phase.None) return;
            _friendsStatus.text = "";
            Ui.Clear(_friendsCol);
            foreach (var f in list)
                DuelUi.FriendRow(App, _friendsCol, f.id, f.name, f.head, Loc.Get("duel_friend_challenge"), Palette.Play, btn =>
                {
                    btn.interactable = false;
                    App.ChallengeFriend(f.id, f.name);
                });
        }

        void SetCode(string code)
        {
            Ui.Clear(_codeTiles);
            code = string.IsNullOrEmpty(code) ? "······" : code;
            int n = code.Length;
            float tile = Mathf.Min(140f, (940f - (n - 1) * 14f) / n), x0 = -(n * tile + (n - 1) * 14f) * 0.5f + tile * 0.5f;
            for (int i = 0; i < n; i++)
            {
                var t = Ui.Image(_codeTiles, Art.Panel, Color.white, "Tuile", true);
                // Petit écart au milieu : « K7Q M2X » se lit en deux moitiés.
                float gap = n == 6 ? (i < 3 ? -12f : 12f) : 0f;
                Ui.Place(t.rectTransform, 0.5f, 0.5f, x0 + i * (tile + 14f) + gap, 0, tile, 160);
                var c = Ui.Plain(Ui.Label(t.transform, code[i].ToString(), 104, Palette.Ink));
                Ui.Stretch(c.rectTransform, 0, 6, 0, 18);
                c.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        public override void Tick(float dt)
        {
            if (_theirRing.gameObject.activeInHierarchy) _theirRing.rectTransform.Rotate(0, 0, -240f * dt);
            if (_hosting.gameObject.activeSelf && _waitText.gameObject.activeSelf)
            {
                int dots = 1 + (int)(Time.unscaledTime * 2.5f) % 3;
                _waitText.text = Loc.Get("duel_ready_hint") + "\n" + Loc.Get("duel_waiting_friend") + new string('.', dots);
            }
        }
    }

    /// <summary>
    /// Invitation d'un ami, en grand : son portrait face au tien, « Léa te défie ! », et deux boutons. Elle arrive
    /// sur n'importe quel menu (jamais pendant une partie), quelques secondes après l'envoi.
    /// </summary>
    public sealed class DuelInviteScreen : UiScreen
    {
        Text _title;
        Image _theirs, _mine;
        System.Action _accept;

        protected override void Build()
        {
            var card = Ui.Image(Safe, Art.Panel, ArtCache.Hex("#2B1B54"), "Fenetre", true);
            Ui.Place(card.rectTransform, 0.5f, 0.5f, 0, 40, 980, 1180);
            card.gameObject.AddComponent<PopIn>();
            card.gameObject.AddComponent<RectMask2D>();
            var rays = Ui.Image(card.transform, Art.Rays, new Color(1f, 0.85f, 0.4f, 0.32f), "Rayons");
            Ui.Place(rays.rectTransform, 0.5f, 1, 0, -380, 1300, 1300);
            rays.gameObject.AddComponent<Spin>().Speed = 16f;
            Ui.Isolate(rays.gameObject);
            var swords = Ui.Icon(card.transform, Art.IconSprite(Icon.Swords), "Epees");
            swords.color = Palette.Accent;
            Ui.Place(swords.rectTransform, 0.5f, 1, 0, -110, 130, 130);
            swords.gameObject.AddComponent<Pulse>().Amount = 0.06f;
            _theirs = DuelUi.Portrait(card.transform, Palette.Primary, 250, "Ami");
            Ui.Place((RectTransform)_theirs.transform.parent, 0.5f, 1, -230, -380, 250, 250);
            _theirs.transform.parent.gameObject.AddComponent<Rock>().Angle = 5f;
            _mine = DuelUi.Portrait(card.transform, Palette.Secondary, 250, "Moi");
            Ui.Place((RectTransform)_mine.transform.parent, 0.5f, 1, 230, -380, 250, 250);
            var vs = Ui.Title(card.transform, "VS", 96, Palette.Accent);
            Ui.Place(vs.rectTransform, 0.5f, 1, 0, -380, 200, 130);
            _title = Ui.Title(card.transform, "", 70, Color.white);
            Ui.Place(_title.rectTransform, 0.5f, 1, 0, -600, 920, 110);
            var sub = Ui.Label(card.transform, Loc.Get("duel_inv_sub"), 38, ArtCache.Hex("#D8E9FF"));
            Ui.Place(sub.rectTransform, 0.5f, 1, 0, -700, 880, 110);
            var yes = Ui.Button(card.transform, Loc.Get("duel_inv_accept"), Palette.Play, () => { Hide(); var a = _accept; _accept = null; a?.Invoke(); }, Icon.Swords, 62, "Accepter");
            Ui.Place((RectTransform)yes.transform, 0.5f, 0, 0, 244, 800, 170);
            yes.gameObject.AddComponent<Pulse>().Amount = 0.03f;
            var no = Ui.Button(card.transform, Loc.Get("duel_inv_later"), Palette.Muted, () => { Hide(); _accept = null; }, null, 46, "PlusTard");
            Ui.Place((RectTransform)no.transform, 0.5f, 0, 0, 72, 520, 120);
        }

        public void Open(string name, int head, System.Action accept)
        {
            _accept = accept;
            Show();
            _title.text = Loc.Format("duel_inv_title", name);
            _theirs.sprite = Art.Portrait(HeadCatalog.Get(head));
            _mine.sprite = Art.Portrait(HeadCatalog.Get(App.Profile.selectedHead));
        }
    }

    // ===================================================================== En partie

    /// <summary>
    /// Au-dessus de l'interface de jeu pendant un duel : les deux joueurs et le chrono en haut, la partie de l'adversaire
    /// en direct (petite vue au cadre arrondi), le compte à rebours, le K.O. et la fenêtre du dernier poids.
    /// Rien ici ne capte le doigt (sauf le petit bouton pour abandonner) : on vise et on lâche à travers.
    /// </summary>
    public sealed class DuelHudScreen : UiScreen
    {
        public const float CardTop = -196f, CardWidth = 330f, CardHeight = 392f;
        const float Inset = 10f, Strip = 62f, InnerRadius = 38f;

        Text _myScore, _theirScore, _theirName, _timer, _status, _lean, _same, _count, _banner, _bannerSub, _lastNum, _gust;
        Image _myFace, _theirFace, _frame, _danger, _dot, _lastRing;
        RectTransform _card, _bannerRt, _countRt, _lastRt, _gustRt, _vs;
        CanvasGroup _bannerGroup, _gustGroup;
        Vector2 _cardHome;
        float _countPop, _bannerPop, _bannerTimer, _cardZoom, _shownScoreMine = -1, _shownScoreTheirs = -1, _punchMine, _punchTheirs;
        int _shownCount = -99;
        DuelController.Phase _shownPhase;
        bool _timeUpShown;
        float _timerPunch;

        protected override void Build()
        {
            // ---- En-tête : TOI | VS chrono | adversaire
            var left = Ui.Glass(Safe, "Moi");
            Ui.Place(left.rectTransform, 0, 1, 24, -24, 380, 150, 0, 1);
            _myFace = DuelUi.Portrait(left.transform, Palette.Secondary, 118);
            Ui.Place((RectTransform)_myFace.transform.parent, 0, 0.5f, 72, 0, 118, 118);
            var me = Ui.Title(left.transform, Loc.Get("duel_you"), 38, ArtCache.Hex("#7CC4FF"), "Nom");
            Ui.Place(me.rectTransform, 0, 1, 142, -14, 220, 50, 0, 1);
            me.alignment = TextAnchor.MiddleLeft;
            _myScore = Ui.Title(left.transform, "0", 70, Color.white, "Score");
            Ui.Place(_myScore.rectTransform, 0, 0, 142, 10, 228, 84, 0, 0);
            _myScore.alignment = TextAnchor.MiddleLeft;

            var right = Ui.Glass(Safe, "Adversaire");
            Ui.Place(right.rectTransform, 1, 1, -24, -24, 380, 150, 1, 1);
            _theirFace = DuelUi.Portrait(right.transform, Palette.Primary, 118);
            Ui.Place((RectTransform)_theirFace.transform.parent, 1, 0.5f, -72, 0, 118, 118);
            _theirName = Ui.Title(right.transform, "", 38, ArtCache.Hex("#FF8A8E"), "Nom");
            Ui.Place(_theirName.rectTransform, 1, 1, -142, -14, 220, 50, 1, 1);
            _theirName.alignment = TextAnchor.MiddleRight;
            _theirScore = Ui.Title(right.transform, "0", 70, Color.white, "Score");
            Ui.Place(_theirScore.rectTransform, 1, 0, -142, 10, 228, 84, 1, 0);
            _theirScore.alignment = TextAnchor.MiddleRight;

            var vs = Ui.Image(Safe, Art.Button, Palette.Accent, "VS", true);
            _vs = vs.rectTransform;
            Ui.Place(_vs, 0.5f, 1, 0, -20, 196, 158, 0.5f, 1);
            var vsText = Ui.Title(vs.transform, "VS", 40);
            Ui.Place(vsText.rectTransform, 0.5f, 1, 0, -30, 180, 50);
            _timer = Ui.Title(vs.transform, "0:00", 68);
            Ui.Place(_timer.rectTransform, 0.5f, 0, 0, 34, 186, 80, 0.5f, 0);

            // ---- Vue en direct : cadre arrondi, image de la seconde caméra découpée aux mêmes coins.
            _frame = DuelUi.Rounded(Safe, Palette.Outline, InnerRadius + Inset, "EnDirect");
            _card = _frame.rectTransform;
            _cardHome = new Vector2(24 + CardWidth * 0.5f, CardTop - CardHeight * 0.5f);
            Ui.Place(_card, 0, 1, _cardHome.x, _cardHome.y, CardWidth, CardHeight);
            var mask = DuelUi.Rounded(_frame.transform, Color.white, InnerRadius, "Decoupe");
            Ui.Stretch(mask.rectTransform, Inset, Inset, Inset, Strip);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var feedGo = Ui.Rect("Image", mask.transform);
            Ui.Stretch(feedGo);
            var raw = feedGo.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            raw.texture = App.Duel.View != null ? App.Duel.View.Texture : null;
            // Voile rouge du danger de l'adversaire : dans la découpe, il suit exactement les coins arrondis.
            _danger = Ui.Image(mask.transform, Art.Vignette, new Color(1, 0.2f, 0.2f, 0), "Danger");
            Ui.Stretch(_danger.rectTransform);
            _dot = Ui.Image(_frame.transform, Art.Circle, Palette.Primary, "Point");
            Ui.Place(_dot.rectTransform, 0, 0, 40, Strip * 0.5f + 2, 24, 24);
            _status = Ui.Title(_frame.transform, "", 34, Color.white, "Statut");
            Ui.Place(_status.rectTransform, 0, 0, 62, 4, CardWidth - 76, Strip - 4, 0, 0);
            _status.alignment = TextAnchor.MiddleLeft;
            _lean = Ui.Title(Safe, "", 38, ArtCache.Hex("#FF8A8E"), "Penche");
            Ui.Place(_lean.rectTransform, 0, 1, 24, CardTop - CardHeight - 8, 520, 54, 0, 1);
            _lean.alignment = TextAnchor.MiddleLeft;

            _same = Ui.Title(Safe, "", 36, ArtCache.Hex("#BFE0FF"), "Meme");
            Ui.Place(_same.rectTransform, 1, 1, -26, -470, 520, 50, 1, 1);
            _same.alignment = TextAnchor.MiddleRight;

            // ---- Rafale commune bientôt (en bas, sur l'herbe)
            _gustRt = Ui.Rect("RafaleBientot", Safe);
            Ui.Place(_gustRt, 0.5f, 0, 0, 110, 820, 96, 0.5f, 0);
            _gustGroup = _gustRt.gameObject.AddComponent<CanvasGroup>();
            _gustGroup.blocksRaycasts = false;
            var gp = Ui.Image(_gustRt, Art.Pill, ArtCache.Hex("#2F7BEA"), "Fond", true);
            Ui.Stretch(gp.rectTransform);
            _gust = Ui.Title(_gustRt, "", 42);
            Ui.Stretch(_gust.rectTransform, 30, 0, 30, 4);

            // ---- Compte à rebours
            _countRt = Ui.Rect("Compte", Safe);
            Ui.Place(_countRt, 0.5f, 0.5f, 0, 10, 900, 360); // sous la phrase d'aide (« Glisse pour viser… »), sans la toucher
            _count = Ui.Title(_countRt, "3", 300, Palette.Accent);
            Ui.Stretch(_count.rectTransform);
            _count.horizontalOverflow = HorizontalWrapMode.Overflow;
            // Contour fin : celui d'un titre (6 % de la taille) rendait les gros chiffres pâteux.
            var fx = _count.GetComponents<Shadow>(); // deux contours (Outline hérite de Shadow), puis l'ombre portée
            if (fx.Length >= 3)
            {
                fx[0].effectDistance = new Vector2(6f, -6f);
                fx[1].effectDistance = new Vector2(4f, 4f);
                fx[2].effectDistance = new Vector2(0f, -12f);
            }
            Ui.Isolate(_countRt.gameObject);

            // ---- Bandeau K.O. / attente
            _bannerRt = Ui.Rect("Bandeau", Safe);
            Ui.Place(_bannerRt, 0.5f, 0.5f, 0, 300, 1000, 300);
            _bannerGroup = _bannerRt.gameObject.AddComponent<CanvasGroup>();
            _bannerGroup.blocksRaycasts = false;
            _banner = Ui.Title(_bannerRt, "", 150, Palette.Accent);
            Ui.Place(_banner.rectTransform, 0.5f, 1, 0, -90, 1000, 180);
            _banner.horizontalOverflow = HorizontalWrapMode.Overflow;
            _bannerSub = Ui.Title(_bannerRt, "", 52);
            Ui.Place(_bannerSub.rectTransform, 0.5f, 0, 0, 30, 1000, 80, 0.5f, 0);
            Ui.Isolate(_bannerRt.gameObject);

            // ---- Dernier poids : anneau qui se vide
            _lastRt = Ui.Rect("DernierPoids", Safe);
            Ui.Place(_lastRt, 0.5f, 0.5f, 0, 40, 200, 200);
            var back = Ui.Image(_lastRt, Art.Circle, new Color(0.1f, 0.08f, 0.22f, 0.55f), "Fond");
            Ui.Stretch(back.rectTransform, 14, 14, 14, 14);
            _lastRing = Ui.Image(_lastRt, Art.Ring, Palette.Accent, "Anneau");
            Ui.Stretch(_lastRing.rectTransform);
            _lastRing.type = Image.Type.Filled; _lastRing.fillMethod = Image.FillMethod.Radial360;
            _lastRing.fillOrigin = (int)Image.Origin360.Top; _lastRing.fillClockwise = false;
            _lastNum = Ui.Title(_lastRt, "3", 100);
            Ui.Stretch(_lastNum.rectTransform);
            Ui.Isolate(_lastRt.gameObject);

            // ---- Abandonner (seul élément touchable, petit, en bas à gauche)
            var quit = Ui.IconButton(Safe, Icon.Close, Palette.Dark, App.DuelAskQuit, "Abandonner");
            Ui.Place((RectTransform)quit.transform, 0, 0, 24, 24, 110, 116, 0, 0);
        }

        public override void Show()
        {
            Root.gameObject.SetActive(true);
            Refresh();
        }

        public override void Refresh()
        {
            var d = App.Duel;
            _myFace.sprite = Art.Portrait(HeadCatalog.Get(App.Profile.selectedHead));
            _theirFace.sprite = Art.Portrait(HeadCatalog.Get(d.Them != null ? d.Them.Head : 0));
            _theirName.text = d.TheirName.ToUpperInvariant();
            _same.text = Loc.Format("duel_same", d.TheirName);
            _shownPhase = (DuelController.Phase)(-1);
            _shownCount = -99;
            _shownScoreMine = _shownScoreTheirs = -1;
            _timeUpShown = false;
            _timerPunch = 0f;
            _cardZoom = 0f;
            _bannerTimer = 0f;
            _bannerRt.gameObject.SetActive(false);
            _lastRt.gameObject.SetActive(false);
            _countRt.gameObject.SetActive(false);
            _gustRt.gameObject.SetActive(false);
        }

        void ShowBanner(string title, string sub, Color color, float seconds)
        {
            _banner.text = title;
            _banner.color = color;
            _bannerSub.text = sub;
            _bannerPop = 1f;
            _bannerTimer = seconds;
            _bannerRt.gameObject.SetActive(true);
        }

        public override void Tick(float dt)
        {
            var d = App.Duel;
            var s = App.Session;
            var phase = d.Current;
            if (phase != _shownPhase)
            {
                if (phase == DuelController.Phase.LastWeight)
                {
                    ShowBanner(Loc.Get("duel_ko"), Loc.Format("duel_they_fell", d.TheirName), Palette.Accent, 99f);
                    App.PlaySfx(Sfx.Lift);
                    App.HapticMedium();
                }
                else if (phase == DuelController.Phase.Waiting && d.IFell && !d.TheyFell)
                    ShowBanner(Loc.Get("duel_you_fell"), Loc.Format("duel_wait_them", d.TheirName), Color.white, 99f);
                else if (phase == DuelController.Phase.Playing && _shownPhase == DuelController.Phase.Countdown)
                    _countPop = 1f;
                _shownPhase = phase;
            }

            // Chrono à rebours (3:00 -> 0:00) ; les 10 dernières secondes en rouge, qui battent et cliquettent.
            float time = d.Elapsed;
            if (d.HasTimeLimit)
            {
                float left = d.TimeLeft;
                int sec = Mathf.CeilToInt(left);
                string clock = DuelUi.Clock(sec);
                if (_timer.text != clock)
                {
                    _timer.text = clock;
                    bool playing = phase == DuelController.Phase.Playing && !d.IFell;
                    if (playing && sec <= 10 && sec > 0) { _timerPunch = 1f; App.PlaySfx(Sfx.Click, 10 - sec, sec <= 5 ? 0.8f : 0.5f); }
                }
                bool hurry = left <= 10.5f && phase == DuelController.Phase.Playing;
                _timer.color = hurry ? ArtCache.Hex("#FF4D57") : Color.white;
                _timerPunch = Mathf.MoveTowards(_timerPunch, 0f, dt * 3f);
                _timer.rectTransform.localScale = Vector3.one * (1f + 0.22f * _timerPunch);
            }
            else _timer.text = DuelUi.Clock(time);
            // Temps écoulé : les deux parties s'arrêtent, le meilleur score gagne.
            if (d.TimeUp && !_timeUpShown)
            {
                _timeUpShown = true;
                ShowBanner(Loc.Get("duel_time_up"), Loc.Get("duel_time_up_sub"), ArtCache.Hex("#FF8A3D"), 99f);
                App.PlaySfx(Sfx.Chime, 4);
                App.HapticMedium();
            }
            int mine = s.Score;
            if (mine != _shownScoreMine) { if (_shownScoreMine >= 0 && mine > _shownScoreMine) _punchMine = 1f; _shownScoreMine = mine; _myScore.text = Loc.FormatScore(mine); }
            int theirs = d.Opponent != null ? d.Opponent.Score : 0;
            if (theirs != _shownScoreTheirs) { if (_shownScoreTheirs >= 0 && theirs > _shownScoreTheirs) _punchTheirs = 1f; _shownScoreTheirs = theirs; _theirScore.text = Loc.FormatScore(theirs); }
            _punchMine = Mathf.MoveTowards(_punchMine, 0f, dt * 4f);
            _punchTheirs = Mathf.MoveTowards(_punchTheirs, 0f, dt * 4f);
            _myScore.rectTransform.localScale = Vector3.one * (1f + 0.18f * _punchMine);
            _theirScore.rectTransform.localScale = Vector3.one * (1f + 0.18f * _punchTheirs);
            // Le chrono bat doucement quand la pression monte (la planche devient plus sensible).
            float pressure = Mathf.Clamp01((time - App.Tuning.duel.pressureFromSeconds) / 60f);
            _vs.localScale = Vector3.one * (1f + pressure * 0.04f * Mathf.Sin(Time.unscaledTime * 6f));

            // Vue en direct : cadre qui rougit avec le danger de l'adversaire, pastille qui clignote.
            float danger = d.Opponent != null && !d.TheyFell ? Mathf.Clamp01((d.Opponent.Danger - 0.55f) / 0.45f) : 0f;
            float beat = danger > 0.6f ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 10f) : 1f;
            _frame.color = Color.Lerp(Palette.Outline, ArtCache.Hex("#FF3B47"), danger * beat);
            _danger.color = new Color(1f, 0.25f, 0.25f, danger * 0.8f * beat);
            _danger.enabled = danger > 0.01f;
            bool live = d.Live;
            _status.text = live ? Loc.Format("duel_live", d.TheirName.ToUpperInvariant()) : Loc.Get("duel_live_wait");
            _dot.color = live ? new Color(1f, 0.3f, 0.3f, Mathf.PingPong(Time.unscaledTime * 1.6f, 1f) > 0.35f ? 1f : 0.25f) : Palette.Muted;
            bool leanOn = danger > 0.35f && phase == DuelController.Phase.Playing;
            _lean.gameObject.SetActive(leanOn);
            if (leanOn) _lean.text = Loc.Format("duel_leans", d.TheirName, Mathf.RoundToInt(Mathf.Clamp01(d.Opponent.Danger) * 100f));

            // Quand j'ai lâché, la vue de l'adversaire grandit au centre : on regarde s'il tient.
            float zoomTarget = d.IFell && !d.TheyFell && (phase == DuelController.Phase.Waiting || phase == DuelController.Phase.LastWeight || phase == DuelController.Phase.Playing) ? 1f : 0f;
            _cardZoom = Mathf.MoveTowards(_cardZoom, zoomTarget, dt * 2.2f);
            float z = Mathf.SmoothStep(0f, 1f, _cardZoom);
            var parent = (RectTransform)_card.parent;
            var center = new Vector2(parent.rect.width * 0.5f, -parent.rect.height * 0.5f + 60f);
            _card.anchoredPosition = Vector2.Lerp(_cardHome, center, z);
            _card.localScale = Vector3.one * Mathf.Lerp(1f, 2.3f, z);
            _same.gameObject.SetActive(z < 0.05f);

            // Compte à rebours : 3, 2, 1, Partez !
            bool counting = phase == DuelController.Phase.Countdown || _countPop > 0f;
            if (_countRt.gameObject.activeSelf != counting) _countRt.gameObject.SetActive(counting);
            if (counting)
            {
                int n = phase == DuelController.Phase.Countdown ? Mathf.CeilToInt(d.Countdown) : 0;
                if (n != _shownCount)
                {
                    _shownCount = n;
                    _count.text = n > 0 ? n.ToString() : Loc.Get("duel_go");
                    _count.fontSize = Mathf.RoundToInt((n > 0 ? 300 : 190) * Ui.TextScale);
                    _count.color = n > 0 ? Palette.Accent : Palette.Play;
                    _countRt.localScale = Vector3.one * 1.6f;
                    App.PlaySfx(n > 0 ? Sfx.Click : Sfx.Lift, n > 0 ? 3 - n : 0, n > 0 ? 0.9f : 1f);
                    if (n <= 0) { _countPop = 0.9f; App.HapticMedium(); }
                }
                _countRt.localScale = Vector3.Lerp(_countRt.localScale, Vector3.one, 1f - Mathf.Exp(-dt * 10f));
                if (phase != DuelController.Phase.Countdown)
                {
                    _countPop = Mathf.MoveTowards(_countPop, 0f, dt);
                    var c = _count.color; c.a = Mathf.Clamp01(_countPop / 0.4f); _count.color = c;
                }
            }

            // Bandeau
            if (_bannerRt.gameObject.activeSelf)
            {
                if (phase == DuelController.Phase.Results || phase == DuelController.Phase.Countdown) _bannerTimer = 0f;
                _bannerTimer -= dt;
                _bannerPop = Mathf.MoveTowards(_bannerPop, 0f, dt * 3f);
                _bannerRt.localScale = Vector3.one * (1f + 0.5f * _bannerPop * _bannerPop);
                _bannerGroup.alpha = Mathf.Clamp01(_bannerTimer / 0.3f);
                if (_bannerTimer <= 0f) _bannerRt.gameObject.SetActive(false);
            }

            // Dernier poids : l'anneau se vide, chaque seconde tape.
            bool last = phase == DuelController.Phase.LastWeight;
            if (_lastRt.gameObject.activeSelf != last) _lastRt.gameObject.SetActive(last);
            if (last)
            {
                float left = d.LastWeightLeft, total = Mathf.Max(0.1f, d.LastWeightTotal);
                _lastRing.fillAmount = left / total;
                int sec = Mathf.CeilToInt(left);
                string txt = sec.ToString();
                if (_lastNum.text != txt) { _lastNum.text = txt; _lastRt.localScale = Vector3.one * 1.25f; }
                _lastRt.localScale = Vector3.Lerp(_lastRt.localScale, Vector3.one, 1f - Mathf.Exp(-dt * 9f));
                _bannerSub.text = Loc.Get("duel_last_weight");
            }

            // Rafale commune bientôt
            float next = s.NextGustIn;
            bool gust = phase == DuelController.Phase.Playing && next > 0f && next <= 8f && s.WindPhase == WindPhase.None;
            if (_gustRt.gameObject.activeSelf != gust) _gustRt.gameObject.SetActive(gust);
            if (gust)
            {
                _gust.text = Loc.Format("duel_gust_soon", DuelUi.Clock(Mathf.Ceil(next)));
                _gustGroup.alpha = Mathf.Clamp01((8f - next) / 0.4f);
            }
        }
    }

    // ===================================================================== Résultat

    /// <summary>
    /// Fin du duel (1.4), dans le style de l'entrée du duel : le titre (Victoire / Défaite / Égalité) qui claque, des
    /// rayons et des confettis pour une victoire, puis une carte VS aux coins arrondis : toi en bleu à gauche, l'ami en
    /// rouge à droite, avec portrait, nom, score en grand et temps tenu. Le gagnant porte la couronne et flotte dans un
    /// halo doré ; le perdant pâlit. Duel fini au temps (3 minutes, personne n'a lâché) : étiquette « Temps écoulé ».
    /// Dessous : les pièces gagnées qui défilent, la progression vers la tête Challenger, Revanche, Accueil, Inviter.
    /// </summary>
    public sealed class DuelResultScreen : UiScreen
    {
        const float CardTop = -430f, CardHeight = 560f, ColX = 250f, FaceY = -165f, CrownY = -50f;

        sealed class Side
        {
            public RectTransform Col, Crown;
            public Image Halo, Face, Disc;
            public Text Name, Score, Time;
        }

        Text _title, _sub, _coins, _wins, _record, _timeTag;
        Image _rays, _challenger, _coinPile;
        RectTransform _winsFill, _tagRt, _confettiRoot, _coinRow;
        Side _me, _them;
        Button _rematch;
        float _t;
        int _coinsTarget, _coinsShown = -1;
        bool _coinsDone;
        readonly List<(RectTransform rt, Image img, Vector2 v, float spin, float age)> _confetti = new List<(RectTransform, Image, Vector2, float, float)>();

        protected override void Build()
        {
            _rays = Ui.Image(Safe, Art.Rays, new Color(1, 0.9f, 0.5f, 0.35f), "Rayons");
            Ui.Place(_rays.rectTransform, 0.5f, 1, 0, -260, 1400, 1400);
            _rays.gameObject.AddComponent<Spin>().Speed = 14f;
            Ui.Isolate(_rays.gameObject);
            _title = Ui.Title(Safe, "", 130, Palette.Accent);
            Ui.Place(_title.rectTransform, 0.5f, 1, 0, -190, 1000, 170);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            _sub = Ui.Title(Safe, "", 46);
            Ui.Place(_sub.rectTransform, 0.5f, 1, 0, -308, 980, 70);

            // « Temps écoulé » : le duel s'est fini au temps, le meilleur score a gagné.
            var tag = Ui.Image(Safe, Art.Pill, ArtCache.Hex("#FF8A3D"), "TempsEcoule", true);
            _tagRt = tag.rectTransform;
            Ui.Place(_tagRt, 0.5f, 1, 0, -380, 560, 64);
            _timeTag = Ui.Title(tag.transform, "", 34, Color.white);
            Ui.Stretch(_timeTag.rectTransform, 24, 2, 24, 0);

            // ---- Carte VS : bleu à gauche, rouge à droite, rayons, découpés aux coins arrondis.
            var card = Ui.Image(Safe, Art.Panel, ArtCache.Hex("#2B1B54"), "Duel", true);
            Ui.Place(card.rectTransform, 0.5f, 1, 0, CardTop, 1000, CardHeight, 0.5f, 1);
            var clip = DuelUi.Rounded(card.transform, Color.white, 52, "Decoupe");
            Ui.Stretch(clip.rectTransform, 10, 10, 10, 10);
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var blue = Ui.Image(clip.transform, null, new Color(0.29f, 0.61f, 1f, 0.55f), "Bleu");
            Ui.Place(blue.rectTransform, 0, 0.5f, -120, 0, 720, 1000, 0, 0.5f);
            blue.rectTransform.localRotation = Quaternion.Euler(0, 0, -14f);
            var red = Ui.Image(clip.transform, null, new Color(1f, 0.35f, 0.37f, 0.55f), "Rouge");
            Ui.Place(red.rectTransform, 1, 0.5f, 120, 0, 720, 1000, 1, 0.5f);
            red.rectTransform.localRotation = Quaternion.Euler(0, 0, -14f);
            var cardRays = Ui.Image(clip.transform, Art.Rays, new Color(1f, 0.85f, 0.4f, 0.3f), "Rayons");
            Ui.Place(cardRays.rectTransform, 0.5f, 1, 0, FaceY, 900, 900);
            cardRays.gameObject.AddComponent<Spin>().Speed = -10f;
            Ui.Isolate(cardRays.gameObject);

            _me = Column(card.transform, -ColX, Palette.Secondary, ArtCache.Hex("#BFE0FF"));
            _them = Column(card.transform, ColX, Palette.Primary, ArtCache.Hex("#FFC2C4"));
            var vsGlow = Ui.Image(card.transform, Art.SoftDisc, new Color(1f, 0.8f, 0.3f, 0.55f), "Halo");
            Ui.Place(vsGlow.rectTransform, 0.5f, 1, 0, FaceY, 260, 260);
            var vs = Ui.Title(card.transform, "VS", 104, Palette.Accent, "VS");
            Ui.Place(vs.rectTransform, 0.5f, 1, 0, FaceY, 220, 140);

            // ---- Pièces gagnées : un tas de pièces et le gain qui défile.
            var coinRow = Ui.Glass(Safe, "Pieces");
            _coinRow = coinRow.rectTransform;
            Ui.Place(_coinRow, 0.5f, 1, 0, CardTop - CardHeight - 92, 460, 124);
            _coinPile = Ui.Icon(coinRow.transform, Art.CoinPile(3), "Tas");
            Ui.Place(_coinPile.rectTransform, 0.5f, 0.5f, -120, 2, 120, 120);
            _coins = Ui.Title(coinRow.transform, "+0", 70, Palette.Accent);
            Ui.Place(_coins.rectTransform, 0.5f, 0.5f, 60, 0, 260, 100);
            _coins.horizontalOverflow = HorizontalWrapMode.Overflow;

            // ---- Vers la tête Challenger (même bandeau que l'entrée du duel).
            var ch = Ui.Glass(Safe, "Challenger");
            Ui.Place(ch.rectTransform, 0.5f, 1, 0, CardTop - CardHeight - 184, 1000, 140, 0.5f, 1);
            _challenger = DuelUi.Portrait(ch.transform, Palette.Primary, 116);
            Ui.Place((RectTransform)_challenger.transform.parent, 0, 0.5f, 84, 0, 116, 116);
            _wins = Ui.Title(ch.transform, "", 42, Color.white, "Victoires");
            Ui.Place(_wins.rectTransform, 0, 0.5f, 170, 36, 790, 54, 0, 0.5f);
            _wins.alignment = TextAnchor.MiddleLeft;
            var bar = Ui.Rect("Jauge", ch.transform);
            Ui.Place(bar, 0, 0.5f, 170, -6, 780, 30, 0, 0.5f);
            _winsFill = MetaUi.Bar(bar, Art, Palette.Primary);
            Ui.Stretch((RectTransform)_winsFill.parent);
            _record = Ui.Title(ch.transform, "", 32, ArtCache.Hex("#BFE0FF"), "Bilan");
            Ui.Place(_record.rectTransform, 0, 0.5f, 170, -44, 790, 44, 0, 0.5f);
            _record.alignment = TextAnchor.MiddleLeft;

            _rematch = Ui.Button(Safe, Loc.Get("duel_rematch"), Palette.Play, App.DuelRematch, Icon.Replay, 60, "Revanche");
            Ui.Place((RectTransform)_rematch.transform, 0.5f, 0, 0, 300, 860, 180, 0.5f, 0);
            _rematch.gameObject.AddComponent<Pulse>().Amount = 0.02f;
            var home = Ui.Button(Safe, Loc.Get("home"), Palette.Muted, App.DuelHome, Icon.Home, 50, "Accueil");
            Ui.Place((RectTransform)home.transform, 0.5f, 0, -232, 120, 440, 140, 0.5f, 0);
            // Parrainage : défier un autre ami (code ami et lien du jeu), surtout après une victoire.
            var invite = Ui.Button(Safe, Loc.Get("duel_brag"), Palette.Purple, App.ShareDuelBrag, Icon.Share, 46, "Inviter");
            Ui.Place((RectTransform)invite.transform, 0.5f, 0, 232, 120, 440, 140, 0.5f, 0);

            _confettiRoot = Ui.Stretch(Ui.Rect("Confettis", Safe));
            Ui.Isolate(_confettiRoot.gameObject);
        }

        Side Column(Transform card, float x, Color ring, Color nameColor)
        {
            var s = new Side();
            s.Col = Ui.Rect("Joueur", card);
            Ui.Place(s.Col, 0.5f, 1, x, 0, 440, CardHeight, 0.5f, 1);
            s.Halo = Ui.Image(s.Col, Art.SoftDisc, new Color(1f, 0.82f, 0.3f, 0f), "Halo");
            Ui.Place(s.Halo.rectTransform, 0.5f, 1, 0, FaceY, 360, 360);
            s.Face = DuelUi.Portrait(s.Col, ring, 210, "Portrait");
            s.Disc = s.Face.transform.parent.GetComponent<Image>();
            Ui.Place((RectTransform)s.Face.transform.parent, 0.5f, 1, 0, FaceY, 210, 210);
            // Couronne du gagnant, posée de travers sur le portrait.
            var crown = Ui.Icon(s.Col, Art.IconSprite(Icon.Crown), "Couronne");
            crown.color = Palette.Accent;
            s.Crown = crown.rectTransform;
            Ui.Place(s.Crown, 0.5f, 1, x < 0 ? -34 : 34, CrownY, 124, 124);
            var crownOutline = crown.gameObject.AddComponent<Outline>();
            crownOutline.effectColor = Palette.Outline; crownOutline.effectDistance = new Vector2(4, -4);
            s.Name = Ui.Title(s.Col, "", 44, nameColor, "Nom");
            Ui.Place(s.Name.rectTransform, 0.5f, 1, 0, -305, 420, 56);
            s.Score = Ui.Title(s.Col, "", 84, Color.white, "Score");
            Ui.Place(s.Score.rectTransform, 0.5f, 1, 0, -392, 420, 100);
            s.Score.horizontalOverflow = HorizontalWrapMode.Overflow;
            s.Time = Ui.Title(s.Col, "", 34, ArtCache.Hex("#D8E9FF"), "Temps");
            Ui.Place(s.Time.rectTransform, 0.5f, 1, 0, -478, 420, 50);
            return s;
        }

        public override void Show()
        {
            _t = 0f;
            _coinsShown = -1;
            _coinsDone = false;
            ClearConfetti();
            base.Show();
            if (App.Duel.Outcome == DuelOutcome.Win && !App.Profile.settings.reducedMotion) Burst(70);
        }

        public override void Hide()
        {
            ClearConfetti();
            base.Hide();
        }

        public override void Refresh()
        {
            var d = App.Duel;
            var o = d.Outcome;
            _title.text = Loc.Get(o == DuelOutcome.Win ? "duel_win" : o == DuelOutcome.Lose ? "duel_lose" : "duel_draw");
            _title.color = o == DuelOutcome.Win ? Palette.Accent : o == DuelOutcome.Lose ? ArtCache.Hex("#BFE0FF") : Color.white;
            _rays.gameObject.SetActive(o != DuelOutcome.Lose);
            // Fini au temps : personne n'a lâché pendant les 3 minutes, le meilleur score gagne.
            bool byTime = !d.ByForfeit && (d.TimeUp || (!d.IFell && !d.TheyFell));
            string sub;
            if (d.ByForfeit) sub = o == DuelOutcome.Win ? Loc.Format("duel_forfeit_sub", d.TheirName) : Loc.Get("duel_gave_up_sub");
            else if (byTime) sub = Loc.Format(o == DuelOutcome.Win ? "duel_time_win_sub" : o == DuelOutcome.Lose ? "duel_time_lose_sub" : "duel_time_draw_sub", d.TheirName);
            else sub = Loc.Format(o == DuelOutcome.Win ? "duel_win_sub" : o == DuelOutcome.Lose ? "duel_lose_sub" : "duel_draw_sub", d.TheirName);
            _sub.text = sub;
            _tagRt.gameObject.SetActive(byTime);
            if (byTime) _timeTag.text = Loc.Format("duel_time_tag", DuelUi.Clock(App.Tuning.duel.timeLimitSeconds));

            Fill(_me, HeadCatalog.Get(App.Profile.selectedHead), Loc.Get("duel_you"), d.MyScore, d.MySeconds, o == DuelOutcome.Win, o == DuelOutcome.Lose);
            Fill(_them, HeadCatalog.Get(d.Them != null ? d.Them.Head : 0), d.TheirName, d.TheirScore, d.TheirSeconds, o == DuelOutcome.Lose, o == DuelOutcome.Win);

            _coinsTarget = d.CoinsWon;
            _coinPile.sprite = Art.CoinPile(d.CoinsWon);
            if (_coinsShown < 0) _coins.text = "+0";

            var p = App.Profile;
            var head = DuelUi.ChallengerHead();
            int need = head != null ? head.UnlockValue : App.Tuning.duel.challengerWins;
            bool done = head != null && p.unlockedHeads.Contains(head.Id);
            if (head != null) _challenger.sprite = done ? Art.Portrait(head) : Art.Locked(head);
            _wins.text = done ? Loc.Get("duel_challenger_done") : Loc.Format("duel_challenger", Mathf.Min(p.duelWins, need), need);
            _record.text = Loc.Format("duel_record", p.duelWins, p.duelsPlayed);
            Canvas.ForceUpdateCanvases();
            MetaUi.SetFill(_winsFill, need > 0 ? p.duelWins / (float)need : 1f);

            var label = Ui.ButtonLabel(_rematch);
            if (label != null)
                label.text = d.IWantRematch ? Loc.Format("duel_rematch_wait", d.TheirName)
                    : d.TheyWantRematch ? Loc.Format("duel_rematch_asked", d.TheirName) : Loc.Get("duel_rematch");
            _rematch.interactable = !d.IWantRematch;
        }

        void Fill(Side s, HeadDef head, string name, int score, float seconds, bool winner, bool loser)
        {
            s.Face.sprite = Art.Portrait(head);
            // Le perdant pâlit un peu ; le gagnant a la couronne, le halo doré et son score en or.
            s.Face.color = loser ? new Color(0.68f, 0.68f, 0.76f, 1f) : Color.white;
            s.Disc.transform.localScale = Vector3.one * (loser ? 0.9f : 1f);
            s.Crown.gameObject.SetActive(winner);
            s.Halo.gameObject.SetActive(winner);
            s.Name.text = name;
            s.Score.text = Loc.FormatScore(score);
            s.Score.color = winner ? Palette.Accent : loser ? ArtCache.Hex("#D8E9FF") : Color.white;
            s.Time.text = Loc.Format("duel_held", DuelUi.Clock(seconds));
        }

        public override void Tick(float dt)
        {
            _t += dt;
            var o = App.Duel.Outcome;
            bool reduced = App.Profile.settings.reducedMotion;
            // Le titre claque, le gagnant flotte dans son halo, sa couronne se balance.
            float pop = Mathf.Clamp01(_t / 0.35f);
            _title.rectTransform.localScale = Vector3.one * (reduced ? 1f : 1f + 0.4f * (1f - pop) * (1f - pop));
            Animate(_me, o == DuelOutcome.Win, reduced, 0f);
            Animate(_them, o == DuelOutcome.Lose, reduced, 1.3f);

            // Pièces : le gain défile, puis un petit tintement.
            float k = Mathf.Clamp01((_t - 0.45f) / 0.9f);
            int shown = Mathf.RoundToInt(_coinsTarget * (1f - (1f - k) * (1f - k)));
            if (shown != _coinsShown) { _coinsShown = shown; _coins.text = "+" + shown; }
            float punch = !_coinsDone ? 0f : Mathf.Clamp01(1f - (_t - 1.35f) / 0.3f);
            if (!_coinsDone && k >= 1f)
            {
                _coinsDone = true;
                if (_coinsTarget > 0) App.PlaySfx(Sfx.Chime, 6, 0.6f);
            }
            _coinRow.localScale = Vector3.one * (1f + 0.08f * punch);
            TickConfetti(dt);
        }

        void Animate(Side s, bool winner, bool reduced, float phase)
        {
            float bob = winner && !reduced ? 10f + Mathf.Sin(_t * 3f + phase) * 8f : 0f;
            s.Col.anchoredPosition = new Vector2(s.Col.anchoredPosition.x, bob);
            if (!winner) return;
            float glow = reduced ? 0.5f : 0.42f + 0.16f * Mathf.Sin(_t * 4f);
            s.Halo.color = new Color(1f, 0.82f, 0.3f, glow);
            // La couronne tombe sur la tête au début, puis se balance.
            float drop = reduced ? 1f : Mathf.Clamp01((_t - 0.3f) / 0.35f);
            float tilt = s.Col.anchoredPosition.x < 0 ? 14f : -14f;
            float sx = s.Crown.anchoredPosition.x;
            s.Crown.anchoredPosition = new Vector2(sx, CrownY + (1f - drop) * (1f - drop) * 260f);
            s.Crown.localEulerAngles = new Vector3(0, 0, tilt + (reduced ? 0f : Mathf.Sin(_t * 2.2f) * 5f));
            s.Crown.localScale = Vector3.one * (drop < 1f ? 1f : 1f + 0.05f * Mathf.Sin(_t * 5f));
        }

        // ------------------------------------------------------------------ Confettis (victoire)

        static readonly Color[] Party =
        {
            new Color(1f, 0.79f, 0.24f), new Color(1f, 0.36f, 0.4f), new Color(0.29f, 0.61f, 1f), new Color(0.36f, 0.82f, 0.43f), new Color(0.66f, 0.42f, 1f),
        };

        void Burst(int n)
        {
            for (int i = 0; i < n; i++)
            {
                var img = Ui.Image(_confettiRoot, Art.Pixel, Party[UnityEngine.Random.Range(0, Party.Length)], "Confetti");
                var rt = img.rectTransform;
                Ui.Place(rt, 0.5f, 1, UnityEngine.Random.Range(-160f, 160f), -190f, UnityEngine.Random.Range(14f, 26f), UnityEngine.Random.Range(8f, 14f));
                float a = UnityEngine.Random.Range(0.1f, 0.9f) * Mathf.PI;
                float sp = UnityEngine.Random.Range(0.45f, 1f) * 1700f;
                _confetti.Add((rt, img, new Vector2(Mathf.Cos(a) * sp, Mathf.Sin(a) * sp), UnityEngine.Random.Range(-720f, 720f), 0f));
            }
        }

        void TickConfetti(float dt)
        {
            for (int i = _confetti.Count - 1; i >= 0; i--)
            {
                var c = _confetti[i];
                c.age += dt;
                if (c.rt == null) { _confetti.RemoveAt(i); continue; }
                c.v.y -= 2000f * dt;
                c.v *= 1f - 0.9f * dt;
                c.rt.anchoredPosition += c.v * dt;
                c.rt.localEulerAngles = new Vector3(0, 0, c.rt.localEulerAngles.z + c.spin * dt);
                c.rt.localScale = new Vector3(1f, Mathf.Abs(Mathf.Cos(c.age * 9f + i)), 1f);
                if (c.age > 2.2f) { var col = c.img.color; col.a = Mathf.Clamp01(1f - (c.age - 2.2f) / 0.4f); c.img.color = col; }
                if (c.age > 2.6f) { UnityEngine.Object.Destroy(c.rt.gameObject); _confetti.RemoveAt(i); continue; }
                _confetti[i] = c;
            }
        }

        void ClearConfetti()
        {
            foreach (var c in _confetti) if (c.rt != null) UnityEngine.Object.Destroy(c.rt.gameObject);
            _confetti.Clear();
        }
    }
}
