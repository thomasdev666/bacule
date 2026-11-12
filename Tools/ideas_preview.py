"""
Maquettes d'IDÉES (rien de tout cela n'est dans le jeu) : passe de saison, ligue de la semaine, duel fantôme,
mode du jour et poids spéciaux. Mêmes images et mêmes aides que les autres aperçus.

  python3 Tools/ideas_preview.py Exports/art docs/idees
"""
import os
import sys

from PIL import Image, ImageChops, ImageDraw

ART, OUT = sys.argv[1], sys.argv[2]
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.argv = [sys.argv[0], ART, "/tmp"]
import screens_preview_v14 as v  # noqa: E402

sp = v.sp
W, H = sp.W, sp.H
c, at, child, hexc = v.c, v.at, v.child, v.hexc
SOFT, SKY, INK, MUTED = v.SOFT, v.SKY, v.INK, v.MUTED
GOLD = sp.rgb("#FFC93C")


def idea_tag(img):
    """Étiquette « IDÉE » : ces écrans n'existent pas encore."""
    b = (W / 2 - 110, H - 86, W / 2 + 110, H - 26)
    sp.pill(img, b, color=(255, 90, 95, 255))
    sp.title(img, "IDÉE", b, 34)


def head(n, size):
    return sp.load(f"heads/{n:02d}.png").resize((size, size), Image.LANCZOS)


def tint_weight(value, color):
    im = sp.load(f"weights/{value}.png")
    return ImageChops.multiply(im, Image.new("RGBA", im.size, color + (255,)))


# ---------------------------------------------------------------- 1. Passe de saison

def season_pass():
    s = v.backdrop(215)
    img = s.img
    v.back_and_header(img, "Passe de saison")
    bn = at(0.5, 1, 0, -230, 1010, 300, 0.5, 1)
    sp.card(img, bn, tint=hexc("#3C096C"))
    v.masked(img, bn, lambda layer: v.rays(layer, child(bn, 0, 0.5, 200, 0, 800, 800), (255, 217, 102, 80)), 40)
    img.alpha_composite(head(46, 230), (int(bn[0] + 50), int(bn[1] + 36)))
    sp.title(img, "Saison 1 : Les Vikings", child(bn, 0, 1, 310, -70, 660, 80, 0, 1), 56, align="lm")
    sp.title(img, "Encore 18 jours", child(bn, 0, 1, 310, -128, 660, 46, 0, 1), 34, color=sp.rgb("#FFF1B8"), align="lm", outline=False)
    sp.title(img, "Palier 7 / 30", child(bn, 0, 0, 310, 84, 400, 46, 0, 0), 38, color=GOLD, align="lm")
    sp.bar(img, child(bn, 0, 0, 310, 34, 640, 36, 0, 0), 0.6, "Accent")

    # Deux rangées : gratuit et premium.
    top = bn[3] + 30
    sp.title(img, "GRATUIT", (60, top + 148, 250, top + 208), 34, color=SKY, align="lm")
    sp.title(img, "PREMIUM", (60, top + 438, 250, top + 498), 34, color=GOLD, align="lm")
    free = [("coins_1", "+100"), ("icon_Gift", "Coffre"), ("coins_2", "+200"), ("head:20", "Tête")]
    prem = [("head:31", "Tête"), ("coins_3", "+500"), ("plank", "Planche"), ("head:46", "Viking")]
    for k in range(4):
        x = 70 + k * 245
        level = 6 + k
        done = level <= 7
        nb = (x + 80, top + 30, x + 160, top + 96)
        v.disc(img, nb, c("Accent") if done else (70, 60, 110, 255))
        sp.title(img, str(level), (nb[0], nb[1], nb[2], nb[3] - 4), 38)
        for row, items, tint in ((0, free, None), (1, prem, hexc("#5A3A00"))):
            b = (x, top + 110 + row * 290, x + 228, top + 110 + row * 290 + 260)
            if tint:
                sp.card(img, b, tint=hexc("#FFE08A") if done else hexc("#8C7A4A"))
            else:
                sp.card(img, b, tint=None if done else hexc("#C8CCDA"))
            kind, label = items[k]
            ib = (b[0] + 44, b[1] + 24, b[0] + 184, b[1] + 164)
            if kind.startswith("head:"):
                img.alpha_composite(head(int(kind[5:]), 140), (int(ib[0]), int(ib[1])))
            elif kind == "plank":
                p = sp.load("planches/apercu_3.png")
                p = p.resize((180, int(180 * p.height / p.width)), Image.LANCZOS)
                img.alpha_composite(p, (int(b[0] + 24), int(b[1] + 70)))
            elif kind.startswith("icon_"):
                sp.tinted_icon(img, kind[5:], ib, "Primary")
            else:
                img.alpha_composite(sp.load(f"ui/{kind}.png").resize((140, 140), Image.LANCZOS), (int(ib[0]), int(ib[1])))
            sp.title(img, label, (b[0] + 8, b[3] - 84, b[2] - 8, b[3] - 30), 36, color=INK, outline=False)
            if done and (row == 0):
                ch = (b[2] - 70, b[1] + 10, b[2] - 10, b[1] + 70)
                v.disc(img, ch, c("Play"))
                sp.tinted_icon(img, "Check", (ch[0] + 10, ch[1] + 10, ch[2] - 10, ch[3] - 10), "#FFFFFF")
            if row == 1:
                lk = (b[2] - 70, b[1] + 10, b[2] - 10, b[1] + 70)
                v.disc(img, lk, (43, 27, 84, 255))
                sp.tinted_icon(img, "Lock", (lk[0] + 12, lk[1] + 12, lk[2] - 12, lk[3] - 12), "#FFC93C")
    sp.title(img, "Chaque partie fait avancer le passe.", at(0.5, 0, 0, 330, 980, 60), 40, color=SOFT, outline=False)
    sp.button(img, at(0.5, 0, 0, 200, 900, 170), "Accent", "Débloquer le Premium · 3,99 €", "Crown", 50)
    sp.title(img, "Que des objets de style : rien ne change le score.", at(0.5, 0, 0, 80, 980, 50), 32, color=SKY, outline=False)
    idea_tag(img)
    return s


# ---------------------------------------------------------------- 2. Ligue de la semaine

def league():
    s = v.backdrop(215)
    img = s.img
    v.back_and_header(img, "Ligue de la semaine")
    bn = at(0.5, 1, 0, -230, 1010, 210, 0.5, 1)
    sp.card(img, bn, tint=hexc("#1F4E8C"))
    v.disc(img, child(bn, 0, 0.5, 110, 0, 150, 150), sp.rgb("#C9D3E3"))
    sp.tinted_icon(img, "Trophy", child(bn, 0, 0.5, 110, 0, 96, 96), "#FFFFFF")
    sp.title(img, "Ligue Argent", child(bn, 0, 0.5, 210, 36, 560, 70, 0, 0.5), 56, align="lm")
    sp.title(img, "20 joueurs · fin dans 3 j 7 h", child(bn, 0, 0.5, 210, -34, 620, 50, 0, 0.5), 36, color=sp.rgb("#D8E9FF"), align="lm", outline=False)
    rows = [(1, "MegaPanda42", "41 200", 31), (2, "Léa", "38 950", 14), (3, "TurboBasile", "36 400", 5), (4, "Capitaine", "33 180", 47),
            (5, "TOI", "31 020", 12), (6, "Nina", "29 870", 22), (7, "Comète", "27 300", 9), (8, "Pingouin", "24 950", 40)]
    y = bn[3] + 70
    sp.title(img, "Les 5 premiers montent en Ligue Or", (40, y - 56, W - 40, y - 6), 34, color=c("Play"), align="lm")
    for rank, name, score, hd in rows:
        me = name == "TOI"
        b = (36, y, W - 36, y + 124)
        sp.card(img, b, tint=hexc("#FFE9A8") if me else (hexc("#E3F7E6") if rank <= 5 else None))
        sp.title(img, str(rank), child(b, 0, 0.5, 24, 0, 96, 100, 0, 0.5), 48, color=sp.rgb("#3FA34D") if rank <= 5 else sp.rgb("#AEB6C8"))
        img.alpha_composite(head(hd, 104), (int(b[0] + 130), int(b[1] + 10)))
        sp.title(img, name, child(b, 0, 0.5, 256, 0, 380, 90, 0, 0.5), 44, color=INK, align="lm", outline=False)
        sp.title(img, score, child(b, 1, 0.5, -36, 0, 240, 90, 1, 0.5), 46, color=INK, align="rm", outline=False)
        y += 136
        if rank == 5:
            ImageDraw.Draw(img).line([(60, y + 2), (W - 60, y + 2)], fill=(92, 209, 110, 255), width=5)
            y += 14
    rb = at(0.5, 0, 0, 150, 1000, 150)
    sp.pill(img, rb)
    img.alpha_composite(sp.load("ui/coins_3.png").resize((110, 110), Image.LANCZOS), (int(rb[0] + 30), int(rb[1] + 20)))
    sp.title(img, "Récompense : 400 pièces + tête de la ligue", (rb[0] + 160, rb[1], rb[2] - 20, rb[3]), 36, align="lm")
    idea_tag(img)
    return s


# ---------------------------------------------------------------- 3. Duel fantôme

def ghost_duel():
    s = v.backdrop(215)
    img = s.img
    v.back_and_header(img, "Duel fantôme")
    sp.title(img, "Affronte la partie enregistrée d'un autre joueur.", at(0.5, 1, 0, -250, 1000, 60), 40, color=SOFT, outline=False)
    sp.title(img, "Toujours disponible, même sans ami en ligne.", at(0.5, 1, 0, -306, 1000, 60), 40, color=SOFT, outline=False)
    rivals = [("Facile", "Nina", "2 140", 22, "Play", "+20"), ("Normal", "Capitaine", "4 870", 47, "Secondary", "+40"), ("Difficile", "MegaPanda42", "9 310", 31, "Primary", "+80")]
    y = 380
    for level, name, score, hd, col, coins in rivals:
        b = (36, y, W - 36, y + 330)
        sp.card(img, b, tint=hexc("#2B1B54"))
        tg = (b[0] + 30, b[1] + 24, b[0] + 280, b[1] + 86)
        sp.pill(img, tg, color=c(col) + (255,))
        sp.title(img, level, tg, 36)
        pb = (b[0] + 40, b[1] + 110, b[0] + 230, b[1] + 300)
        v.portrait(img, pb, c(col), hd)
        ghost = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ImageDraw.Draw(ghost).ellipse((pb[0] + 7, pb[1] + 7, pb[2] - 7, pb[3] - 7), fill=(170, 200, 255, 90))
        img.alpha_composite(ghost)
        sp.title(img, name, (b[0] + 270, b[1] + 110, b[2] - 320, b[1] + 180), 50, align="lm")
        sp.title(img, f"Son score : {score}", (b[0] + 270, b[1] + 184, b[2] - 320, b[1] + 236), 38, color=SKY, align="lm", outline=False)
        img.alpha_composite(sp.load("ui/coins_1.png").resize((70, 70), Image.LANCZOS), (int(b[0] + 270), int(b[1] + 240)))
        sp.title(img, f"{coins} si tu gagnes", (b[0] + 350, b[1] + 246, b[2] - 320, b[1] + 304), 36, color=GOLD, align="lm")
        sp.button(img, (b[2] - 310, b[1] + 130, b[2] - 30, b[1] + 280), col, "Défier", "Swords", 46)
        y += 352
    sp.title(img, "Mêmes poids, mêmes rafales : le meilleur score gagne.", at(0.5, 0, 0, 330, 1000, 60), 38, color=SOFT, outline=False)
    sp.button(img, at(0.5, 0, 0, 190, 860, 160), "Purple", "Défier un ami en direct", "Swords", 50)
    idea_tag(img)
    return s


# ---------------------------------------------------------------- 4. Mode du jour

def daily_mode():
    s = sp.home_screen()
    img = s.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 190)))
    cb = at(0.5, 0.5, 0, 20, 980, 1400)
    sp.card(img, cb, tint=hexc("#2B1B54"))
    v.masked(img, cb, lambda layer: v.rays(layer, child(cb, 0.5, 1, 0, -360, 1300, 1300), (255, 217, 102, 70)))
    tg = child(cb, 0.5, 1, 0, -70, 420, 70)
    sp.pill(img, tg, color=c("Primary") + (255,))
    sp.title(img, "MODE DU JOUR", tg, 36)
    sp.title(img, "Contre-la-montre", child(cb, 0.5, 1, 0, -180, 900, 110), 84, color=c("Accent"))
    sp.title(img, "60 secondes pour faire le plus de points.", child(cb, 0.5, 1, 0, -270, 900, 60), 40, color=SOFT, outline=False)
    v.glow(img, child(cb, 0.5, 1, 0, -500, 380, 380), (255, 217, 102, 150), 60)
    db = child(cb, 0.5, 1, 0, -500, 300, 300)
    v.disc(img, db, sp.OUTLINE)
    v.disc(img, (db[0] + 12, db[1] + 12, db[2] - 12, db[3] - 12), c("Accent"))
    sp.title(img, "60", (db[0], db[1], db[2], db[3] - 14), 150)
    week = [("L", "Contre-la-montre", True), ("M", "Gros poids", False), ("M", "Vent fou", False), ("J", "Planche glissante", False), ("V", "Poids dorés", False)]
    for k, (d, name, on) in enumerate(week):
        b = child(cb, 0.5, 1, (k - 2) * 184, -800, 172, 210)
        sp.pill(img, b, color=c("Secondary") + (255,) if on else None)
        sp.title(img, d, (b[0], b[1] + 14, b[2], b[1] + 74), 46)
        words = name.split(" ")
        for i, wd in enumerate(words):
            sp.title(img, wd, (b[0] + 4, b[1] + 84 + i * 40, b[2] - 4, b[1] + 124 + i * 40), 26, outline=False)
    rb = child(cb, 0.5, 0, 0, 380, 860, 110)
    sp.pill(img, rb)
    sp.title(img, "Ton record du jour : 1 840 · 12e", rb, 40)
    sp.button(img, child(cb, 0.5, 0, 0, 232, 760, 166), "Play", "Jouer", "Play", 62)
    sp.button(img, child(cb, 0.5, 0, 0, 70, 460, 112), "Muted", "Retour", None, 46)
    idea_tag(img)
    return s


# ---------------------------------------------------------------- 5. Poids spéciaux

def special_weights():
    cv = sp.world(1)
    pivot, along, hands = sp.mascot(cv, 0, 9.0, 0.1, "Tense")
    center, al, nm = sp.plank(cv, pivot, 9.0, 6.3)
    th = 0.32
    for val, lx in [(4, -2.4), (8, -1.3), (2, 2.4)]:
        x, y = sp.on_plank(center, al, nm, lx, th / 2 + sp.body_half_height(val))
        sp.weight(cv, val, x, y, rot=-9.0)
    import json
    P = json.load(open(os.path.join(ART, "mascot/pivots.json")))
    for hand, side in hands:
        cv.sprite(sp.load("mascot/hand.png"), *hand, pivot=P["hand"][:2], scale=sp.PPU / sp.PART_PPU, rot=-9.0, flipx=side < 0)
    sp.hud(cv, angle=9.0, danger=0.15)
    img = cv.img
    # Trois poids spéciaux posés sur la planche, avec leur explication.
    specials = [("Doré", "vaut double", (255, 210, 70), "Star", -0.2), ("Joker", "fusionne avec tout", (190, 130, 255), "Wheel", 0.95), ("Bombe", "fait sauter un voisin", (90, 90, 110), "Flame", None)]
    for name, desc, col, ic, lx in specials:
        if lx is None:
            continue
        x, y = sp.on_plank(center, al, nm, lx, th / 2 + sp.body_half_height(8))
        im = tint_weight(8, col)
        s8 = sp.size_of(8)
        tex_world = s8 / (2 * sp.BOTTOM_HW)
        cv.sprite(im, x, y, pivot=(im.width / 2, im.height * sp.BODY_CY), scale=sp.PPU * tex_world / im.width, rot=-9.0)
        px, py = sp.to_px(x, y)
        sp.tinted_icon(img, ic, (px - 34, py - 44, px + 34, py + 24), "#FFFFFF")
    # Bombe tenue en l'air.
    im = tint_weight(8, (90, 90, 110))
    s8 = sp.size_of(8)
    tex_world = s8 / (2 * sp.BOTTOM_HW)
    cv.sprite(im, -1.9, sp.ORTHO - 5.1, pivot=(im.width / 2, im.height * sp.BODY_CY), scale=sp.PPU * tex_world / im.width)
    px, py = sp.to_px(-1.9, sp.ORTHO - 5.1)
    sp.tinted_icon(img, "Flame", (px - 34, py - 44, px + 34, py + 24), "#FF8A3D")
    # Légende en bas.
    lb = (36, H - 420, W - 36, H - 40)
    sp.card(img, lb, tint=hexc("#2B1B54"))
    sp.title(img, "Poids spéciaux (rares)", (lb[0] + 30, lb[1] + 20, lb[2] - 30, lb[1] + 86), 46, color=c("Accent"), align="lm")
    for k, (name, desc, col, ic, _) in enumerate(specials):
        y = lb[1] + 100 + k * 96
        v.disc(img, (lb[0] + 40, y, lb[0] + 120, y + 80), col)
        sp.tinted_icon(img, ic, (lb[0] + 56, y + 16, lb[0] + 104, y + 64), "#FFFFFF")
        sp.title(img, name, (lb[0] + 150, y, lb[0] + 400, y + 80), 44, align="lm")
        sp.title(img, desc, (lb[0] + 400, y, lb[2] - 30, y + 80), 38, color=SOFT, align="lm", outline=False)
    idea_tag(img)
    return cv


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    shots = [("passe_saison", season_pass()), ("ligue_semaine", league()), ("duel_fantome", ghost_duel()), ("mode_du_jour", daily_mode()), ("poids_speciaux", special_weights())]
    for name, s in shots:
        s.img.convert("RGB").save(os.path.join(OUT, f"idee_{name}.png"))
    strip = Image.new("RGB", (W * len(shots) + 20 * (len(shots) + 1), H + 40), (24, 22, 40))
    for i, (_, s) in enumerate(shots):
        strip.paste(s.img.convert("RGB"), (20 + i * (W + 20), 20))
    strip.resize((strip.width * 2 // 5, strip.height * 2 // 5), Image.LANCZOS).save(os.path.join(OUT, "idees.png"))
    print("ok")
