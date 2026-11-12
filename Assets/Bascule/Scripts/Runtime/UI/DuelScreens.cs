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
            banner.gameObject.AddComponent<RectMask2D>();
            var blue = Ui.Image(banner.transform, null, new Color(0.29f, 0.61f, 1f, 0.55f), "Bleu");
            Ui.Place(blue.rectTransform, 0, 0.5f, -120, 0, 720, 900, 0, 0.5f);
            blue.rectTransform.localRotation = Quaternion.Euler(0, 0, -14f);
            var red = Ui.Image(banner.transform, null, new Color(1f, 0.35f, 0.37f, 0.55f), "Rouge");
            Ui.Place(red.rectTransform, 1, 0.5f, 120, 0, 720, 900, 1, 0.5f);
            red.rectTransform.localRotation = Quaternion.Euler(0, 0, -14f);
            var rays = Ui.Image(banner.transform, Art.Rays, new Color(1f, 0.85f, 0.4f, 0.35f), "Rayons");
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
            // Les trois règles.
            Rule(_choice, -1, Icon.Swords, Palette.Accent, Loc.Get("duel_rule_1"));
            Rule(_choice, 0, Icon.Film, Palette.Secondary, Loc.Get("duel_rule_2"));
            Rule(_choice, 1, Icon.Flag, Palette.Primary, Loc.Get("duel_rule_3"));

            // Amis à défier en un geste.
            var ft = Ui.Title(_choice, Loc.Get("duel_friends_section"), 52, Palette.Accent);
            Ui.Place(ft.rectTransform, 0, 1, 50, -925, 700, 70, 0, 0.5f);
            ft.alignment = TextAnchor.MiddleLeft;
            var add = Ui.Button(_choice, "", Palette.Play, App.AskFriendCode, Icon.Plus, 44, "AjouterAmi");
            Ui.Place((RectTransform)add.transform, 1, 1, -40, -925, 110, 110, 1, 0.5f);
            var area = Ui.Rect("Amis", _choice);
            Ui.Stretch(area, 40, 985, 40, 520);
            _friendsCol = Ui.Scroll(area, out _);
            Ui.Column(_friendsCol, 14);
            _friendsStatus = Ui.Label(_choice, "", 40, ArtCache.Hex("#D8E9FF"));
            Ui.Stretch(_friendsStatus.rectTransform, 70, 1000, 70, 560);

            // Avec un code.
            var or = Ui.Title(_choice, Loc.Get("duel_or_code"), 42, ArtCache.Hex("#BFE0FF"));
            Ui.Place(or.rectTransform, 0.5f, 0, 0, 470, 900, 60);
            var host = Ui.Button(_choice, Loc.Get("duel_new_code"), Palette.Purple, App.DuelHost, Icon.Plus, 50, "CreerCode");
            Ui.Place((RectTransform)host.transform, 0.5f, 0, -245, 330, 470, 150);
            var join = Ui.Button(_choice, Loc.Get("duel_enter_code"), Palette.Secondary, App.DuelJoinPrompt, Icon.Play, 50, "JaiUnCode");
            Ui.Place((RectTransform)join.transform, 0.5f, 0, 245, 330, 470, 150);

            // Victoires et tête Challenger.
            var card = Ui.Glass(_choice, "Challenger");
            Ui.Place(card.rectTransform, 0.5f, 0, 0, 60, 1000, 210, 0.5f, 0);
            _challenger = DuelUi.Portrait(card.transform, Palette.Primary, 160);
            Ui.Place((RectTransform)_challenger.transform.parent, 0, 0.5f, 110, 0, 160, 160);
            _wins = Ui.Title(card.transform, "", 44, Color.white, "Victoires");
            Ui.Place(_wins.rectTransform, 0, 0.5f, 220, 52, 740, 60, 0, 0.5f);
            _wins.alignment = TextAnchor.MiddleLeft;
            var bar = Ui.Rect("Jauge", card.transform);
            Ui.Place(bar, 0, 0.5f, 220, -6, 720, 40, 0, 0.5f);
            _winsFill = MetaUi.Bar(bar, Art, Palette.Primary);
            Ui.Stretch((RectTransform)_winsFill.parent);
            _record = Ui.Title(card.transform, "", 36, ArtCache.Hex("#BFE0FF"), "Bilan");
            Ui.Place(_record.rectTransform, 0, 0.5f, 220, -60, 740, 50, 0, 0.5f);
            _record.alignment = TextAnchor.MiddleLeft;
        }

        void Rule(RectTransform parent, int i, Icon icon, Color tint, string text)
        {
            var chip = Ui.Glass(parent, "Regle");
            Ui.Place(chip.rectTransform, 0.5f, 1, i * 336, -795, 322, 150);
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
            Ui.Place((RectTransform)copy.transform, 0.5f, 1, -228, -1340, 444, 140);
            var friends = Ui.Button(_hosting, Loc.Get("duel_friends"), Palette.Purple, App.OpenFriendPicker, Icon.Face, 44, "Amis");
            Ui.Place((RectTransform)friends.transform, 0.5f, 1, 228, -1340, 444, 140);

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
            Ui.Place((RectTransform)yes.transform, 0.5f, 0, 0, 220, 800, 170);
            yes.gameObject.AddComponent<Pulse>().Amount = 0.03f;
            var no = Ui.Button(card.transform, Loc.Get("duel_inv_later"), Palette.Muted, () => { Hide(); _accept = null; }, null, 46, "PlusTard");
            Ui.Place((RectTransform)no.transform, 0.5f, 0, 0, 70, 520, 120);
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

            // Chrono et scores
            float time = d.Elapsed;
            _timer.text = DuelUi.Clock(time);
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

    public sealed class DuelResultScreen : UiScreen
    {
        Text _title, _sub, _myName, _theirName, _myTime, _theirTime, _myScore, _theirScore, _coins, _wins;
        Image _myFace, _theirFace, _rays, _challenger;
        RectTransform _winsFill, _mine, _theirs;
        Button _rematch;
        float _t;

        protected override void Build()
        {
            _rays = Ui.Image(Safe, Art.Rays, new Color(1, 0.9f, 0.5f, 0.35f), "Rayons");
            Ui.Place(_rays.rectTransform, 0.5f, 1, 0, -300, 1300, 1300);
            _rays.gameObject.AddComponent<Spin>().Speed = 14f;
            _title = Ui.Title(Safe, "", 130, Palette.Accent);
            Ui.Place(_title.rectTransform, 0.5f, 1, 0, -190, 1000, 180);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            _sub = Ui.Title(Safe, "", 48);
            Ui.Place(_sub.rectTransform, 0.5f, 1, 0, -320, 960, 80);

            _mine = Column(-250, Palette.Secondary, out _myFace, out _myName, out _myTime, out _myScore);
            _theirs = Column(250, Palette.Primary, out _theirFace, out _theirName, out _theirTime, out _theirScore);
            var vs = Ui.Title(Safe, "VS", 64, Palette.Accent);
            Ui.Place(vs.rectTransform, 0.5f, 1, 0, -560, 160, 90);

            var coinRow = Ui.Glass(Safe, "Pieces");
            Ui.Place(coinRow.rectTransform, 0.5f, 1, 0, -1010, 420, 120);
            var coin = Ui.Icon(coinRow.transform, Art.IconSprite(Icon.Coin), "Piece");
            coin.color = Palette.Accent;
            Ui.Place(coin.rectTransform, 0.5f, 0.5f, -110, 0, 84, 84);
            _coins = Ui.Title(coinRow.transform, "+0", 66, Color.white);
            Ui.Place(_coins.rectTransform, 0.5f, 0.5f, 50, 0, 240, 100);

            var card = Ui.Glass(Safe, "Challenger");
            Ui.Place(card.rectTransform, 0.5f, 1, 0, -1210, 900, 180);
            _challenger = DuelUi.Portrait(card.transform, Palette.Primary, 140);
            Ui.Place((RectTransform)_challenger.transform.parent, 0, 0.5f, 100, 0, 140, 140);
            _wins = Ui.Title(card.transform, "", 44, Color.white, "Victoires");
            Ui.Place(_wins.rectTransform, 0, 0.5f, 200, 30, 660, 60, 0, 0.5f);
            _wins.alignment = TextAnchor.MiddleLeft;
            var bar = Ui.Rect("Jauge", card.transform);
            Ui.Place(bar, 0, 0.5f, 200, -34, 640, 38, 0, 0.5f);
            _winsFill = MetaUi.Bar(bar, Art, Palette.Primary);
            Ui.Stretch((RectTransform)_winsFill.parent);

            _rematch = Ui.Button(Safe, Loc.Get("duel_rematch"), Palette.Play, App.DuelRematch, Icon.Replay, 60, "Revanche");
            Ui.Place((RectTransform)_rematch.transform, 0.5f, 0, 0, 300, 860, 180, 0.5f, 0);
            var home = Ui.Button(Safe, Loc.Get("home"), Palette.Muted, App.DuelHome, Icon.Home, 50, "Accueil");
            Ui.Place((RectTransform)home.transform, 0.5f, 0, 0, 120, 560, 140, 0.5f, 0);
        }

        RectTransform Column(float x, Color ring, out Image face, out Text name, out Text time, out Text score)
        {
            var col = Ui.Rect("Joueur", Safe);
            Ui.Place(col, 0.5f, 1, x, -640, 440, 360);
            face = DuelUi.Portrait(col, ring, 190);
            Ui.Place((RectTransform)face.transform.parent, 0.5f, 1, 0, -100, 190, 190);
            name = Ui.Title(col, "", 44, Color.white, "Nom");
            Ui.Place(name.rectTransform, 0.5f, 1, 0, -225, 420, 60);
            time = Ui.Title(col, "", 52, Palette.Accent, "Temps");
            Ui.Place(time.rectTransform, 0.5f, 1, 0, -290, 420, 70);
            score = Ui.Title(col, "", 40, ArtCache.Hex("#BFE0FF"), "Score");
            Ui.Place(score.rectTransform, 0.5f, 1, 0, -350, 420, 56);
            return col;
        }

        public override void Show()
        {
            base.Show();
            _t = 0f;
        }

        public override void Refresh()
        {
            var d = App.Duel;
            var o = d.Outcome;
            _title.text = Loc.Get(o == DuelOutcome.Win ? "duel_win" : o == DuelOutcome.Lose ? "duel_lose" : "duel_draw");
            _title.color = o == DuelOutcome.Win ? Palette.Accent : o == DuelOutcome.Lose ? ArtCache.Hex("#BFE0FF") : Color.white;
            _rays.gameObject.SetActive(o != DuelOutcome.Lose);
            string sub;
            if (d.ByForfeit) sub = o == DuelOutcome.Win ? Loc.Format("duel_forfeit_sub", d.TheirName) : Loc.Get("duel_gave_up_sub");
            else sub = Loc.Format(o == DuelOutcome.Win ? "duel_win_sub" : o == DuelOutcome.Lose ? "duel_lose_sub" : "duel_draw_sub", d.TheirName);
            _sub.text = sub;

            _myFace.sprite = Art.Portrait(HeadCatalog.Get(App.Profile.selectedHead));
            _theirFace.sprite = Art.Portrait(HeadCatalog.Get(d.Them != null ? d.Them.Head : 0));
            _myName.text = Loc.Get("duel_you");
            _theirName.text = d.TheirName;
            _myTime.text = Loc.Format("duel_held", DuelUi.Clock(d.MySeconds));
            _theirTime.text = Loc.Format("duel_held", DuelUi.Clock(d.TheirSeconds));
            _myScore.text = Loc.FormatScore(d.MyScore);
            _theirScore.text = Loc.FormatScore(d.TheirScore);
            _coins.text = "+" + d.CoinsWon;

            var p = App.Profile;
            var head = DuelUi.ChallengerHead();
            int need = head != null ? head.UnlockValue : App.Tuning.duel.challengerWins;
            bool done = head != null && p.unlockedHeads.Contains(head.Id);
            if (head != null) _challenger.sprite = done ? Art.Portrait(head) : Art.Locked(head);
            _wins.text = done ? Loc.Get("duel_challenger_done") : Loc.Format("duel_challenger", Mathf.Min(p.duelWins, need), need);
            MetaUi.SetFill(_winsFill, need > 0 ? p.duelWins / (float)need : 1f);

            var label = Ui.ButtonLabel(_rematch);
            if (label != null)
                label.text = d.IWantRematch ? Loc.Format("duel_rematch_wait", d.TheirName)
                    : d.TheyWantRematch ? Loc.Format("duel_rematch_asked", d.TheirName) : Loc.Get("duel_rematch");
            _rematch.interactable = !d.IWantRematch;
        }

        public override void Tick(float dt)
        {
            _t += dt;
            // Le gagnant se soulève un peu, le perdant s'efface légèrement.
            var o = App.Duel.Outcome;
            float bob = Mathf.Sin(_t * 3f) * 8f;
            _mine.anchoredPosition = new Vector2(_mine.anchoredPosition.x, -640 + (o == DuelOutcome.Win ? 20f + bob : 0f));
            _theirs.anchoredPosition = new Vector2(_theirs.anchoredPosition.x, -640 + (o == DuelOutcome.Lose ? 20f + bob : 0f));
            float pop = Mathf.Clamp01(_t / 0.35f);
            _title.rectTransform.localScale = Vector3.one * (1f + 0.4f * (1f - pop) * (1f - pop));
        }
    }
}
