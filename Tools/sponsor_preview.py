"""
Aperçu d'une tête de partenaire (offerte avec un code, Core/Promo.cs), avec les MÊMES images que le jeu.

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
# Clé du partenaire -> (nom, numéro de la tête dans HeadCatalog).
NAME, HEAD = {"sambbach": ("Samb'bach", 66)}[KEY]
BODY = f"mascot_h/{HEAD}"
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
    sp.mascot_scene(c, angle=-4.0, expression="Happy", weights=False, head=HEAD, body=BODY)
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
    sp.title(img, "Tresse sur l'épaule, coiffe du carnaval", (40, 1010, W - 40, 1070), 42, color=(216, 233, 255), outline=False)
    # Gros plan sur la robe : le logo du BDE en broche dorée sur le corsage.
    card = (W / 2 - 430, 1100, W / 2 + 430, 1100 + 780)
    sp.card(img, card, tint=(255, 244, 214))
    sp.title(img, "Logo du BDE en broche sur la robe", (card[0] + 20, card[1] + 26, card[2] - 20, card[1] + 92), 42, color=sp.INK, outline=False)
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    old_ppu, old_ground = sp.PPU, sp.GROUND_Y
    ppu, ground = 205.0, -5.2
    sp.PPU, sp.GROUND_Y = ppu, ground
    sp.mascot(sp.Canvas(layer), 0, -2.0, 0.0, "Grin", head=HEAD, body=BODY)
    sp.PPU, sp.GROUND_Y = old_ppu, old_ground
    # Des épaules au bas de la jupe, centré dans la carte.
    top_y = H / 2 - (ground + 3.42) * ppu
    crop = layer.crop((int(W / 2 - 420), int(top_y), int(W / 2 + 420), int(top_y + 640)))
    mask = Image.new("L", crop.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, crop.width, crop.height), 40, fill=255)
    crop.putalpha(Image.composite(crop.split()[3], mask, mask))
    img.alpha_composite(crop, (int(W / 2 - 420), int(card[1] + 104)))
    return img


def home():
    s = sp.home_screen(head=HEAD, body=BODY, expression="Happy")
    return s.img


def game():
    c = sp.world(1)
    sp.mascot_scene(c, angle=9.0, expression="Tense", head=HEAD, body=BODY)
    sp.weight(c, 8, -1.9, sp.ORTHO - 5.1)
    sp.hud(c, angle=9.0, danger=0.2)
    return c.img


def child(parent, ax, ay, x, y, w, h, px=0.5, py=0.5):
    """Ui.Place dans un parent (boîte écran)."""
    l, t, r, b = parent
    axp, ayp = l + ax * (r - l), t + (1 - ay) * (b - t)
    cx, cy = axp + x - (px - 0.5) * w, ayp - y + (py - 0.5) * h
    return (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)


def shaded_settings():
    img = sp.settings_screen().img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 170)))
    return img


def code_prompt():
    """Réglages → Code promo : la fenêtre de saisie (PromptScreen), le code tapé."""
    img = shaded_settings()
    card = (W / 2 - 470, H * (1 - 0.66) - 470, W / 2 + 470, H * (1 - 0.66) + 470)
    sp.card(img, card)
    sp.title(img, "Entre ton code", child(card, 0.5, 1, 0, -110, 860, 140), 56, color=sp.INK, outline=False)
    sp.title(img, "Le code d'un partenaire (BDE, créateur…)", child(card, 0.5, 1, 0, -230, 860, 60), 40, color=(140, 153, 173), outline=False)
    sp.title(img, "t'offre une tête introuvable ailleurs.", child(card, 0.5, 1, 0, -285, 860, 60), 40, color=(140, 153, 173), outline=False)
    field = child(card, 0.5, 0.5, 0, 30, 800, 150)
    sp.pill(img, field, color=(255, 255, 255, 255))
    sp.title(img, "Samb'bach", field, 60, color=sp.INK, outline=False)
    sp.button(img, child(card, 0.5, 0, 205, 120, 390, 150), "Play", "Valider", "Check", 54)
    sp.button(img, child(card, 0.5, 0, -205, 120, 390, 150), "Muted", "Plus tard", "Close", 50)
    return img


def code_reward():
    """Code accepté (HeadRevealScreen) : le personnage complet tombe du ciel avec sa planche, atterrit dans les
    confettis ; bandeau, nom, partenaire, puis « La porter » / « Plus tard ». Image figée à la fin de l'animation."""
    img = Image.new("RGBA", (W, H), (255, 231, 168, 255))
    glow(img, (W / 2 - 1200, H - 850 + 200 - 850, W / 2 + 1200, H + 200 + 850 - 850 + 850), (255, 153, 46, 150), 120)
    rays(img, W / 2, H / 2 + 60, 1500, (255, 255, 255, 107), 18)
    glow(img, (W / 2 - 575, H / 2 + 120 - 575, W / 2 + 575, H / 2 + 120 + 575), (255, 255, 255, 190), 90)
    old_ppu, old_ground = sp.PPU, sp.GROUND_Y
    sp.PPU, sp.GROUND_Y = 150.0, -560.0 / 150.0
    sp.mascot_scene(sp.Canvas(img), angle=0.8, expression="Happy", weights=False, head=HEAD, body=BODY)
    sp.PPU, sp.GROUND_Y = old_ppu, old_ground
    # Confettis aux couleurs du Brésil, en l'air.
    rnd = __import__("random").Random(3)
    cols = [(0, 166, 81), (255, 214, 10), (47, 111, 224), (255, 255, 255), (255, 143, 184)]
    for _ in range(46):
        x, y = rnd.uniform(60, W - 60), rnd.uniform(380, 1500)
        if abs(x - W / 2) < 260 and 700 < y < 1500:
            continue
        piece = Image.new("RGBA", (26, 14), cols[rnd.randrange(len(cols))] + (255,)).rotate(rnd.uniform(0, 180), expand=True)
        img.alpha_composite(piece, (int(x), int(y)))
    kick = (W / 2 - 280, 130 - 48, W / 2 + 280, 130 + 48)
    sp.pill(img, kick, color=GREEN + (255,))
    sp.title(img, "Code accepté !", (kick[0] + 24, kick[1] + 2, kick[2] - 24, kick[3] - 6), 46)
    sp.title(img, NAME, (40, 290 - 85, W - 40, 290 + 85), 128, color=GREEN)
    sp.title(img, "Offerte par le BDE Samb'bach", (50, 405 - 35, W - 50, 405 + 35), 48)
    sp.button(img, (W / 2 - 380, H - 200 - 83, W / 2 + 380, H - 200 + 83), "Play", "La porter", "Face", 60)
    sp.button(img, (W / 2 - 260, H - 62 - 58, W / 2 + 260, H - 62 + 58), "Muted", "Plus tard", None, 46)
    return img


if __name__ == "__main__":
    out = sp.OUT
    shots = [("carte", character_card()), ("portrait", portrait_card()), ("accueil", home()), ("partie", game())]
    for name, im in shots:
        im.convert("RGB").save(os.path.join(out, f"sponsor_{KEY}_{name}.png"))
    strip = Image.new("RGB", (W * len(shots) + 20 * (len(shots) + 1), H + 40), (24, 22, 40))
    for i, (_, im) in enumerate(shots):
        strip.paste(im.convert("RGB"), (20 + i * (W + 20), 20))
    strip.resize((strip.width // 2, strip.height // 2), Image.LANCZOS).save(os.path.join(out, f"sponsor_{KEY}.png"))
    # Saisie du code dans les réglages, puis la récompense.
    code = [code_prompt(), code_reward()]
    strip = Image.new("RGB", (W * 2 + 60, H + 40), (24, 22, 40))
    for i, im in enumerate(code):
        strip.paste(im.convert("RGB"), (20 + i * (W + 20), 20))
    strip.resize((strip.width // 2, strip.height // 2), Image.LANCZOS).save(os.path.join(out, f"sponsor_{KEY}_code.png"))
    print("ok")
