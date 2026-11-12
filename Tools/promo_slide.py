"""
Diapositive de présentation de Bascule (16:9), à glisser dans un diaporama : logo, accroche, points forts,
Basile en équilibre, l'écran du jeu dans un téléphone et un QR code vers l'App Store.
Mêmes images que le jeu (Tools/ArtPreview) et mêmes maquettes (docs/apercu).

  dotnet run --project Tools/ArtPreview -c Release -- Exports/art
  python3 Tools/promo_slide.py Exports/art docs/promo

Produit bascule_diapo.png (1920 x 1080) et bascule_diapo_4k.png (3840 x 2160, pour les grands écrans).
"""
import io
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
ART = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Exports/art")
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "docs/promo")
sys.argv = [sys.argv[0], ART, os.path.join(ROOT, "docs/apercu")]
sys.path.insert(0, HERE)
import scene_preview as sp  # noqa: E402
import segno  # noqa: E402

STORE_URL = "https://apps.apple.com/app/id6831568074"
S = 2                      # tout est dessiné en 4K puis réduit
W, H = 1920 * S, 1080 * S
INK = (43, 45, 66)
LOGO = ["Primary", "Accent", "Play", "Secondary", "Purple", "Primary", "Accent"]


def px(v):
    return int(round(v * S))


def soft_shadow(img, box, radius, alpha=90, blur=24, dy=14):
    l, t, r, b = box
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).rounded_rectangle((l, t + px(dy), r, b + px(dy)), px(radius), fill=(30, 20, 60, alpha))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(px(blur))))


def background():
    """Ciel du jeu (premier monde), soleil, nuages, collines et prairie."""
    sky = sp.load("scene/sky0.png").resize((W, H), Image.BICUBIC)
    img = sky.copy()
    sun = sp.load("scene/sun0.png")
    sun = sun.resize((px(420), px(420) * sun.height // sun.width), Image.LANCZOS)
    img.alpha_composite(sun, (W - px(560), px(-60)))
    for i, (x, y, w) in enumerate([(120, 70, 330), (760, 40, 260), (1230, 120, 300)]):
        c = sp.load(f"scene/cloud0_{i % 3}.png")
        c = c.resize((px(w), px(w) * c.height // c.width), Image.LANCZOS)
        img.alpha_composite(c, (px(x), px(y)))
    for name, top, k in (("far0", 640, 1.0), ("near0", 760, 1.05)):
        hill = sp.load(f"scene/{name}.png")
        hh = int(W * k * hill.height / hill.width)
        img.alpha_composite(hill.resize((int(W * k), hh), Image.LANCZOS), (int(-(W * k - W) / 2), px(top)))
    ground = sp.load("scene/ground0.png")
    gh = int(W * ground.height / ground.width)
    img.alpha_composite(ground.resize((W, gh), Image.LANCZOS), (0, H - gh + px(40)))
    # Voile clair à gauche : le texte reste lisible sur le ciel.
    veil = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(veil).ellipse((-px(500), -px(300), px(1150), px(1250)), fill=(255, 255, 255, 70))
    img.alpha_composite(veil.filter(ImageFilter.GaussianBlur(px(120))))
    return img


def basile():
    """Basile qui tient la planche chargée de poids, détouré (mêmes pièces que dans le jeu)."""
    old = (sp.W, sp.H, sp.PPU, sp.GROUND_Y)
    sp.W, sp.H, sp.PPU, sp.GROUND_Y = 2400, 2600, 260.0, -4.2
    canvas = Image.new("RGBA", (sp.W, sp.H), (0, 0, 0, 0))
    sp.mascot_scene(sp.Canvas(canvas), angle=7.0, expression="Grin", weights=True)
    sp.W, sp.H, sp.PPU, sp.GROUND_Y = old
    return canvas.crop(canvas.getbbox())


def phone(screen_path, height):
    """Téléphone : coque sombre arrondie, écran du jeu, île en haut, reflet."""
    shot = Image.open(screen_path).convert("RGBA")
    sh = height - px(36)
    sw = int(sh * shot.width / shot.height)
    w = sw + px(36)
    ph = Image.new("RGBA", (w, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(ph)
    d.rounded_rectangle((0, 0, w - 1, height - 1), px(58), fill=(28, 27, 34, 255))
    d.rounded_rectangle((px(4), px(4), w - px(5), height - px(5)), px(55), outline=(90, 92, 110, 255), width=px(3))
    screen = shot.resize((sw, sh), Image.LANCZOS)
    mask = Image.new("L", (sw, sh), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, sw - 1, sh - 1), px(42), fill=255)
    ph.paste(screen, (px(18), px(18)), mask)
    d.rounded_rectangle((w / 2 - px(62), px(30), w / 2 + px(62), px(64)), px(17), fill=(10, 10, 14, 255))
    gloss = Image.new("RGBA", ph.size, (0, 0, 0, 0))
    ImageDraw.Draw(gloss).polygon([(0, 0), (w * 0.55, 0), (0, height * 0.42)], fill=(255, 255, 255, 26))
    ph.alpha_composite(gloss)
    return ph


def qr_card(img, box):
    """QR code vers l'App Store en grand (l'icône du jeu au milieu), « Scanne et joue ! » dessous."""
    l, t, r, b = box
    soft_shadow(img, box, 34)
    ImageDraw.Draw(img).rounded_rectangle(box, px(34), fill=(255, 255, 255, 255))
    qr = segno.make(STORE_URL, error="h")
    buf = io.BytesIO()
    qr.save(buf, kind="png", scale=20, border=1, dark="#2B2D42")
    code = Image.open(io.BytesIO(buf.getvalue())).convert("RGBA")
    size = (r - l) - px(70)
    code = code.resize((size, size), Image.NEAREST)
    qx, qy = l + px(35), t + px(30)
    img.alpha_composite(code, (qx, qy))
    icon = sp.load("icone/icone_1024.png").resize((int(size * 0.24), int(size * 0.24)), Image.LANCZOS)
    pad = px(8)
    ix, iy = qx + (size - icon.width) // 2, qy + (size - icon.height) // 2
    ImageDraw.Draw(img).rounded_rectangle((ix - pad, iy - pad, ix + icon.width + pad, iy + icon.height + pad), px(20), fill=(255, 255, 255, 255))
    img.alpha_composite(icon, (ix, iy))
    y = qy + size + px(14)
    sp.title(img, "Scanne et joue !", (l, y, r, y + px(56)), px(46), color=INK, outline=False)
    sp.title(img, "Gratuit sur l'App Store", (l, y + px(54), r, y + px(96)), px(30), color=sp.rgb("#5A5F80"), outline=False)


def feature(img, x, y, icon, color, text):
    r = px(40)
    cx, cy = x + r, y
    ImageDraw.Draw(img).ellipse((cx - r - px(4), cy - r - px(4), cx + r + px(4), cy + r + px(4)), fill=sp.OUTLINE + (255,))
    ImageDraw.Draw(img).ellipse((cx - r, cy - r, cx + r, cy + r), fill=sp.rgb(sp.COL.get(color, color)) + (255,))
    sp.tinted_icon(img, icon, (cx - px(25), cy - px(25), cx + px(25), cy + px(25)), "#FFFFFF")
    sp.title(img, text, (x + px(104), cy - px(36), x + px(900), cy + px(36)), px(42), align="lm")


def slide():
    img = background()
    # Logo : icône du jeu puis les lettres de BASCULE qui se balancent, comme sur l'accueil.
    icon = sp.load("icone/icone_1024.png").resize((px(170), px(170)), Image.LANCZOS)
    soft_shadow(img, (px(110), px(78), px(280), px(248)), 38, alpha=70, blur=14, dy=8)
    img.alpha_composite(icon, (px(110), px(78)))
    for i, ch in enumerate("BASCULE"):
        layer = Image.new("RGBA", (px(150), px(200)), (0, 0, 0, 0))
        sp.title(layer, ch, (0, 0, px(150), px(200)), px(150), color=sp.rgb(sp.COL[LOGO[i]]))
        layer = layer.rotate(math.sin(i * 0.6) * 5 + 2, resample=Image.BICUBIC)
        img.alpha_composite(layer, (px(300) + i * px(103), px(62) + int(math.sin(i * 1.2) * px(8))))
    sp.title(img, "Garde Basile en équilibre !", (px(110), px(275), px(1100), px(365)), px(68), align="lm")
    sp.title(img, "Le jeu d'équilibre gratuit qui rend accro.", (px(112), px(355), px(1100), px(415)), px(40), color=INK, align="lm", outline=False)
    feats = [
        ("Target", "Accent", "Fusionne les poids, vise le 128"),
        ("Swords", "Secondary", "Défie tes amis en duel, en direct"),
        ("Face", "Purple", "67 têtes à collectionner"),
        ("Trophy", "Play", "Un défi par jour, un classement mondial"),
    ]
    for k, (ic, col, text) in enumerate(feats):
        feature(img, px(118), px(520) + k * px(112), ic, col, text)

    # Basile au milieu, posé sur la prairie, la planche chargée de poids.
    b = basile()
    bh = px(660)
    b = b.resize((int(b.width * bh / b.height), bh), Image.LANCZOS)
    img.alpha_composite(b, (px(1165) - b.width // 2, px(1035) - bh))
    # À droite : le jeu dans un téléphone, et le QR code en grand pour l'installer.
    ph = phone(os.path.join(ROOT, "docs/apercu/ecran_partie.png"), px(520))
    ph = ph.rotate(-6, resample=Image.BICUBIC, expand=True)
    pos = (px(1675) - ph.width // 2, px(36))
    shadow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    mask = ph.split()[3].point(lambda a: int(a * 0.45))
    shadow.paste((30, 20, 60, 255), (pos[0] + px(14), pos[1] + px(22)), mask)
    img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(px(20))))
    img.alpha_composite(ph, pos)
    qr_card(img, (px(1500), px(610), px(1850), px(1050)))
    sp.title(img, "ORPHÉE X", (px(110), H - px(62), px(500), H - px(20)), px(28), color=(255, 255, 255), align="lm")
    return img


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    big = slide().convert("RGB")
    big.save(os.path.join(OUT, "bascule_diapo_4k.png"))
    big.resize((1920, 1080), Image.LANCZOS).save(os.path.join(OUT, "bascule_diapo.png"))
    print("ok")
