"""
Maquettes des écrans de Bascule, assemblées avec les MÊMES images que le jeu (exportées par Tools/ArtPreview)
et la même géométrie (squelette de la mascotte, planche, poids). Sert à juger la direction artistique sans Unity.

  dotnet run --project Tools/ArtPreview -- Exports/art
  python3 Tools/scene_preview.py Exports/art docs/apercu
"""
import json
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Exports/art")
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "docs/apercu")
FONT = os.path.join(ROOT, "Assets/Bascule/Resources/Fonts/Fredoka-Bold.ttf")
T = json.load(open(os.path.join(ROOT, "Assets/Bascule/Resources/bascule_tuning.json"), encoding="utf-8"))
os.makedirs(OUT, exist_ok=True)

W, H = 1080, 1920
ORTHO = 9.6
PPU = H / (2 * ORTHO)  # pixels écran par unité monde
INK = (43, 45, 66)

# Constantes copiées de Core (MascotRig, WeightArtist, WorldLayout)
RIG = dict(HipX=0.34, HipY=1.62, Thigh=0.7, Shin=0.68, AnkleX=0.5, AnkleY=0.34, ShortsY=1.72, TorsoY=2.62,
           ShoulderX=0.8, ShoulderY=3.2, UpperArm=1.16, Forearm=1.12, HeadY=4.14, HeadSize=2.3, CushionY=4.86,
           PivotHeight=4.96, HandReach=1.02, HandBelow=0.05)
PART_PPU = 160.0
BODY_BOTTOM, BODY_TOP, BOTTOM_HW, PLATE_Y = 0.07, 0.69, 0.465, 0.395
BODY_CY = (BODY_BOTTOM + BODY_TOP) / 2
GROUND_Y = -ORTHO + 2.1


def font(size):
    return ImageFont.truetype(FONT, int(size))


def load(path):
    return Image.open(os.path.join(ART, path)).convert("RGBA")


def to_px(x, y):
    return W / 2 + x * PPU, H / 2 - y * PPU


class Canvas:
    def __init__(self, bg):
        self.img = bg

    def sprite(self, im, wx, wy, pivot=None, scale=1.0, rot=0.0, flipx=False, flipy=False, alpha=1.0):
        """Pose un sprite : pivot en pixels du sprite (origine en bas à gauche, comme le Raster), rotation en degrés (sens trigo)."""
        w, h = im.size
        px, py = pivot if pivot else (w / 2, h / 2)
        if flipx:
            im = im.transpose(Image.FLIP_LEFT_RIGHT); px = w - px
        if flipy:
            im = im.transpose(Image.FLIP_TOP_BOTTOM); py = h - py
        nw, nh = max(1, int(round(w * scale))), max(1, int(round(h * scale)))
        im = im.resize((nw, nh), Image.LANCZOS)
        pxs, pys = px * scale, (h - py) * scale  # pivot en coordonnées PIL
        side = int(2 * max(math.hypot(pxs, pys), math.hypot(nw - pxs, pys), math.hypot(pxs, nh - pys), math.hypot(nw - pxs, nh - pys))) + 4
        pad = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        pad.paste(im, (int(side / 2 - pxs), int(side / 2 - pys)))
        if rot:
            pad = pad.rotate(rot, resample=Image.BICUBIC)
        if alpha < 1:
            a = pad.split()[3].point(lambda v: int(v * alpha))
            pad.putalpha(a)
        sx, sy = to_px(wx, wy)
        self.img.alpha_composite(pad, (int(sx - side / 2), int(sy - side / 2)))


def two_bone(ax, ay, tx, ty, l1, l2, bend):
    dx, dy = tx - ax, ty - ay
    d = math.hypot(dx, dy)
    d = max(abs(l1 - l2) + 1e-4, min(l1 + l2 - 1e-4, d))
    base = math.atan2(dy, dx)
    c = max(-1, min(1, (l1 * l1 + d * d - l2 * l2) / (2 * l1 * d)))
    a = base + bend * math.acos(c)
    return ax + math.cos(a) * l1, ay + math.sin(a) * l1


def angle_deg(ax, ay, bx, by):
    return math.degrees(math.atan2(by - ay, bx - ax))


def world(theme):
    """Fond : ciel, soleil, collines, nuages, prairie."""
    sky = load(f"scene/sky{theme}.png").resize((W, H), Image.BICUBIC)
    c = Canvas(sky)
    if theme in (4, 10, 12, 13):
        stars = load("scene/stars.png").resize((W, W))
        c.img.alpha_composite(stars, (0, 0))
    c.sprite(load(f"scene/sun{theme}.png"), 2.4, 6.6, scale=PPU * 3.4 / 384)
    for i, (x, y, s) in enumerate([(-3.3, 7.6, 3.4), (2.9, 3.6, 2.6), (-2.4, 1.6, 2.2)] if theme != 13 else []):  # pas de nuages sur la Lune
        c.sprite(load(f"scene/cloud{theme}_{i}.png"), x, y, scale=PPU * s / 384)
    far = load(f"scene/far{theme}.png")
    c.sprite(far, 0, GROUND_Y + 1.0, pivot=(far.width / 2, 0), scale=W / far.width)
    near = load(f"scene/near{theme}.png")
    c.sprite(near, 0, GROUND_Y - 0.6, pivot=(near.width / 2, 0), scale=W * 1.1 / near.width)
    ground = load(f"scene/ground{theme}.png")
    c.sprite(ground, 0, GROUND_Y + 0.35, pivot=(ground.width / 2, ground.height * 0.82), scale=W / ground.width)
    return c


def mascot(c, x0, angle, squash, expression="Calm", head=0, defeated=False, body="mascot"):
    """head : numéro de tête (calques layers/NN_*.png) ou clé d'une tête en test (layers/<clé>_*.png) ;
    body : dossier des pièces du corps (« mascot » = Basile, « mascot_h/<id ou clé> » = tenue d'une tête)."""
    P = json.load(open(os.path.join(ART, f"{body}/pivots.json")))
    part = lambda k: load(f"{body}/{k}.png")
    hk = f"{head:02}" if isinstance(head, int) else head
    s = PART_PPU and PPU / PART_PPU
    g = GROUND_Y
    # Ombre au sol
    sh = Image.new("RGBA", (400, 120), (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse([20, 20, 380, 100], fill=(30, 20, 60, 90))
    sh = sh.filter(ImageFilter.GaussianBlur(14))
    c.sprite(sh, x0, g + 0.05, scale=PPU * 2.4 / 400)

    pivot = (x0, g + RIG["PivotHeight"] - squash)
    rad = math.radians(-angle)
    along = (math.cos(rad), math.sin(rad))
    below = (math.sin(rad) * RIG["HandBelow"], -math.cos(rad) * RIG["HandBelow"])

    # Jambes
    for side in (-1, 1):
        hip = (x0 + side * RIG["HipX"], g + RIG["HipY"] - squash)
        ankle = (x0 + side * RIG["AnkleX"], g + RIG["AnkleY"])
        knee = two_bone(*hip, *ankle, RIG["Thigh"], RIG["Shin"], 1 if side > 0 else -1)
        c.sprite(part("thigh"), *hip, pivot=P["thigh"][:2], scale=s, rot=angle_deg(*hip, *knee))
        c.sprite(part("shin"), *knee, pivot=P["shin"][:2], scale=s, rot=angle_deg(*knee, *ankle))
        c.sprite(part("shoe"), *ankle, pivot=P["shoe"][:2], scale=s, flipx=side < 0)
    c.sprite(part("shorts"), x0, g + RIG["ShortsY"] - squash, pivot=P["shorts"][:2], scale=s)
    c.sprite(part("torso"), x0, g + RIG["TorsoY"] - squash, pivot=P["torso"][:2], scale=s)

    # Tête (3 calques)
    hs = PPU * RIG["HeadSize"] / load(f"layers/{hk}_back.png").width
    head_y = g + RIG["HeadY"] - squash
    tilt = max(-6, min(6, angle * 0.15))
    for layer in (f"layers/{hk}_back.png", f"faces/{expression}.png", f"layers/{hk}_front.png"):
        c.sprite(load(layer), x0, head_y, scale=hs, rot=tilt)
    c.sprite(part("cushion"), *pivot, pivot=(P["cushion"][0], P["cushion"][1] - 0.08 * PART_PPU), scale=s, rot=-angle)

    # Bras
    hands = []
    for side in (-1, 1):
        sh_ = (x0 + side * RIG["ShoulderX"], g + RIG["ShoulderY"] - squash)
        if defeated:
            hand = (x0 + side * 1.15, g + RIG["ShoulderY"] - 1.6)
        else:
            hand = (pivot[0] + side * along[0] * RIG["HandReach"] + below[0], pivot[1] + side * along[1] * RIG["HandReach"] + below[1])
        elbow = two_bone(*sh_, *hand, RIG["UpperArm"], RIG["Forearm"], -1 if side > 0 else 1)
        c.sprite(part("upperArm"), *sh_, pivot=P["upperArm"][:2], scale=s, rot=angle_deg(*sh_, *elbow), flipy=side < 0)
        c.sprite(part("forearm"), *elbow, pivot=P["forearm"][:2], scale=s, rot=angle_deg(*elbow, *hand))
        hands.append((hand, side))
    return pivot, along, hands


def plank(c, pivot, angle, length):
    """La planche du jeu AVEC ses rebords (SceneArtist.PlankPreview, exportée en planches/jeu_0.png) : le bois est en bas
    de l'image (épaisseur 0,3 + 6 px de contour de chaque côté), les rebords au-dessus de ses deux bouts."""
    pl = load("planches/jeu_0.png")
    th = 0.32
    rad = math.radians(-angle)
    nx, ny = -math.sin(rad), math.cos(rad)
    cx, cy = pivot[0] + nx * th / 2, pivot[1] + ny * th / 2
    body_cy = (0.3 * PART_PPU + 12) / 2  # centre du bois, en pixels depuis le bas de l'image
    c.sprite(pl, cx, cy, pivot=(pl.width / 2, body_cy), scale=PPU / PART_PPU * (length / 6.6), rot=-angle)
    return (cx, cy), (math.cos(rad), math.sin(rad)), (nx, ny)


def size_of(v):
    w = T["weights"]
    return min(w["maxSize"], w["baseSize"] * v ** w["sizeExponent"])


def weight(c, v, wx, wy, rot=0.0, alpha=1.0):
    """Pose un poids : (wx, wy) = centre du corps."""
    s = size_of(v)
    tex_world = s / (2 * BOTTOM_HW)
    im = load(f"weights/{v}.png")
    c.sprite(im, wx, wy, pivot=(im.width / 2, im.height * BODY_CY), scale=PPU * tex_world / im.width, rot=rot, alpha=alpha)
    # Chiffre gravé (TextMesh dans Unity)
    t = str(v)
    hgt = [0.28, 0.24, 0.17][len(t) - 1] * tex_world * PPU * 1.32
    r = math.radians(rot)
    off = (PLATE_Y - BODY_CY) * tex_world
    tx, ty = wx - math.sin(r) * off, wy + math.cos(r) * off
    layer = Image.new("RGBA", (int(hgt * 4), int(hgt * 2)), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.text((layer.width / 2, layer.height / 2 + hgt * 0.05), t, font=font(hgt), fill=(255, 255, 255, int(200 * alpha)), anchor="mm")
    d.text((layer.width / 2, layer.height / 2 - hgt * 0.02), t, font=font(hgt), fill=INK + (int(255 * alpha),), anchor="mm")
    c.sprite(layer, tx, ty, rot=rot)
    return s


def on_plank(center, along, normal, lx, ly):
    return center[0] + along[0] * lx + normal[0] * ly, center[1] + along[1] * lx + normal[1] * ly


def body_half_height(v):
    return (BODY_TOP - BODY_BOTTOM) / 2 * size_of(v) / (2 * BOTTOM_HW)


def game_scene(theme=0, angle=11.0, expression="Tense", squash=0.1):
    c = world(theme)
    pivot, along, hands = mascot(c, 0, angle, squash, expression)
    center, al, nm = plank(c, pivot, angle, 6.3)
    th = 0.32
    stack = [(4, -2.45, 0), (2, -1.55, 0), (8, -0.55, 0), (16, 1.05, 0), (1, 2.55, 0)]
    for v, lx, _ in stack:
        hh = body_half_height(v)
        x, y = on_plank(center, al, nm, lx, th / 2 + hh)
        weight(c, v, x, y, rot=-angle)
    hh16 = body_half_height(16)
    x, y = on_plank(center, al, nm, 1.05, th / 2 + 2 * hh16 + body_half_height(2))
    weight(c, 2, x, y, rot=-angle)
    P = json.load(open(os.path.join(ART, "mascot/pivots.json")))
    for hand, side in hands:
        c.sprite(load("mascot/hand.png"), *hand, pivot=P["hand"][:2], scale=PPU / PART_PPU, rot=-angle, flipx=side < 0)
    return c


def main():
    c = game_scene()
    c.img.convert("RGB").save(os.path.join(OUT, "scene_brute.png"))
    print("ok")




# ============================================================================ Interface (mêmes positions que Screens.cs)

from PIL import ImageChops

OUTLINE = (42, 30, 61)
COL = dict(Paper="#FFF6E5", Primary="#FF5A5F", Play="#5BD16E", Secondary="#4A9BFF", Accent="#FFC93C", Success="#2EC4A6", Purple="#A66CFF",
           Muted="#8D99AE", Dark="#3D3B5C", Ink="#2B2D42")


def rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


SAFE_TOP = 0  # zone sûre = plein écran pour la maquette


def place(ax, ay, x, y, w, h, px=0.5, py=0.5):
    """Boîte écran (gauche, haut, droite, bas) comme Ui.Place (ancres 0..1, y vers le haut)."""
    axp, ayp = ax * W, (1 - ay) * H
    cx = axp + x - (px - 0.5) * w
    cy = ayp - y + (py - 0.5) * h
    return (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)


def nine(dst, src, box, border, tint=None, alpha=1.0):
    l, t, r, b = [int(round(v)) for v in box]
    w, h = r - l, b - t
    if tint:
        src = ImageChops.multiply(src, Image.new("RGBA", src.size, rgb(tint) + (255,) if isinstance(tint, str) else tint))
    sw, sh = src.size
    bl, bt, br, bb = border
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    xs = [(0, bl, 0, bl), (bl, sw - br, bl, w - br), (sw - br, sw, w - br, w)]
    ys = [(0, bt, 0, bt), (bt, sh - bb, bt, h - bb), (sh - bb, sh, h - bb, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 - dx0 <= 0 or dy1 - dy0 <= 0 or sx1 - sx0 <= 0 or sy1 - sy0 <= 0:
                continue
            out.paste(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    if alpha < 1:
        out.putalpha(out.split()[3].point(lambda v: int(v * alpha)))
    dst.alpha_composite(out, (l, t))


def title(img, text, box, size, color=(255, 255, 255), align="mm", outline=True):
    d = ImageDraw.Draw(img)
    l, t, r, b = box
    x = {"l": l, "m": (l + r) / 2, "r": r}[align[0]]
    y = (t + b) / 2
    f = font(size)
    if outline:
        sw = max(3, int(size * 0.07))
        d.text((x, y + size * 0.11), text, font=f, fill=(40, 26, 64, 200), anchor=align[0] + "m", stroke_width=sw, stroke_fill=(40, 26, 64, 200))
        d.text((x, y), text, font=f, fill=color, anchor=align[0] + "m", stroke_width=sw, stroke_fill=OUTLINE)
    else:
        d.text((x, y), text, font=f, fill=color, anchor=align[0] + "m")


def icon(img, name, box):
    im = load(f"ui/icon_{name}.png")
    l, t, r, b = box
    s = int(min(r - l, b - t))
    img.alpha_composite(im.resize((s, s), Image.LANCZOS), (int((l + r) / 2 - s / 2), int((t + b) / 2 - s / 2)))


def button(img, box, color, label=None, ic=None, size=60):
    nine(img, load("ui/button.png"), box, (64, 64, 64, 66), tint=COL.get(color, color))
    nine(img, load("ui/gloss.png"), box, (64, 64, 64, 66))  # reflet liquide (Ui.Button)
    l, t, r, b = box
    cl, ct, cr, cb = l + 18, t + 6, r - 18, b - 20
    cy = (ct + cb) / 2
    isz = max(56, min(120, size * 1.35)) if ic else 0
    tw = ImageDraw.Draw(img).textlength(label, font=font(size)) if label else 0
    total = isz + tw + (18 if ic and label else 0)
    x = (cl + cr) / 2 - total / 2
    if ic:
        icon(img, ic, (x, cy - isz / 2, x + isz, cy + isz / 2))
        x += isz + 18
    if label:
        title(img, label, (x, cy - size / 2, x + tw, cy + size / 2), size, align="lm")


def card(img, box, tint=None):
    nine(img, load("ui/panel.png"), box, (70, 70, 70, 70), tint=tint)


def pill(img, box, color=None):
    if color is None:
        # Verre liquide (Ui.Glass) : dessiné avec ses propres couleurs, sans teinte.
        g = load("ui/glass.png")
        l, t, r, b = box
        k = int(min(46, (b - t) / 2, (r - l) / 2))
        if k < 46:
            f = k / 46
            g = g.resize((max(2 * k + 2, int(g.width * f)), max(2 * k + 2, int(g.height * f))), Image.LANCZOS)
        nine(img, g, box, (k, k, k, k))
        return
    p = load("ui/pill.png")
    p = ImageChops.multiply(p, Image.new("RGBA", p.size, color))
    l, t, r, b = box
    k = int(min(46, (b - t) / 2, (r - l) / 2))
    if k < 46:  # comme Unity (pixelsPerUnitMultiplier) : on réduit le sprite pour que les bouts restent ronds
        f = k / 46
        p = p.resize((max(2 * k + 2, int(p.width * f)), max(2 * k + 2, int(p.height * f))), Image.LANCZOS)
    nine(img, p, box, (k, k, k, k))


def hud(c, score="1 284", best="2 310", angle=11.0, nxt=2, danger=0.0, progress=0.55):
    img = c.img
    if danger > 0:
        v = load("ui/vignette.png").resize((W, H), Image.BICUBIC)
        v.putalpha(v.split()[3].point(lambda a: int(a * danger)))
        img.alpha_composite(v)
    button(img, place(0, 1, 30, -30, 140, 148, 0, 1), "Dark", ic="Pause")
    pill(img, place(0.5, 1, 0, -40, 440, 150, 0.5, 1))
    title(img, score, place(0.5, 1, 0, -40 - 75 + 6, 440, 150), 108)
    title(img, f"Record : {best}", place(0.5, 1, 0, -222, 600, 56), 40, color=(255, 231, 160))
    tube = place(0.5, 1, 0, -300, 600, 90)
    img.alpha_composite(load("ui/level.png").resize((600, 90), Image.LANCZOS), (int(tube[0]), int(tube[1])))
    bx = max(-1.12, min(1.12, -angle / 30)) * 255 / 1.12
    bb = place(0.5, 1, bx, -300, 74, 74)
    img.alpha_composite(load("ui/bubble.png").resize((74, 74), Image.LANCZOS), (int(bb[0]), int(bb[1])))
    # Jauge de la prochaine tête (comme HudScreen) : rail sombre cerné, remplissage doré avec reflet, médaillon rond.
    rim = place(0.5, 1, -40, -400, 412, 46)
    pill(img, rim, color=OUTLINE + (255,))
    rail = (rim[0] + 6, rim[1] + 6, rim[2] - 6, rim[3] - 6)
    pill(img, rail, color=rgb("#4A4E78") + (255,))
    fill = (rail[0], rail[1], rail[0] + max(34, 400 * progress), rail[3])
    pill(img, fill, color=rgb(COL["Accent"]) + (255,))
    fh = fill[3] - fill[1]
    pill(img, (fill[0] + 7, fill[1] + fh * 0.14, fill[2] - 7, fill[1] + fh * 0.48), color=(255, 255, 255, 97))
    mc = (rim[2] + 40 - 6, (rim[1] + rim[3]) / 2)
    d = ImageDraw.Draw(img)
    d.ellipse((mc[0] - 52, mc[1] - 52, mc[0] + 52, mc[1] + 52), fill=OUTLINE)
    d.ellipse((mc[0] - 45, mc[1] - 45, mc[0] + 45, mc[1] + 45), fill=rgb(COL["Paper"]))
    head = load("heads/01.png")
    sil = Image.new("RGBA", head.size, (43, 45, 66, 0))
    sil.putalpha(head.split()[3].point(lambda a: int(a * 80 / 255)))
    img.alpha_composite(sil.resize((84, 84), Image.LANCZOS), (int(mc[0] - 42), int(mc[1] - 44)))
    box = place(1, 1, -26, -26, 210, 250, 1, 1)
    card(img, box)
    title(img, "Suivant", (box[0], box[1] + 16, box[2], box[1] + 72), 40, color=rgb(COL["Ink"]), outline=False)
    wbox = (box[0] + 30, box[3] - 108 - 75, box[0] + 180, box[3] - 108 + 75)
    w = load(f"weights/{nxt}.png").resize((150, 150), Image.LANCZOS)
    img.alpha_composite(w, (int(wbox[0]), int(wbox[1])))
    py = (wbox[1] + wbox[3]) / 2 - (0.395 - 0.5) * 150
    hgt = [0.28, 0.24, 0.17][len(str(nxt)) - 1] * 150 * 1.32
    ImageDraw.Draw(img).text(((wbox[0] + wbox[2]) / 2, py), str(nxt), font=font(hgt), fill=rgb(COL["Ink"]), anchor="mm")


def coin_pill(img, box, amount, size=50, plus=False):
    pill(img, box)
    l, t, r, b = box
    ic = load("ui/icon_Coin.png")
    ic = ImageChops.multiply(ic, Image.new("RGBA", ic.size, rgb(COL["Accent"]) + (255,)))
    s = int(size * 1.25)
    img.alpha_composite(ic.resize((s, s), Image.LANCZOS), (int(l + 18), int((t + b) / 2 - s / 2)))
    title(img, amount, (l + 30 + s, t, r - (size + 20 if plus else 20), b), size, align="lm")
    if plus:
        pl = ImageChops.multiply(load("ui/icon_Plus.png"), Image.new("RGBA", (128, 128), rgb(COL["Play"]) + (255,)))
        ps = int(size * 0.9)
        img.alpha_composite(pl.resize((ps, ps), Image.LANCZOS), (int(r - ps - 22), int((t + b) / 2 - ps / 2)))


def tinted_icon(img, name, box, color):
    im = ImageChops.multiply(load(f"ui/icon_{name}.png"), Image.new("RGBA", (128, 128), rgb(COL.get(color, color)) + (255,)))
    l, t, r, b = box
    sz = int(min(r - l, b - t))
    img.alpha_composite(im.resize((sz, sz), Image.LANCZOS), (int((l + r) / 2 - sz / 2), int((t + b) / 2 - sz / 2)))


def bar(img, box, frac, color):
    pill(img, box, color=(43, 45, 66, 56))
    l, t, r, b = box
    pill(img, (l, t, l + max(b - t, (r - l) * frac), b), color=rgb(COL[color]) + (255,))


def badge(img, button_box, text):
    l, t, r, b = button_box
    if (r - l) > 140:
        s = 76; bb = (r - 66, t - 10, r + 10, t + 66)
    else:
        s = 50; bb = (r + 26 - 50, t - 26, r + 26, t - 26 + 50)
        ImageDraw.Draw(img).ellipse(bb, fill=rgb(COL["Primary"]), outline=(255, 255, 255), width=4)
        title(img, text, bb, 32)
        return
    pill(img, bb, color=rgb(COL["Primary"]) + (255,))
    title(img, text, bb, 44 if s == 76 else 32)


def home_screen(head=0, body="mascot", expression="Calm"):
    c = world(0)
    mascot_scene(c, angle=-3.0, expression=expression, weights=False, head=head, body=body)
    img = c.img
    button(img, place(0, 1, 28, -28, 124, 130, 0, 1), "Dark", ic="Gear")
    lb = place(0, 1, 168, -32, 330, 120, 0, 1)
    pill(img, lb)
    tinted_icon(img, "Star", (lb[0] + 14, lb[1] + 14, lb[0] + 102, lb[1] + 102), "Accent")
    title(img, "Niv. 7", (lb[0] + 114, lb[1] + 12, lb[0] + 310, lb[1] + 66), 44, align="lm")
    bar(img, (lb[0] + 116, lb[3] - 40, lb[0] + 302, lb[3] - 22), 0.62, "Play")
    coin_pill(img, place(1, 1, -28, -36, 330, 112, 1, 1), "1 240", plus=True)
    word, colors = "BASCULE", ["Primary", "Accent", "Play", "Secondary", "Purple", "Primary", "Accent"]
    for i, ch in enumerate(word):
        x = (i - 3) * 118
        layer = Image.new("RGBA", (200, 240), (0, 0, 0, 0))
        title(layer, ch, (0, 0, 200, 240), 156, color=rgb(COL[colors[i]]))
        layer = layer.rotate(math.sin(i * 0.6) * 5 + 2, resample=Image.BICUBIC)
        b = place(0.5, 1, x, -262 + math.sin(i * 1.2) * 10, 200, 240)
        img.alpha_composite(layer, (int(b[0]), int(b[1])))
    tw = ImageDraw.Draw(img).textlength("Record : 2 544", font=font(44))
    tbw = tw + 48 + 58 + 12
    tb = place(0.5, 1, 0, -452, tbw, 82)
    pill(img, tb)
    icon(img, "Trophy", (tb[0] + 24, tb[1] + 12, tb[0] + 82, tb[3] - 12))
    title(img, "Record : 2 544", (tb[0] + 94, tb[1], tb[2] - 24, tb[3]), 44)
    button(img, place(0.5, 1, 0, -574, 640, 110), "Success", "Reprendre la partie", "Replay", 50)
    button(img, place(0.5, 1, 0, -748, 760, 200), "Play", "JOUER", "Play", 100)
    db = place(0.5, 1, 0, -932, 700, 130)
    button(img, db, "Secondary", "Défi du jour", "Calendar", 54)
    # Barre du bas (dock), sous les pieds de Basile.
    dk = place(0.5, 0, 0, 12, 1040, 182, 0.5, 0)
    pill(img, dk)
    for i, (ic, lab, col) in enumerate([("Face", "Têtes", "Purple"), ("Target", "Missions", "Success"), ("Bag", "Boutique", "Primary"), ("Trophy", "Classement", "Accent"), ("Swords", "Duel", "Secondary")]):
        x = (i - 2) * 204
        bb = (W / 2 + x - 58, dk[3] - 50 - 122, W / 2 + x + 58, dk[3] - 50)
        button(img, bb, col, ic=ic, size=44)
        lb = (W / 2 + x - 100, dk[3] - 8 - 44, W / 2 + x + 100, dk[3] - 8)
        f = 34
        while ImageDraw.Draw(img).textlength(lab, font=font(f)) > 196 and f > 26:
            f -= 1
        title(img, lab, lb, f)
        if i == 4:
            tg = (W / 2 + x - 80, bb[1] - 26, W / 2 + x + 80, bb[1] + 28)
            pill(img, tg, color=(255, 255, 255, 255))
            tin = (tg[0] + 4, tg[1] + 4, tg[2] - 4, tg[3] - 4)
            pill(img, tin, color=rgb(COL["Primary"]) + (255,))
            title(img, "Direct", tin, 30)
        if i == 1:
            badge(img, bb, "2")
        if i == 2:
            badge(img, bb, "!")
    return c


def gift_screen():
    """Cadeau du jour (GiftScreen V20) : 3 + 3 tuiles, puis le gros cadeau du jour 7 ; aujourd'hui brille, demain annoncé."""
    c = home_screen()
    img = c.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 170)))
    cb = place(0.5, 0.5, 0, 10, 960, 1430)
    card(img, cb)
    title(img, "Cadeau du jour", (cb[0], cb[1] + 32, cb[2], cb[1] + 152), 84, color=rgb(COL["Accent"]))
    title(img, "Jour 3 de ta série · reviens demain pour la suite !", (cb[0], cb[1] + 148, cb[2], cb[1] + 204), 40, color=rgb(COL["Muted"]), outline=False)
    today = 3
    items = ["coins", "chest", "coins", "ticket", "coins", "chest", "plank"]
    coins = [50, 0, 120, 0, 200, 0, 300]
    x0 = W / 2
    for d in range(1, 8):
        big = d == 7
        row, col = (d - 1) // 3, (d - 1) % 3
        w, h = (880, 250) if big else (262, 262)
        cx = x0 if big else x0 + (col - 1) * 280
        top = cb[1] + 220 + row * 280
        box = (cx - w / 2, top, cx + w / 2, top + h)
        done, now, nxt = d < today, d == today, d == today + 1
        if now:
            glow = Image.new("RGBA", (int(w * 1.35), int(h * 1.5)), (0, 0, 0, 0))
            ImageDraw.Draw(glow).ellipse((20, 20, glow.width - 20, glow.height - 20), fill=(255, 209, 77, 170))
            glow = glow.filter(ImageFilter.GaussianBlur(40))
            img.alpha_composite(glow, (int(cx - glow.width / 2), int(top + h / 2 - glow.height / 2)))
        card(img, box, tint=(199, 237, 209, 255) if done else (255, 237, 168, 255) if now else None)
        if (now or nxt) and not big:
            tg = (box[0] + 20, box[1] + 13, box[2] - 20, box[1] + 67)
            pill(img, tg, color=rgb(COL["Primary" if now else "Secondary"]) + (255,))
            title(img, "Aujourd'hui" if now else "Demain", tg, 32)
        else:
            lab = ("Jour 7 · Le gros cadeau" + (" · Demain" if nxt else "")) if big else f"Jour {d}"
            if big:
                title(img, lab, (box[0] + 36, box[1] + 18, box[0] + 836, box[1] + 74), 44, color=rgb(COL["Ink"]), align="lm", outline=False)
            else:
                title(img, lab, (box[0], box[1] + 14, box[2], box[1] + 66), 40, color=rgb(COL["Ink"]), outline=False)
        it = items[d - 1]
        mid = (box[1] + box[3]) / 2
        if big:
            pl = load("planches/apercu_rebords_1.png") if os.path.exists(os.path.join(ART, "planches/apercu_rebords_1.png")) else None
            if pl is not None:
                pl = pl.resize((420, int(420 * pl.height / pl.width)), Image.LANCZOS)
                img.alpha_composite(pl, (int(box[0] + 40), int(mid + 16 - pl.height / 2)))
            title(img, "Planche Bambou", (box[0] + 490, mid - 50, box[2] - 20, mid + 10), 44, color=rgb(COL["Purple"]), align="lm", outline=False)
            tinted_icon(img, "Coin", (box[0] + 492, mid + 20, box[0] + 548, mid + 76), "Accent")
            title(img, "+300", (box[0] + 560, mid + 15, box[0] + 820, mid + 81), 52, color=rgb(COL["Accent"]), align="lm")
        else:
            vy = mid - 14
            if it == "coins":
                tinted_icon(img, "Coin", (cx - 50, vy - 50, cx + 50, vy + 50), "Accent")
            elif it == "chest":
                mini_chest(img, cx - 62, vy, 124)
            else:
                dd = ImageDraw.Draw(img)
                dd.ellipse((cx - 52, vy - 52, cx + 52, vy + 52), fill=rgb(COL["Purple"]))
                title(img, "?", (cx - 52, vy - 58, cx + 52, vy + 46), 90, color=rgb(COL["Accent"]))
            cap = str(coins[d - 1]) if it == "coins" else "Coffre" if it == "chest" else "Surprise"
            if done:
                dd = ImageDraw.Draw(img)
                dd.ellipse((cx - 32, box[3] - 76, cx + 32, box[3] - 12), fill=rgb(COL["Success"]))
                icon(img, "Check", (cx - 21, box[3] - 65, cx + 21, box[3] - 23))
            elif it == "coins":
                title(img, cap, (box[0], box[3] - 92, box[2], box[3] - 40), 50, color=rgb(COL["Accent"]))
            else:
                title(img, cap, (box[0] + 12, box[3] - 92, box[2] - 12, box[3] - 40), 38, color=rgb(COL["Ink"]), outline=False)
    button(img, (W / 2 - 380, cb[3] - 244 - 80, W / 2 + 380, cb[3] - 244 + 80), "Play", "Récupérer", "Gift", 60)
    button(img, (W / 2 - 300, cb[3] - 96 - 68, W / 2 + 300, cb[3] - 96 + 68), "Purple", "x2 en vidéo", "Film", 52)
    return c


def mascot_scene(c, angle=11.0, expression="Tense", weights=True, defeated=False, squash=0.1, head=0, body="mascot"):
    pivot, along, hands = mascot(c, 0, angle, squash, expression, head=head, defeated=defeated, body=body)
    center, al, nm = plank(c, pivot, angle, 6.3)
    th = 0.32
    if weights:
        for v, lx in [(4, -2.45), (2, -1.55), (8, -0.55), (16, 1.05), (1, 2.55)]:
            x, y = on_plank(center, al, nm, lx, th / 2 + body_half_height(v))
            weight(c, v, x, y, rot=-angle)
        x, y = on_plank(center, al, nm, 1.05, th / 2 + 2 * body_half_height(16) + body_half_height(2))
        weight(c, 2, x, y, rot=-angle)
    P = json.load(open(os.path.join(ART, f"{body}/pivots.json")))
    for hand, side in hands:
        c.sprite(load(f"{body}/hand.png"), *hand, pivot=P["hand"][:2], scale=PPU / PART_PPU, rot=-angle if not defeated else side * -70, flipx=side < 0)


def game_screen():
    c = world(1)
    mascot_scene(c, angle=12.0, expression="Tense")
    # Poids tenu + texte de fusion.
    weight(c, 8, -1.9, ORTHO - 5.1)
    d = ImageDraw.Draw(c.img)
    x, y0 = to_px(-1.9, ORTHO - 5.1 - 0.5)
    _, y1 = to_px(-1.9, GROUND_Y + 5.1)
    for yy in range(int(y0), int(y1), 34):
        d.line([x, yy, x, min(yy + 18, y1)], fill=(255, 255, 255, 150), width=5)
    title(c.img, "COMBO ×1.5", (560, 590, 980, 650), 62, color=rgb(COL["Accent"]))
    title(c.img, "+48", (620, 660, 920, 730), 70)
    hud(c, angle=12.0, danger=0.25)
    return c


def gameover_screen():
    c = world(3)
    mascot_scene(c, angle=-28.0, expression="Sad", weights=False, defeated=True, squash=0.25)
    img = c.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 158)))
    rays = load("ui/rays.png").resize((1500, 1500), Image.BICUBIC).rotate(10)
    rays.putalpha(rays.split()[3].point(lambda a: int(a * 0.35)))
    rb = place(0.5, 1, 0, -560, 1500, 1500)
    img.alpha_composite(rays, (int(rb[0]), int(rb[1])))
    title(img, "Basile a lâché !", place(0.5, 1, 0, -70, 1000, 60), 44, color=(191, 224, 255))
    title(img, "« Funambule de l'extrême »", place(0.5, 1, 0, -150, 1000, 100), 66, color=rgb(COL["Accent"]))
    cb = place(0.5, 1, 0, -220, 940, 860, 0.5, 1)
    card(img, cb)
    title(img, "SCORE", (cb[0], cb[1] + 30, cb[2], cb[1] + 86), 46, color=rgb(COL["Muted"]), outline=False)
    title(img, "2 486", (cb[0], cb[1] + 75, cb[2], cb[1] + 265), 170, color=rgb(COL["Accent"]))
    title(img, "NOUVEAU RECORD !", (cb[0], cb[1] + 254, cb[2], cb[1] + 318), 50, color=rgb(COL["Primary"]))
    title(img, "Sauvetage de justesse : 97 %", (cb[0], cb[1] + 316, cb[2], cb[1] + 376), 44, color=rgb(COL["Ink"]), outline=False)
    title(img, "Niv. 7", (cb[0] + 40, cb[1] + 388, cb[0] + 230, cb[1] + 448), 46, color=rgb(COL["Secondary"]), align="lm")
    bar(img, (cb[0] + 230, cb[1] + 399, cb[0] + 700, cb[1] + 437), 0.7, "Play")
    title(img, "+258 XP", (cb[0] + 714, cb[1] + 388, cb[0] + 934, cb[1] + 448), 44, color=rgb(COL["Muted"]), align="lm", outline=False)
    # Coffre gagné : un bouton « bonbon » doré avec un petit coffre dedans (GameOverScreen.SetChestRow).
    bb = (W / 2 - 400, cb[1] + 522 - 65, W / 2 + 400, cb[1] + 522 + 65)
    button(img, bb, "Accent")
    mini_chest(img, bb[0] + 40, (bb[1] + bb[3]) / 2 - 8, 92)
    title(img, "Ouvrir le coffre !", (bb[0] + 150, bb[1] + 6, bb[2] - 18, bb[3] - 20), 50)
    # Prochain objectif : tête de la boutique.
    yc = cb[3] - 128
    img.alpha_composite(load("heads/45.png").resize((200, 200), Image.LANCZOS), (int(cb[0] + 30), int(yc - 100)))
    title(img, "Tu peux t'offrir Pirate !", (cb[0] + 260, yc - 58 - 42, cb[2] - 20, yc - 58 + 42), 44, color=rgb(COL["Ink"]), align="lm", outline=False)
    button(img, (cb[0] + 260, yc + 42 - 66, cb[0] + 600, yc + 42 + 66), "Play", "Voir", "Bag", 44)
    coin_pill(img, place(0.5, 1, -220, -1155, 340, 116), "+99", size=56)
    button(img, place(0.5, 1, 220, -1155, 330, 124), "Purple", "x2", "Film", 54)
    button(img, place(0.5, 0, 0, 610, 780, 160), "Play", "Revivre (pub)", "Film", 60)
    button(img, place(0.5, 0, 0, 430, 780, 170), "Primary", "Rejouer", "Replay", 64)
    button(img, place(0.5, 0, -200, 260, 380, 140), "Secondary", "Partager", "Share", 50)
    button(img, place(0.5, 0, 200, 260, 380, 140), "Dark", "Accueil", "Home", 50)
    return c


def shop_screen():
    c = world(2)
    img = c.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 158)))
    button(img, place(0, 1, 36, -36, 150, 156, 0, 1), "Dark", ic="Back")
    title(img, "Boutique", place(0.5, 1, -70, -112, 440, 130), 76)
    coin_pill(img, place(1, 1, -36, -60, 270, 104, 1, 1), "1 240")
    y = 230 + 10
    def offer(h, color, name, desc, price, head=None, ic=None, badge_text=None):
        nonlocal y
        x = 36 + (300 if head else 220)
        d = ImageDraw.Draw(img)
        lines, line = [], ""
        for w_ in desc.split():
            if d.textlength(line + " " + w_, font=font(44)) > W - 36 - x - 40:
                lines.append(line); line = w_
            else:
                line = (line + " " + w_).strip()
        lines.append(line)
        h = max(h, 130 + len(lines) * 52 + 200)
        box = (36, y, W - 36, y + h)
        card(img, box)
        pill(img, (box[0] + 24, box[1] + 22, box[0] + 324, box[1] + 34), color=rgb(COL[color]) + (255,))
        if head:
            hi = load(f"heads/{head}.png").resize((260, 260), Image.LANCZOS)
            img.alpha_composite(hi, (int(box[0] + 20), int(box[1] + 40)))
            x = box[0] + 300
        else:
            tinted_icon(img, ic, (box[0] + 40, box[1] + 50, box[0] + 190, box[1] + 200), color)
            x = box[0] + 220
        title(img, name, (x, box[1] + 35, box[2] - 200, box[1] + 125), 64, color=rgb(COL[color]), align="lm")
        d.multiline_text((x, box[1] + 130), "\n".join(lines), font=font(44), fill=rgb(COL["Ink"]), spacing=6)
        if badge_text:
            bb = (box[2] - 210, box[1] - 2, box[2] - 20, box[1] + 82)
            pill(img, bb, color=rgb(COL["Primary"]) + (255,))
            title(img, badge_text, bb, 46)
        button(img, (box[2] - 328, box[3] - 166, box[2] - 28, box[3] - 26), "Play", price, None, 56)
        y += h + 24
        return box
    b = offer(420, "Accent", "Offre de départ", "Plus de pubs imposées + 2 000 pièces + Basile doré", "3,99 €", head="61", badge_text="-60 %")
    title(img, "Se termine dans 47 h 12", (b[0] + 40, b[3] - 126, b[0] + 640, b[3] - 66), 44, color=rgb(COL["Primary"]), align="lm")
    offer(300, "Purple", "Sans publicité", "Plus aucune pub entre les parties, et la seconde chance sans vidéo", "2,99 €", ic="Film")
    box = (36, y, W - 36, y + 230)
    card(img, box)
    tinted_icon(img, "Film", (box[0] + 34, box[1] + 50, box[0] + 164, box[1] + 180), "Play")
    title(img, "60 pièces gratuites", (box[0] + 190, box[1] + 40, box[2] - 260, box[1] + 120), 52, color=rgb(COL["Ink"]), align="lm", outline=False)
    title(img, "Encore 5 aujourd'hui", (box[0] + 190, box[1] + 125, box[2] - 260, box[1] + 185), 44, color=rgb(COL["Muted"]), align="lm", outline=False)
    button(img, (box[2] - 250, box[1] + 40, box[2] - 30, box[1] + 190), "Play", None, "Play", 50)
    y += 254
    cw = (W - 72 - 36) / 3
    for i, (amount, price, k, bt) in enumerate([("600", "0,99 €", 0.75, None), ("2 200", "2,99 €", 0.9, "+20 %"), ("6 000", "6,99 €", 1.05, "+40 %")]):
        box = (36 + i * (cw + 18), y, 36 + i * (cw + 18) + cw, y + 470)
        card(img, box)
        sz = 150 * k
        tinted_icon(img, "Coin", ((box[0] + box[2]) / 2 - sz / 2, box[1] + 40, (box[0] + box[2]) / 2 + sz / 2, box[1] + 40 + sz), "Accent")
        title(img, amount, (box[0], box[1] + 190, box[2], box[1] + 270), 58, color=rgb(COL["Accent"]))
        if bt:
            bb = (box[2] - 144, box[1] - 6, box[2] + 6, box[1] + 64)
            pill(img, bb, color=rgb(COL["Primary"]) + (255,))
            title(img, bt, bb, 44)
        button(img, ((box[0] + box[2]) / 2 - 140, box[3] - 154, (box[0] + box[2]) / 2 + 140, box[3] - 24), "Play", price, None, 50)
    return c


def missions_screen():
    c = world(1)
    img = c.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 158)))
    button(img, place(0, 1, 36, -36, 150, 156, 0, 1), "Dark", ic="Back")
    title(img, "Missions du jour", place(0.5, 1, 70, -112, 760, 130), 76)
    rows = [("Joue 3 parties", 3, 3, "+50", True), ("Fais 32 fusions", 21, 45, "+60", False), ("Fabrique un 64", 0, 1, "+90", False)]
    y = 260
    for text, prog, target, reward, done in rows:
        box = (40, y, W - 40, y + 250)
        card(img, box, tint=(216, 247, 227, 255) if done else None)
        tinted_icon(img, "Check" if done else "Target", (box[0] + 30, box[1] + 65, box[0] + 150, box[1] + 185), "Success" if done else "Secondary")
        title(img, text, (box[0] + 170, box[1] + 15, box[0] + 730, box[1] + 125), 48, color=rgb(COL["Ink"]), align="lm", outline=False)
        bar(img, (box[0] + 170, box[3] - 80, box[0] + 530, box[3] - 40), prog / target, "Success" if done else "Secondary")
        title(img, f"{prog}/{target}", (box[0] + 548, box[3] - 88, box[0] + 708, box[3] - 32), 44, color=rgb(COL["Muted"]), align="lm", outline=False)
        tinted_icon(img, "Coin", (box[2] - 230, box[1] + 29, box[2] - 160, box[1] + 99), "Accent")
        title(img, reward, (box[2] - 150, box[1] + 29, box[2] - 20, box[1] + 99), 50, color=rgb(COL["Accent"]), align="lm")
        if not done:
            button(img, (box[2] - 284, box[3] - 136, box[2] - 24, box[3] - 26), "Purple", "Changer", "Film", 44)
        y += 276
    bb = place(0.5, 0, 0, 250, 1000, 200)
    card(img, bb)
    tinted_icon(img, "Gift", (bb[0] + 40, bb[1] + 35, bb[0] + 170, bb[1] + 165), "Accent")
    title(img, "Les 3 missions : +150 pièces", (bb[0] + 200, bb[1] + 33, bb[2] - 20, bb[1] + 103), 46, color=rgb(COL["Ink"]), align="lm", outline=False)
    bar(img, (bb[0] + 200, bb[1] + 120, bb[0] + 940, bb[1] + 160), 1 / 3, "Accent")
    title(img, "Nouvelles missions dans 5 h 12", place(0.5, 0, 0, 90, 1000, 70), 44, color=(191, 224, 255))
    return c


def share_card():
    """Carte de partage 9:16 (mêmes positions que Runtime/UI/ScoreCard.cs)."""
    PW, PH = 760, 860
    # La « photo » : la scène au moment de la bascule, sans interface, au format de l'écran.
    photo = game_scene(theme=2, angle=24.0, expression="Surprised", squash=0.2).img
    pivot_vy = 1 - 1200 / H
    focus = min(1, pivot_vy + 0.12)
    uvh = min(1, (W / (PW / PH)) / H)
    y0 = max(0, min(1 - uvh, focus - uvh / 2))
    top = int((1 - (y0 + uvh)) * H)
    crop = photo.crop((0, top, W, top + int(uvh * H))).resize((PW, PH), Image.LANCZOS)

    img = load("scene/sky2.png").resize((W, H), Image.BICUBIC)
    near = load("scene/near2.png")
    nw = int(W * 1.1); nh = int(near.height * nw / near.width)
    img.alpha_composite(near.resize((nw, nh), Image.LANCZOS), ((W - nw) // 2, H - 170 - nh))
    ground = load("scene/ground2.png")
    gh = int(ground.height * (W + 20) / ground.width)
    img.alpha_composite(ground.resize((W + 20, max(480, gh)), Image.LANCZOS), (-10, H - 440))
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 107)))
    logo = [COL[k] for k in ("Primary", "Accent", "Play", "Secondary", "Purple", "Primary", "Accent")]
    for i, ch in enumerate("BASCULE"):
        layer = Image.new("RGBA", (200, 220), (0, 0, 0, 0))
        title(layer, ch, (0, 0, 200, 220), 130, color=rgb(logo[i]))
        layer = layer.rotate(math.sin(i * 1.3) * 5, resample=Image.BICUBIC)
        bx = place(0.5, 1, (i - 3) * 112, -120 + math.sin(i * 0.9) * 10, 200, 220)
        img.alpha_composite(layer, (int(bx[0]), int(bx[1])))
    rays = load("ui/rays.png").resize((1300, 1300), Image.BICUBIC)
    rays.putalpha(rays.split()[3].point(lambda a: int(a * 0.5)))
    rb = place(0.5, 1, 0, -720, 1300, 1300)
    img.alpha_composite(rays, (int(rb[0]), int(rb[1])))

    fw, fh = PW + 60, PH + 150
    frame = Image.new("RGBA", (fw + 200, fh + 200), (0, 0, 0, 0))
    card(frame, (100, 100, 100 + fw, 100 + fh))
    frame.alpha_composite(crop, (100 + 30, 100 + 30))
    title(frame, "« Funambule de l'extrême »", (100 + 60, 100 + fh - 110, 100 + fw - 200, 100 + fh - 14), 46, color=rgb(COL["Ink"]), outline=False)
    head = load("heads/00.png").resize((280, 280), Image.LANCZOS).rotate(10, resample=Image.BICUBIC, expand=True)
    frame.alpha_composite(head, (100 + fw - 10 - head.width // 2, 100 + fh - 70 - head.height // 2))
    frame = frame.rotate(-2.5, resample=Image.BICUBIC)
    fb = place(0.5, 1, 0, -230, fw, fh, 0.5, 1)
    img.alpha_composite(frame, (int(fb[0]) - 100, int(fb[1]) - 100))

    title(img, "4 820", place(0.5, 1, 0, -1360, 1000, 220), 200, color=rgb(COL["Accent"]))
    title(img, "NOUVEAU RECORD !", place(0.5, 1, 0, -1490, 1000, 70), 54)
    title(img, "Sauvetage de justesse : 97 %", place(0.5, 1, 0, -1560, 1000, 64), 46, color=rgb("#BFFFE0"))
    peaks = [0.2, 0.55, 0.4, 0.8, 0.97, 0.5, 0.7]
    cols = ["#5BD16B", "#FFD447", "#FF9A3C", "#F2545B"]
    n = len(peaks) + 1
    for i in range(n):
        bx = place(0.5, 1, (i - (n - 1) * 0.5) * 74, -1640, 62, 62)
        if i < len(peaks):
            d = peaks[i]
            col = cols[0 if d < 0.5 else 1 if d < 0.75 else 2 if d < 0.9 else 3]
            sq = ImageChops.multiply(load("ui/button.png").resize((62, 62), Image.LANCZOS), Image.new("RGBA", (62, 62), rgb(col) + (255,)))
            img.alpha_composite(sq, (int(bx[0]), int(bx[1])))
        else:
            sq = Image.new("RGBA", (120, 120), (0, 0, 0, 0))
            sq.alpha_composite(ImageChops.multiply(load("ui/button.png").resize((62, 62), Image.LANCZOS), Image.new("RGBA", (62, 62), rgb(COL["Dark"]) + (255,))), (29, 29))
            sq = sq.rotate(18, resample=Image.BICUBIC)
            img.alpha_composite(sq, (int(bx[0]) - 29, int(bx[1]) - 29))
    bb = place(0.5, 0, 0, 36, 1000, 190, 0.5, 0)
    nine(img, load("ui/button.png"), bb, (64, 64, 64, 66), tint=COL["Primary"])
    title(img, "Tu fais mieux ? Défie-moi en duel !", (bb[0] + 30, bb[1] + 14, bb[2] - 30, bb[3] - 70), 48)
    title(img, "orpheex.fr/bascule", place(0.5, 0, 0, 22 + 36, 900, 52, 0.5, 0), 40, color=rgb("#FFE8B0"))
    return Canvas(img)


def settings_screen():
    """Réglages (mêmes positions que SettingsScreen) : sections en cartes, interrupteurs, flèches."""
    c = world(0)
    img = c.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 158)))
    button(img, place(0, 1, 36, -36, 150, 156, 0, 1), "Dark", ic="Back")
    title(img, "Réglages", place(0.5, 1, 70, -112, 700, 130), 84)
    d = ImageDraw.Draw(img)
    RowH, Pad = 128, 18
    x0, x1 = 44, W - 44
    y = 240 + 4

    def section(name, rows):
        nonlocal y
        title(img, name, (x0 + 6, y + 8, x1, y + 78), 44, color=rgb(COL["Accent"]), align="lm")
        y += 78 + 14
        box = (x0, y, x1, y + rows * RowH + Pad * 2)
        card(img, box)
        top = y + Pad
        y = box[3] + 14
        return box, top

    def row(box, top, i, ic, col, label, right, danger=False):
        ry = top + i * RowH
        l, r = box[0] + Pad + 12, box[2] - Pad - 12
        cy = ry + RowH / 2
        if i > 0:
            d.rectangle((l + 126, ry - 1, r, ry + 2), fill=(43, 45, 66, 22))
        d.ellipse((l + 6, cy - 46, l + 98, cy + 46), fill=rgb(COL[col]))
        icon(img, ic, (l + 23, cy - 29, l + 81, cy + 29))
        title(img, label, (l + 124, ry, r - 190, ry + RowH), 44, color=rgb(COL["Primary"] if danger else COL["Ink"]), align="lm", outline=False)
        if right[0] == "switch":
            on = right[1]
            tb = (r - 150, cy - 42, r, cy + 42)
            pill(img, tb, color=rgb(COL["Play"]) + (255,) if on else (140, 153, 173, 140))
            kx = r - 75 + (33 if on else -33)
            d.ellipse((kx - 33, cy - 33 + 5, kx + 33, cy + 33 + 5), fill=(26, 20, 51, 64))
            d.ellipse((kx - 33, cy - 33, kx + 33, cy + 33), fill=(255, 255, 255))
        elif right[0] == "link":
            a = load("ui/icon_Back.png").resize((52, 52), Image.LANCZOS).rotate(180)
            a = ImageChops.multiply(a, Image.new("RGBA", a.size, (140, 153, 173, 230)))
            img.alpha_composite(a, (int(r - 62), int(cy - 26)))
        elif right[0] == "size":
            for k, sz in enumerate((34, 42, 50)):
                on = k == right[1]
                bx = (r - (2 - k) * 104 - 96, cy - 42, r - (2 - k) * 104, cy + 42)
                pill(img, bx, color=rgb(COL["Secondary"]) + (255,) if on else (43, 45, 66, 26))
                title(img, "A", bx, sz, color=(255, 255, 255) if on else rgb(COL["Ink"]), outline=on)

    box, top = section("Son et vibrations", 3)
    row(box, top, 0, "Sound", "Secondary", "Son", ("switch", True))
    row(box, top, 1, "Star", "Purple", "Musique", ("switch", True))
    row(box, top, 2, "Vibrate", "Success", "Vibrations", ("switch", False))
    box, top = section("Affichage", 3)
    row(box, top, 0, "Star", "Secondary", "Taille du texte", ("size", 0))
    row(box, top, 1, "Play", "Accent", "Animations réduites", ("switch", False))
    row(box, top, 2, "Film", "Primary", "Mode vidéo", ("switch", True))
    box, top = section("Compte et achats", 5)
    row(box, top, 0, "Star", "Success", "Mes statistiques", ("link",))
    row(box, top, 1, "Bag", "Primary", "Boutique", ("link",))
    row(box, top, 2, "Gift", "Accent", "Code promo", ("link",))
    row(box, top, 3, "Film", "Purple", "Retirer les publicités", ("link",))
    row(box, top, 4, "Replay", "Dark", "Restaurer mes achats", ("link",))
    box, top = section("Aide", 5)
    row(box, top, 0, "Star", "Accent", "Noter Bascule", ("link",))
    row(box, top, 1, "Bell", "Secondary", "Aide et contact", ("link",))
    row(box, top, 2, "Lock", "Dark", "Confidentialité", ("link",))
    row(box, top, 3, "Film", "Purple", "Pubs personnalisées", ("switch", True))
    row(box, top, 4, "Close", "Primary", "Supprimer mes données en ligne", ("link",), danger=True)
    return c


def shop_coins_screen():
    """Boutique, partie « dépenser ses pièces » (mêmes positions que ShopScreen : SurpriseCard, PlankSection, ShieldCard)."""
    c = world(2)
    img = c.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 158)))
    button(img, place(0, 1, 36, -36, 150, 156, 0, 1), "Dark", ic="Back")
    title(img, "Boutique", place(0.5, 1, 0, -112, 460, 130), 76)
    coin_pill(img, place(1, 1, -36, -60, 270, 104, 1, 1), "2 340")
    d = ImageDraw.Draw(img)
    x0, x1, y = 36, W - 36, 240

    def coin_button(box, price, can=True):
        button(img, box, "Play" if can else "Muted", price, "Coin", 50)

    # Machine à surprises
    box = (x0, y, x1, y + 420)
    card(img, box)
    pill(img, (box[0] + 24, box[1] + 22, box[0] + 324, box[1] + 34), color=rgb(COL["Purple"]) + (255,))
    cx, cy = box[0] + 46 + 110, box[1] + 210
    d.ellipse((cx - 110, cy - 110, cx + 110, cy + 110), fill=rgb(COL["Purple"]))
    d.ellipse((cx - 35 - 41, cy - 40 - 27, cx - 35 + 41, cy - 40 + 27), fill=(190, 160, 255))
    title(img, "?", (cx - 110, cy - 120, cx + 110, cy + 100), 150, color=rgb(COL["Accent"]))
    tinted_icon(img, "Star", (box[0] + 244 - 29, cy - 92 - 29, box[0] + 244 + 29, cy - 92 + 29), "Accent")
    tinted_icon(img, "Star", (box[0] + 70 - 21, cy + 92 - 21, box[0] + 70 + 21, cy + 92 + 21), "Accent")
    title(img, "Machine à surprises", (box[0] + 310, box[1] + 36, box[0] + 980, box[1] + 120), 60, color=rgb(COL["Purple"]), align="lm")
    d.multiline_text((box[0] + 310, box[1] + 124), "Une tête que tu n'as pas encore,\nau hasard. Jamais de doublon !", font=font(40), fill=rgb(COL["Ink"]), spacing=6)
    chip = (box[0] + 310, box[3] - 82 - 32, box[0] + 640, box[3] - 82 + 32)
    pill(img, chip, color=(166, 108, 255, 41))
    title(img, "13 têtes à gagner", chip, 36, color=rgb(COL["Purple"]), outline=False)
    coin_button((box[2] - 32 - 260, box[3] - 30 - 112, box[2] - 32, box[3] - 30), "1 000")
    y = box[3] + 24
    # Planches
    title(img, "Planches", (x0 + 6, y, x1, y + 84), 52, color=rgb(COL["Accent"]), align="lm")
    y += 84 + 24
    title(img, "Change l'allure de la planche de Basile.", (x0 + 6, y, x1, y + 56), 40, color=(216, 233, 255), align="lm", outline=False)
    y += 56 + 24
    names = [("Bois", "worn"), ("Bambou", "owned"), ("Skate", "800"), ("Surf", "1 100"), ("Marbre", "1 500"), ("Sucre d'orge", "2 000")]
    cw = (x1 - x0 - 18) / 2
    for i, (nm, st) in enumerate(names):
        cx0 = x0 + (i % 2) * (cw + 18); cy0 = y + (i // 2) * 318
        cb = (cx0, cy0, cx0 + cw, cy0 + 300)
        card(img, cb, tint=(255, 241, 194, 255) if st == "worn" else None)
        pl = load(f"planches/apercu_{i}.png")
        pw = 400; ph = int(pl.height * pw / pl.width)
        pi = pl.resize((pw, ph), Image.LANCZOS).rotate(5, resample=Image.BICUBIC, expand=True)
        img.alpha_composite(pi, (int(cx0 + cw / 2 - pi.width / 2), int(cy0 + 66 - pi.height / 2)))
        title(img, nm, (cx0, cy0 + 108, cx0 + cw, cy0 + 172), 46, color=rgb(COL["Ink"]), outline=False)
        bb = (cx0 + cw / 2 - 190, cb[3] - 22 - 116, cx0 + cw / 2 + 190, cb[3] - 22)
        if st == "worn":
            button(img, bb, "Muted", "Équipée", "Check", 44)
        elif st == "owned":
            button(img, bb, "Secondary", "Choisir", None, 46)
        else:
            coin_button(bb, st, can=st in ("800", "1 100", "1 500", "2 000"[:0]))
    return c


RARITY = {"Commune": "#8D99AE", "Rare": "#4A9BFF", "Épique": "#A66CFF", "Légendaire": "#FFB627"}


def collection_screen():
    """Menu des têtes (mêmes positions que CollectionScreen) : tête portée, onglets, grille par rareté."""
    c = world(0)
    img = c.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 158)))
    d = ImageDraw.Draw(img)
    button(img, place(0, 1, 36, -36, 150, 156, 0, 1), "Dark", ic="Back")
    title(img, "Têtes", place(0.5, 1, -70, -112, 440, 130), 84)
    coin_pill(img, place(1, 1, -36, -60, 270, 104, 1, 1), "2 340", size=46)

    def halo(box, color, alpha):
        disc = load("ui/bubble.png") if False else None
        l, t, r, b = [int(v) for v in box]
        layer = Image.new("RGBA", (r - l, b - t), (0, 0, 0, 0))
        ImageDraw.Draw(layer).ellipse((0, 0, r - l - 1, b - t - 1), fill=rgb(color) + (int(255 * alpha),))
        layer = layer.filter(ImageFilter.GaussianBlur((r - l) * 0.08))
        img.alpha_composite(layer, (l, t))

    hb = place(0.5, 1, 0, -236, 1008, 280, 0.5, 1)
    card(img, hb)
    halo((hb[0] + 10, hb[1], hb[0] + 290, hb[3]), RARITY["Commune"], 0.45)
    head = load("heads/02.png").resize((220, 220), Image.LANCZOS)
    img.alpha_composite(head, (int(hb[0] + 40), int((hb[1] + hb[3]) / 2 - 110)))
    title(img, "Tête portée", (hb[0] + 300, hb[1] + 23, hb[0] + 720, hb[1] + 73), 38, color=rgb(COL["Muted"]), align="lm", outline=False)
    title(img, "Pétillante", (hb[0] + 300, hb[1] + 66, hb[0] + 740, hb[1] + 142), 62, align="lm")
    chip = (hb[0] + 300, hb[1] + 136, hb[0] + 530, hb[1] + 192)
    pill(img, chip, color=rgb(RARITY["Commune"]) + (255,))
    title(img, "Commune", chip, 36)
    title(img, "Au hasard", (hb[2] - 266, hb[1] + 22, hb[2] - 36, hb[1] + 70), 36, color=rgb(COL["Muted"]), align="rm", outline=False)
    sw = (hb[2] - 176, hb[1] + 73, hb[2] - 36, hb[1] + 151)
    pill(img, sw, color=rgb(COL["Play"]) + (255,))
    d = ImageDraw.Draw(img)
    kx, ky = (sw[0] + sw[2]) / 2 + 33, (sw[1] + sw[3]) / 2
    d.ellipse((kx - 33, ky - 28, kx + 33, ky + 38), fill=(26, 20, 51, 64))
    d.ellipse((kx - 33, ky - 33, kx + 33, ky + 33), fill=(255, 255, 255))
    title(img, "18 / 62 têtes", (hb[2] - 316, hb[3] - 122, hb[2] - 36, hb[3] - 62), 44, color=rgb(COL["Accent"]), align="rm")
    bar(img, (hb[2] - 436, hb[3] - 59, hb[2] - 36, hb[3] - 29), 18 / 62, "Accent")

    tb = place(0.5, 1, 0, -536, 1008, 128, 0.5, 1)
    pill(img, tb)
    tabs = [("Records", "9/21"), ("Niveaux", "4/13"), ("Boutique", "3/16"), ("Exploits", "2/6"), ("Légendes", "0/6")]
    for i, (lab, cnt) in enumerate(tabs):
        bx = (tb[0] + 10 + i * 198, (tb[1] + tb[3]) / 2 + 4 - 56, tb[0] + 10 + i * 198 + 196, (tb[1] + tb[3]) / 2 + 4 + 56)
        nine(img, load("ui/button.png"), bx, (64, 64, 64, 66), tint=COL["Primary" if i == 0 else "Dark"])
        title(img, lab, (bx[0] + 10, bx[1] + 8, bx[2] - 10, bx[1] + 58), 34)
        title(img, cnt, (bx[0], bx[3] - 58, bx[2], bx[3] - 24), 30, color=(216, 233, 255), outline=False)

    cells = [("Tranquille", "Commune", "owned", "01"), ("Pétillante", "Commune", "worn", "02"), ("Le Malin", "Commune", "owned", "03"),
             ("Frisette", "Commune", "owned", "04"), ("Lunettes", "Commune", "next", "05"), ("???", "Rare", "locked", "06"),
             ("???", "Rare", "locked", "07"), ("???", "Rare", "locked", "08"), ("???", "Épique", "locked", "09")]
    conds = {"next": "2 000 points", "locked": "3 000 points"}
    gx0, gy0 = 24, 680
    cw, ch = 326, 420
    for k, (nm, rar, st, hid) in enumerate(cells):
        cx0 = gx0 + (k % 3) * (cw + 14) + (W - 48 - 3 * cw - 28) / 2
        cy0 = gy0 + (k // 3) * (ch + 18)
        cb = (cx0, cy0, cx0 + cw, cy0 + ch)
        tint = (255, 233, 168, 255) if st == "worn" else None if st in ("owned", "next") else (204, 207, 222, 255)
        card(img, cb, tint=tint)
        rc = RARITY[rar]
        pill(img, (cx0 + cw / 2 - 120, cy0 + 16, cx0 + cw / 2 + 120, cy0 + 28), color=rgb(rc) + (255,))
        halo((cx0 + cw / 2 - 135, cy0 + 26, cx0 + cw / 2 + 135, cy0 + 296), rc, 0.42 if st in ("owned", "worn") else 0.18)
        hp = load(f"heads/{hid}.png").resize((236, 236), Image.LANCZOS)
        if st == "locked":
            sil = Image.new("RGBA", hp.size, (43, 45, 66, 0)); sil.putalpha(hp.split()[3].point(lambda a: int(a * 0.32))); hp = sil
        img.alpha_composite(hp, (int(cx0 + cw / 2 - 118), int(cy0 + 36)))
        if st == "locked":
            icon(img, "Lock", (cx0 + cw / 2 - 42, cy0 + 140 - 42, cx0 + cw / 2 + 42, cy0 + 140 + 42))
        if st in ("worn", "next"):
            tg = Image.new("RGBA", (260, 150), (0, 0, 0, 0))
            pill(tg, (40, 46, 216, 104), color=rgb(COL["Primary" if st == "worn" else "Play"]) + (255,))
            title(tg, "Portée" if st == "worn" else "Prochaine", (50, 46, 206, 100), 34)
            tg = tg.rotate(-8, resample=Image.BICUBIC)
            img.alpha_composite(tg, (int(cb[2] - 6 - 176 - 40), int(cy0 + 6 - 46)))
        shown = nm if st != "locked" else "???"
        title(img, shown, (cx0, cb[3] - 112 - 27, cb[2], cb[3] - 112 + 27), 42, color=rgb(COL["Ink"]), outline=False)
        title(img, rar, (cx0, cb[3] - 78 - 18, cb[2], cb[3] - 78 + 18), 30, color=rgb(rc), outline=False)
        if st in conds:
            title(img, conds[st], (cx0, cb[3] - 36 - 24, cb[2], cb[3] - 36 + 24), 36, color=rgb(COL["Dark"]), outline=False)
    return c


def mini_chest(img, x, cy, w):
    """Petit coffre fermé (MetaUi.MiniChest) : bord gauche x, centre vertical cy."""
    h = w * 0.9
    bot = cy + h / 2
    img.alpha_composite(load("ui/chest_base.png").resize((int(w), int(w * 0.66)), Image.LANCZOS), (int(x), int(bot - w * 0.66)))
    hinge = bot - w * 0.53
    img.alpha_composite(load("ui/chest_lid.png").resize((int(w), int(w * 0.42)), Image.LANCZOS), (int(x), int(hinge - w * 0.42 + w * 0.03)))


def chest_screen(opened=True):
    """Écran du coffre (ChestScreen) : fermé (il attend le toucher) ou ouvert (contenu + récompenses de la partie)."""
    c = gameover_screen()
    img = c.img
    img.alpha_composite(Image.new("RGBA", (W, H), (20, 13, 46, 247)))
    def at(x, y, w, h):  # comme Ui.Place(…, 0.5, 0.5, x, y, w, h)
        return place(0.5, 0.5, x, y, w, h)
    if True:
        glow = Image.new("RGBA", (900, 900), (0, 0, 0, 0))
        ImageDraw.Draw(glow).ellipse((150, 150, 750, 750), fill=(255, 217, 89, 190 if opened else 95))
        glow = glow.filter(ImageFilter.GaussianBlur(110))
        g = at(0, 330, 900, 900); img.alpha_composite(glow, (int(g[0]), int(g[1])))
        rays = load("ui/rays.png").resize((1500, 1500), Image.BICUBIC).rotate(8)
        rays.putalpha(rays.split()[3].point(lambda a: int(a * (0.4 if opened else 0.16))))
        r = at(0, 330, 1500, 1500); img.alpha_composite(rays, (int(r[0]), int(r[1])))
        for k, (sx, sy) in enumerate([(-390, 420), (380, 470), (-300, 80), (330, 120), (0, 640), (-420, 260), (430, 300)]):
            sp = load("ui/spark.png") if os.path.exists(os.path.join(ART, "ui/spark.png")) else None
            sz = [40, 60, 34, 52, 46, 30, 56][k]
            q = at(sx, sy, sz, sz); dd = ImageDraw.Draw(img); cx, cy = (q[0] + q[2]) / 2, (q[1] + q[3]) / 2
            dd.polygon([(cx, cy - sz / 2), (cx + sz * 0.12, cy - sz * 0.12), (cx + sz / 2, cy), (cx + sz * 0.12, cy + sz * 0.12), (cx, cy + sz / 2), (cx - sz * 0.12, cy + sz * 0.12), (cx - sz / 2, cy), (cx - sz * 0.12, cy - sz * 0.12)], fill=(255, 242, 178, 230))
    title(img, "Coffre gagné !", place(0.5, 1, 0, -150, 980, 130), 84, color=rgb(COL["Accent"]))
    cw = 700
    cb = at(0, 180, cw, cw * 0.66)
    img.alpha_composite(load("ui/chest_base.png").resize((cw, int(cw * 0.66)), Image.LANCZOS), (int(cb[0]), int(cb[1])))
    if not opened:
        lid = load("ui/chest_lid.png").resize((cw, int(cw * 0.42)), Image.LANCZOS)
        hinge = cb[3] - cw * 0.53
        img.alpha_composite(lid, (int(cb[0]), int(hinge - cw * 0.42 + cw * 0.03)))
        title(img, "Encore une fois !", at(0, -115, 900, 80), 60)
        return c
    tinted_icon(img, "Coin", at(-95, 560, 130, 130), "Accent")
    title(img, "+120", at(90, 560, 360, 140), 110, color=rgb(COL["Accent"]))
    rows = [("Target", "Success", "Objectif atteint !", "4 300 points", "+25"), ("Star", "Secondary", "Niveau 6 !", None, "+110")]
    for k, (ic, col, ttl, det, coins) in enumerate(rows):
        y = -110 - 68 - k * 154
        sh = at(0, y - 8, 880, 128); pill(img, sh, color=(13, 8, 30, 90))
        rb = at(0, y, 880, 128); pill(img, rb, color=OUTLINE + (255,))
        rr = (rb[0] + 6, rb[1] + 6, rb[2] - 6, rb[3] - 6); pill(img, rr, color=rgb(COL["Paper"]) + (255,))
        hh = (rr[3] - rr[1])
        pill(img, (rr[0] + 60, rr[1] + hh * 0.08, rr[2] - 60, rr[1] + hh * 0.4), color=(255, 255, 255, 140))
        d = ImageDraw.Draw(img)
        cx, cy = rr[0] + 10 + 48, (rr[1] + rr[3]) / 2
        d.ellipse((cx - 48, cy - 48, cx + 48, cy + 48), fill=OUTLINE)
        d.ellipse((cx - 42, cy - 42, cx + 42, cy + 42), fill=rgb(COL[col]))
        icon(img, ic, (cx - 27, cy - 27, cx + 27, cy + 27))
        tx = rr[0] + 126
        if det:
            title(img, ttl, (tx, cy - 50, rr[2] - 236, cy + 2), 44, color=rgb(COL["Ink"]), align="lm", outline=False)
            title(img, det, (tx, cy + 2, rr[2] - 236, cy + 46), 34, color=rgb(COL["Muted"]), align="lm", outline=False)
        else:
            title(img, ttl, (tx, cy - 30, rr[2] - 236, cy + 30), 44, color=rgb(COL["Ink"]), align="lm", outline=False)
        fw = ImageDraw.Draw(img).textlength(coins, font=font(48))
        title(img, coins, (rr[2] - 30 - fw, cy - 35, rr[2] - 30, cy + 35), 48, color=rgb(COL["Accent"]), align="rm")
        tinted_icon(img, "Coin", (rr[2] - 30 - fw - 38 - 29, cy - 29, rr[2] - 30 - fw - 38 + 29, cy + 29), "Accent")
    button(img, place(0.5, 0, 0, 170, 700, 160), "Play", "Super !", None, 64)
    return c


def surprise_screen(stage="offer"):
    """Machine à surprises (SurpriseScreen) : offre (chances), carrousel qui tourne, tête gagnée."""
    c = shop_screen()
    img = c.img
    img.alpha_composite(Image.new("RGBA", (W, H), (20, 13, 46, 251)))
    def at(x, y, w, h):
        return place(0.5, 0.5, x, y, w, h)
    coin_pill(img, place(1, 1, -36, -40, 270, 104, 1, 1), "2 340", size=46)
    heads = [("40", "Commune", 10), ("41", "Commune", 9), ("42", "Rare", 7), ("43", "Rare", 6), ("44", "Rare", 5), ("45", "Épique", 4), ("46", "Épique", 4), ("47", "Épique", 3),
             ("48", "Légendaire", 3), ("49", "Légendaire", 2), ("50", "Légendaire", 2), ("51", "Légendaire", 2)]
    offer = [("coins", 400, 20), ("head", 0, 0), ("head", 1, 0), ("coins", 1200, 8), ("plank", 1, 5), ("head", 2, 0), ("head", 3, 0), ("plank", 2, 4),
             ("head", 4, 0), ("head", 5, 0), ("plank", 3, 3), ("coins", 3000, 2)]
    if stage == "offer":
        title(img, "Machine à surprises", place(0.5, 1, 0, -200, 980, 120), 80, color=rgb(COL["Accent"]))
        title(img, "Une tête, une planche ou des pièces, au hasard. Jamais de doublon !", place(0.5, 1, 0, -300, 960, 60), 34)
        title(img, "Tes chances pour chaque lot", place(0.5, 1, 0, -392, 900, 56), 40, color=(191, 224, 255))
        x0 = (W - (4 * 232 + 3 * 14)) / 2
        for k, (kind, v, pct0) in enumerate(offer):
            cx0 = x0 + (k % 4) * 246; cy0 = 430 + (k // 4) * 306
            cb = (cx0, cy0, cx0 + 232, cy0 + 290)
            card(img, cb)
            if kind == "head":
                hid, rar, pct = heads[v]
                rc = RARITY[rar]
                hp = load(f"heads/{hid}.png").resize((150, 150), Image.LANCZOS)
                sil = Image.new("RGBA", hp.size, (43, 45, 66, 0)); sil.putalpha(hp.split()[3].point(lambda a: int(a * 80 / 255)))
                img.alpha_composite(sil, (int(cx0 + 41), int(cy0 + 23)))
                title(img, "?", (cx0, cy0 + 53, cx0 + 232, cy0 + 143), 64)
                tag = rar
            elif kind == "plank":
                pct, rc, tag = pct0, COL["Secondary"], "Planche"
                pl = load(f"planches/apercu_rebords_{v}.png")
                pl = pl.resize((188, int(188 * pl.height / pl.width)), Image.LANCZOS)
                img.alpha_composite(pl, (int(cx0 + 22), int(cy0 + 98 - pl.height / 2)))
            else:
                pct = pct0
                rc = "#FFB627" if v >= 3000 else COL["Success"]
                tag = "JACKPOT !" if v >= 3000 else "Pièces"
                n = 3 if v >= 3000 else 2 if v >= 1000 else 1
                for q in range(n):
                    cs = 93 if n == 1 else 75
                    qx = cx0 + 116 + (q - (n - 1) / 2) * 39
                    tinted_icon(img, "Coin", (qx - cs / 2, cy0 + 74 - cs / 2 + q * 8, qx + cs / 2, cy0 + 74 + cs / 2 + q * 8), "Accent")
                title(img, f"{v:,}".replace(",", " "), (cx0, cy0 + 120, cx0 + 232, cy0 + 166), 40, color=rgb(COL["Accent"]))
            pill(img, (cx0 + 31, cy0 + 7, cx0 + 201, cy0 + 21), color=rgb(rc) + (255,))
            title(img, tag, (cx0, cb[3] - 119, cx0 + 232, cb[3] - 81), 30, color=rgb(rc), outline=False)
            ch = (cx0 + 31, cb[3] - 78, cx0 + 201, cb[3] - 18)
            pill(img, ch, color=rgb(rc) + (255,))
            title(img, f"{pct} %", ch, 42)
        button(img, place(0.5, 0, 0, 250, 780, 160), "Play", "Ouvrir · 1 000", "Coin", 60)
        button(img, place(0.5, 0, 0, 90, 520, 132), "Dark", "Annuler", None, 50)
        return c
    won = 5
    if stage == "reveal":
        glow = Image.new("RGBA", (900, 900), (0, 0, 0, 0))
        ImageDraw.Draw(glow).ellipse((150, 150, 750, 750), fill=(255, 217, 102, 180))
        glow = glow.filter(ImageFilter.GaussianBlur(110))
        g = at(0, 120, 900, 900); img.alpha_composite(glow, (int(g[0]), int(g[1])))
        rays = load("ui/rays.png").resize((1500, 1500), Image.BICUBIC).rotate(8)
        rays.putalpha(rays.split()[3].point(lambda a: int(a * 0.35)))
        r = at(0, 120, 1500, 1500); img.alpha_composite(rays, (int(r[0]), int(r[1])))
    title(img, "Ça tourne…" if stage == "spin" else "Nouvelle tête !", place(0.5, 1, 0, -200, 980, 120), 80, color=rgb(COL["Accent"]))
    band = at(0, 120, 1080, 330)
    pill(img, band, color=(13, 8, 36, 140))
    gold = at(0, 120, 324, 344)
    pill(img, gold, color=rgb(COL["Accent"]) + (255,))
    offset = 0.45 if stage == "spin" else 0.0
    layer = Image.new("RGBA", (W, 330), (0, 0, 0, 0))
    order = [2, 9, 0, 7, 3, won, 1, 8, 4]
    for i, hi in enumerate(order):
        d = abs(i - 4 - offset)
        sc = (1.12 if stage == "reveal" and i == 4 else max(0.82, 1.08 - 0.26 * min(1, d)))
        cw, chh = 250 * sc, 290 * sc
        cx = W / 2 + (i - 4 - offset) * 274
        cbx = (cx - cw / 2, 165 - chh / 2, cx + cw / 2, 165 + chh / 2)
        card(layer, cbx)
        hid, rar, _ = heads[hi]
        rc = RARITY[rar]
        halo = Image.new("RGBA", (int(240 * sc), int(240 * sc)), (0, 0, 0, 0))
        ImageDraw.Draw(halo).ellipse((0, 0, halo.width - 1, halo.height - 1), fill=rgb(rc) + (110,))
        halo = halo.filter(ImageFilter.GaussianBlur(18 * sc))
        layer.alpha_composite(halo, (int(cx - halo.width / 2), int(157 - halo.height / 2)))
        hp = load(f"heads/{hid}.png").resize((int(210 * sc), int(210 * sc)), Image.LANCZOS)
        layer.alpha_composite(hp, (int(cx - hp.width / 2), int(157 - hp.height / 2)))
        pill(layer, (cx - 85 * sc, cbx[3] - 36 * sc, cx + 85 * sc, cbx[3] - 20 * sc), color=rgb(rc) + (255,))
    top = int(H / 2 - 120 - 165)
    img.alpha_composite(layer, (0, top))
    d = ImageDraw.Draw(img)
    for side in (1, -1):
        cy = H / 2 - 120 - side * 200
        tri = [(W / 2 - 34, cy - side * 24), (W / 2 + 34, cy - side * 24), (W / 2, cy + side * 26)]
        d.polygon(tri, fill=rgb(COL["Accent"]), outline=OUTLINE, width=6)
    if stage == "reveal":
        title(img, "Fêtard", at(0, -170, 980, 100), 70)
        chip = at(0, -262, 280, 66)
        pill(img, chip, color=rgb(RARITY["Épique"]) + (255,))
        title(img, "Épique", chip, 40)
        button(img, place(0.5, 0, 0, 250, 780, 160), "Play", "Porter", "Face", 60)
        button(img, place(0.5, 0, 0, 90, 520, 132), "Dark", "Plus tard", None, 50)
    return c


def all_screens():
    shots = [("accueil", home_screen()), ("partie", game_screen()), ("fin_de_partie", gameover_screen())]
    extra = [("boutique", shop_screen()), ("missions", missions_screen()), ("cadeau", gift_screen())]
    share_card().img.convert("RGB").save(os.path.join(OUT, "carte_partage.png"))
    settings_screen().img.convert("RGB").save(os.path.join(OUT, "ecran_reglages.png"))
    shop_coins_screen().img.convert("RGB").save(os.path.join(OUT, "ecran_boutique_pieces.png"))
    collection_screen().img.convert("RGB").save(os.path.join(OUT, "ecran_tetes.png"))
    chest_screen(False).img.convert("RGB").save(os.path.join(OUT, "ecran_coffre_ferme.png"))
    chest_screen(True).img.convert("RGB").save(os.path.join(OUT, "ecran_coffre.png"))
    for st in ("offer", "spin", "reveal"):
        surprise_screen(st).img.convert("RGB").save(os.path.join(OUT, f"ecran_surprise_{st}.png"))
    for name, c in shots + extra:
        c.img.convert("RGB").save(os.path.join(OUT, f"ecran_{name}.png"))
    strip2 = Image.new("RGB", (W * 3 + 80, H + 40), (24, 22, 40))
    for i, (_, c) in enumerate(extra):
        strip2.paste(c.img.convert("RGB"), (20 + i * (W + 20), 20))
    strip2.resize((strip2.width // 2, strip2.height // 2), Image.LANCZOS).save(os.path.join(OUT, "ecrans_progression.png"))
    strip = Image.new("RGB", (W * 3 + 80, H + 40), (24, 22, 40))
    for i, (_, c) in enumerate(shots):
        strip.paste(c.img.convert("RGB"), (20 + i * (W + 20), 20))
    strip.resize((strip.width // 2, strip.height // 2), Image.LANCZOS).save(os.path.join(OUT, "ecrans.png"))


if __name__ == "__main__" and len(sys.argv) > 3 and sys.argv[3] == "ecrans":
    all_screens()


if __name__ == "__main__" and not (len(sys.argv) > 3 and sys.argv[3] == "ecrans"):
    main()


def daily_screen():
    """Défi du jour (DailyScreen) : carte violette du défi, récompense, meilleur score et série, compte à rebours."""
    c = home_screen()
    img = c.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 170)))
    cb = place(0.5, 0.5, 0, 20, 960, 1400)
    card(img, cb)
    title(img, "Défi du jour", (cb[0], cb[1] + 32, cb[2], cb[1] + 152), 84, color=rgb(COL["Accent"]))
    title(img, "Défi n°42 · nouveau défi dans 5 h 07", (cb[0], cb[1] + 144, cb[2], cb[1] + 200), 38, color=rgb(COL["Muted"]), outline=False)
    glow = Image.new("RGBA", (1000, 520), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((60, 40, 940, 480), fill=(166, 108, 255, 110))
    glow = glow.filter(ImageFilter.GaussianBlur(50))
    img.alpha_composite(glow, (int(W / 2 - 500), int(cb[1] + 440 - 260)))
    pb = (W / 2 - 430, cb[1] + 220, W / 2 + 430, cb[1] + 660)
    card(img, pb, tint=rgb(COL["Purple"]) + (255,))
    tinted_icon(img, "Target", (W / 2 - 48, pb[1] + 22, W / 2 + 48, pb[1] + 118), "#FFFFFF")
    title(img, "TON DÉFI", (pb[0], pb[1] + 121, pb[2], pb[1] + 171), 36, color=(255, 255, 255), outline=False)
    title(img, "Fais 60 fusions en une partie", (pb[0] + 40, pb[1] + 173, pb[2] - 40, pb[1] + 323), 60)
    rb = (W / 2 - 235, pb[3] - 126, W / 2 + 235, pb[3] - 34)
    pill(img, rb, color=rgb(COL["Accent"]) + (255,))
    tinted_icon(img, "Coin", (rb[0] + 19, rb[1] + 13, rb[0] + 85, rb[1] + 79), "#FFE678")
    title(img, "+150 pièces", (rb[0] + 90, rb[1], rb[2] - 24, rb[3] - 4), 44)
    for side, ic, colr, val, cap in ((-1, "Trophy", "Accent", "2 310", "Meilleur du jour"), (1, "Flame", "Primary", "3", "Jours de suite")):
        cx = W / 2 + side * 215
        tb = (cx - 205, cb[1] + 700, cx + 205, cb[1] + 900)
        pill(img, tb, color=(255, 255, 255, 190))
        tinted_icon(img, ic, (tb[0] + 32, tb[1] + 44, tb[0] + 108, tb[1] + 120), colr)
        title(img, val, (cx + 40 - 135, tb[1] + 32, cx + 40 + 135, tb[1] + 116), 62, color=rgb(COL["Ink"]), outline=False)
        title(img, cap, (tb[0] + 20, tb[3] - 76, tb[2] - 20, tb[3] - 22), 30, color=rgb(COL["Muted"]), outline=False)
    t1 = "Les mêmes poids pour tout le monde aujourd'hui."
    t2 = "Ton meilleur score compte pour le classement du jour."
    title(img, t1, (cb[0] + 60, cb[1] + 950, cb[2] - 60, cb[1] + 1000), 34, color=rgb(COL["Muted"]), outline=False)
    title(img, t2, (cb[0] + 60, cb[1] + 1000, cb[2] - 60, cb[1] + 1050), 34, color=rgb(COL["Muted"]), outline=False)
    button(img, (W / 2 - 380, cb[3] - 316, W / 2 + 380, cb[3] - 156), "Play", "Jouer", "Play", 60)
    button(img, (W / 2 - 260, cb[3] - 144, W / 2 + 260, cb[3] - 28), "Muted", "Retour", None, 46)
    return c


if __name__ == "__main__" and len(sys.argv) > 3 and sys.argv[3] == "defi":
    daily_screen().img.convert("RGB").save(os.path.join(OUT, "ecran_defi_du_jour.png"))


def welcome_screen():
    """Bienvenue (WelcomeScreen) : lancement du jeu, montré une fois à tout le monde."""
    c = home_screen()
    img = c.img
    img.alpha_composite(Image.new("RGBA", (W, H), (26, 20, 51, 170)))
    cb = place(0.5, 0.5, 0, 20, 960, 1480)
    card(img, cb)
    title(img, "Bienvenue !", (cb[0], cb[1] + 31, cb[2], cb[1] + 161), 100, color=rgb(COL["Accent"]))
    title(img, "Merci d'avoir téléchargé Bascule !", (cb[0], cb[1] + 154, cb[2], cb[1] + 214), 44, color=rgb(COL["Ink"]), outline=False)
    hb = (W / 2 - 430, cb[1] + 236, W / 2 + 430, cb[1] + 616)
    card(img, hb, tint=rgb(COL["Purple"]) + (255,))
    hero = Image.new("RGBA", (int(hb[2] - hb[0]), int(hb[3] - hb[1])), (0, 0, 0, 0))
    rays = load("ui/rays.png").resize((900, 900))
    rays = ImageChops.multiply(rays, Image.new("RGBA", rays.size, (255, 230, 128, 90)))
    hero.alpha_composite(rays, (int(hero.width / 2 - 450), int(hero.height / 2 + 10 - 450)))
    g = Image.new("RGBA", hero.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse((hero.width / 2 - 170, hero.height / 2 - 190, hero.width / 2 + 170, hero.height / 2 + 150), fill=(255, 217, 102, 150))
    hero.alpha_composite(g.filter(ImageFilter.GaussianBlur(40)))
    por = load("heads/00.png").resize((270, 270), Image.LANCZOS)
    hero.alpha_composite(por, (int(hero.width / 2 - 135), int(hero.height / 2 - 18 - 135)))
    mask = Image.new("L", hero.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((8, 8, hero.width - 8, hero.height - 8), 50, fill=255)
    img.paste(hero, (int(hb[0]), int(hb[1])), Image.composite(hero, Image.new("RGBA", hero.size, (0, 0, 0, 0)), mask))
    tb = (W / 2 - 260, hb[3] - 96, W / 2 + 260, hb[3] - 22)
    pill(img, tb, color=rgb(COL["Primary"]) + (255,))
    title(img, "C'EST LE LANCEMENT", tb, 38)
    rows = [("Star", "Secondary", "14 mondes à traverser, jusqu'à la Lune"), ("Face", "Purple", "63 têtes à gagner, chacune avec sa tenue"),
            ("Calendar", "Accent", "Un nouveau défi chaque jour"), ("Swords", "Primary", "Défie tes amis en duel, en direct")]
    for i, (ic, colr, text) in enumerate(rows):
        rb = (W / 2 - 430, cb[1] + 650 + i * 120, W / 2 + 430, cb[1] + 754 + i * 120)
        pill(img, rb, color=(255, 255, 255, 190))
        d = ImageDraw.Draw(img)
        cx, cy = rb[0] + 62, (rb[1] + rb[3]) / 2
        d.ellipse((cx - 40, cy - 40, cx + 40, cy + 40), fill=rgb(COL[colr]))
        tinted_icon(img, ic, (cx - 26, cy - 26, cx + 26, cy + 26), "#FFFFFF")
        title(img, text, (rb[0] + 124, rb[1], rb[2] - 30, rb[3]), 38, color=rgb(COL["Ink"]), align="lm", outline=False)
    title(img, "Les classements sont tout neufs :", (cb[0] + 60, cb[1] + 1130, cb[2] - 60, cb[1] + 1178), 36, color=rgb(COL["Muted"]), outline=False)
    title(img, "le premier record du monde peut être le tien !", (cb[0] + 60, cb[1] + 1176, cb[2] - 60, cb[1] + 1224), 36, color=rgb(COL["Muted"]), outline=False)
    button(img, (W / 2 - 380, cb[3] - 200, W / 2 + 380, cb[3] - 40), "Play", "C'est parti !", "Play", 62)
    return c


if __name__ == "__main__" and len(sys.argv) > 3 and sys.argv[3] == "bienvenue":
    welcome_screen().img.convert("RGB").save(os.path.join(OUT, "ecran_bienvenue.png"))
