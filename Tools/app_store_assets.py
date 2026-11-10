"""
Images de la page produit de l'App Store (iOS 27) avec les vrais dessins du jeu :
- en-tête de la page produit : 21:9, 3840 × 1646 px ;
- résultats de recherche : 3:2, 2880 × 1920 px (Apple accepte de 1920 × 1280 à 3840 × 2560).
PNG sans transparence (Apple refuse le canal alpha). Le sujet reste au centre (zone sûre), une seule idée par image.

Les collines, le sol, le soleil et les nuages doivent être dessinés en haute définition (Tools/ArtPreview les exporte
trop petits pour 3840 px) : voir la commande « hires » dans docs/STORE.md.

  python3 Tools/app_store_assets.py Exports/art <dossier des décors HD> docs/apercu
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

ART, HI, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
sys.argv = [sys.argv[0], ART, "/tmp", "rien"]
import scene_preview as sp  # noqa: E402

os.makedirs(OUT, exist_ok=True)


def hi(name):
    return Image.open(os.path.join(HI, name)).convert("RGBA")


def setup(w, h, ppu, ground_px):
    """Toile de w × h pixels, ppu pixels par unité monde, sol à ground_px pixels du haut."""
    sp.W, sp.H, sp.PPU = w, h, ppu
    sp.GROUND_Y = (h / 2 - ground_px) / ppu


def world_hi(theme, clouds):
    W, H, PPU, G = sp.W, sp.H, sp.PPU, sp.GROUND_Y
    c = sp.Canvas(sp.load(f"scene/sky{theme}.png").resize((W, H), Image.BICUBIC))
    sun = hi(f"sun{theme}.png")
    c.sprite(sun, *clouds["sun"], scale=PPU * 3.4 / sun.width * 1.6)
    for i, (x, y, s) in enumerate(clouds["clouds"]):
        cl = hi(f"cloud{theme}_{i % 3}.png")
        c.sprite(cl, x, y, scale=PPU * s / cl.width * 1.6)
    far = hi(f"far{theme}.png")
    c.sprite(far, 0, G + 1.0, pivot=(far.width / 2, 0), scale=W / far.width)
    near = hi(f"near{theme}.png")
    c.sprite(near, 0, G - 0.6, pivot=(near.width / 2, 0), scale=W * 1.1 / near.width)
    ground = hi(f"ground{theme}.png")
    c.sprite(ground, 0, G + 0.35, pivot=(ground.width / 2, ground.height * 0.82), scale=W / ground.width)
    return c


def hero(angle, expression):
    """Basile, sa planche et ses poids, sur fond transparent (la vraie scène du jeu)."""
    world = sp.world
    sp.world = lambda theme: sp.Canvas(Image.new("RGBA", (sp.W, sp.H), (0, 0, 0, 0)))
    try:
        return sp.game_scene(theme=0, angle=angle, expression=expression, squash=0.05).img
    finally:
        sp.world = world


def logo(img, x, y, size):
    """BASCULE en lettres de couleur inclinées, comme sur l'accueil du jeu (HomeScreen.LogoColors)."""
    word, colors = "BASCULE", ["Primary", "Accent", "Play", "Secondary", "Purple", "Primary", "Accent"]
    step, cell = int(size * 0.71), int(size * 1.5)
    for i, ch in enumerate(word):
        layer = Image.new("RGBA", (cell, int(cell * 1.15)), (0, 0, 0, 0))
        sp.title(layer, ch, (0, 0, layer.width, layer.height), size, color=sp.rgb(sp.COL[colors[i]]))
        layer = layer.rotate(math.sin(i * 0.6) * 5 + 2, resample=Image.BICUBIC)
        sh = Image.new("RGBA", layer.size, (0, 0, 0, 0))
        sh.putalpha(layer.getchannel("A").point(lambda a: int(a * 0.35)))
        lx, ly = x + i * step, y + int(math.sin(i * 1.2) * size * 0.09)
        img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(size * 0.05)), (lx + int(size * 0.04), ly + int(size * 0.08)))
        img.alpha_composite(layer, (lx, ly))
    return step * 6 + cell


def phrase(img, text, cx, cy, size):
    w = int(sp.ImageDraw.Draw(img).textlength(text, font=sp.font(size))) + int(size * 1.4)
    h = int(size * 1.7)
    box = (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)
    sp.pill(img, box, color=sp.rgb(sp.COL["Ink"]) + (235,))
    sp.title(img, text, box, size)


# ============================================================================ En-tête (21:9)

setup(3840, 1646, 165, 1646 - 150)
bg = world_hi(0, {"sun": (8.2, 3.2), "clouds": [(-9.5, 3.6, 2.6), (3.5, 3.9, 2.0), (-2.6, 2.5, 1.6)]})
img = bg.img
basile = hero(-6.0, "Happy")
img.alpha_composite(basile, (430, 0))
# Logo et phrase à gauche de Basile, dans la moitié centrale : rien d'important près des bords.
lw = logo(img, 300, 360, 250)
phrase(img, "Garde l'équilibre !", 300 + lw // 2 - 60, 960, 104)
img.convert("RGB").save(os.path.join(OUT, "appstore_entete_3840x1646.png"))
print("en-tête", img.size)

# ============================================================================ Résultats de recherche (3:2)

setup(2880, 1920, 190, 1920 - 170)
bg = world_hi(0, {"sun": (5.4, 3.6), "clouds": [(-5.6, 4.0, 2.2), (4.6, 1.6, 1.6)]})
img = bg.img
img.alpha_composite(hero(9.0, "Tense"))
# Un poids qui tombe, avec sa ligne de visée : on comprend le jeu d'un coup d'œil.
c = sp.Canvas(img)
pivot_y = sp.GROUND_Y + 4.96
fx, fy = 3.3, pivot_y + 3.0
d = ImageDraw.Draw(img)
x0, y0 = sp.to_px(fx, fy - 0.6)
x1, y1 = sp.to_px(fx, pivot_y + 0.9)
for yy in range(int(y0), int(y1), 46):
    d.ellipse((x0 - 7, yy - 7, x0 + 7, yy + 7), fill=(255, 255, 255, 200))
sp.weight(c, 8, fx, fy, rot=0.0)
# Score en haut, comme pendant une partie.
box = (sp.W / 2 - 430, 60, sp.W / 2 + 430, 330)
sp.pill(img, box)
sp.title(img, "2 310", box, 200)
sp.title(img, "Record : 8 880", (sp.W / 2 - 500, 340, sp.W / 2 + 500, 430), 70, color=(255, 231, 160))
img.convert("RGB").save(os.path.join(OUT, "appstore_recherche_2880x1920.png"))
print("recherche", img.size)
