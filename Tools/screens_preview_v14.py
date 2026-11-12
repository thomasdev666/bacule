"""
Aperçus des écrans de la 1.4 (sans Unity), avec les mêmes positions que le code de l'interface :
- duel : entrée (bannière VS, règles, amis à défier, code), attente (code en tuiles), invitation reçue ;
- classement : podium et « Ta place » ;
- premier lancement : pseudo, année de naissance, pubs ;
- boutique : « Gratuit avec une vidéo » (tête et planche exclusives, coffre).

  python3 Tools/screens_preview_v14.py Exports/art docs/apercu
"""
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
    sp.title(img, "1 contre 1 · en direct · mêmes poids", child(bb, 0.5, 0, 0, 22, 940, 54, 0.5, 0), 38)


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
    for i, (ic, col, text) in zip((-1, 0, 1), (("Swords", "Accent", "Mêmes poids pour vous deux"), ("Film", "Secondary", "Tu vois sa planche en direct"), ("Flag", "Primary", "Le premier qui lâche a perdu"))):
        cb = at(0.5, 1, i * 336, -795, 322, 150)
        sp.pill(img, cb)
        db = child(cb, 0.5, 1, 0, -38, 64, 64)
        disc(img, db, c(col))
        sp.tinted_icon(img, ic, (db[0] + 12, db[1] + 12, db[2] - 12, db[3] - 12), "#FFFFFF")
        lines = text.split(" ", 2)
        t = " ".join(lines[:2]) + "\n" + (lines[2] if len(lines) > 2 else "")
        tb = child(cb, 0.5, 0, 0, 12, 300, 72, 0.5, 0)
        for k, ln in enumerate(t.split("\n")):
            sp.title(img, ln, (tb[0], tb[1] + k * 36 - 4, tb[2], tb[1] + k * 36 + 32), 30)
    sp.title(img, "Défie un ami", at(0, 1, 50, -925, 700, 70, 0, 0.5), 52, color=c("Accent"), align="lm")
    sp.button(img, at(1, 1, -40, -925, 110, 110, 1, 0.5), "Play", None, "Plus", 44)
    friends = [("Léa", 14), ("Tom", 33), ("Nina", 22)]
    top = 985
    for i, (n, h) in enumerate(friends):
        rb = (40, top + i * 146, W - 40, top + i * 146 + 132)
        friend_row(img, rb, n, h, "Défier")
    sp.title(img, "Ou avec un code", at(0.5, 0, 0, 470, 900, 60), 42, color=sp.rgb("#BFE0FF"))
    sp.button(img, at(0.5, 0, -245, 330, 470, 150), "Purple", "Créer un code", "Plus", 50)
    sp.button(img, at(0.5, 0, 245, 330, 470, 150), "Secondary", "J'ai un code", "Play", 50)
    gb = at(0.5, 0, 0, 60, 1000, 210, 0.5, 0)
    sp.pill(img, gb)
    portrait(img, child(gb, 0, 0.5, 110, 0, 160, 160), c("Primary"), 62)
    sp.title(img, "Victoires : 4 / 15", child(gb, 0, 0.5, 220, 52, 740, 60, 0, 0.5), 44, align="lm")
    sp.bar(img, child(gb, 0, 0.5, 220, -6, 720, 40, 0, 0.5), 4 / 15, "Primary")
    sp.title(img, "4 victoires · 9 duels", child(gb, 0, 0.5, 220, -60, 740, 50, 0, 0.5), 36, color=sp.rgb("#BFE0FF"), align="lm")
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
    for i, name in enumerate(("Monde", "Jour", "Amis", "Moi")):
        tb = child(bar, 0, 0.5, 10 + i * 248, 0, 242, 100, 0, 0.5)
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


# ============================================================================ Premier lancement

def onboard_base(title, sub, step, steps=3):
    s = backdrop(170)
    img = s.img
    cb = at(0.5, 0.5, 0, 20, 960, 1520)
    sp.card(img, cb)
    dots = child(cb, 0.5, 1, 0, -64, 300, 30)
    xs = []
    total = sum(34 if i == step else 24 for i in range(steps)) + 22 * (steps - 1)
    x = (dots[0] + dots[2]) / 2 - total / 2
    for i in range(steps):
        d = 34 if i == step else 24
        cy = (dots[1] + dots[3]) / 2
        disc(img, (x, cy - d / 2, x + d, cy + d / 2), c("Accent") if i == step else (43, 45, 66, 51))
        x += d + 22
        xs.append(x)
    sp.title(img, title, child(cb, 0.5, 1, 0, -150, 880, 120), 80, color=c("Accent"))
    sb = child(cb, 0.5, 1, 0, -262, 860, 110)
    words, lines, cur = sub.split(" "), [], ""
    for w in words:
        if ImageDraw.Draw(img).textlength(cur + " " + w, font=sp.font(42)) > 840 and cur:
            lines.append(cur)
            cur = w
        else:
            cur = (cur + " " + w).strip()
    lines.append(cur)
    for k, ln in enumerate(lines):
        sp.title(img, ln, (sb[0], sb[1] + 6 + k * 50, sb[2], sb[1] + 56 + k * 50), 42, color=INK, outline=False)
    return s, img, cb


def onboard_pseudo():
    s, img, cb = onboard_base("Ton pseudo", "Il s'affiche dans les classements et pendant les duels.", 0)
    rays(img, child(cb, 0.5, 1, 0, -560, 560, 560), (255, 209, 89, 115))
    pb = child(cb, 0.5, 1, 0, -560, 330, 330)
    disc(img, pb, c("Purple"))
    img.alpha_composite(sp.load("heads/00.png").resize((290, 290), Image.LANCZOS), (int(pb[0] + 20), int(pb[1] + 14)))
    fb = child(cb, 0.5, 1, -70, -850, 700, 150)
    sp.pill(img, fb, color=sp.OUTLINE + (255,))
    sp.pill(img, (fb[0] + 7, fb[1] + 7, fb[2] - 7, fb[3] - 7), color=(255, 255, 255, 255))
    sp.title(img, "MegaPanda42", fb, 60, color=INK, outline=False)
    sp.button(img, child(cb, 0.5, 1, 380, -850, 150, 150), "Secondary", None, "Replay", 60)
    sp.title(img, "Pas ton vrai nom, pas d'insulte. Le dé te donne une idée.", child(cb, 0.5, 1, 0, -1060, 860, 100), 36, color=MUTED, outline=False)
    sp.button(img, child(cb, 0.5, 0, 0, 200, 760, 160), "Play", "Valider", "Check", 62)
    sp.button(img, child(cb, 0.5, 0, 0, 62, 460, 110), "Muted", "Plus tard", None, 44)
    return s


def onboard_age():
    s, img, cb = onboard_base("Ton année de naissance", "Pour te montrer des pubs adaptées à ton âge.", 1)
    ab = child(cb, 0.5, 1, 0, -360, 880, 760, 0.5, 1)
    sp.card(img, ab, tint=(237, 230, 250, 255))
    y0 = ab[1] + 24
    year = 2022
    for r in range(6):
        for k in range(4):
            x = ab[0] + 22 + k * 210
            bx = (x, y0 + r * 118, x + 196, y0 + r * 118 + 104)
            picked = year == 2008
            sp.pill(img, bx, color=c("Secondary") + (255,) if picked else (255, 255, 255, 255))
            sp.title(img, str(year), bx, 46, color=(255, 255, 255) if picked else INK, outline=False)
            year -= 1
    sp.title(img, "Né(e) en 2008", child(cb, 0.5, 0, 0, 380, 860, 70), 48)
    sp.title(img, "Elle reste sur ton téléphone et n'est jamais affichée.", child(cb, 0.5, 0, 0, 300, 860, 80), 34, color=MUTED, outline=False)
    sp.button(img, child(cb, 0.5, 0, 0, 120, 760, 160), "Play", "Valider", "Check", 62)
    return s


def onboard_ads():
    s, img, cb = onboard_base("Des pubs qui te ressemblent ?", "Bascule est gratuit grâce aux pubs. À toi de choisir lesquelles tu vois.", 2)
    glow(img, child(cb, 0.5, 1, 0, -500, 520, 380), (166, 108, 255, 90))
    tb = child(cb, 0.5, 1, 0, -500, 220, 220)
    disc(img, tb, c("Purple"))
    sp.tinted_icon(img, "Film", child(tb, 0.5, 0.5, 0, 0, 130, 130), "#FFFFFF")
    rows = [("Heart", "Primary", ["Accepter : nos partenaires utilisent des données de ton", "téléphone (identifiant pub, appareil) : pubs plus adaptées."]),
            ("Gift", "Play", ["Refuser : autant de pubs, mais au hasard.", "Le jeu reste exactement le même."]),
            ("Gear", "Secondary", ["Tu peux changer d'avis à tout moment", "dans les réglages."])]
    for i, (ic, col, lines) in enumerate(rows):
        rb = child(cb, 0.5, 1, 0, -690 - i * 150, 860, 132, 0.5, 1)
        sp.pill(img, rb, color=(255, 255, 255, 199))
        db = child(rb, 0, 0.5, 62, 0, 84, 84)
        disc(img, db, c(col))
        sp.tinted_icon(img, ic, (db[0] + 15, db[1] + 15, db[2] - 15, db[3] - 15), "#FFFFFF")
        for k, ln in enumerate(lines):
            sp.title(img, ln, (rb[0] + 124, rb[1] + 22 + k * 44, rb[2] - 26, rb[1] + 66 + k * 44), 32, color=INK, align="lm", outline=False)
    sp.button(img, child(cb, 0.5, 0, -215, 190, 410, 150), "Secondary", "Refuser", None, 52)
    sp.button(img, child(cb, 0.5, 0, 215, 190, 410, 150), "Play", "Accepter", None, 52)
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


if __name__ == "__main__":
    shots = {
        "duel_entree": duel_choice(), "duel_attente": duel_hosting(), "duel_invitation": duel_invite(),
        "classement_podium": leaderboard(), "premier_pseudo": onboard_pseudo(), "premier_age": onboard_age(),
        "premier_pubs": onboard_ads(), "boutique_videos": shop_video(),
    }
    for name, s in shots.items():
        s.img.convert("RGB").save(os.path.join(OUT, f"ecran_{name}.png"))
    keys = list(shots)
    for part, names in (("ecrans_duel_1_4", keys[:4]), ("ecrans_accueil_1_4", keys[4:])):
        strip = Image.new("RGB", (W * len(names) + 20 * (len(names) + 1), H + 40), (24, 22, 40))
        for i, k in enumerate(names):
            strip.paste(shots[k].img.convert("RGB"), (20 + i * (W + 20), 20))
        strip.resize((strip.width // 2, strip.height // 2), Image.LANCZOS).save(os.path.join(OUT, part + ".png"))
    print("ok")
