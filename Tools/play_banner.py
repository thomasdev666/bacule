"""
Bandeau Google Play (image de présentation, 1024 × 500) avec les images du jeu : le logo BASCULE en lettres
de couleur inclinées comme sur l'accueil (même police, mêmes couleurs, même contour), Basile qui tient sa planche,
le décor du matin et son village.

  dotnet run --project Tools/ArtPreview -- Exports/art
  python3 Tools/play_banner.py Exports/art docs/apercu/bandeau_play.png
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

sys.argv = [sys.argv[0], sys.argv[1] if len(sys.argv) > 1 else "Exports/art", "/tmp", "rien"] + sys.argv[2:3]
import scene_preview as sp  # noqa: E402

OUT = sys.argv[4] if len(sys.argv) > 4 else os.path.join(sp.ROOT, "docs/apercu/bandeau_play.png")
W, H = 1024, 500
c = sp.Canvas(Image.new("RGBA", (W, H)))
img = c.img

# Décor du matin (collines, village, prairie), recadré sur le bas de l'écran.
bg = sp.world(0).img
y1 = int(sp.to_px(0, sp.GROUND_Y - 1.3)[1])
bh = int(sp.W * H / W)
img.alpha_composite(bg.crop((0, y1 - bh, sp.W, y1)).resize((W, H), Image.LANCZOS))

# Basile qui tient sa planche chargée : la vraie scène du jeu, dessinée sur un fond transparent.
_world = sp.world
sp.world = lambda theme: sp.Canvas(Image.new("RGBA", (sp.W, sp.H), (0, 0, 0, 0)))
hero = sp.game_scene(theme=0, angle=-7.0, expression="Happy", squash=0.05).img
sp.world = _world
hx0, hy0 = sp.to_px(-3.6, sp.GROUND_Y + 9.2)
hx1, hy1 = sp.to_px(3.6, sp.GROUND_Y - 0.4)
hero = hero.crop((int(hx0), int(hy0), int(hx1), int(hy1)))
k = 470 / hero.height
hero = hero.resize((int(hero.width * k), 470), Image.LANCZOS)
img.alpha_composite(hero, (W - hero.width - 30, H - hero.height + 8))

# Voile clair à gauche : le logo se détache.
veil = Image.new("RGBA", (W, H), (0, 0, 0, 0))
ImageDraw.Draw(veil).ellipse((-260, -120, 600, 520), fill=(255, 250, 235, 70))
img.alpha_composite(veil.filter(ImageFilter.GaussianBlur(60)))

# Logo : même dessin que l'accueil (HomeScreen.LogoColors), lettres inclinées et décalées.
word, colors = "BASCULE", ["Primary", "Accent", "Play", "Secondary", "Purple", "Primary", "Accent"]
size, step = 104, 74
for i, ch in enumerate(word):
    layer = Image.new("RGBA", (150, 170), (0, 0, 0, 0))
    sp.title(layer, ch, (0, 0, 150, 170), size, color=sp.rgb(sp.COL[colors[i]]))
    layer = layer.rotate(math.sin(i * 0.6) * 5 + 2, resample=Image.BICUBIC)
    sh = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    sh.putalpha(layer.getchannel("A").point(lambda a: int(a * 0.35)))
    x, y = 26 + i * step, 110 + int(math.sin(i * 1.2) * 9)
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(5)), (x + 4, y + 8))
    img.alpha_composite(layer, (x, y))

pill = (40, 300, 470, 368)
sp.pill(img, pill, color=sp.rgb(sp.COL["Ink"]) + (235,))
sp.title(img, "Garde l'équilibre !", pill, 42)
img.convert("RGB").save(OUT)
print(OUT, img.size)
