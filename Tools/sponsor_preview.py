"""
Aperçu d'une tête de partenaire en cours de validation (pas encore dans le jeu), avec les MÊMES images que le jeu.

  dotnet run --project Tools/ArtPreview -c Release -- Exports/art
  python3 Tools/sponsor_preview.py Exports/art docs/apercu sambbach

Produit : la tête en grand, le personnage en pied (maillot et écusson), l'accueil et une partie avec ce personnage.
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import scene_preview as sp  # noqa: E402

KEY = sys.argv[3] if len(sys.argv) > 3 else "sambbach"
NAME = {"sambbach": "Samb'bach"}.get(KEY, KEY)
BODY = f"mascot_h/{KEY}"
W, H = sp.W, sp.H
GREEN, YELLOW, ORANGE = (0, 166, 81), (255, 214, 10), (247, 162, 59)


def gradient(top, bottom):
    g = Image.new("RGBA", (1, H))
    for y in range(H):
        t = y / (H - 1)
        g.putpixel((0, y), tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))
    return g.resize((W, H))


def rays(img, cx, cy, radius, color, n=16):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for i in range(n):
        a0 = i * 2 * math.pi / n
        a1 = a0 + math.pi / n
        d.polygon([(cx, cy), (cx + math.cos(a0) * radius, cy + math.sin(a0) * radius), (cx + math.cos(a1) * radius, cy + math.sin(a1) * radius)], fill=color)
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(6)))


def glow(img, box, color, blur):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse(box, fill=color)
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(blur)))


def character_card():
    """La tête en grand et le personnage en pied, sur les couleurs du BDE (jaune pâle, orange, vert)."""
    img = gradient((255, 241, 184), (255, 196, 102))
    rays(img, W / 2, 760, 1300, (255, 255, 255, 46))
    sp.title(img, NAME, (40, 70, W - 40, 230), 132, color=GREEN)
    sp.title(img, "Tête partenaire du BDE", (40, 220, W - 40, 290), 50, color=(255, 255, 255))
    # Personnage en pied, en grand : on agrandit le monde (PPU) le temps du dessin.
    old_ppu, old_ground = sp.PPU, sp.GROUND_Y
    sp.PPU, sp.GROUND_Y = 158.0, -4.0
    c = sp.Canvas(img)
    glow(img, (W / 2 - 420, 520, W / 2 + 420, 1500), (255, 255, 255, 120), 80)
    sp.mascot_scene(c, angle=-4.0, expression="Happy", weights=False, head=KEY, body=BODY)
    sp.PPU, sp.GROUND_Y = old_ppu, old_ground
    # Étiquette du code (comme il sera proposé dans Réglages → Code promo).
    tag = (W / 2 - 330, H - 190, W / 2 + 330, H - 80)
    sp.pill(img, tag, color=GREEN + (255,))
    sp.title(img, "Code : SAMB'BACH", (tag[0] + 20, tag[1], tag[2] - 20, tag[3] - 6), 58)
    return img


def portrait_card():
    """Portrait (collection, carte de score) et gros plan de l'écusson cousu sur le maillot."""
    img = gradient((43, 27, 84), (26, 18, 52))
    rays(img, W / 2, 560, 1200, (255, 214, 102, 34))
    glow(img, (W / 2 - 380, 180, W / 2 + 380, 940), (255, 214, 102, 110), 70)
    p = sp.load(f"sponsors/{KEY}_portrait.png").resize((760, 760), Image.LANCZOS)
    img.alpha_composite(p, (int(W / 2 - 380), 170))
    sp.title(img, NAME, (40, 900, W - 40, 1020), 96, color=YELLOW)
    sp.title(img, "Coiffe du carnaval, créoles dorées", (40, 1010, W - 40, 1070), 42, color=(216, 233, 255), outline=False)
    # Gros plan sur le torse : le logo du BDE en écusson brodé.
    torso = sp.load(f"{BODY}/torso.png")
    t = torso.resize((int(torso.width * 1.8), int(torso.height * 1.8)), Image.LANCZOS)
    card = (W / 2 - 430, 1100, W / 2 + 430, 1100 + 780)
    sp.card(img, card, tint=(255, 244, 214))
    sp.title(img, "Logo du BDE cousu sur le maillot", (card[0] + 20, card[1] + 26, card[2] - 20, card[1] + 92), 42, color=sp.INK, outline=False)
    img.alpha_composite(t, (int(W / 2 - t.width / 2), int(card[1] + 112)))
    return img


def home():
    s = sp.home_screen(head=KEY, body=BODY, expression="Happy")
    return s.img


def game():
    c = sp.world(1)
    sp.mascot_scene(c, angle=9.0, expression="Tense", head=KEY, body=BODY)
    sp.weight(c, 8, -1.9, sp.ORTHO - 5.1)
    sp.hud(c, angle=9.0, danger=0.2)
    return c.img


if __name__ == "__main__":
    out = sp.OUT
    shots = [("carte", character_card()), ("portrait", portrait_card()), ("accueil", home()), ("partie", game())]
    for name, im in shots:
        im.convert("RGB").save(os.path.join(out, f"sponsor_{KEY}_{name}.png"))
    strip = Image.new("RGB", (W * len(shots) + 20 * (len(shots) + 1), H + 40), (24, 22, 40))
    for i, (_, im) in enumerate(shots):
        strip.paste(im.convert("RGB"), (20 + i * (W + 20), 20))
    strip.resize((strip.width // 2, strip.height // 2), Image.LANCZOS).save(os.path.join(out, f"sponsor_{KEY}.png"))
    print("ok")
