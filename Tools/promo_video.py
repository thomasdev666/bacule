"""
Vidéo verticale de lancement (1080 × 1920, 30 images/s, environ 20 s) pour TikTok, Reels et Shorts, faite avec les
dessins et la musique du jeu : accroche, partie qui penche, sauvetage de justesse, chute, duel en direct, appel à
chercher « Bascule » sur l'App Store. Voir docs/promo/VIDEO.md.

C'est une animation rejouée par ce script (pas une capture de l'iPhone) : les positions sont écrites à la main.

  dotnet run --project Tools/AudioPreview -- Exports/audio
  python3 Tools/promo_video.py Exports/art Exports/audio docs/promo/bascule_lancement.mp4
"""
import functools
import math
import os
import random
import subprocess
import sys
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ART, AUDIO, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
ONLY = [float(x) for x in sys.argv[4].split(",")] if len(sys.argv) > 4 else None  # instants à sortir en PNG (contrôle)
sys.argv = [sys.argv[0], ART, "/tmp", "rien"]
import scene_preview as sp  # noqa: E402

sp.load = functools.lru_cache(maxsize=None)(sp.load)  # mêmes dessins à chaque image : on ne relit pas le disque
W, H, FPS = 1080, 1920, 30
K_ANGLE = 0.85            # degrés de planche par unité de couple (poids × distance)
T_GAME, T_DUEL, T_END = 14.6, 17.7, 20.8
TH = 0.32                 # épaisseur de la planche
HELD_Y = 2.7              # hauteur (monde) du poids tenu avant d'être lâché
random.seed(7)


def ease(x):
    x = max(0.0, min(1.0, x))
    return 1 - (1 - x) ** 3


def fmt(n):
    return f"{n:,}".replace(",", " ")


# ---------------------------------------------------------------------------- Scénario de la partie
START = [dict(v=4, lx=-2.3), dict(v=2, lx=-1.3), dict(v=8, lx=0.6), dict(v=2, lx=2.8)]
# (instant de la visée, position sur la planche, valeur, fusion ?, score après)
DROPS = [
    (0.9, 0.70, 8, True, 1872),
    (2.6, -2.25, 4, True, 1904),
    (4.3, 1.05, 16, True, 2032),
    (6.0, -1.30, 2, True, 2048),
    (7.6, 2.00, 8, False, 2056),
    (9.3, -2.30, 16, False, 2072),
    (11.6, 0.95, 32, True, 2310),
]
AIM, FALL = 0.45, 0.30
KO_AT = DROPS[-1][0] + AIM + FALL + 0.35
CAPTIONS = [
    (0.0, 2.5, "POV : tu crois que c'est facile", 76, "white"),
    (5.2, 6.9, "ça penche…", 96, "white"),
    (8.5, 10.0, "NON NON NON", 112, "Primary"),
    (10.35, 11.9, "SAUVÉ !", 124, "Play"),
    (12.0, 13.0, "allez, un dernier…", 84, "white"),
]


def half_width(v):
    return sp.size_of(v) / 2


def rest_height(weights, v, lx):
    """Hauteur du centre d'un poids posé en lx : sur la planche, ou sur les poids qu'il recouvre."""
    top = TH / 2
    for w in weights:
        if abs(w["lx"] - lx) < (half_width(w["v"]) + half_width(v)) * 0.8:
            top = max(top, w["ly"] + sp.body_half_height(w["v"]))
    return top + sp.body_half_height(v)


def simulate():
    """Rejoue la partie image par image : poids posés, fusions, angle de la planche (ressort), score."""
    weights = [dict(w, ly=TH / 2 + sp.body_half_height(w["v"]), born=-9) for w in START]
    # La planche penche déjà fort à la première image : l'œil est accroché avant même de lire le texte.
    frames, ang, vel, score, done = [], 17.0, 0.0, 1840, 0
    pops, bump = [], -9
    n = int(T_GAME * FPS)
    for f in range(n):
        t = f / FPS
        while done < len(DROPS) and t >= DROPS[done][0] + AIM + FALL:
            _, lx, v, merge, new_score = DROPS[done]
            if merge:
                target = min((w for w in weights if w["v"] == v), key=lambda w: abs(w["lx"] - lx))
                weights.remove(target)
                nv, nlx = v * 2, (target["lx"] + lx) / 2
                weights.append(dict(v=nv, lx=nlx, ly=TH / 2 + sp.body_half_height(nv), born=t))
                pops.append(dict(t=t, lx=nlx, text=f"+{new_score - score}", merge=True))
            else:
                weights.append(dict(v=v, lx=lx, ly=rest_height(weights, v, lx), born=t))
                pops.append(dict(t=t, lx=lx, text=f"+{new_score - score}", merge=False))
            score, bump, done = new_score, t, done + 1
        torque = sum(w["v"] * w["lx"] for w in weights)
        target_angle = 78.0 if t >= KO_AT else max(-30.0, min(30.0, torque * K_ANGLE))
        k, damp = (30.0, 0.7) if t >= KO_AT else (42.0, 5.5)
        vel += ((target_angle - ang) * k - vel * damp) / FPS
        ang += vel / FPS
        frames.append(dict(t=t, ang=ang, score=score, weights=[dict(w) for w in weights], pops=list(pops), bump=bump, done=done))
    return frames


# ---------------------------------------------------------------------------- Dessin
def caption(img, text, cy, size, color="white", alpha=1.0, scale=1.0):
    col = (255, 255, 255) if color == "white" else sp.rgb(sp.COL[color])
    layer = Image.new("RGBA", (W, int(size * 2.2)), (0, 0, 0, 0))
    sp.title(layer, text, (0, 0, W, layer.height), size, color=col)
    if scale <= 0.02:
        return
    if scale != 1.0:
        layer = layer.resize((max(1, int(layer.width * scale)), max(1, int(layer.height * scale))), Image.BICUBIC)
    if alpha < 1.0:
        layer.putalpha(layer.getchannel("A").point(lambda a: int(a * alpha)))
    img.alpha_composite(layer, (int((W - layer.width) / 2), int(cy - layer.height / 2)))


def pop_scale(age, length=0.28):
    """Petit rebond d'apparition : 0 → 1,15 → 1."""
    if age < 0:
        return 0.0
    x = min(1.0, age / length)
    return 1 + 0.35 * math.sin(x * math.pi) * (1 - x) if x >= 0.35 else 1.15 * x / 0.35


BG = {}


def background(theme):
    if theme not in BG:
        BG[theme] = sp.world(theme).img
    return BG[theme].copy()


def game_frame(fr, fallen):
    t, ang = fr["t"], fr["ang"]
    ko = t >= KO_AT
    c = sp.Canvas(background(0))
    squash = 0.06 + 0.10 * max(0.0, 1 - (t - fr["bump"]) / 0.25) + (0.25 * ease((t - KO_AT) / 0.4) if ko else 0)
    a = abs(ang)
    expr = "Sad" if ko else "Surprised" if 10.05 <= t < 10.5 else "Grin" if 10.5 <= t < 11.9 else \
        "Grimace" if a > 17 else "Effort" if a > 11 else "Tense" if a > 5 else "Calm"
    shown = max(-58.0, min(58.0, ang))
    pivot, along, hands = sp.mascot(c, 0, 0.0 if ko and t > KO_AT + 0.35 else shown * (1 if not ko else 0.5), squash, expr,
                                    defeated=ko and t > KO_AT + 0.35)
    drop = 0.0
    if ko:
        dt = t - KO_AT
        drop = 9.0 * max(0.0, dt - 0.25) ** 2
        pivot = (pivot[0] + 1.6 * max(0.0, dt - 0.25), pivot[1] - drop)
    center, al, nm = sp.plank(c, pivot, shown, 6.3)
    # Poids posés (ou qui glissent et tombent après la chute).
    for i, w in enumerate(fr["weights"]):
        if ko:
            s = fallen.setdefault(i, dict(lx=w["lx"], v=0.0, free=None))
            dt = 1.0 / FPS
            if s["free"] is None:
                s["v"] += 22.0 * math.sin(math.radians(shown)) * dt
                s["lx"] += s["v"] * dt
                x, y = sp.on_plank(center, al, nm, s["lx"], w["ly"])
                if s["lx"] > 3.3:
                    s["free"] = dict(x=x, y=y, vx=s["v"] * al[0] + 0.6, vy=s["v"] * al[1], rot=-shown, spin=random.uniform(-260, 260))
                rot = -shown
            else:
                p = s["free"]
                p["vy"] -= 26.0 * dt
                p["x"] += p["vx"] * dt
                p["y"] += p["vy"] * dt
                p["rot"] += p["spin"] * dt
                x, y, rot = p["x"], p["y"], p["rot"]
            sp.weight(c, w["v"], x, y, rot=rot)
        else:
            x, y = sp.on_plank(center, al, nm, w["lx"], w["ly"])
            sp.weight(c, w["v"], x, y, rot=-shown)
    P = sp.json.load(open(os.path.join(ART, "mascot/pivots.json")))
    if not (ko and t > KO_AT + 0.35):
        for hand, side in hands:
            c.sprite(sp.load("mascot/hand.png"), *hand, pivot=P["hand"][:2], scale=sp.PPU / sp.PART_PPU, rot=-shown, flipx=side < 0)
    img = c.img
    d = ImageDraw.Draw(img, "RGBA")
    # Poids tenu, ligne de visée, puis chute.
    nxt = 2
    if fr["done"] < len(DROPS):
        t0, lx, v, _, _ = DROPS[fr["done"]]
        nxt = v
        rx, ry = sp.on_plank(center, al, nm, lx, rest_height(fr["weights"], v, lx))
        if t0 <= t < t0 + AIM + FALL:
            k = max(0.0, (t - t0 - AIM) / FALL)
            wx, wy = rx, HELD_Y + (ry - HELD_Y) * k * k
            if k == 0:
                x0, y0 = sp.to_px(wx, wy - 0.7)
                x1, y1 = sp.to_px(rx, ry + 0.5)
                for yy in range(int(y0), int(y1), 38):
                    d.ellipse((x0 - 7, yy - 7, x0 + 7, yy + 7), fill=(255, 255, 255, 210))
            sp.weight(c, v, wx, wy, rot=0.0)
    # Éclats de fusion et points gagnés.
    for p in fr["pops"]:
        age = t - p["t"]
        if 0 <= age < 0.9:
            x, y = sp.on_plank(center, al, nm, p["lx"], 1.3)
            px, py = sp.to_px(x, y + age * 1.4)
            if p["merge"] and age < 0.3:
                r = int(60 + 500 * age)
                d.ellipse((px - r, py - r + 60, px + r, py + r + 60), outline=(255, 255, 255, int(255 * (1 - age / 0.3))), width=14)
            layer = Image.new("RGBA", (420, 150), (0, 0, 0, 0))
            sp.title(layer, p["text"], (0, 0, 420, 150), 84 if p["merge"] else 60, color=sp.rgb(sp.COL["Accent"]))
            layer.putalpha(layer.getchannel("A").point(lambda v_, a_=min(1.0, (0.9 - age) / 0.3): int(v_ * a_)))
            img.alpha_composite(layer, (int(px - 210), int(py - 75)))
    danger = max(0.0, min(1.0, (a - 9) / 13)) if not ko else 1.0
    sp.hud(c, score=fmt(fr["score"]), best="2 310", angle=shown, nxt=nxt, danger=danger * 0.9, progress=min(1.0, 0.45 + fr["score"] / 9000))
    for a0, a1, text, size, color in CAPTIONS:
        if a0 <= t < a1:
            # Le texte d'accroche est là dès la première image (pas d'effet d'apparition).
            caption(img, text, 545, size, color, alpha=min(1.0, (a1 - t) / 0.15), scale=1.0 if a0 == 0 else pop_scale(t - a0))
    if ko and t > KO_AT + 0.3:
        age = t - KO_AT - 0.3
        veil = Image.new("RGBA", (W, H), (20, 14, 40, int(150 * ease(age / 0.3))))
        img.alpha_composite(veil)
        caption(img, "K.O.", 700, 300, "Primary", scale=pop_scale(age, 0.35))
        if age > 0.45:
            caption(img, "2 310 points", 960, 110, "white", scale=pop_scale(age - 0.45))
        if age > 0.8:
            caption(img, "Tu fais mieux ?", 1120, 110, "Accent", scale=pop_scale(age - 0.8))
    # Tremblement de l'écran quand ça penche fort.
    shake = int(10 * danger) if not ko else int(18 * max(0.0, 1 - (t - KO_AT) / 0.5))
    if shake:
        out = Image.new("RGBA", (W, H), (20, 14, 40, 255))
        out.alpha_composite(img, (random.randint(-shake, shake), random.randint(-shake, shake)))
        img = out
    return img


def put(img, sprite, cx, cy, width, rot=0.0):
    s = width / sprite.width
    im = sprite.resize((max(1, int(sprite.width * s)), max(1, int(sprite.height * s))), Image.LANCZOS)
    if rot:
        im = im.rotate(rot, resample=Image.BICUBIC, expand=True)
    x, y = int(cx - im.width / 2), int(cy - im.height / 2)
    sh = Image.new("RGBA", im.size, (30, 20, 60, 0))
    sh.putalpha(im.getchannel("A").point(lambda a: int(a * 0.35)))
    img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(14)), (x + 14, y + 26))
    img.alpha_composite(im, (x, y))


def duel_frame(t):
    """Carte « Duel en direct » : rouge contre bleu, comme la carte VS du jeu."""
    img = Image.new("RGBA", (W, H), sp.rgb(sp.COL["Secondary"]) + (255,))
    d = ImageDraw.Draw(img, "RGBA")
    lean = 230
    d.polygon([(0, 0), (W // 2 + lean, 0), (W // 2 - lean, H), (0, H)], fill=sp.rgb(sp.COL["Primary"]) + (255,))
    k = ease(t / 0.35)
    cy = 930
    put(img, sp.load("heads/00.png"), -300 + 570 * k, cy - 60, 430, rot=6)
    put(img, sp.load("heads/62.png"), W + 300 - 570 * k, cy + 60, 430, rot=-6)
    s = pop_scale(t - 0.3, 0.3)
    if s > 0:
        r = int(120 * s)
        d.ellipse((W // 2 - r, cy - r, W // 2 + r, cy + r), fill=sp.rgb(sp.COL["Ink"]) + (255,), outline=(255, 255, 255, 255), width=12)
        caption(img, "VS", cy, max(8, 120 * s), "Accent")
    if t > 0.1:
        box = (W / 2 - 230, 300, W / 2 + 230, 410)
        sp.pill(img, box, color=sp.rgb(sp.COL["Ink"]) + (240,))
        sp.title(img, "NOUVEAU", box, 64, color=sp.rgb(sp.COL["Accent"]))
        caption(img, "DUEL EN DIRECT", 520, 124, "white", scale=pop_scale(t - 0.1))
    if t > 0.9:
        caption(img, "Défie tes potes", 1330, 104, "white", scale=pop_scale(t - 0.9))
    if t > 1.4:
        caption(img, "Le premier qui lâche a perdu", 1470, 68, "Accent", scale=pop_scale(t - 1.4))
    return img


LOGO_COLORS = ["Primary", "Accent", "Play", "Secondary", "Purple", "Primary", "Accent"]


def logo(img, cx, y, size, t):
    """BASCULE en lettres de couleur inclinées, comme sur l'accueil du jeu : chaque lettre saute à son tour."""
    step, cell = int(size * 0.71), int(size * 1.5)
    x = int(cx - (step * 6 + cell) / 2)
    for i, ch in enumerate("BASCULE"):
        s = pop_scale(t - i * 0.06, 0.3)
        if s <= 0:
            continue
        layer = Image.new("RGBA", (cell, int(cell * 1.15)), (0, 0, 0, 0))
        sp.title(layer, ch, (0, 0, layer.width, layer.height), size, color=sp.rgb(sp.COL[LOGO_COLORS[i]]))
        layer = layer.rotate(math.sin(i * 0.6) * 5 + 2, resample=Image.BICUBIC)
        layer = layer.resize((max(1, int(layer.width * s)), max(1, int(layer.height * s))), Image.BICUBIC)
        lx = x + i * step + (cell - layer.width) // 2
        ly = y + int(math.sin(i * 1.2) * size * 0.09) + (int(cell * 1.15) - layer.height) // 2
        img.alpha_composite(layer, (lx, ly))


HERO = {}


def end_frame(t):
    """Fin : Basile content, le logo, et la consigne « cherche Bascule sur l'App Store »."""
    if "img" not in HERO:
        HERO["img"] = sp.game_scene(theme=0, angle=-5.0, expression="Happy", squash=0.05).img
    img = HERO["img"].copy()
    logo(img, W / 2, 290, 190, t)
    if t > 0.6:
        s = pop_scale(t - 0.6)
        box = (W / 2 - 470 * s, 720 - 70 * s, W / 2 + 470 * s, 720 + 70 * s)
        sp.pill(img, box, color=sp.rgb(sp.COL["Ink"]) + (240,))
        sp.title(img, "Cherche « Bascule »", box, max(8, 84 * s))
    if t > 0.95:
        caption(img, "sur l'App Store · Gratuit", 860, 68, "Accent", scale=pop_scale(t - 0.95))
    return img


def frame_at(f, sim, fallen):
    t = f / FPS
    if t < T_GAME:
        img = game_frame(sim[f], fallen)
    elif t < T_DUEL:
        img = duel_frame(t - T_GAME)
    else:
        img = end_frame(t - T_DUEL)
    # Éclair blanc de 3 images à chaque changement de plan.
    for cut in (T_GAME, T_DUEL):
        if 0 <= t - cut < 0.1:
            img.alpha_composite(Image.new("RGBA", (W, H), (255, 255, 255, int(230 * (1 - (t - cut) / 0.1)))))
    return img.convert("RGB")


# ---------------------------------------------------------------------------- Son
def soundtrack(path):
    """Musique du jeu (montée de tension de demo_partie.wav) + bruitages calés sur l'image."""
    sr = 44100
    out = np.zeros(int(T_END * sr) + sr, dtype=np.float64)
    w = wave.open(os.path.join(AUDIO, "demo_partie.wav"))
    msr = w.getframerate()
    music = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float64) / 32768
    music = music[int(52 * msr):int((52 + T_END + 1) * msr)]
    music = np.interp(np.arange(len(out)) * msr / sr, np.arange(len(music)), music)
    env = np.ones(len(out))
    env[:int(0.15 * sr)] = np.linspace(0, 1, int(0.15 * sr))
    tail = int(0.8 * sr)
    env[int(T_END * sr) - tail:int(T_END * sr)] = np.linspace(1, 0, tail)
    env[int(T_END * sr):] = 0
    out += music * env * 0.85

    def add(t, sig, gain=1.0):
        i = int(t * sr)
        out[i:i + len(sig)] += sig[:len(out) - i] * gain

    def tone(f0, f1, dur, decay):
        x = np.arange(int(dur * sr)) / sr
        freq = f0 + (f1 - f0) * x / dur
        return np.sin(2 * np.pi * np.cumsum(freq) / sr) * np.exp(-x * decay)

    for t0, _, v, merge, _ in DROPS:
        land = t0 + AIM + FALL
        add(land, tone(150, 70, 0.22, 16), 0.55)                      # le poids se pose
        if merge:
            add(land + 0.02, tone(660, 660, 0.18, 14), 0.22)          # fusion : deux notes qui montent
            add(land + 0.10, tone(990, 990, 0.30, 10), 0.22)
    add(DROPS[5][0] + AIM + FALL + 0.25, tone(520, 1040, 0.35, 5), 0.20)   # sauvé
    add(KO_AT, tone(420, 90, 0.9, 3), 0.35)                               # chute
    noise = np.random.RandomState(3).uniform(-1, 1, int(0.5 * sr)) * np.exp(-np.arange(int(0.5 * sr)) / sr * 9)
    add(KO_AT + 0.75, noise, 0.30)
    for cut in (T_GAME, T_DUEL):
        swoosh = np.random.RandomState(5).uniform(-1, 1, int(0.3 * sr)) * np.sin(np.linspace(0, np.pi, int(0.3 * sr))) ** 2
        add(cut - 0.12, swoosh, 0.18)
    add(T_GAME + 0.3, tone(110, 55, 0.4, 8), 0.6)                         # « VS »
    add(T_DUEL + 0.6, tone(784, 784, 0.25, 9), 0.2)
    add(T_DUEL + 0.75, tone(1175, 1175, 0.5, 6), 0.2)
    out = np.tanh(out * 1.4) * 0.9
    with wave.open(path, "wb") as o:
        o.setnchannels(1)
        o.setsampwidth(2)
        o.setframerate(sr)
        o.writeframes((out * 32767).astype(np.int16).tobytes())


def main():
    sim = simulate()
    total = int(T_END * FPS)
    fallen = {}
    if ONLY:
        os.makedirs(OUT, exist_ok=True)
        want = {int(x * FPS) for x in ONLY}
        for f in range(max(want) + 1):
            img = frame_at(f, sim, fallen) if (f in want or f / FPS >= KO_AT) else None
            if f in want:
                img.save(os.path.join(OUT, f"t_{f / FPS:05.2f}.png"))
        return
    os.makedirs(os.path.dirname(OUT) or ".", exist_ok=True)
    wav = OUT + ".wav"
    soundtrack(wav)
    ff = subprocess.Popen(["ffmpeg", "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS),
                           "-i", "-", "-i", wav, "-c:v", "libx264", "-preset", "slow", "-crf", "17", "-pix_fmt", "yuv420p",
                           "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", "-shortest", OUT], stdin=subprocess.PIPE)
    for f in range(total):
        ff.stdin.write(frame_at(f, sim, fallen).tobytes())
        if f % 90 == 0:
            print(f"{f}/{total}", flush=True)
    ff.stdin.close()
    ff.wait()
    os.remove(wav)
    print("ok", OUT)


main()
