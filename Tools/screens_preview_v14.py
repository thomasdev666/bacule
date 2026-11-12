"""
Aperçus des écrans de la 1.4 (sans Unity), avec les mêmes positions que le code de l'interface :
- duel : entrée (bannière VS, règles, amis à défier, code), attente (code en tuiles), invitation reçue ;
- classement : podium et « Ta place » ;
- premier lancement : pseudo, année de naissance, pubs ;
- boutique : « Gratuit avec une vidéo » (tête et planche exclusives, coffre).

  python3 Tools/screens_preview_v14.py Exports/art docs/apercu
"""
import math
import random
import os
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ART, OUT = sys.argv[1], sys.argv[2]
sys.argv = [sys.argv[0], ART, "/tmp", "rien"]
import scene_preview as sp  # noqa: E402

os.makedirs(OUT, exist_ok=True)
W, H = sp.W, sp.H
COL = sp.COL
INK = sp.rgb(COL["Ink"])
MUTED = sp.rgb(COL["Muted"])


def c(name):
    return sp.rgb(COL[name])


def hexc(h, a=255):
    return sp.rgb(h) + (a,)


def at(ax, ay, x, y, w, h, px=0.5, py=0.5):
    return sp.place(ax, ay, x, y, w, h, px, py)


def child(parent, ax, ay, x, y, w, h, px=0.5, py=0.5):
    """Ui.Place dans un parent (boîte écran)."""
    l, t, r, b = parent
    pw, ph = r - l, b - t
    axp, ayp = l + ax * pw, t + (1 - ay) * ph
    cx = axp + x - (px - 0.5) * w
    cy = ayp - y + (py - 0.5) * h
    return (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)


def backdrop(dim=170):
    s = sp.home_screen()
    s.img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, dim)))
    return s


def disc(img, box, color):
    ImageDraw.Draw(img).ellipse(box, fill=color)


def portrait(img, box, ring, head=None, question=False):
    l, t, r, b = box
    disc(img, box, ring)
    disc(img, (l + 7, t + 7, r - 7, b - 7), c("Paper"))
    if head is not None:
        p = sp.load(f"heads/{head:02d}.png").resize((int(r - l - 20), int(b - t - 20)), Image.LANCZOS)
        img.alpha_composite(p, (int(l + 10), int(t + 8)))
    if question:
        disc(img, (l + 7, t + 7, r - 7, b - 7), ring)
        sp.title(img, "?", (l, t, r, b - 10), int((r - l) * 0.62))


def glow(img, box, color, blur=40):
    l, t, r, b = [int(v) for v in box]
    g = Image.new("RGBA", (r - l + 2 * blur, b - t + 2 * blur), (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse((blur, blur, blur + r - l, blur + b - t), fill=color)
    img.alpha_composite(g.filter(ImageFilter.GaussianBlur(blur / 2)), (l - blur, t - blur))


def rays(img, box, color):
    l, t, r, b = [int(v) for v in box]
    rr = sp.load("ui/rays.png").resize((r - l, b - t))
    rr = ImageChops.multiply(rr, Image.new("RGBA", rr.size, color))
    img.alpha_composite(rr, (l, t))


def masked(img, box, draw, radius=50):
    """Dessine dans une couche découpée au rectangle arrondi <box> (RectMask2D)."""
    l, t, r, b = [int(v) for v in box]
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw(layer)
    m = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m).rounded_rectangle((l + 6, t + 6, r - 6, b - 6), radius, fill=255)
    img.alpha_composite(Image.composite(layer, Image.new("RGBA", (W, H), (0, 0, 0, 0)), m))


def back_and_header(img, text):
    sp.button(img, at(0, 1, 36, -36, 150, 156, 0, 1), "Dark", None, "Back", 60)
    sp.title(img, text, at(0.5, 1, 70, -112, 700, 130), 84)


# ============================================================================ Duel

def vs_banner(img, mine=0, theirs=None, their_name="Ami", ring=False):
    bb = at(0.5, 1, 0, -250, 1000, 450, 0.5, 1)
    sp.card(img, bb, tint=hexc("#2B1B54"))

    def inner(layer):
        d = ImageDraw.Draw(layer)
        l, t, r, b = bb
        cy = (t + b) / 2
        blue = Image.new("RGBA", (720, 900), (74, 155, 255, 140)).rotate(14, expand=True)
        layer.alpha_composite(blue, (int(l - 120 - blue.width / 2 + 360 - 330), int(cy - blue.height / 2)))
        red = Image.new("RGBA", (720, 900), (255, 90, 95, 140)).rotate(14, expand=True)
        layer.alpha_composite(red, (int(r + 120 - red.width / 2 - 360 + 330), int(cy - red.height / 2)))
        rays(layer, child(bb, 0.5, 0.5, 0, 10, 900, 900), (255, 217, 102, 90))
        _ = d
    masked(img, bb, inner)
    portrait(img, child(bb, 0.5, 0.5, -300, 56, 220, 220), c("Secondary"), mine)
    sp.title(img, "TOI", child(bb, 0.5, 0.5, -300, -92, 320, 60), 46, color=sp.rgb("#BFE0FF"))
    if ring:
        rb = child(bb, 0.5, 0.5, 300, 56, 262, 262)
        ImageDraw.Draw(img).arc(rb, 40, 290, fill=c("Accent"), width=16)
    portrait(img, child(bb, 0.5, 0.5, 300, 56, 220, 220), c("Primary"), theirs, question=theirs is None)
    sp.title(img, their_name, child(bb, 0.5, 0.5, 300, -92, 340, 60), 46, color=sp.rgb("#FFC2C4"))
    glow(img, child(bb, 0.5, 0.5, 0, 50, 300, 300), (255, 204, 77, 150))
    sp.title(img, "VS", child(bb, 0.5, 0.5, 0, 50, 260, 170), 130, color=c("Accent"))
    sp.title(img, "1 contre 1 · en direct · 3 minutes max", child(bb, 0.5, 0, 0, 22, 940, 54, 0.5, 0), 38)


def friend_row(img, box, name, head, action, color="Play"):
    sp.pill(img, box, color=(255, 255, 255, 230))
    portrait(img, child(box, 0, 0.5, 74, 0, 108, 108), c("Secondary"), head)
    sp.title(img, name, (box[0] + 150, box[1], box[2] - 320, box[3]), 44, color=INK, align="lm", outline=False)
    sp.button(img, child(box, 1, 0.5, -16, 0, 290, 108, 1, 0.5), color, action, "Swords", 40)


def duel_choice():
    s = backdrop(200)
    img = s.img
    back_and_header(img, "Duel en direct")
    vs_banner(img, 12)
    challenger_strip(img, at(0.5, 1, 0, -722, 1000, 140, 0.5, 1), 4, 9)
    for i, (ic, col, text) in zip((-1, 0, 1), (("Swords", "Accent", "Mêmes poids pour vous deux"), ("Film", "Secondary", "Tu vois sa planche en direct"), ("Flag", "Primary", "Le premier qui lâche a perdu"))):
        cb = at(0.5, 1, i * 336, -957, 322, 150)
        sp.pill(img, cb)
        db = child(cb, 0.5, 1, 0, -38, 64, 64)
        disc(img, db, c(col))
        sp.tinted_icon(img, ic, (db[0] + 12, db[1] + 12, db[2] - 12, db[3] - 12), "#FFFFFF")
        lines = text.split(" ", 2)
        t = " ".join(lines[:2]) + "\n" + (lines[2] if len(lines) > 2 else "")
        tb = child(cb, 0.5, 0, 0, 12, 300, 72, 0.5, 0)
        for k, ln in enumerate(t.split("\n")):
            sp.title(img, ln, (tb[0], tb[1] + k * 36 - 4, tb[2], tb[1] + k * 36 + 32), 30)
    sp.title(img, "Défie un ami", at(0, 1, 50, -1094, 700, 70, 0, 0.5), 52, color=c("Accent"), align="lm")
    sp.button(img, at(1, 1, -40, -1094, 110, 110, 1, 0.5), "Play", None, "Plus", 44)
    friends = [("Léa", 14), ("Tom", 33)]
    top = 1158
    for i, (n, h) in enumerate(friends):
        rb = (40, top + i * 146, W - 40, top + i * 146 + 132)
        friend_row(img, rb, n, h, "Défier")
    sp.title(img, "Ou avec un code", at(0.5, 0, 0, 342, 900, 60), 42, color=sp.rgb("#BFE0FF"))
    sp.button(img, at(0.5, 0, -245, 200, 470, 150), "Purple", "Créer un code", "Plus", 50)
    sp.button(img, at(0.5, 0, 245, 200, 470, 150), "Secondary", "J'ai un code", "Play", 50)
    return s


def challenger_strip(img, gb, wins, duels):
    """Bandeau « Victoires : 4 / 15 » vers la tête Challenger (entrée et fin du duel)."""
    sp.pill(img, gb)
    portrait(img, child(gb, 0, 0.5, 84, 0, 116, 116), c("Primary"), 62)
    sp.title(img, f"Victoires : {wins} / 15", child(gb, 0, 0.5, 170, 36, 790, 54, 0, 0.5), 42, align="lm")
    sp.bar(img, child(gb, 0, 0.5, 170, -6, 780, 30, 0, 0.5), wins / 15, "Primary")
    sp.title(img, f"{wins} victoires · {duels} duels", child(gb, 0, 0.5, 170, -44, 790, 44, 0, 0.5), 32, color=sp.rgb("#BFE0FF"), align="lm")


def duel_result(outcome="win", by_time=False):
    """Fin du duel (DuelResultScreen 1.4) : titre, carte VS arrondie, couronne du gagnant, pièces, Challenger."""
    s = backdrop(235)
    img = s.img
    if outcome != "lose":
        rays(img, at(0.5, 1, 0, -260, 1400, 1400), (255, 230, 128, 90))
    title = {"win": "Victoire !", "lose": "Défaite", "draw": "Égalité !"}[outcome]
    tcol = {"win": c("Accent"), "lose": sp.rgb("#BFE0FF"), "draw": (255, 255, 255)}[outcome]
    sp.title(img, title, at(0.5, 1, 0, -190, 1000, 170), 130, color=tcol)
    if by_time:
        sub = {"win": "Personne n'a lâché : ton score bat celui de Léa !", "lose": "Personne n'a lâché : Léa a le meilleur score.", "draw": "Personne n'a lâché, et même score !"}[outcome]
    else:
        sub = {"win": "Tu as tenu plus longtemps que Léa !", "lose": "Léa a tenu plus longtemps.", "draw": "Lâchés ensemble, même score !"}[outcome]
    sp.title(img, sub, at(0.5, 1, 0, -308, 980, 70), 46 if len(sub) < 40 else 40)
    if by_time:
        tg = at(0.5, 1, 0, -380, 560, 64)
        sp.pill(img, tg, color=sp.rgb("#FF8A3D") + (255,))
        sp.title(img, "Temps écoulé · 3:00", tg, 34)
    cb = at(0.5, 1, 0, -430, 1000, 560, 0.5, 1)
    sp.card(img, cb, tint=hexc("#2B1B54"))

    def inner(layer):
        l, t, r, b = cb
        cy = (t + b) / 2
        blue = Image.new("RGBA", (720, 1000), (74, 155, 255, 140)).rotate(14, expand=True)
        layer.alpha_composite(blue, (int(l - 120 - blue.width / 2 + 360 - 330), int(cy - blue.height / 2)))
        red = Image.new("RGBA", (720, 1000), (255, 90, 95, 140)).rotate(14, expand=True)
        layer.alpha_composite(red, (int(r + 120 - red.width / 2 - 360 + 330), int(cy - red.height / 2)))
        rays(layer, child(cb, 0.5, 1, 0, -165, 900, 900), (255, 217, 102, 76))
    masked(img, cb, inner)
    me_win, them_win = outcome == "win", outcome == "lose"
    times = ("3:00", "3:00") if by_time else (("2:14", "2:11") if outcome == "win" else ("2:11", "2:14") if outcome == "lose" else ("1:52", "1:52"))
    scores = ("6 420", "5 980") if outcome == "win" else ("5 980", "6 420") if outcome == "lose" else ("5 120", "5 120")
    for x, head, name, ncol, ring, win, lose, sc, tm in (
            (-250, 12, "TOI", "#BFE0FF", "Secondary", me_win, them_win, scores[0], times[0]),
            (250, 14, "Léa", "#FFC2C4", "Primary", them_win, me_win, scores[1], times[1])):
        col = child(cb, 0.5, 1, x, 10 if win else 0, 440, 560, 0.5, 1)
        if win:
            glow(img, child(col, 0.5, 1, 0, -165, 360, 360), (255, 209, 77, 130), 50)
        size = int(210 * (0.9 if lose else 1))
        pb = child(col, 0.5, 1, 0, -165, size, size)
        portrait(img, pb, c(ring), head)
        if lose:
            # Le perdant pâlit (dans le jeu : portrait teinté gris-bleu).
            veil = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            ImageDraw.Draw(veil).ellipse((pb[0] + 7, pb[1] + 7, pb[2] - 7, pb[3] - 7), fill=(70, 70, 110, 90))
            img.alpha_composite(veil)
        if win:
            cr = Image.new("RGBA", (134, 134), (0, 0, 0, 0))
            sp.tinted_icon(cr, "Crown", (5, 5, 129, 129), "Accent")
            cr = cr.rotate(14 if x < 0 else -14, resample=Image.BICUBIC)
            cbx = child(col, 0.5, 1, -34 if x < 0 else 34, -50, 134, 134)
            img.alpha_composite(cr, (int(cbx[0]), int(cbx[1])))
        sp.title(img, name, child(col, 0.5, 1, 0, -305, 420, 56), 44, color=sp.rgb(ncol))
        sp.title(img, sc, child(col, 0.5, 1, 0, -392, 420, 100), 84, color=c("Accent") if win else sp.rgb("#D8E9FF") if lose else (255, 255, 255))
        sp.title(img, f"Tenu {tm}", child(col, 0.5, 1, 0, -478, 420, 50), 34, color=sp.rgb("#D8E9FF"))
    glow(img, child(cb, 0.5, 1, 0, -165, 260, 260), (255, 204, 77, 140))
    sp.title(img, "VS", child(cb, 0.5, 1, 0, -165, 220, 140), 104, color=c("Accent"))
    coins = {"win": 60, "lose": 25, "draw": 40}[outcome]
    pr = at(0.5, 1, 0, -1082, 460, 124)
    sp.pill(img, pr)
    img.alpha_composite(sp.load("ui/coins_3.png").resize((120, 120), Image.LANCZOS), (int((pr[0] + pr[2]) / 2 - 120 - 60), int((pr[1] + pr[3]) / 2 - 62)))
    sp.title(img, f"+{coins}", child(pr, 0.5, 0.5, 60, 0, 260, 100), 70, color=c("Accent"))
    challenger_strip(img, at(0.5, 1, 0, -1174, 1000, 140, 0.5, 1), 5 if outcome == "win" else 4, 10)
    sp.button(img, at(0.5, 0, 0, 300, 860, 180, 0.5, 0), "Play", "Revanche", "Replay", 60)
    sp.button(img, at(0.5, 0, -232, 120, 440, 140, 0.5, 0), "Muted", "Accueil", "Home", 50)
    sp.button(img, at(0.5, 0, 232, 120, 440, 140, 0.5, 0), "Purple", "Inviter un ami", "Share", 46)
    if outcome == "win":
        rnd = random.Random(4)
        cols = [c("Accent"), c("Primary"), c("Secondary"), c("Play"), c("Purple")]
        for _ in range(60):
            x, y = rnd.uniform(40, W - 40), rnd.uniform(60, 900)
            piece = Image.new("RGBA", (22, 12), cols[rnd.randrange(len(cols))] + (255,)).rotate(rnd.uniform(0, 180), expand=True)
            img.alpha_composite(piece, (int(x), int(y)))
    return s


def duel_hosting():
    s = backdrop(200)
    img = s.img
    back_and_header(img, "Duel en direct")
    vs_banner(img, 12, 14, "Léa", ring=True)
    sp.title(img, "Ton duel est prêt !", at(0.5, 1, 0, -775, 960, 90), 64, color=c("Accent"))
    sp.title(img, "Ton code de duel", at(0.5, 1, 0, -855, 900, 60), 42, color=sp.rgb("#BFE0FF"))
    tb = at(0.5, 1, 0, -980, 960, 170)
    code = "K7QM2X"
    n, tile = 6, 140
    x0 = -(n * tile + (n - 1) * 14) / 2 + tile / 2
    for i, ch in enumerate(code):
        gap = -12 if i < 3 else 12
        b = child(tb, 0.5, 0.5, x0 + i * (tile + 14) + gap, 0, tile, 160)
        sp.card(img, b)
        sp.title(img, ch, (b[0], b[1] + 6, b[2], b[3] - 18), 104, color=INK, outline=False)
    sp.button(img, at(0.5, 1, 0, -1165, 900, 170), "Play", "Envoyer l'invitation", "Share", 58)
    sp.button(img, at(0.5, 1, -228, -1340, 444, 140), "Secondary", "Copier le code", "Check", 44)
    sp.button(img, at(0.5, 1, 228, -1340, 444, 140), "Purple", "Mes amis", "Face", 44)
    pb = at(0.5, 1, 0, -1475, 820, 86)
    sp.pill(img, pb, color=c("Purple") + (255,))
    sp.title(img, "Invitation envoyée à Léa", pb, 40)
    sp.button(img, at(0.5, 0, 0, 90, 520, 130, 0.5, 0), "Muted", "Annuler", "Close", 46)
    return s


def duel_invite():
    s = sp.home_screen()
    img = s.img
    img.alpha_composite(Image.new("RGBA", (W, H), (20, 13, 46, 230)))
    cb = at(0.5, 0.5, 0, 40, 980, 1180)
    sp.card(img, cb, tint=hexc("#2B1B54"))
    masked(img, cb, lambda layer: rays(layer, child(cb, 0.5, 1, 0, -380, 1300, 1300), (255, 217, 102, 82)))
    sp.tinted_icon(img, "Swords", child(cb, 0.5, 1, 0, -110, 130, 130), COL["Accent"])
    portrait(img, child(cb, 0.5, 1, -230, -380, 250, 250), c("Primary"), 14)
    portrait(img, child(cb, 0.5, 1, 230, -380, 250, 250), c("Secondary"), 12)
    sp.title(img, "VS", child(cb, 0.5, 1, 0, -380, 200, 130), 96, color=c("Accent"))
    sp.title(img, "Léa te défie !", child(cb, 0.5, 1, 0, -600, 920, 110), 70)
    sub = child(cb, 0.5, 1, 0, -700, 880, 110)
    sp.title(img, "Duel en direct · mêmes poids", (sub[0], sub[1], sub[2], sub[1] + 55), 38, color=sp.rgb("#D8E9FF"), outline=False)
    sp.title(img, "le premier qui lâche a perdu", (sub[0], sub[1] + 50, sub[2], sub[1] + 105), 38, color=sp.rgb("#D8E9FF"), outline=False)
    sp.button(img, child(cb, 0.5, 0, 0, 220, 800, 170), "Play", "J'accepte !", "Swords", 62)
    sp.button(img, child(cb, 0.5, 0, 0, 70, 520, 120), "Muted", "Pas maintenant", None, 46)
    return s


# ============================================================================ Classement

def leaderboard():
    s = backdrop(200)
    img = s.img
    back_and_header(img, "Classement")
    bar = at(0.5, 1, 0, -270, 1010, 120)
    sp.pill(img, bar)
    for i, name in enumerate(("Monde", "Jour", "Amis")):
        tb = child(bar, 0, 0.5, 10 + i * 330, 0, 324, 100, 0, 0.5)
        if i == 0:
            sp.pill(img, tb, color=c("Primary") + (255,))
        sp.title(img, name, tb, 40, color=(255, 255, 255) if i == 0 else sp.rgb("#D8E9FF"))
    # Podium (premier élément de la liste, 590 de haut).
    area_top = 350
    hb = (36, area_top, W - 36, area_top + 590)
    xs, hs = (0, -330, 330), (250, 200, 170)
    medals = ("#FFC93C", "#C9D3E3", "#E08A4F")
    data = [("MegaPanda42", "18 450", 31), ("Léa", "15 200", 14), ("TurboBasile", "12 980", 5)]
    for i in (1, 2, 0):
        x, h, size = xs[i], hs[i], (230 if i == 0 else 190)
        name, score, head = data[i]
        blk = child(hb, 0.5, 0, x, 0, 312, h, 0.5, 0)
        sp.card(img, blk, tint=hexc(medals[i]))
        sp.title(img, name, child(blk, 0.5, 1, 0, -54, 290, 56), 40)
        sp.title(img, score, child(blk, 0.5, 1, 0, -112, 290, 54), 40, color=sp.rgb("#FFF5D6"))
        rb = child(hb, 0.5, 0, x, h + size * 0.5 - 14, size, size)
        portrait(img, rb, sp.rgb(medals[i]), head)
        bd = child(rb, 1, 0, -18, 18, 76, 76)
        disc(img, bd, sp.rgb(medals[i]))
        ImageDraw.Draw(img).ellipse(bd, outline=(255, 255, 255), width=6)
        sp.title(img, str(i + 1), (bd[0], bd[1], bd[2], bd[3] - 4), 44)
        if i == 0:
            sp.tinted_icon(img, "Crown", child(hb, 0.5, 0, x, h + size + 20, 130, 104), "#FFC93C")
    y = area_top + 590 + 14
    rows = [(4, "Pingouin", "11 420", 40), (5, "Nina", "10 060", 22), (6, "Capitaine", "9 870", 47), (7, "Comète", "9 120", 9)]
    for rank, name, score, head in rows:
        rb = (36, y, W - 36, y + 140)
        sp.card(img, rb)
        sp.title(img, str(rank), child(rb, 0, 0.5, 24, 0, 116, 110, 0, 0.5), 50, color=sp.rgb("#AEB6C8"))
        img.alpha_composite(sp.load(f"heads/{head:02d}.png").resize((120, 120), Image.LANCZOS), (int(rb[0] + 146), int(rb[1] + 10)))
        sp.title(img, name, child(rb, 0, 0.5, 286, 0, 330, 100, 0, 0.5), 46, color=INK, align="lm", outline=False)
        sp.title(img, score, child(rb, 1, 0.5, -36, 0, 220, 100, 1, 0.5), 50, color=INK, align="rm", outline=False)
        y += 154
    # Ta place.
    mb = at(0.5, 0, 0, 265, 1008, 136)
    sp.card(img, mb, tint=hexc("#FFE9A8"))
    sp.title(img, "Ta place", child(mb, 0, 1, 34, -8, 200, 36, 0, 1), 30, color=MUTED, align="lm", outline=False)
    sp.title(img, "34e", child(mb, 0, 0.5, 30, -24, 190, 66, 0, 0.5), 56, color=c("Primary"), align="lm")
    img.alpha_composite(sp.load("heads/12.png").resize((116, 116), Image.LANCZOS), (int(mb[0] + 220), int(mb[1] + 10)))
    sp.title(img, "SuperBasile", child(mb, 0, 0.5, 350, 0, 380, 90, 0, 0.5), 46, color=INK, align="lm", outline=False)
    sp.title(img, "6 310", child(mb, 1, 0.5, -34, 0, 260, 90, 1, 0.5), 52, color=INK, align="rm", outline=False)
    sp.button(img, at(0.5, 0, 0, 100, 760, 140), "Secondary", "SuperBasile", "Face", 48)
    return s


# ============================================================================ Premier lancement (OnboardScreen)

SOFT, SKY = sp.rgb("#D8E9FF"), sp.rgb("#BFE0FF")


def wrap(img, text, width, size):
    words, lines, cur = text.split(" "), [], ""
    for w in words:
        if ImageDraw.Draw(img).textlength(cur + " " + w, font=sp.font(size)) > width and cur:
            lines.append(cur)
            cur = w
        else:
            cur = (cur + " " + w).strip()
    lines.append(cur)
    return lines


def onboard_base(title, sub, step, steps=3):
    s = backdrop(170)
    img = s.img
    cb = at(0.5, 0.5, 0, 20, 980, 1560)
    sp.card(img, cb, tint=hexc("#2B1B54"))
    masked(img, cb, lambda layer: rays(layer, child(cb, 0.5, 1, 0, -500, 1400, 1400), (255, 217, 102, 66)))
    dots = child(cb, 0.5, 1, 0, -62, 300, 34)
    total = sum(34 if i == step else 24 for i in range(steps)) + 22 * (steps - 1)
    x = (dots[0] + dots[2]) / 2 - total / 2
    cy = (dots[1] + dots[3]) / 2
    for i in range(steps):
        d = 34 if i == step else 24
        disc(img, (x, cy - d / 2, x + d, cy + d / 2), c("Accent") if i == step else (255, 255, 255, 64))
        x += d + 22
    sp.title(img, title, child(cb, 0.5, 1, 0, -146, 920, 110), 76 if len(title) < 22 else 62, color=c("Accent"))
    sb = child(cb, 0.5, 1, 0, -250, 880, 100)
    lines = wrap(img, sub, 860, 40)
    y0 = (sb[1] + sb[3]) / 2 - len(lines) * 24
    for k, ln in enumerate(lines):
        sp.title(img, ln, (sb[0], y0 + k * 48, sb[2], y0 + 48 + k * 48), 40, color=SOFT, outline=False)
    return s, img, cb


def onboard_pseudo():
    s, img, cb = onboard_base("Ton pseudo", "Il s'affiche dans les classements et pendant les duels.", 0)
    glow(img, child(cb, 0.5, 1, 0, -500, 460, 460), (255, 199, 89, 128), 60)
    portrait(img, child(cb, 0.5, 1, 0, -500, 270, 270), c("Secondary"), 0)
    tb = child(cb, 0.5, 1, 0, -682, 460, 84)
    sp.pill(img, tb, color=c("Primary") + (255,))
    sp.title(img, "MegaPanda42", tb, 42)
    fb = child(cb, 0.5, 1, -84, -862, 720, 146)
    sp.pill(img, fb, color=sp.OUTLINE + (255,))
    sp.pill(img, (fb[0] + 7, fb[1] + 7, fb[2] - 7, fb[3] - 7), color=(255, 255, 255, 255))
    sp.title(img, "MegaPanda42", fb, 58, color=INK, outline=False)
    sp.button(img, child(cb, 0.5, 1, 386, -862, 150, 150), "Purple", None, "Replay", 60)
    rb = child(cb, 0.5, 1, 0, -1062, 860, 96)
    for k, ln in enumerate(wrap(img, "Pas ton vrai nom, pas d'insulte. Le dé te donne une idée.", 840, 36)):
        sp.title(img, ln, (rb[0], rb[1] + 4 + k * 44, rb[2], rb[1] + 48 + k * 44), 36, color=SKY, outline=False)
    sp.button(img, child(cb, 0.5, 0, 0, 210, 780, 166), "Play", "Valider", "Check", 62)
    sp.button(img, child(cb, 0.5, 0, 0, 66, 460, 112), "Muted", "Plus tard", None, 44)
    return s


def arrow_button(img, box, up):
    sp.button(img, box, "Purple", None, None, 60)
    l, t, r, b = box
    ic = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    sp.tinted_icon(ic, "Back", (0, 0, 64, 64), "#FFFFFF")
    ic = ic.rotate(-90 if up else 90, resample=Image.BICUBIC)
    img.alpha_composite(ic, (int((l + r) / 2 - 32), int((t + 6 + b - 20) / 2 - 32)))


def onboard_age(pos=15):
    """Année de naissance (OnboardScreen 1.4) : roue qu'on fait défiler, « Fais défiler » au départ (pos = 0)."""
    s, img, cb = onboard_base("Ton année de naissance", "Pour te montrer des pubs adaptées à ton âge.", 1)
    ab = child(cb, 0.5, 1, 0, -350, 640, 760, 0.5, 1)
    sp.pill(img, ab)
    band = child(ab, 0.5, 0.5, 0, 0, 590, 118)
    sp.pill(img, band, color=c("Secondary") + (255,))
    items = ["Fais défiler"] + [str(y) for y in range(2022, 1919, -1)]
    cx, cy = (ab[0] + ab[2]) / 2, (ab[1] + ab[3]) / 2
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    row_h, drum = 110, 2.8
    for i in range(pos - 5, pos + 6):
        if i < 0 or i >= len(items):
            continue
        d = pos - i
        ang = max(-1.45, min(1.45, d / drum))
        y = math.sin(ang) * drum * row_h
        a = abs(d)
        sc = 1 - 0.3 * min(1, a / 2.6)
        sy = sc * (1 + (math.cos(ang) - 1) * 0.7)
        k = min(1, a * 1.6)
        col = tuple(int(255 + (SKY[j] - 255) * k) for j in range(3))
        alpha = max(0, min(1, 1.1 - a * 0.34))
        row = Image.new("RGBA", (560, row_h), (0, 0, 0, 0))
        sp.title(row, items[i], (0, 0, 560, row_h), 64 if i > 0 else 56, color=col)
        row = row.resize((max(1, int(560 * sc)), max(1, int(row_h * sy))), Image.LANCZOS)
        row.putalpha(row.split()[3].point(lambda v: int(v * alpha)))
        layer.alpha_composite(row, (int(cx - row.width / 2), int(cy - y - row.height / 2)))
    m = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m).rectangle(ab, fill=255)
    img.alpha_composite(Image.composite(layer, Image.new("RGBA", (W, H), (0, 0, 0, 0)), m))
    arrow_button(img, child(cb, 0.5, 1, 0, -350, 104, 96), True)
    arrow_button(img, child(cb, 0.5, 1, 0, -1110, 104, 96), False)
    nb = child(cb, 0.5, 0, 0, 300, 860, 76)
    sp.title(img, "Elle reste sur ton téléphone et n'est jamais affichée.", nb, 34, color=SKY, outline=False)
    sp.button(img, child(cb, 0.5, 0, 0, 120, 780, 166), "Play" if pos > 0 else "Muted", "Valider", "Check", 62)
    return s


def onboard_ads():
    s, img, cb = onboard_base("Des pubs qui te ressemblent ?", "Bascule est gratuit grâce aux pubs. À toi de choisir lesquelles tu vois.", 2)
    glow(img, child(cb, 0.5, 1, 0, -460, 480, 400), (191, 128, 255, 140), 60)
    tb = child(cb, 0.5, 1, 0, -460, 230, 230)
    disc(img, tb, c("Purple"))
    sp.tinted_icon(img, "Film", child(tb, 0.5, 0.5, 0, 0, 132, 132), "#FFFFFF")
    for ic, col, x, y, size in (("Coin", "Accent", -250, -400, 96), ("Gift", "Play", 250, -410, 96), ("Heart", "Primary", 200, -560, 72)):
        sp.tinted_icon(img, ic, child(cb, 0.5, 1, x, y, size, size), COL[col])
    rows = [("Heart", "Primary", "Accepter : nos partenaires utilisent l'identifiant pub de ton téléphone pour des pubs adaptées."),
            ("Gift", "Play", "Refuser : autant de pubs, mais au hasard. Le jeu reste le même."),
            ("Gear", "Secondary", "Tu peux changer d'avis quand tu veux, dans les réglages.")]
    for i, (ic, col, text) in enumerate(rows):
        rb = child(cb, 0.5, 1, 0, -640 - i * 156, 900, 140, 0.5, 1)
        sp.pill(img, rb)
        db = child(rb, 0, 0.5, 66, 0, 88, 88)
        disc(img, db, c(col))
        sp.tinted_icon(img, ic, (db[0] + 16, db[1] + 16, db[2] - 16, db[3] - 16), "#FFFFFF")
        lines = wrap(img, text, rb[2] - rb[0] - 160, 30)
        size = 36 if len(wrap(img, text, rb[2] - rb[0] - 160, 36)) <= 2 else 32
        lines = wrap(img, text, rb[2] - rb[0] - 160, size)
        y0 = (rb[1] + rb[3]) / 2 - len(lines) * (size + 6) / 2
        for k, ln in enumerate(lines):
            sp.title(img, ln, (rb[0] + 130, y0 + k * (size + 6), rb[2] - 30, y0 + (k + 1) * (size + 6)), size, color=(255, 255, 255), align="lm", outline=False)
    sp.button(img, child(cb, 0.5, 0, -222, 196, 424, 156), "Secondary", "Refuser", "Close", 52)
    sp.button(img, child(cb, 0.5, 0, 222, 196, 424, 156), "Play", "Accepter", "Check", 52)
    sp.button(img, child(cb, 0.5, 0, 0, 62, 460, 100), "Muted", "En savoir plus", None, 40)
    return s


# ============================================================================ Boutique : vidéos

def shop_video():
    s = backdrop(200)
    img = s.img
    back_and_header(img, "Boutique")
    y = 250
    sp.title(img, "Gratuit avec une vidéo", (36, y, W - 36, y + 84), 52, color=c("Accent"), align="lm")
    y += 84
    sp.title(img, "Une vidéo quand tu veux : chacune compte, même plusieurs jours après.", (36, y, W - 36, y + 50), 34, color=sp.rgb("#D8E9FF"), align="lm", outline=False)
    y += 124

    def exclusive(y, h, color, deep, name, desc, prog, wide, picture):
        b = (36, y, W - 36, y + h)
        sp.card(img, b, tint=hexc(color))
        cx = b[0] + (230 if wide else 180)

        def inner(layer):
            glow(layer, (cx - 260, (b[1] + b[3]) / 2 - 10 - 260, cx + 260, (b[1] + b[3]) / 2 - 10 + 260), (255, 140, 217, 115), 60)
            rays(layer, (cx - 360, (b[1] + b[3]) / 2 - 10 - 360, cx + 360, (b[1] + b[3]) / 2 - 10 + 360), (255, 217, 102, 72))
        masked(img, b, inner)
        if wide:
            pic = picture.resize((400, int(400 * picture.height / picture.width)), Image.LANCZOS).rotate(6, expand=True, resample=Image.BICUBIC)
            img.alpha_composite(pic, (int(cx - pic.width / 2), int((b[1] + b[3]) / 2 - 40 - pic.height / 2)))
        else:
            pic = picture.resize((300, 300), Image.LANCZOS)
            img.alpha_composite(pic, (int(cx - 150), int((b[1] + b[3]) / 2 - 20 - 150)))
        tg = child(b, 0, 1, 26, -24, 230, 62, 0, 1)
        sp.pill(img, tg, color=c("Accent") + (255,))
        sp.title(img, "EXCLUSIF", tg, 34)
        x = 450 if wide else 360
        sp.title(img, name, child(b, 0, 1, x, -60, 1000 - x - 40, 80, 0, 1), 58, align="lm")
        db = child(b, 0, 1, x, -146, 1000 - x - 40, 100, 0, 1)
        words, lines, cur = desc.split(" "), [], ""
        for w in words:
            if ImageDraw.Draw(img).textlength(cur + " " + w, font=sp.font(36)) > db[2] - db[0] and cur:
                lines.append(cur)
                cur = w
            else:
                cur = (cur + " " + w).strip()
        lines.append(cur)
        for k, ln in enumerate(lines):
            sp.title(img, ln, (db[0], db[1] + k * 44, db[2], db[1] + 44 + k * 44), 36, color=(255, 255, 255), align="lm", outline=False)
        sp.title(img, f"{prog} / 8 vidéos", child(b, 0, 0, x, 112, 380, 56, 0, 0), 40, color=c("Accent"), align="lm")
        bb = child(b, 0, 0, x, 52, 1000 - x - 330, 44, 0, 0)
        sp.bar(img, bb, prog / 8, "Accent")
        sp.button(img, child(b, 1, 0, -24, 26, 290, 130, 1, 0), "Play", "Regarder", "Film", 44)
        return y + h + 24

    y = exclusive(y, 460, "#A66CFF", "#5A189A", "Superstar", "Tête introuvable ailleurs : 8 vidéos pour la gagner.", 3, False, sp.load("heads/63.png"))
    plank = sp.load("planches/apercu_7.png")
    y = exclusive(y, 400, "#3C096C", "#240046", "Planche Scène étoilée", "Assortie à la Superstar : 8 vidéos.", 0, True, plank)
    cb = (36, y, W - 36, y + 250)
    sp.card(img, cb)
    sp.mini_chest(img, cb[0] + 34, (cb[1] + cb[3]) / 2 + 6, 170)
    sp.title(img, "Coffre gratuit", child(cb, 0, 0.5, 230, 36, 450, 80, 0, 0.5), 52, color=INK, align="lm", outline=False)
    sp.title(img, "Encore 3 aujourd'hui", child(cb, 0, 0.5, 230, -40, 450, 64, 0, 0.5), 40, color=MUTED, align="lm", outline=False)
    sp.button(img, child(cb, 1, 0.5, -30, 0, 220, 150, 1, 0.5), "Play", None, "Film", 50)
    return s


# ============================================================================ Roue, quête, événement, accueil

# SideWidget (LiveOpsScreens.cs) : médaillon rond + dessin en couleur, anneau doré, étiquette sombre, ancré en bas.
WIDGET_W, WIDGET_H, MEDAL, EDGE, ROW1, ROW2 = 156, 162, 112, 88, 555, 365


def side_widget(img, side, y, picture, color, label, ring=None, badge_on=False, wheel=False):
    r = sp.place(0 if side < 0 else 1, 0, EDGE if side < 0 else -EDGE, y, WIDGET_W, WIDGET_H)
    cx = (r[0] + r[2]) / 2
    my = r[1] + 5 + MEDAL / 2 + (6 if ring is not None else 0)
    d = ImageDraw.Draw(img)
    if ring is not None:
        rr = (MEDAL + 20) / 2
        bb = (cx - rr, my - rr, cx + rr, my + rr)
        d.ellipse(bb, outline=(255, 255, 255, 102), width=10)
        d.arc(bb, -90, -90 + 360 * ring, fill=c("Accent"), width=10)
    m = MEDAL / 2
    disc(img, (cx - m, my - m, cx + m, my + m), sp.OUTLINE)
    m2 = (MEDAL - 11) / 2
    disc(img, (cx - m2, my - m2, cx + m2, my + m2), c(color) if color in sp.COL else sp.rgb(color))
    glow(img, (cx - MEDAL * 0.3, my - m2 + 5, cx + MEDAL * 0.3, my - m2 + 5 + MEDAL * 0.3), (255, 255, 255, 70), 10)
    ps = MEDAL - (26 if wheel else 20)
    pic = sp.load(picture).resize((ps, ps), Image.LANCZOS)
    if wheel:
        pic = pic.rotate(14, resample=Image.BICUBIC)
    img.alpha_composite(pic, (int(cx - ps / 2), int(my - ps / 2)))
    if wheel:
        disc(img, (cx - 13, my - 13, cx + 13, my + 13), sp.OUTLINE)
        disc(img, (cx - 9, my - 9, cx + 9, my + 9), c("Accent"))
        pt = Image.new("RGBA", (38, 38), (0, 0, 0, 0))
        sp.tinted_icon(pt, "Play", (0, 0, 38, 38), "Primary")
        pt = pt.rotate(-90)
        img.alpha_composite(pt, (int(cx - 19), int(my - m - 6 - 19)))
    if badge_on:
        bx, by = cx + m - 8, my - m + 8
        disc(img, (bx - 23, by - 23, bx + 23, by + 23), (255, 255, 255))
        disc(img, (bx - 20, by - 20, bx + 20, by + 20), c("Primary"))
        sp.title(img, "!", (bx - 20, by - 20, bx + 20, by + 17), 28)
    lb = (cx - WIDGET_W / 2, r[3] - 46, cx + WIDGET_W / 2, r[3])
    sp.pill(img, lb, color=(31, 18, 71, 224))
    tb = (lb[0] + 10, lb[1] + 3, lb[2] - 10, lb[3] - 2)
    f = 30
    while ImageDraw.Draw(img).textlength(label, font=sp.font(f)) > tb[2] - tb[0] and f > 20:
        f -= 1
    sp.title(img, label, tb, f)


def home_widgets():
    s = sp.home_screen()
    img = s.img
    side_widget(img, -1, ROW1, "ui/wheel_small.png", "#2E2A6B", "Roue", badge_on=True, wheel=True)
    side_widget(img, 1, ROW1, "ui/quest.png", "Success", "Quête 1/3", ring=1 / 3)
    side_widget(img, 1, ROW2, "ui/week.png", "Purple", "Semaine", ring=0.49)
    return s


def balloon_screen():
    """Un 128 s'envole (UI/BalloonBanner.cs) : grande bannière dorée sous la planche, conseil au-dessus."""
    s = sp.world(1)
    sp.mascot_scene(s, angle=4.0, expression="Calm")
    sp.hud(s, angle=4.0)
    img = s.img
    sp.title(img, "Le 128 est le plus gros coup du jeu :\nrefais-en pour exploser ton record !", at(0.5, 0.5, 0, 330, 900, 150), 48)
    rb = at(0.5, 0, 0, 300, 1000, 230)
    lay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sp.pill(lay, (rb[0] - 10, rb[1] - 10 + 14, rb[2] + 10, rb[3] + 10 + 14), color=(64, 20, 5, 115))
    sp.pill(lay, (rb[0] - 10, rb[1] - 10, rb[2] + 10, rb[3] + 10), color=hexc("#D9480F"))
    sp.pill(lay, rb, color=hexc("#FFB703"))
    glow(lay, (rb[0] + 60, rb[1] + 18, rb[2] - 60, rb[1] + 90), (255, 255, 255, 80), 12)
    lay = lay.rotate(2.5, resample=Image.BICUBIC, center=((rb[0] + rb[2]) / 2, (rb[1] + rb[3]) / 2))
    img.alpha_composite(lay)
    bx, by = rb[0] + 120, (rb[1] + rb[3]) / 2 - 30
    b = sp.load("balloon.png").resize((230, 230), Image.LANCZOS)
    img.alpha_composite(b, (int(bx - 115), int(by - 115)))
    sp.title(img, "128", (bx - 80, by - 34 - 30, bx + 80, by - 34 + 30), 46)
    tc = (rb[0] + rb[2]) / 2 + 90
    sp.title(img, "MONTGOLFIÈRE !", (tc - 350, (rb[1] + rb[3]) / 2 - 36 - 55, tc + 350, (rb[1] + rb[3]) / 2 - 36 + 55), 92)
    sp.title(img, "+984 points d'un coup", (tc - 350, (rb[1] + rb[3]) / 2 + 54 - 40, tc + 350, (rb[1] + rb[3]) / 2 + 54 + 40), 62, color=sp.rgb("#FFF4C2"))
    for (x, y) in [(-430, 96), (392, 104), (456, -64), (-300, -100), (150, 118)]:
        cx, cy = (rb[0] + rb[2]) / 2 + x, (rb[1] + rb[3]) / 2 - y
        sp.tinted_icon(img, "Star", (cx - 32, cy - 32, cx + 32, cy + 32), "#FFFFFF")
    return s


def wheel_screen(spun=False):
    s = sp.home_screen()
    img = s.img
    img.alpha_composite(Image.new("RGBA", (W, H), (20, 13, 46, 242)))
    back_and_header(img, "Roue de la chance")
    sp.title(img, "Ton tour gratuit est prêt !", at(0.5, 1, 0, -228, 900, 70), 40, color=SOFT, outline=False)
    size = 860
    hb = at(0.5, 0.5, 0, 70, size, size)
    cx, cy = (hb[0] + hb[2]) / 2, (hb[1] + hb[3]) / 2
    glow(img, (cx - size * 0.62, cy - size * 0.62, cx + size * 0.62, cy + size * 0.62), (255, 199, 77, 120), 80)
    rays(img, (cx - size * 0.85, cy - size * 0.85, cx + size * 0.85, cy + size * 0.85), (255, 217, 102, 77))
    layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    layer.alpha_composite(sp.load("ui/wheel.png").resize((size, size), Image.LANCZOS))
    # Comme LiveOpsScreens.SlotLabel : le lot en image au milieu de la part (IconR), son nom près du bord (TextR).
    icon_r, text_r = size * 0.238, size * 0.37
    labels = [("coins_1", "50", 116), ("chest", "Coffre", 0), ("coins_2", "100", 134), ("Gift", "Surprise", 108),
              ("coins_3", "250", 150), ("Flame", "Série", 108), ("boost", "Boost", 118), ("head65", "JACKPOT", 136)]
    for i, (ic, txt, w) in enumerate(labels):
        arm = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        acx, acy = size / 2, size / 2
        iy = acy - icon_r
        if ic == "chest":
            sp.mini_chest(arm, acx - 62, iy - 56, 124)
        elif ic == "head65":
            arm.alpha_composite(sp.load("heads/65.png").resize((w, w), Image.LANCZOS), (int(acx - w / 2), int(iy - w / 2)))
        elif ic.startswith("coins"):
            arm.alpha_composite(sp.load(f"ui/{ic}.png").resize((w, w), Image.LANCZOS), (int(acx - w / 2), int(iy - 4 - w / 2)))
        elif ic == "boost":
            arm.alpha_composite(sp.load("ui/coins_1.png").resize((w, w), Image.LANCZOS), (int(acx - w / 2), int(iy - 4 - w / 2)))
            sp.title(arm, "×2", (acx - 55, iy - 4 - 6 - 35, acx + 55, iy - 4 - 6 + 35), 56)
        else:
            sp.tinted_icon(arm, ic, (acx - w / 2, iy - w / 2, acx + w / 2, iy + w / 2), "#FFB347" if ic == "Flame" else "#FFFFFF")
        number = ic.startswith("coins")
        tw = 200 if number else 226
        sp.title(arm, txt, (acx - tw / 2, acy - text_r - 31, acx + tw / 2, acy - text_r + 31), 54 if number else 40,
                 color=sp.rgb("#FFD447") if ic == "head65" else (255, 255, 255))
        layer.alpha_composite(arm.rotate(-i * 45, resample=Image.BICUBIC))
    br = size * 0.5 * 0.914
    d = ImageDraw.Draw(layer)
    for i in range(16):
        a = i * math.pi / 8 + math.pi / 16
        bx, by = size / 2 + math.cos(a) * br, size / 2 - math.sin(a) * br
        col = (255, 248, 190) if i % 2 == 0 else (255, 158, 51)
        d.ellipse((bx - 17, by - 17, bx + 17, by + 17), fill=col)
    if spun:
        layer = layer.rotate(4 * 45 - 10, resample=Image.BICUBIC)
    img.alpha_composite(layer, (int(hb[0]), int(hb[1])))
    disc(img, (cx - 100, cy - 100, cx + 100, cy + 100), sp.OUTLINE)
    disc(img, (cx - 88, cy - 88, cx + 88, cy + 88), c("Accent"))
    sp.title(img, "GO", (cx - 88, cy - 88, cx + 88, cy + 82), 56)
    pt = Image.new("RGBA", (130, 130), (0, 0, 0, 0))
    sp.tinted_icon(pt, "Play", (0, 0, 130, 130), "Primary")
    pt = pt.rotate(-90)
    img.alpha_composite(pt, (int(cx - 65), int(hb[1] - 6 - 130 * 0.3 - 40)))
    sp.button(img, at(0.5, 0, 0, 260, 820, 170), "Play", "Tourner la roue !", "Wheel", 60)
    sp.button(img, at(0.5, 0, 0, 100, 420, 110), "Muted", "Chances", None, 40)
    if spun:
        cb = at(0.5, 0.5, 0, 60, 860, 820)
        sp.card(img, cb, tint=hexc("#1E1240"))
        masked(img, cb, lambda layer: rays(layer, child(cb, 0.5, 1, 0, -300, 1100, 1100), (255, 217, 102, 102)))
        sp.title(img, "Tu gagnes…", child(cb, 0.5, 1, 0, -80, 800, 90), 60, color=c("Accent"))
        glow(img, child(cb, 0.5, 1, 0, -300, 420, 420), (255, 217, 102, 160), 60)
        ib = child(cb, 0.5, 1, 0, -300, 260, 260)
        img.alpha_composite(sp.load("ui/coins_3.png").resize((260, 260), Image.LANCZOS), (int(ib[0]), int(ib[1])))
        sp.title(img, "250 pièces", child(cb, 0.5, 1, 0, -500, 800, 100), 68)
        sp.title(img, "Elles sont déjà dans ta tirelire.", child(cb, 0.5, 1, 0, -590, 780, 90), 40, color=SOFT, outline=False)
        sp.button(img, child(cb, 0.5, 0, 0, 80, 600, 150), "Play", "Super !", "Check", 58)
        # Pluie de pièces (WheelScreen.StartRain) : 18 pièces pour 250, figées à mi-course.
        rnd = random.Random(7)
        ccx, ccy = (ib[0] + ib[2]) / 2, (ib[1] + ib[3]) / 2
        coin = sp.load("ui/coins_1.png")
        for k in range(18):
            a = math.radians(rnd.uniform(25, 155)); v = rnd.uniform(950, 1750); t = rnd.uniform(0.22, 0.62)
            x = ccx + math.cos(a) * v * t; y = ccy - (math.sin(a) * v * t - 1500 * t * t)
            w = int(rnd.uniform(62, 96))
            cimg = coin.resize((w, w), Image.LANCZOS).rotate(rnd.uniform(-180, 180), resample=Image.BICUBIC)
            img.alpha_composite(cimg, (int(x - w / 2), int(y - w / 2)))
    return s


def quest_screen():
    s = home_widgets()
    img = s.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 170)))
    cb = at(0.5, 0.5, 0, 20, 980, 1300)
    sp.card(img, cb, tint=hexc("#2B1B54"))
    masked(img, cb, lambda layer: rays(layer, child(cb, 0.5, 1, 0, -260, 1300, 1300), (255, 217, 102, 64)))
    ib = child(cb, 0.5, 1, 0, -150, 170, 170)
    disc(img, ib, c("Success"))
    img.alpha_composite(sp.load("ui/quest.png").resize((138, 138), Image.LANCZOS), (int(ib[0] + 16), int(ib[1] + 16)))
    sp.title(img, "Quête du débutant", child(cb, 0.5, 1, 0, -300, 900, 110), 76, color=c("Accent"))
    sp.title(img, "1 / 3 étapes : une récompense à chacune !", child(cb, 0.5, 1, 0, -380, 860, 70), 38, color=SOFT, outline=False)
    rows = [("Joue ta première partie", "Récompense gagnée !", True, False), ("Fais 500 points en une partie", "Un coffre  ·  380 / 500", False, True), ("Joue 5 parties", "La planche Bambou", False, False)]
    for i, (t, r, done, cur) in enumerate(rows):
        rb = child(cb, 0.5, 1, 0, -450 - i * 170, 900, 150, 0.5, 1)
        sp.pill(img, rb)
        db = child(rb, 0, 0.5, 70, 0, 96, 96)
        disc(img, db, c("Play") if done else c("Accent") if cur else (255, 255, 255, 64))
        if done:
            sp.tinted_icon(img, "Check", (db[0] + 16, db[1] + 16, db[2] - 16, db[3] - 16), "#FFFFFF")
        else:
            sp.title(img, str(i + 1), (db[0], db[1], db[2], db[3] - 4), 50)
        sp.title(img, t, child(rb, 0, 1, 140, -46, 500, 60, 0, 0.5), 42, color=(255, 255, 255, 160) if done else (255, 255, 255), align="lm")
        sp.title(img, r, child(rb, 0, 0, 140, 42, 520, 50, 0, 0.5), 34, color=c("Accent"), align="lm", outline=False)
    sp.button(img, child(cb, 0.5, 0, 0, 220, 760, 166), "Play", "Jouer", "Play", 62)
    sp.button(img, child(cb, 0.5, 0, 0, 74, 460, 112), "Muted", "Retour", None, 46)
    return s


def event_screen():
    s = home_widgets()
    img = s.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 170)))
    cb = at(0.5, 0.5, 0, 20, 980, 1400)
    sp.card(img, cb, tint=hexc("#2B1B54"))
    bn = child(cb, 0.5, 1, 0, -30, 920, 300, 0.5, 1)
    sp.card(img, bn, tint=hexc("#3A1220"))
    masked(img, bn, lambda layer: rays(layer, child(bn, 0.5, 0.5, 0, 0, 1100, 1100), (255, 255, 255, 56)), 40)
    tg = child(bn, 0.5, 1, 0, -50, 420, 70)
    sp.pill(img, tg, color=c("Primary") + (255,))
    sp.title(img, "ÉVÉNEMENT DE LA SEMAINE", tg, 30)
    sp.title(img, "Semaine brûlante", child(bn, 0.5, 0.5, 0, -10, 860, 110), 70)
    sp.title(img, "Se termine dans 3 j 7 h", child(bn, 0.5, 0, 0, 40, 860, 56), 36, color=sp.rgb("#FFF1B8"))
    sp.title(img, "Cumule 50 000 points en plusieurs parties", child(cb, 0.5, 1, 0, -400, 900, 120), 46, color=c("Accent"))
    sp.bar(img, child(cb, 0.5, 1, 0, -500, 860, 56), 0.49, "Accent")
    sp.title(img, "24 460 / 50 000", child(cb, 0.5, 1, 0, -572, 860, 64), 44)
    for i in range(3):
        x = (i - 1) * 300
        tb = child(cb, 0.5, 1, x, -640, 280, 340, 0.5, 1)
        sp.pill(img, tb)
        sp.title(img, f"Palier {i + 1}", child(tb, 0.5, 1, 0, -32, 190, 44), 30, color=SKY)
        db = child(tb, 0.5, 1, 0, -136, 156, 156)
        disc(img, db, (92, 209, 110, 115) if i == 0 else (255, 255, 255, 38))
        ib = child(tb, 0.5, 1, 0, -136, 136, 136)
        if i == 0:
            img.alpha_composite(sp.load("ui/coins_2.png").resize((136, 136), Image.LANCZOS), (int(ib[0]), int(ib[1]))); lab = ["+150 pièces"]
        elif i == 1:
            sp.tinted_icon(img, "Gift", ib, "#FFFFFF"); lab = ["Surprise", "gratuite"]
        else:
            img.alpha_composite(sp.load("heads/46.png").resize((136, 136), Image.LANCZOS), (int(ib[0]), int(ib[1]))); lab = ["Tête Fêtard"]
        lb = child(tb, 0.5, 0, 0, 62, 256, 96)
        y0 = (lb[1] + lb[3]) / 2 - len(lab) * 19
        for k, ln in enumerate(lab):
            sp.title(img, ln, (lb[0], y0 + k * 38, lb[2], y0 + 38 + k * 38), 32)
        if i == 0:
            ch = child(tb, 1, 1, -14, -14, 58, 58, 1, 1)
            disc(img, ch, c("Play"))
            sp.tinted_icon(img, "Check", (ch[0] + 10, ch[1] + 10, ch[2] - 10, ch[3] - 10), "#FFFFFF")
    sp.button(img, child(cb, 0.5, 0, 0, 220, 760, 166), "Play", "Jouer", "Play", 62)
    sp.button(img, child(cb, 0.5, 0, 0, 74, 460, 112), "Muted", "Retour", None, 46)
    return s


if __name__ == "__main__":
    shots = {
        "duel_entree": duel_choice(), "duel_attente": duel_hosting(), "duel_invitation": duel_invite(),
        "classement_podium": leaderboard(), "premier_pseudo": onboard_pseudo(), "premier_age": onboard_age(),
        "premier_pubs": onboard_ads(), "boutique_videos": shop_video(),
    }
    for name, s in shots.items():
        s.img.convert("RGB").save(os.path.join(OUT, f"ecran_{name}.png"))
    shots.update({"accueil_rendez_vous": home_widgets(), "roue": wheel_screen(), "roue_lot": wheel_screen(True), "quete": quest_screen(), "evenement": event_screen()})
    for name in ("accueil_rendez_vous", "roue", "roue_lot", "quete", "evenement"):
        shots[name].img.convert("RGB").save(os.path.join(OUT, f"ecran_{name}.png"))
    balloon_screen().img.convert("RGB").save(os.path.join(OUT, "ecran_montgolfiere.png"))
    # 1.4 : roue des années au départ, et fin du duel (victoire, défaite au temps, égalité).
    onboard_age(0).img.convert("RGB").save(os.path.join(OUT, "ecran_premier_age_debut.png"))
    ends = [("duel_fin_victoire", duel_result("win")), ("duel_fin_temps", duel_result("lose", True)), ("duel_fin_egalite", duel_result("draw", True))]
    for name, e in ends:
        e.img.convert("RGB").save(os.path.join(OUT, f"ecran_{name}.png"))
    strip = Image.new("RGB", (W * 3 + 80, H + 40), (24, 22, 40))
    for i, (_, e) in enumerate(ends):
        strip.paste(e.img.convert("RGB"), (20 + i * (W + 20), 20))
    strip.resize((strip.width // 2, strip.height // 2), Image.LANCZOS).save(os.path.join(OUT, "ecrans_duel_fin_1_4.png"))
    keys = list(shots)
    for part, names in (("ecrans_duel_1_4", keys[:4]), ("ecrans_accueil_1_4", keys[4:8]), ("ecrans_rendez_vous", keys[8:])):
        strip = Image.new("RGB", (W * len(names) + 20 * (len(names) + 1), H + 40), (24, 22, 40))
        for i, k in enumerate(names):
            strip.paste(shots[k].img.convert("RGB"), (20 + i * (W + 20), 20))
        strip.resize((strip.width // 2, strip.height // 2), Image.LANCZOS).save(os.path.join(OUT, part + ".png"))
    print("ok")
