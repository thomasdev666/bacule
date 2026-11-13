"""
Images de la fiche App Store faites avec les dessins du jeu (docs/STORE.md) :
- achats intégrés mis en avant : 1024 × 1024 px, un par produit ;
- événements dans l'app : carte 16:9 (1920 × 1080) et page de détail 9:16 (1080 × 1920), sans texte
  (Apple affiche le nom et la description par-dessus, en bas) ;
- captures d'écran avec titre : 1290 × 2796 px (iPhone 6,7 pouces). Apple lit le texte des captures pour la recherche.
PNG sans transparence (Apple refuse le canal alpha).

  python3 Tools/store_listing_assets.py Exports/art docs/apercu docs/store
"""
import os
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ART, SHOTS, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
sys.argv = [sys.argv[0], ART, "/tmp", "rien"]
import scene_preview as sp  # noqa: E402

for sub in ("achats", "evenements", "captures"):
    os.makedirs(os.path.join(OUT, sub), exist_ok=True)


def background(w, h, theme=0, rays=0.5, tint=None):
    """Ciel du jeu en dégradé, avec les rayons de l'écran de récompense."""
    img = sp.load(f"scene/sky{theme}.png").resize((w, h), Image.BICUBIC)
    if tint:
        img = Image.alpha_composite(img, Image.new("RGBA", (w, h), sp.rgb(sp.COL[tint]) + (150,)))
    if rays > 0:
        side = int(max(w, h) * 1.5)
        r = sp.load("ui/rays.png").resize((side, side), Image.LANCZOS)
        r.putalpha(r.getchannel("A").point(lambda a: int(a * rays)))
        img.alpha_composite(r, ((w - side) // 2, (h - side) // 2))
    return img


def put(img, sprite, cx, cy, width, rot=0.0, shadow=True):
    """Pose un dessin centré en (cx, cy), à la largeur voulue, avec une ombre douce."""
    s = width / sprite.width
    im = sprite.resize((int(sprite.width * s), int(sprite.height * s)), Image.LANCZOS)
    if rot:
        im = im.rotate(rot, resample=Image.BICUBIC, expand=True)
    x, y = int(cx - im.width / 2), int(cy - im.height / 2)
    if shadow:
        sh = Image.new("RGBA", im.size, (30, 20, 60, 0))
        sh.putalpha(im.getchannel("A").point(lambda a: int(a * 0.35)))
        off = int(width * 0.04)
        img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(width * 0.03)), (x + off, y + off * 2))
    img.alpha_composite(im, (x, y))


def head(i):
    return sp.load(f"heads/{i:02d}.png")


def save(img, *path):
    img.convert("RGB").save(os.path.join(OUT, *path))
    print("/".join(path), img.size)


# ============================================================================ Achats mis en avant (1024 × 1024)

def purchase(name, label, draw, theme=0, tint=None):
    img = background(1024, 1024, theme, 0.55, tint)
    draw(img)
    sp.title(img, label, (0, 800, 1024, 960), 104 if len(label) < 13 else 86)
    save(img, "achats", name + ".png")


def coins(sprite, width):
    return lambda img: put(img, sp.load(f"ui/{sprite}.png"), 512, 430, width)


def starter(img):
    put(img, sp.load("ui/coins_6.png"), 760, 560, 400, rot=-8)
    put(img, head(61), 440, 400, 620)


def no_ads(img):
    put(img, head(0), 512, 410, 640)
    # Panneau « pub barrée » : un film rayé de rouge.
    film = sp.load("ui/icon_Film.png")
    put(img, film, 800, 250, 230, rot=10)
    d = ImageDraw.Draw(img)
    d.ellipse((670, 120, 930, 380), outline=sp.rgb(sp.COL["Primary"]) + (255,), width=26)
    d.line((712, 162, 888, 338), fill=sp.rgb(sp.COL["Primary"]) + (255,), width=26)


def legends(img):
    for i, (x, y, w, r) in zip((56, 60, 57, 59, 58), ((205, 300, 330, 8), (819, 300, 330, -8), (230, 600, 330, -6), (794, 600, 330, 6), (512, 430, 470, 0))):
        put(img, head(i), x, y, w, rot=r)


purchase("bascule.starter", "Offre de départ", starter, tint=None)
purchase("bascule.removeads", "Sans publicité", no_ads, theme=0)
purchase("bascule.coins.small", "600 pièces", coins("coins_2", 520))
purchase("bascule.coins.medium", "2 200 pièces", coins("coins_3", 600))
purchase("bascule.coins.large", "6 000 pièces", coins("coins_6", 680))
purchase("bascule.legends", "Pack Légendes", legends, theme=4)


# ============================================================================ Événements dans l'app

def duel(w, h):
    """Deux joueurs face à face : rouge à gauche, bleu à droite, comme la carte VS du jeu."""
    img = background(w, h, 0, 0.0)
    d = ImageDraw.Draw(img, "RGBA")
    red, blue = sp.rgb(sp.COL["Primary"]), sp.rgb(sp.COL["Secondary"])
    lean = int(h * 0.12)
    d.polygon([(0, 0), (w // 2 + lean, 0), (w // 2 - lean, h), (0, h)], fill=red + (235,))
    d.polygon([(w // 2 + lean, 0), (w, 0), (w, h), (w // 2 - lean, h)], fill=blue + (235,))
    cy = int(h * 0.40)
    size = int(min(w * 0.36, h * 0.60))
    put(img, head(0), w * 0.25, cy, size, rot=6)
    put(img, head(62), w * 0.75, cy, size, rot=-6)
    r = int(size * 0.26)
    d.ellipse((w // 2 - r, cy - r, w // 2 + r, cy + r), fill=sp.rgb(sp.COL["Ink"]) + (255,), outline=(255, 255, 255, 255), width=max(6, r // 9))
    sp.title(img, "VS", (w // 2 - r, cy - r, w // 2 + r, cy + r), r * 0.9, color=sp.rgb(sp.COL["Accent"]))
    return img


def week(w, h):
    """Le trophée de la semaine au milieu des poids, sur le ciel du soir."""
    img = background(w, h, 2, 0.6)
    cy = int(h * 0.40)
    size = int(min(w * 0.30, h * 0.50))
    # Vrais poids du jeu, avec leur chiffre (sp.weight travaille en unités monde).
    sp.W, sp.H, sp.PPU = w, h, size / 2.6
    c = sp.Canvas(img)
    for v, dx, dy, rot in ((8, -1.25, 0.25, -12), (32, 1.25, 0.25, 10), (2, -0.80, -0.50, 14), (64, 0.86, -0.46, -8)):
        sp.weight(c, v, dx * size / sp.PPU, (h / 2 - (cy + dy * size)) / sp.PPU, rot=rot)
    # Trophée doré : l'icône du jeu est blanche, on la teinte en gardant son contour.
    trophy = sp.load("ui/icon_Trophy.png")
    gold = ImageChops.multiply(trophy.convert("RGB"), Image.new("RGB", trophy.size, sp.rgb(sp.COL["Accent"]))).convert("RGBA")
    gold.putalpha(trophy.getchannel("A"))
    put(img, gold, w / 2, cy, size * 1.15)
    return img


for name, draw in (("duel", duel), ("semaine", week)):
    save(draw(1920, 1080), "evenements", f"{name}_carte_1920x1080.png")
    save(draw(1080, 1920), "evenements", f"{name}_detail_1080x1920.png")


# ============================================================================ Captures avec titre (1290 × 2796)

def capture(n, shot, line1, line2, color):
    w, h = 1290, 2796
    img = Image.new("RGBA", (w, h), sp.rgb(sp.COL[color]) + (255,))
    # Dégradé vers le bas et rayons discrets derrière le titre.
    shade = Image.new("L", (1, h))
    shade.putdata([int(90 * y / h) for y in range(h)])
    img.alpha_composite(Image.merge("RGBA", (*[Image.new("L", (w, h), 20)] * 3, shade.resize((w, h)))))
    side = 2200
    r = sp.load("ui/rays.png").resize((side, side), Image.LANCZOS)
    r.putalpha(r.getchannel("A").point(lambda a: int(a * 0.35)))
    img.alpha_composite(r, ((w - side) // 2, 330 - side // 2))
    sp.title(img, line1, (0, 150, w, 330), 128 if len(line1) < 17 else 104)
    sp.title(img, line2, (0, 350, w, 470), 68, color=(255, 246, 229))
    # L'écran du jeu, coins arrondis, avec une ombre.
    sw = 1130
    s = Image.open(os.path.join(SHOTS, shot)).convert("RGBA")
    s = s.resize((sw, int(s.height * sw / s.width)), Image.LANCZOS)
    mask = Image.new("L", s.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, s.width - 1, s.height - 1), 70, fill=255)
    x, y = (w - sw) // 2, 600
    sh = Image.new("RGBA", (s.width + 160, s.height + 160), (0, 0, 0, 0))
    ImageDraw.Draw(sh).rounded_rectangle((80, 80, 80 + s.width, 80 + s.height), 70, fill=(30, 20, 60, 150))
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(36)), (x - 80, y - 50))
    frame = Image.new("RGBA", (s.width + 28, s.height + 28), (0, 0, 0, 0))
    ImageDraw.Draw(frame).rounded_rectangle((0, 0, frame.width - 1, frame.height - 1), 84, fill=(255, 255, 255, 255))
    img.alpha_composite(frame, (x - 14, y - 14))
    img.paste(s, (x, y), mask)
    save(img, "captures", f"{n}_{os.path.splitext(shot)[0].replace('ecran_', '')}.png")


capture(1, "ecran_partie.png", "JEU D'ÉQUILIBRE", "Pose les poids, tiens bon !", "Secondary")
capture(2, "ecran_duel_invitation.png", "DUEL EN DIRECT", "Défie tes amis en 1 contre 1", "Primary")
capture(3, "ecran_montgolfiere.png", "FUSIONNE LES POIDS", "1 + 1 = 2… jusqu'au 128 !", "Success")
capture(4, "ecran_classement_podium.png", "CLASSEMENT MONDIAL", "Bats le record de tes amis", "Purple")
capture(5, "ecran_tetes.png", "66 TÊTES À GAGNER", "Collectionne-les toutes", "Accent")
capture(6, "ecran_roue.png", "CADEAUX CHAQUE JOUR", "Roue, coffres et missions", "Play")
