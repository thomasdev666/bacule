"""
Accroche de 3 s ajoutée devant la vidéo de lancement (docs/promo/VIDEO.md) : gros plan sur l'accueil du jeu, un doigt
s'approche du bouton JOUER, s'arrête à un jeton de l'écran (« N'appuie surtout pas… »), appuie, puis zoom dans le
bouton et passage à la partie. Le doigt est dessiné ici (même contour que le jeu), pas filmé.

  python3 Tools/promo_intro.py Exports/art docs/promo/bascule_lancement.mp4 docs/promo/bascule_lancement_accroche.mp4
"""
import math
import os
import subprocess
import sys
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ART, BASE, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
ONLY = [float(x) for x in sys.argv[4].split(",")] if len(sys.argv) > 4 else None  # instants à sortir en PNG (contrôle)
sys.argv = [sys.argv[0], ART, "/tmp", "rien"]
import scene_preview as sp  # noqa: E402

W, H, FPS = 1080, 1920, 30
T_INTRO = 3.0             # durée de l'accroche
BASE_SKIP = 1.0           # on saute la première seconde de la vidéo de base (le doigt a déjà « lancé » la partie)
ZOOM, ZC = 1.15, (540, 840)   # gros plan de départ sur l'accueil, centré près du bouton
BTN = sp.place(0.5, 1, 0, -748, 760, 200)  # bouton JOUER (mêmes positions que l'accueil du jeu)
BTN_C = ((BTN[0] + BTN[2]) / 2, (BTN[1] + BTN[3]) / 2)
SKIN, NAIL, SHADE = (241, 190, 150), (255, 222, 205), (214, 154, 118)


def ease(x):
    x = max(0.0, min(1.0, x))
    return 1 - (1 - x) ** 3


def home_base():
    """Accueil du jeu sans « Reprendre », sans record et sans le bouton JOUER (dessiné à chaque image, il s'enfonce)."""
    c = sp.world(0)
    sp.mascot_scene(c, angle=-3.0, expression="Calm", weights=False)
    img = c.img
    sp.button(img, sp.place(0, 1, 28, -28, 124, 130, 0, 1), "Dark", ic="Gear")
    lb = sp.place(0, 1, 168, -32, 330, 120, 0, 1)
    sp.pill(img, lb)
    sp.tinted_icon(img, "Star", (lb[0] + 14, lb[1] + 14, lb[0] + 102, lb[1] + 102), "Accent")
    sp.title(img, "Niv. 7", (lb[0] + 114, lb[1] + 12, lb[0] + 310, lb[1] + 66), 44, align="lm")
    sp.bar(img, (lb[0] + 116, lb[3] - 40, lb[0] + 302, lb[3] - 22), 0.62, "Play")
    sp.coin_pill(img, sp.place(1, 1, -28, -36, 330, 112, 1, 1), "1 240", plus=True)
    colors = ["Primary", "Accent", "Play", "Secondary", "Purple", "Primary", "Accent"]
    for i, ch in enumerate("BASCULE"):
        layer = Image.new("RGBA", (200, 240), (0, 0, 0, 0))
        sp.title(layer, ch, (0, 0, 200, 240), 156, color=sp.rgb(sp.COL[colors[i]]))
        layer = layer.rotate(math.sin(i * 0.6) * 5 + 2, resample=Image.BICUBIC)
        b = sp.place(0.5, 1, (i - 3) * 118, -262 + math.sin(i * 1.2) * 10, 200, 240)
        img.alpha_composite(layer, (int(b[0]), int(b[1])))
    sp.button(img, sp.place(0.5, 1, 0, -932, 700, 130), "Secondary", "Défi du jour", "Calendar", 54)
    return img


def finger_image():
    """Index pointé vers l'écran, vu de dos (ongle visible), contour sombre du jeu. Pointe en haut au centre."""
    k = 3  # dessin en grand puis réduit : bords lisses
    w, L = 250, 1700
    im = Image.new("RGBA", (w * k + 60 * k, L * k), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    o = 30 * k
    out = sp.OUTLINE[:3] if len(sp.OUTLINE) >= 3 else (43, 45, 66)
    d.rounded_rectangle((o, 0, o + w * k, L * k + 400), radius=w * k // 2, fill=out + (255,))
    s = 13 * k
    d.rounded_rectangle((o + s, s, o + w * k - s, L * k + 400), radius=w * k // 2 - s, fill=SKIN + (255,))
    # Volume : côté droit plus sombre, reflet à gauche.
    d.rounded_rectangle((o + w * k * 0.66, s * 3, o + w * k - s, L * k + 400), radius=w * k // 3, fill=SHADE + (255,))
    d.rounded_rectangle((o + w * k * 0.2, w * k * 0.55, o + w * k * 0.3, w * k * 2.2), radius=40 * k, fill=(255, 226, 200, 255))
    # Ongle.
    n0, n1 = o + w * k * 0.22, o + w * k * 0.78
    d.rounded_rectangle((n0, 34 * k, n1, 34 * k + w * k * 0.82), radius=int(w * k * 0.3), fill=out + (255,))
    d.rounded_rectangle((n0 + 7 * k, 41 * k, n1 - 7 * k, 34 * k + w * k * 0.82 - 7 * k), radius=int(w * k * 0.26), fill=NAIL + (255,))
    d.rounded_rectangle((n0 + 26 * k, 56 * k, n0 + 52 * k, 34 * k + w * k * 0.55), radius=13 * k, fill=(255, 245, 238, 255))
    # Plis de la phalange.
    for yy in (560, 600, 640):
        d.arc((o + w * k * 0.25, yy * k, o + w * k * 0.75, (yy + 50) * k), 200, 340, fill=SHADE[:3] + (255,), width=9 * k)
    im = im.resize((im.width // k, im.height // k), Image.LANCZOS)
    return im, (im.width / 2, 0)  # pointe : en haut au centre


FINGER, TIP = finger_image()
ANGLE = 28  # le doigt vient d'en bas à droite
ROT = FINGER.rotate(ANGLE, resample=Image.BICUBIC, expand=True)
_r = math.radians(ANGLE)
_dx, _dy = TIP[0] - FINGER.width / 2, TIP[1] - FINGER.height / 2
TIP_ROT = (ROT.width / 2 + _dx * math.cos(_r) + _dy * math.sin(_r), ROT.height / 2 - _dx * math.sin(_r) + _dy * math.cos(_r))


def to_screen(x, y, z, cx, cy):
    return (x - cx) * z + W / 2, (y - cy) * z + H / 2


def frame(t, base):
    img = base.copy()
    # Bouton JOUER : il s'enfonce quand le doigt appuie.
    press = 1.0 if 2.12 <= t < 2.42 else 0.0
    sq = 0.07 * press
    bw, bh = (BTN[2] - BTN[0]) * (1 - sq), (BTN[3] - BTN[1]) * (1 - sq)
    box = (BTN_C[0] - bw / 2, BTN_C[1] - bh / 2 + 10 * press, BTN_C[0] + bw / 2, BTN_C[1] + bh / 2 + 10 * press)
    sp.button(img, box, "#45B85A" if press else "Play", "JOUER", "Play", 100 * (1 - sq))
    # Cadrage : gros plan, puis plongée dans le bouton.
    zt = ease((t - 2.45) / 0.5) ** 1.6
    z = ZOOM * (1 + 5.0 * zt)
    cx = ZC[0] + (BTN_C[0] - ZC[0]) * min(1.0, zt * 3)
    cy = ZC[1] + (BTN_C[1] - ZC[1]) * min(1.0, zt * 3)
    cw, ch = W / z, H / z
    view = img.crop((int(cx - cw / 2), int(cy - ch / 2), int(cx + cw / 2), int(cy + ch / 2))).resize((W, H), Image.BICUBIC)
    if zt > 0.02:  # flou de mouvement : on mélange avec un cadrage un peu moins serré
        z2 = z / (1 + 0.25 * zt)
        cw2, ch2 = W / z2, H / z2
        v2 = img.crop((int(cx - cw2 / 2), int(cy - ch2 / 2), int(cx + cw2 / 2), int(cy + ch2 / 2))).resize((W, H), Image.BICUBIC)
        view = Image.blend(view, v2, 0.45)
    out = view
    # Le doigt : arrive vite, ralentit, s'arrête à un jeton de l'écran, tremble un peu, appuie, repart.
    tx, ty = to_screen(BTN_C[0] + 60, BTN_C[1] + 10, ZOOM, *ZC)
    if t < 0.45:
        k = ease(t / 0.45)
        hgt, slide = 1.0, (1 - k) * 900
    elif t < 2.1:
        hgt, slide = 1.0 - 0.75 * ease((t - 0.45) / 1.65), 0
    elif t < 2.2:
        hgt, slide = 0.25 * (1 - (t - 2.1) / 0.1), 0
    elif t < 2.42:
        hgt, slide = 0.0, 0
    else:
        hgt, slide = 0.3, ease((t - 2.42) / 0.3) * 1300
    shake = 0 if t >= 2.1 or t < 0.45 else 3
    jx = math.sin(t * 37) * shake
    jy = math.cos(t * 29) * shake
    sc = 1.0 + 0.10 * hgt
    fx = tx + slide * math.sin(_r) + jx
    fy = ty + slide * math.cos(_r) + jy
    fing = ROT.resize((int(ROT.width * sc), int(ROT.height * sc)), Image.BICUBIC)
    ox, oy = fx - TIP_ROT[0] * sc, fy - TIP_ROT[1] * sc
    # Ombre sur l'écran : loin et floue quand le doigt est haut, collée à la pointe quand il touche.
    if t < 2.75:
        sh = Image.new("RGBA", fing.size, (25, 15, 45, 0))
        sh.putalpha(fing.getchannel("A").point(lambda a: int(a * 0.6)))
        sh = sh.filter(ImageFilter.GaussianBlur(4 + 26 * hgt))
        out.alpha_composite(sh, (int(ox - 12 - 150 * hgt), int(oy + 10 + 90 * hgt)))  # lumière à droite : l'ombre part à gauche
    # Onde au moment de l'appui.
    if 2.15 <= t < 2.65:
        a = (t - 2.15) / 0.5
        r = 40 + 260 * a
        ImageDraw.Draw(out, "RGBA").ellipse((tx - r, ty - r, tx + r, ty + r), outline=(255, 255, 255, int(230 * (1 - a))), width=14)
    if ox < W and oy < H:
        out.alpha_composite(fing, (int(ox), int(oy)))
    # Textes (au-dessus du bouton, entre le logo et JOUER).
    if t < 2.12:
        sp.pill(out, (W / 2 - 470, 490, W / 2 + 470, 620), color=sp.rgb(sp.COL["Ink"]) + (235,))
        sp.title(out, "N'appuie surtout pas…", (W / 2 - 470, 490, W / 2 + 470, 620), 84)
        if t > 1.0:
            s = min(1.0, (t - 1.0) / 0.2)
            sp.title(out, "…tu vas plus pouvoir t'arrêter", (0, 628, W, 700), int(34 + 30 * s), color=sp.rgb(sp.COL["Accent"]))
    # Éclair blanc vers la partie.
    if t > 2.75:
        out.alpha_composite(Image.new("RGBA", (W, H), (255, 255, 255, int(255 * min(1.0, (t - 2.75) / 0.25)))))
    return out.convert("RGB")


def intro_audio(path):
    sr = 44100
    out = np.zeros(int(T_INTRO * sr), dtype=np.float64)

    def add(t, sig, gain):
        i = int(t * sr)
        out[i:i + len(sig)] += sig[:len(out) - i] * gain

    def tone(f0, f1, dur, decay):
        x = np.arange(int(dur * sr)) / sr
        return np.sin(2 * np.pi * np.cumsum(f0 + (f1 - f0) * x / dur) / sr) * np.exp(-x * decay)

    for b in (0.35, 0.95, 1.5, 1.95):           # cœur qui bat : la tension monte
        add(b, tone(70, 45, 0.25, 14), 0.9)
        add(b + 0.17, tone(62, 40, 0.22, 16), 0.6)
    noise = np.random.RandomState(1).uniform(-1, 1, len(out))
    riser = np.clip((np.arange(len(out)) / sr - 0.4) / 1.7, 0, 1) ** 2 * (np.arange(len(out)) / sr < 2.15)
    out += np.convolve(noise, np.ones(40) / 40, "same") * riser * 0.25
    add(2.15, tone(1100, 500, 0.08, 40), 0.6)    # « tac » de l'appui
    add(2.16, tone(180, 90, 0.2, 20), 0.8)
    n = int(0.5 * sr)                             # souffle du zoom
    add(2.45, np.random.RandomState(2).uniform(-1, 1, n) * np.sin(np.linspace(0, np.pi, n)) ** 2, 0.35)
    out = np.tanh(out * 1.3) * 0.85
    with wave.open(path, "wb") as o:
        o.setnchannels(1)
        o.setsampwidth(2)
        o.setframerate(sr)
        o.writeframes((out * 32767).astype(np.int16).tobytes())


def main():
    base = home_base()
    if ONLY:
        os.makedirs(OUT, exist_ok=True)
        for t in ONLY:
            frame(t, base).save(os.path.join(OUT, f"i_{t:04.2f}.png"))
        return
    tmp_v, tmp_a = OUT + ".intro.mp4", OUT + ".intro.wav"
    intro_audio(tmp_a)
    ff = subprocess.Popen(["ffmpeg", "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS),
                           "-i", "-", "-i", tmp_a, "-c:v", "libx264", "-preset", "slow", "-crf", "17", "-pix_fmt", "yuv420p",
                           "-c:a", "aac", "-b:a", "192k", "-shortest", tmp_v], stdin=subprocess.PIPE)
    for f in range(int(T_INTRO * FPS)):
        ff.stdin.write(frame(f / FPS, base).tobytes())
    ff.stdin.close()
    ff.wait()
    # Accroche + vidéo de base (sans sa première seconde), qui démarre sur un blanc qui s'efface.
    fc = ("[0:v]setsar=1,fps=30[v0];[0:a]aformat=sample_rates=44100:channel_layouts=mono[a0];"
          f"[1:v]trim=start={BASE_SKIP},setpts=PTS-STARTPTS,fade=t=in:st=0:d=0.25:color=white,setsar=1,fps=30[v1];"
          f"[1:a]atrim=start={BASE_SKIP},asetpts=PTS-STARTPTS,afade=t=in:d=0.12,aformat=sample_rates=44100:channel_layouts=mono[a1];"
          "[v0][a0][v1][a1]concat=n=2:v=1:a=1[v][a]")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", tmp_v, "-i", BASE, "-filter_complex", fc, "-map", "[v]", "-map", "[a]",
                    "-c:v", "libx264", "-preset", "slow", "-crf", "17", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k",
                    "-movflags", "+faststart", OUT], check=True)
    os.remove(tmp_v)
    os.remove(tmp_a)
    print("ok", OUT)


main()
