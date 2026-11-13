"""
Aperçus des écrans de la 1.5 (sans Unity), avec les mêmes positions que le code de l'interface
(UI/DuelScreens.cs, UI/LeagueScreens.cs, UI/Screens.cs) :
- duel : entrée avec la carte de ligue et « Duel classé », recherche d'un adversaire, fantôme proposé ;
- ligue : classement par division, fin de semaine, résultat d'un duel classé ;
- amis : profil d'un joueur du classement, demande d'ami.

  dotnet run --project Tools/ArtPreview -c Release -- Exports/art
  python3 Tools/screens_preview_v15.py Exports/art docs/apercu
"""
import os
import sys

from PIL import Image, ImageDraw

ART, OUT = sys.argv[1], sys.argv[2]
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import screens_preview_v14 as v14  # noqa: E402  (relit sys.argv : mêmes arguments)

sp = v14.sp
W, H = sp.W, sp.H
at, child, c, hexc, disc, portrait, glow, rays, masked = v14.at, v14.child, v14.c, v14.hexc, v14.disc, v14.portrait, v14.glow, v14.rays, v14.masked
INK, MUTED = v14.INK, v14.MUTED
SOFT, SKY, CREAM = sp.rgb("#D8E9FF"), sp.rgb("#BFE0FF"), sp.rgb("#FFE9A8")
NIGHT = "#2B1B54"

# Divisions (LeagueUi) : couleur, couleur foncée, symbole, nom.
DIV = [
    ("#FFC93C", "#B8720A", "Crown", "Légende"),
    ("#B07BFF", "#5B2DB5", "Star", "Élite"),
    ("#4AA8FF", "#1B5FB8", "Swords", "Challenger"),
    ("#8FA3BF", "#4A5A75", "Flame", "Espoir"),
]
MEDALS = ("#FFC93C", "#C9D3E3", "#E08A4F")


def badge(img, box, d, halo=True):
    """LeagueUi.MakeBadge : halo, disque foncé, anneau de la couleur de la division, symbole blanc."""
    tint, deep, symbol, _ = DIV[d]
    l, t, r, b = box
    size = r - l
    if halo:
        g = size * 0.25
        glow(img, (l - g, t - g, r + g, b + g), sp.rgb(tint) + (115,), blur=int(size * 0.3))
    disc(img, box, sp.rgb(deep))
    ImageDraw.Draw(img).ellipse(box, outline=sp.rgb(tint), width=max(6, int(size * 0.075)))
    k = size * 0.22
    sp.tinted_icon(img, symbol, (l + k, t + k, r - k, b - k), "#FFFFFF")


def league_card(img, box, d, line, reset):
    """Carte « ma ligue » de l'entrée du duel (DuelLobbyScreen.BuildChoice)."""
    sp.card(img, box, tint=hexc(NIGHT))
    badge(img, child(box, 0, 0.5, 106, 0, 140, 140), d)
    sp.title(img, "Division " + DIV[d][3], child(box, 0, 0.5, 200, 46, 500, 64, 0, 0.5), 50, color=sp.rgb(DIV[d][0]), align="lm")
    sp.title(img, line, child(box, 0, 0.5, 200, -8, 500, 50, 0, 0.5), 36, color=CREAM, align="lm")
    sp.title(img, reset, child(box, 0, 0.5, 200, -56, 500, 40, 0, 0.5), 28, color=SKY, align="lm")
    sp.button(img, child(box, 1, 0.5, -24, 0, 270, 124, 1, 0.5), "Accent", "Classement", "Trophy", 38)


def duel_choice():
    s = v14.backdrop(200)
    img = s.img
    v14.back_and_header(img, "Duel en direct")
    v14.vs_banner(img, 12)
    league_card(img, at(0.5, 1, 0, -722, 1000, 190, 0.5, 1), 1, "4e · 135 points", "Fin de la semaine dans 2 j 5 h")
    sp.button(img, at(0.5, 1, 0, -936, 1000, 180, 0.5, 1), "Play", "DUEL CLASSÉ", "Swords", 70)
    sp.title(img, "Un adversaire trouvé tout seul · +30 points la victoire", at(0.5, 1, 0, -1150, 980, 50), 34, color=SOFT)
    sp.title(img, "Défie un ami", at(0, 1, 50, -1236, 700, 70, 0, 0.5), 52, color=c("Accent"), align="lm")
    sp.button(img, at(1, 1, -40, -1236, 104, 104, 1, 0.5), "Play", None, "Plus", 44)
    top = 1296
    for i, (n, h) in enumerate([("Léa", 14)]):
        v14.friend_row(img, (40, top + i * 146, W - 40, top + i * 146 + 132), n, h, "Défier")
    sp.title(img, "Ou avec un code", at(0.5, 0, 0, 342, 900, 60), 42, color=SKY)
    sp.button(img, at(0.5, 0, -245, 200, 470, 150), "Purple", "Créer un code", "Plus", 50)
    sp.button(img, at(0.5, 0, 245, 200, 470, 150), "Secondary", "J'ai un code", "Play", 50)
    return s


def duel_search(progress=0.45):
    s = v14.backdrop(200)
    img = s.img
    v14.back_and_header(img, "Duel en direct")
    v14.vs_banner(img, 12, None, "Adversaire", ring=True)
    sp.title(img, "Recherche d'un adversaire…", at(0.5, 1, 0, -830, 960, 100), 64)
    sp.title(img, "Duel classé · les points comptent pour ta ligue", at(0.5, 1, 0, -920, 900, 70), 40, color=SOFT)
    sp.bar(img, at(0.5, 1, 0, -1010, 760, 34), progress, "Accent")
    sp.button(img, at(0.5, 0, 0, 90, 520, 130, 0.5, 0), "Muted", "Annuler", "Close", 46)
    return s


def confirm(img, title, body, yes, no, icon="Swords"):
    """Fenêtre de confirmation (ConfirmScreen), posée sur l'écran courant."""
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 150)))
    cb = at(0.5, 0.52, 0, 0, 940, 900)
    sp.card(img, cb)
    db = child(cb, 0.5, 1, 0, -130, 150, 150)
    disc(img, db, c("Play"))
    sp.tinted_icon(img, icon, (db[0] + 30, db[1] + 30, db[2] - 30, db[3] - 30), "#FFFFFF")
    sp.title(img, title, child(cb, 0.5, 1, 0, -270, 860, 90), 60, color=INK, outline=False)
    y = cb[1] + 340
    for ln in v14.wrap(img, body, 800, 40):
        sp.title(img, ln, (cb[0] + 60, y, cb[2] - 60, y + 52), 40, color=MUTED, outline=False)
        y += 54
    sp.button(img, child(cb, 0.5, 0, 0, 250, 780, 150), "Play", yes, icon, 50)
    sp.button(img, child(cb, 0.5, 0, 0, 100, 520, 120), "Muted", no, None, 46)


def duel_ghost_offer():
    s = duel_search(1.0)
    confirm(s.img, "Personne pour l'instant",
            "Affronte le fantôme de Léa : sa vraie partie, rejouée avec les mêmes poids et les mêmes rafales que toi. Moins de points, mais ça compte !",
            "Affronter le fantôme", "Plus tard")
    return s


# ============================================================================ Ligue

def section(img, box, d, first, last):
    tint, deep, _, name = DIV[d]
    sp.pill(img, box, color=sp.rgb(deep) + (255,))
    badge(img, child(box, 0, 0.5, 56, 0, 72, 72), d, halo=False)
    sp.title(img, name, child(box, 0, 0.5, 110, 0, 340, 64, 0, 0.5), 46, color=sp.rgb(tint), align="lm")
    coins = (500, 250, 100, 0)[d]
    right = f"Places {first} à {last} · {coins} pièces lundi" if last else f"À partir de la {first}e place"
    sp.title(img, right, child(box, 1, 0.5, -30, 0, 520, 60, 1, 0.5), 30, color=(245, 245, 250), align="rm", outline=False)


def league_row(img, box, d, rank, name, pts, head, me=False):
    legend = d == 0
    sp.card(img, box, tint=hexc("#FFE9A8") if me else None)
    eb = child(box, 0, 0.5, 12, 0, 14, 112 if legend else 92, 0, 0.5)
    ImageDraw.Draw(img).rounded_rectangle(eb, 7, fill=sp.rgb(DIV[d][0]))
    if rank <= 3:
        mb = child(box, 0, 0.5, 44, 0, 92, 92, 0, 0.5)
        disc(img, mb, sp.rgb(MEDALS[rank - 1]))
        sp.title(img, str(rank), (mb[0], mb[1], mb[2], mb[3] - 4), 50)
    else:
        sp.title(img, str(rank), child(box, 0, 0.5, 34, 0, 112, 100, 0, 0.5), 50, color=c("Primary") if me else sp.rgb("#AEB6C8"))
    face = 128 if legend else 112
    fb = child(box, 0, 0.5, 154, 0, face, face, 0, 0.5)
    img.alpha_composite(sp.load(f"heads/{head:02d}.png").resize((face, face), Image.LANCZOS), (int(fb[0]), int(fb[1])))
    if rank == 1:
        cx, cy = fb[0] + face - 34, fb[1] + 6
        crown = Image.new("RGBA", (68, 56), (0, 0, 0, 0))
        sp.tinted_icon(crown, "Crown", (0, 0, 68, 56), "#FFC93C")
        crown = crown.rotate(-12, expand=True, resample=Image.BICUBIC)
        img.alpha_composite(crown, (int(cx - crown.width / 2 + 34), int(cy - crown.height / 2)))
    sp.title(img, name, child(box, 0, 0.5, 300, 0, 400, 90, 0, 0.5), 46, color=INK, align="lm", outline=False)
    sp.title(img, f"{pts} pts", child(box, 1, 0.5, -34, 0, 250, 90, 1, 0.5), 48, color=INK, align="rm", outline=False)


def free_place(img, box, d, rank):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).rounded_rectangle(box, 40, fill=(255, 255, 255, 40))
    img.alpha_composite(layer)
    sp.title(img, str(rank), child(box, 0, 0.5, 34, 0, 112, 80, 0, 0.5), 46, color=sp.rgb(DIV[d][0]))
    sp.title(img, "Place libre : à toi de la prendre !", child(box, 0, 0.5, 160, 0, 800, 70, 0, 0.5), 38, align="lm")


def league_screen():
    s = v14.backdrop(235)
    img = s.img
    v14.back_and_header(img, "Ligue")
    # Ma division.
    mb = at(0.5, 1, 0, -250, 1000, 250, 0.5, 1)
    sp.card(img, mb, tint=hexc(NIGHT))
    masked(img, mb, lambda layer: rays(layer, child(mb, 0, 0.5, 150, 0, 520, 520), (255, 217, 102, 46)))
    badge(img, child(mb, 0, 0.5, 150, 0, 180, 180), 1)
    sp.title(img, "Division Élite", child(mb, 0, 0.5, 280, 62, 690, 80, 0, 0.5), 64, color=sp.rgb(DIV[1][0]), align="lm")
    sp.title(img, "4e · 135 points", child(mb, 0, 0.5, 280, -4, 690, 60, 0, 0.5), 44, color=CREAM, align="lm")
    sp.title(img, "Fin de la semaine dans 2 j 5 h", child(mb, 0, 0.5, 280, -66, 690, 48, 0, 0.5), 34, color=SKY, align="lm")
    # Récompenses.
    for i, (d, places, coins) in enumerate(((0, "Places 1 à 3", "500"), (1, "Places 4 à 10", "250"), (2, "Places 11 à 30", "100"))):
        cb = at(0.5, 1, (i - 1) * 336, -522, 322, 176, 0.5, 1)
        sp.card(img, cb, tint=hexc(DIV[d][1]))
        tb = child(cb, 0.5, 1, 0, -12, 290, 52, 0.5, 1)
        sp.pill(img, tb, color=sp.rgb(DIV[d][0]) + (255,))
        sp.title(img, DIV[d][3], tb, 34, color=INK, outline=False)
        sp.title(img, places, child(cb, 0.5, 1, 0, -72, 300, 36, 0.5, 1), 28, color=(240, 240, 248), outline=False)
        sp.tinted_icon(img, "Coin", child(cb, 0.5, 0, -66, 36, 46, 46), "#FFD23F")
        sp.title(img, coins, child(cb, 0.5, 0, 44, 36, 150, 52), 44, color=c("Accent"), align="lm")
    # Liste par division.
    y = 720
    left, right = 36, W - 36

    def put(h):
        nonlocal y
        box = (left, y, right, y + h)
        y += h + 12
        return box

    section(img, put(92), 0, 1, 3)
    league_row(img, put(156), 0, 1, "MegaPanda42", 320, 31)
    league_row(img, put(156), 0, 2, "Léa", 262, 14)
    league_row(img, put(156), 0, 3, "TurboBasile", 205, 5)
    section(img, put(92), 1, 4, 10)
    league_row(img, put(136), 1, 4, "SuperBasile", 135, 12, me=True)
    league_row(img, put(136), 1, 5, "Pingouin", 98, 40)
    free_place(img, put(104), 1, 6)
    # Bas de l'écran : le barème (au-dessus de la liste qui défile).
    foot = Image.new("RGBA", (W, 150), (26, 20, 51, 245))
    img.alpha_composite(foot, (0, H - 150))
    sp.title(img, "Victoire +30 · Égalité +15 · Défaite +8 · Fantôme : +10 / +5 / +2", at(0.5, 0, 0, 84, 1000, 90), 30, color=SOFT, outline=False)
    return s


def league_reward():
    s = v14.backdrop(230)
    img = s.img
    cb = at(0.5, 0.52, 0, 0, 960, 1200)
    sp.card(img, cb, tint=hexc(NIGHT))
    masked(img, cb, lambda layer: rays(layer, child(cb, 0.5, 1, 0, -420, 1300, 1300), (255, 217, 102, 76)))
    sp.title(img, "Semaine terminée !", child(cb, 0.5, 1, 0, -110, 880, 110), 78, color=c("Accent"))
    badge(img, child(cb, 0.5, 1, 0, -420, 300, 300), 0)
    sp.title(img, "Division Légende", child(cb, 0.5, 1, 0, -660, 880, 90), 66, color=sp.rgb(DIV[0][0]))
    sp.title(img, "Tu finis 2e", child(cb, 0.5, 1, 0, -750, 880, 110), 46, color=SOFT)
    pile = sp.load("ui/coins3.png") if os.path.exists(os.path.join(ART, "ui/coins3.png")) else None
    pb = child(cb, 0.5, 1, -250, -880, 130, 130)
    if pile is not None:
        img.alpha_composite(pile.resize((130, 130), Image.LANCZOS), (int(pb[0]), int(pb[1])))
    else:
        sp.tinted_icon(img, "Coin", pb, "#FFD23F")
    sp.title(img, "+500 pièces", child(cb, 0.5, 1, 70, -880, 480, 110), 76, color=c("Accent"))
    sp.title(img, "Nouvelle semaine : tout le monde repart de zéro.", child(cb, 0.5, 0, 0, 250, 860, 60), 36, color=SKY, outline=False)
    sp.button(img, child(cb, 0.5, 0, 0, 110, 700, 160), "Play", "Super !", "Check", 58)
    return s


def duel_result_ranked():
    """Résultat d'un duel classé : la bande de ligue remplace celle de la tête Challenger."""
    s = v14.duel_result("win")
    img = s.img
    card_top, card_h = -430, 560
    lb = at(0.5, 1, 0, card_top - card_h - 184, 1000, 150, 0.5, 1)
    # On recouvre l'ancienne bande (même emplacement) par la bande de ligue.
    sp.card(img, lb, tint=hexc(NIGHT))
    badge(img, child(lb, 0, 0.5, 88, 0, 116, 116), 1)
    sp.title(img, "+30 points de ligue", child(lb, 0, 0.5, 172, 30, 560, 64, 0, 0.5), 50, color=c("Accent"), align="lm")
    sp.title(img, "165 points cette semaine  ·  4e", child(lb, 0, 0.5, 172, -32, 560, 46, 0, 0.5), 32, color=SOFT, align="lm")
    sp.button(img, child(lb, 1, 0.5, -20, 0, 240, 110, 1, 0.5), "Accent", "Classement", "Trophy", 34)
    sp.button(img, at(0.5, 0, 0, 300, 860, 180, 0.5, 0), "Play", "Nouveau duel classé", "Replay", 60)
    return s


# ============================================================================ Amis

def player_profile():
    s = v14.leaderboard()
    img = s.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 150)))
    rows = 1
    cb = at(0.5, 0.52, 0, 0, 940, 560 + 350 + rows * 160)
    sp.card(img, cb)
    portrait(img, child(cb, 0.5, 1, 0, -170, 240, 240), c("Primary"), 40)
    sp.title(img, "Pingouin", child(cb, 0.5, 1, 0, -350, 860, 90), 64, color=INK, outline=False)
    sp.title(img, "4e · Record : 11 420", child(cb, 0.5, 1, 0, -430, 860, 64), 44, color=MUTED, outline=False)
    sp.button(img, child(cb, 0.5, 0, 0, 420, 780, 140), "Play", "Ajouter en ami", "Plus", 52)
    sp.button(img, child(cb, 0.5, 0, -198, 262, 384, 120), "Dark", "Signaler", "Flag", 42)
    sp.button(img, child(cb, 0.5, 0, 198, 262, 384, 120), "Dark", "Bloquer", "Close", 42)
    sp.button(img, child(cb, 0.5, 0, 0, 110, 780, 130), "Muted", "Annuler", "Close", 50)
    return s


def friend_request():
    s = v14.backdrop(215)
    img = s.img
    cb = at(0.5, 0.52, 0, 0, 940, 1120)
    sp.card(img, cb)
    portrait(img, child(cb, 0.5, 1, 0, -190, 260, 260), c("Secondary"), 14)
    sp.tinted_icon(img, "Heart", child(cb, 0.5, 1, 120, -290, 96, 96), sp.COL["Primary"])
    sp.title(img, "Léa veut t'ajouter en ami", child(cb, 0.5, 1, 0, -420, 860, 150), 58, color=INK, outline=False)
    y = cb[1] + 520
    for ln in v14.wrap(img, "Accepte pour voir ses scores et vous défier en duel.", 800, 42):
        sp.title(img, ln, (cb[0] + 50, y, cb[2] - 50, y + 54), 42, color=MUTED, outline=False)
        y += 56
    sp.button(img, child(cb, 0.5, 0, 0, 380, 780, 160), "Play", "Accepter", "Check", 58)
    sp.button(img, child(cb, 0.5, 0, 0, 220, 780, 130), "Secondary", "Refuser", None, 50)
    sp.button(img, child(cb, 0.5, 0, 0, 84, 520, 110), "Muted", "Plus tard", None, 46)
    return s


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    shots = {
        "duel_classe_entree": duel_choice(), "duel_classe_recherche": duel_search(), "duel_classe_fantome": duel_ghost_offer(),
        "duel_classe_resultat": duel_result_ranked(), "ligue_classement": league_screen(), "ligue_fin_semaine": league_reward(),
        "profil_joueur": player_profile(), "demande_ami": friend_request(),
    }
    for name, s in shots.items():
        s.img.convert("RGB").save(os.path.join(OUT, f"ecran_{name}.png"))
    keys = list(shots)
    for part, names in (("ecrans_duel_classe_1_5", keys[:4]), ("ecrans_ligue_amis_1_5", keys[4:])):
        strip = Image.new("RGB", (W * len(names) + 20 * (len(names) + 1), H + 40), (24, 22, 40))
        for i, k in enumerate(names):
            strip.paste(shots[k].img.convert("RGB"), (20 + i * (W + 20), 20))
        strip.resize((strip.width // 2, strip.height // 2), Image.LANCZOS).save(os.path.join(OUT, part + ".png"))
    print("ok")
