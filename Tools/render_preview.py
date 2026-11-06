"""
Aperçus visuels de Bascule sans Unity, à partir des PNG exportés par Tools/ArtPreview.

  dotnet run --project Tools/ArtPreview -- Exports/art
  python3 Tools/render_preview.py Exports/art docs/apercu

Produit : tetes.png (toutes les têtes, la collection), expressions.png (les états de la mascotte), poids.png.
Les écrans complets sont produits par Tools/scene_preview.py.
"""
import json
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Exports/art")
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "docs/apercu")
FONT = os.path.join(ROOT, "Assets/Bascule/Resources/Fonts/Fredoka-Bold.ttf")
T = json.load(open(os.path.join(ROOT, "Assets/Bascule/Resources/bascule_tuning.json"), encoding="utf-8"))
os.makedirs(OUT, exist_ok=True)

INK = (43, 45, 66)
COLORS = {1: "#FFC93C", 2: "#FF8A3D", 4: "#FF4F5E", 8: "#F25CA2", 16: "#9B5DE5", 32: "#3A86FF", 64: "#12B5A6", 128: "#7BD43A"}


def font(size):
    return ImageFont.truetype(FONT, size)


def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def heads_sheet():
    rows = [l.rstrip("\n").split("\t") for l in open(os.path.join(ART, "heads.tsv"), encoding="utf-8")]
    cols, cw, ch = 8, 250, 300
    img = Image.new("RGB", (cols * cw + 40, 140 + math.ceil(len(rows) / cols) * ch), (255, 246, 229))
    d = ImageDraw.Draw(img)
    d.text((img.width // 2, 70), f"Bascule — les {len(rows)} têtes", font=font(64), fill=INK, anchor="mm")
    for i, (hid, name, kind, value, _) in enumerate(rows):
        x, y = 20 + (i % cols) * cw, 140 + (i // cols) * ch
        d.rounded_rectangle([x + 8, y + 8, x + cw - 8, y + ch - 8], 28, fill=(255, 255, 255))
        head = Image.open(os.path.join(ART, "heads", f"{int(hid):02}.png")).resize((210, 210), Image.LANCZOS)
        img.paste(head, (x + 20, y + 14), head)
        d.text((x + cw // 2, y + 232), name, font=font(30), fill=INK, anchor="mm")
        label = {"Default": "départ", "Score": f"{int(value):,} pts".replace(",", " "), "DailyStreak": f"{value} jours",
                 "NearMiss": f"sauvé {value} %", "Balloon": "envoler un 128", "Combo": f"combo ×{value}", "WorldTop": f"top {value} monde",
                 "Level": f"niveau {value}", "Shop": f"{int(value):,} pièces".replace(",", " "), "Premium": "achat"}.get(kind, kind)
        d.text((x + cw // 2, y + 268), label, font=font(24), fill=(141, 153, 174), anchor="mm")
    img.save(os.path.join(OUT, "tetes.png"))


def expressions_sheet():
    moods = [("Calm", "Stable"), ("Tense", "Crispé"), ("Grimace", "Ça force"), ("Effort", "Dernier effort"), ("Happy", "Fusion"), ("Grin", "128 envolé"), ("Sad", "A lâché")]
    back = Image.open(os.path.join(ART, "layers", "00_back.png"))
    front = Image.open(os.path.join(ART, "layers", "00_front.png"))
    cw = 260
    img = Image.new("RGB", (len(moods) * cw + 40, 400), (189, 231, 255))
    d = ImageDraw.Draw(img)
    d.text((img.width // 2, 50), "La mascotte réagit à l'inclinaison et aux événements", font=font(44), fill=INK, anchor="mm")
    for i, (face, label) in enumerate(moods):
        x = 20 + i * cw
        layer = Image.alpha_composite(back.copy(), Image.open(os.path.join(ART, "faces", f"{face}.png")))
        layer = Image.alpha_composite(layer, front).resize((230, 230), Image.LANCZOS)
        img.paste(layer, (x + 15, 90), layer)
        d.text((x + cw // 2, 350), label, font=font(32), fill=INK, anchor="mm")
    img.save(os.path.join(OUT, "expressions.png"))


def weights_row():
    vals = [1, 2, 4, 8, 16, 32, 64, 128]
    img = Image.new("RGBA", (8 * 240 + 40, 330), (189, 231, 255, 255))
    d = ImageDraw.Draw(img)
    for i, v in enumerate(vals):
        w = Image.open(os.path.join(ART, "weights", f"{v}.png")).resize((230, 230), Image.LANCZOS)
        x = 20 + i * 240
        img.alpha_composite(w, (x, 40))
        t = str(v)
        fs = int(230 * [0.28, 0.24, 0.17][len(t) - 1] * 1.3)
        cx, cy = x + 115, 40 + 230 - int(230 * 0.395)
        d.text((cx, cy + 3), t, font=font(fs), fill=(255, 255, 255, 200), anchor="mm")
        d.text((cx, cy), t, font=font(fs), fill=INK, anchor="mm")
    img.convert("RGB").save(os.path.join(OUT, "poids.png"))


if __name__ == "__main__":
    heads_sheet()
    expressions_sheet()
    weights_row()
    print("Aperçus écrits dans", OUT)
