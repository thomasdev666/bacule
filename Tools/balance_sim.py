"""
Simulation d'équilibrage de Bascule, sans Unity.

Reproduit la boucle de jeu (poids qui tombent, fusions, couple, planche amortie, 128 qui s'envole,
défaite) avec le moteur physique 2D pymunk, à partir du MÊME fichier de réglages que le jeu
(Assets/Bascule/Resources/bascule_tuning.json). Deux joueurs automatiques :
  - "hasard"  : lâche au hasard au-dessus de la planche (joueur qui ne réfléchit pas) ;
  - "malin"   : vise pour contrebalancer et pour fusionner (joueur appliqué).
Objectif : le hasard doit perdre assez vite, le malin tenir longtemps sans être immortel.

Usage :  pip install pymunk  puis  python3 Tools/balance_sim.py [nombre_de_parties]
Ce n'est qu'une approximation (Chipmunk au lieu de Box2D), utile pour dégrossir les réglages.
"""
import json
import math
import os
import random
import statistics
import sys

import pymunk

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
T = json.load(open(os.path.join(ROOT, "Assets/Bascule/Resources/bascule_tuning.json"), encoding="utf-8"))
for kv in filter(None, os.environ.get("TUNE", "").split(",")):
    k, v = kv.split("=")
    sec, key = k.split(".")
    T[sec][key] = float(v)
P, W, S, SC = T["plank"], T["weights"], T["spawn"], T["score"]
WD = T.get("wind", {})
# Comparaison avec la 1.1 : LEGACY=1 remet la planche qui rétrécit, sans sensibilité ni vent.
if os.environ.get("LEGACY") == "1":
    P["shrinkUntilScore"], P["sensitivityMax"] = 0, 1.0
    WD = dict(WD, startScore=10**9)


def sensitivity(score):
    """Copie de Difficulty.Sensitivity."""
    f = P.get("sensitivityFromScore", 4000)
    if score <= f:
        return 1.0
    return min(P.get("sensitivityMax", 1.0), 1 + P.get("sensitivityPer1000", 0.0) * (score - f) / 1000)


class Wind:
    """Copie de Bascule.Core.Gusts (rafales de vent)."""

    def __init__(self, rng):
        self.rng = rng
        self.phase, self.t, self.dir, self.strength, self.count = "none", 0.0, 1, 0.0, 0
        self.ms = list(WD.get("milestones", [5000, 8000, 10000]))
        self.mi, self.pending, self.since, self.started = 0, False, 0.0, False
        self.gap = self.next_gap()
        self.win_t, self.win_score, self.stagnant = 0.0, 0, False

    def next_gap(self):
        return WD.get("gapMin", 45) + (WD.get("gapMax", 75) - WD.get("gapMin", 45)) * self.rng.random()

    def milestone(self, i):
        if i < len(self.ms):
            return self.ms[i]
        return (self.ms[-1] if self.ms else WD.get("startScore", 5000)) + WD.get("milestoneEvery", 2500) * (i - len(self.ms) + 1)

    def envelope(self):
        if self.phase != "blow":
            return 0.0
        d, r, t = WD.get("blowSeconds", 3), WD.get("rampSeconds", 0.7), self.t
        sm = lambda x: max(0, min(1, x)) ** 2 * (3 - 2 * max(0, min(1, x)))
        e = sm(t / r) if t < r else sm((d - t) / r) if t > d - r else 1.0
        return e * (0.86 + 0.14 * math.sin(t * 7.3) * math.sin(t * 2.1 + 0.7))

    def update(self, dt, score, danger, ratio, blocked):
        if not self.started:
            if score < WD.get("startScore", 5000):
                return
            self.started, self.win_score = True, score
        while score >= self.milestone(self.mi):
            self.pending, self.mi = True, self.mi + 1
        self.win_t += dt
        if self.win_t >= WD.get("stagnationWindow", 20):
            self.stagnant = score - self.win_score < WD.get("stagnationPoints", 200)
            self.win_t, self.win_score = 0.0, score
        if self.phase == "warn":
            self.t += dt
            if self.t >= WD.get("warningSeconds", 1.8):
                self.phase, self.t = "blow", 0.0
            return
        if self.phase == "blow":
            self.t += dt
            if self.t >= WD.get("blowSeconds", 3):
                self.phase, self.t, self.since = "none", 0.0, 0.0
            return
        self.since += dt * (WD.get("stagnationSpeedup", 1.8) if self.stagnant else 1)
        due = self.pending or self.since >= self.gap
        if not due or blocked or self.since < WD.get("minGap", 25) or danger > WD.get("maxDangerToStart", 0.5):
            return
        self.dir = -1 if self.rng.random() < 0.5 else 1
        base = min(WD.get("strengthMax", 0.6), WD.get("strengthStart", 0.42) + WD.get("strengthStep", 0.04) * self.count)
        toward = ratio * self.dir
        self.strength = max(WD.get("strengthMin", 0.3), min(base, WD.get("maxPeakRatio", 0.85) - max(0, toward)))
        self.phase, self.t, self.pending, self.since, self.gap = "warn", 0.0, False, 0.0, self.next_gap()
        self.count += 1

    def torque(self, losing):
        return self.dir * self.strength * losing * self.envelope()


DT = 1 / 60
PIVOT = (0.0, -3.4)
SPAWN_Y = 4.5
HALF_WIDTH = 4.4


def eff(v):
    """Poids effectif pour le couple (PlankTuning.torqueExponent)."""
    return v ** P.get("torqueExponent", 1.0)


def size(v):
    return min(W["maxSize"], W["baseSize"] * v ** W["sizeExponent"])


class Plank:
    """Copie de Bascule.Core.PlankModel."""

    def __init__(self):
        self.angle = 0.0
        self.vel = 0.0
        self.strain = 0.0
        self.lost = False
        self.peak = 0.0
        self.sens = 1.0

    @property
    def losing(self):
        return P["maxAngle"] / (P["degreesPerTorque"] * self.sens)

    @property
    def danger(self):
        r = abs(self.angle) / P["maxAngle"]
        if r < 1:
            return r
        return 1 + self.strain / max(1e-6, P["loseGraceSeconds"])

    def step(self, dt, torque, load):
        limit = P["maxAngle"] + P["overshootAngle"]
        target = max(-limit * 1.5, min(limit * 1.5, P["tiltDirection"] * P["degreesPerTorque"] * self.sens * torque))
        freq = P["frequency"] / math.sqrt(1 + P["loadSlowdown"] * max(0.0, load))
        w0 = 2 * math.pi * freq
        steps = max(1, math.ceil(dt / (1 / 120)))
        h = dt / steps
        for _ in range(steps):
            acc = w0 * w0 * (target - self.angle) - 2 * P["dampingRatio"] * w0 * self.vel
            self.vel += acc * h
            self.angle += self.vel * h
            if self.angle > limit:
                self.angle, self.vel = limit, min(self.vel, 0)
            if self.angle < -limit:
                self.angle, self.vel = -limit, max(self.vel, 0)
        if abs(self.angle) > P["maxAngle"]:
            self.strain += dt
        else:
            self.strain = max(0.0, self.strain - dt * 2)
        if self.strain > P["loseGraceSeconds"]:
            self.lost = True


class Game:
    def __init__(self, seed, bot, skill=0.7, lives=None):
        self.rng = random.Random(seed)
        self.bot = bot
        # Compétence d'un joueur humain simulé (0 = débutant maladroit, 1 = expert) : bruit de décision et de visée.
        self.skill = skill
        self.max_lives = int(SC.get("lives", 0)) if lives is None else lives
        self.lives = self.max_lives
        self.next_life = SC.get("lifeEveryPoints", 0) or 10**9
        self.lost_by_value = {}
        self.cause = "planche"
        self.best_chain = 0
        self.merge_count = 0
        self.max_value = 0
        self.made = {}
        self.space = pymunk.Space()
        self.space.gravity = (0, -9.81 * W["gravityScale"])
        self.space.iterations = 20
        self.plank = Plank()
        self.wind = Wind(random.Random(seed * 7919 + 13))
        self.score_trace = []  # (temps, score) pour mesurer la difficulté à haut score
        self.length = P["startLength"]
        self.body = pymunk.Body(body_type=pymunk.Body.KINEMATIC)
        self.body.position = PIVOT
        self.shape = None
        self.set_length(self.length)
        self.weights = {}  # shape -> dict
        self.score = 0
        self.drops = 0
        self.balloons = 0
        self.lost_weights = 0
        self.chain = 0
        self.last_merge = -99.0
        self.time = 0.0
        self.pending = []
        self.phantoms = []
        self.best_save = 0.0
        self._armed, self._peak = False, 0.0
        if hasattr(self.space, "on_collision"):  # pymunk 7
            self.space.on_collision(0, 0, post_solve=self.on_touch)
        else:  # pymunk 6
            h = self.space.add_default_collision_handler()
            h.post_solve = lambda arb, space, data: self.on_touch(arb, space, data) or True
        self.current = self.draw()
        self.next = self.draw()

    def draw(self):
        tier = S["tiers"][0]
        for t in S["tiers"]:
            if self.drops >= t["fromDrop"]:
                tier = t
        return self.rng.choices(tier["values"], weights=tier["chances"])[0]

    def set_length(self, length):
        if self.shape is not None:
            self.space.remove(self.shape)
        else:
            self.space.add(self.body)
        th = P["thickness"]
        self.shape = pymunk.Poly.create_box(self.body, (length, th), radius=0.02)
        # Rebords aux bouts (PlankTuning.endLipHeight / endLipWidth).
        for lip in getattr(self, "lips", []):
            self.space.remove(lip)
        self.lips = []
        lh, lw = P.get("endLipHeight", 0), P.get("endLipWidth", 0)
        if lh > 0.01 and lw > 0.01:
            for sx in (-1, 1):
                cx = sx * (length / 2 - lw / 2)
                lip = pymunk.Poly(self.body, [(cx - lw / 2, th / 2 - 0.02), (cx + lw / 2, th / 2 - 0.02), (cx + lw / 2, th / 2 + lh), (cx - lw / 2, th / 2 + lh)])
                lip.friction = math.sqrt(W["friction"])
                lip.collision_type = 0
                self.lips.append(lip)
        self.shape.friction = math.sqrt(W["friction"])
        self.shape.elasticity = W["bounciness"]
        self.shape.collision_type = 0
        self.space.add(self.shape)
        for lip in self.lips:
            self.space.add(lip)
        self.length = length

    def spawn(self, v, pos, landed, vel=(0, -1)):
        s = size(v)
        edge = s * 0.18
        mass = v ** W["massExponent"]
        inner = (s - 2 * edge) * 0.96
        moment = pymunk.moment_for_box(mass, (s, s))
        b = pymunk.Body(mass, moment)
        b.position = pos
        b.velocity = vel
        sh = pymunk.Poly.create_box(b, (inner, inner), radius=edge)
        sh.friction = math.sqrt(W["friction"])
        sh.elasticity = W["bounciness"]
        sh.collision_type = 0
        self.space.add(b, sh)
        self.weights[sh] = {"v": v, "landed": landed, "body": b, "merging": False, "size": s}

    def on_touch(self, arb, space, data):
        a, b = arb.shapes
        wa, wb = self.weights.get(a), self.weights.get(b)
        for w, other in ((wa, b), (wb, a)):
            if w is None or w["landed"]:
                continue
            if other is self.shape or other in self.lips or (self.weights.get(other) and self.weights[other]["landed"]):
                w["landed"] = True
        if wa and wb and wa["v"] == wb["v"]:
            self.pending.append((a, b))
        return True

    def local(self, pos):
        rad = -math.radians(self.plank.angle)
        ax, ay = math.cos(rad), math.sin(rad)
        nx, ny = -ay, ax
        cx, cy = PIVOT[0] + nx * P["thickness"] / 2, PIVOT[1] + ny * P["thickness"] / 2
        dx, dy = pos[0] - cx, pos[1] - cy
        return dx * ax + dy * ay, dx * nx + dy * ny

    def torque(self):
        tq = load = 0.0
        half = self.length / 2
        for w in self.weights.values():
            if not w["landed"] or w["merging"]:
                continue
            lx, ly = self.local(w["body"].position)
            if abs(lx) > half + w["size"] / 2 or ly < -0.3:
                continue
            tq += eff(w["v"]) * lx / P["referenceHalfLength"]
            load += w["v"]
        for p in self.phantoms:
            k = p["t"] / P["balloonLiftSeconds"] if P["balloonLiftSeconds"] > 0 else 0
            tq += eff(p["v"]) * p["x"] / P["referenceHalfLength"] * k
            load += p["v"] * k
            p["t"] -= DT
        self.phantoms = [p for p in self.phantoms if p["t"] > 0]
        return tq, load

    def merges(self):
        pairs, self.pending = self.pending, []
        for a, b in pairs:
            wa, wb = self.weights.get(a), self.weights.get(b)
            if not wa or not wb or wa["merging"] or wb["merging"] or wa["v"] != wb["v"]:
                continue
            wa["merging"] = wb["merging"] = True
            pa, pb = wa["body"].position, wb["body"].position
            low = pa if pa.y <= pb.y else pb
            mid = ((pa.x + pb.x) / 2, (pa.y + pb.y) / 2)
            pos = (low.x + (mid[0] - low.x) * 0.35, low.y + (mid[1] - low.y) * 0.35)
            landed = wa["landed"] or wb["landed"]
            v = wa["v"] * 2
            for sh in (a, b):
                self.space.remove(sh, self.weights[sh]["body"])
                del self.weights[sh]
            self.chain = self.chain + 1 if self.chain > 0 and self.time - self.last_merge <= SC["comboWindow"] else 1
            self.last_merge = self.time
            self.best_chain = max(self.best_chain, self.chain)
            self.merge_count += 1
            self.max_value = max(self.max_value, v)
            self.made[v] = self.made.get(v, 0) + 1
            mult = min(SC["maxComboMultiplier"], 1 + SC["comboStep"] * (self.chain - 1))
            self.score += round(v * SC["pointsPerValue"] * mult)
            if self.max_lives > 0 and self.score >= self.next_life:
                self.next_life += SC.get("lifeEveryPoints", 10**9)
                self.lives = min(self.max_lives, self.lives + 1)
            if v >= 128:
                self.balloons += 1
                if self.max_lives > 0:
                    self.lives = min(self.max_lives, self.lives + 1)
                self.score += round(SC["balloonBonus"] * mult)
                if landed:
                    self.phantoms.append({"v": v, "x": self.local(pos)[0], "t": P["balloonLiftSeconds"]})
            else:
                self.spawn(v, pos, landed, (0, 0))

    def choose_x(self):
        half = self.length / 2
        if self.bot == "hasard":
            return self.rng.uniform(-half, half)
        if self.bot == "joueur":
            k = self.skill
            # Un débutant lâche parfois sans réfléchir, réfléchit mal et vise imprécisément.
            if self.rng.random() < 0.35 * (1 - k):
                x = self.rng.uniform(-half, half)
            else:
                x = self.think(half, noise=0.3 + 3.2 * (1 - k))
            x += self.rng.gauss(0, 0.06 + 0.7 * (1 - k))
            lim = max(0.1, half * math.cos(math.radians(self.plank.angle)) - size(self.current) * 0.45)
            return max(-lim, min(lim, x))
        return self.think(half, 1.0)

    def think(self, half, noise):
        # Malin : prévoit le couple (y compris les poids encore en l'air), contrebalance,
        # et ne vise une fusion que si elle n'aggrave pas trop le déséquilibre.
        tq = 0.0
        for w in self.weights.values():
            lx, ly = self.local(w["body"].position)
            if abs(lx) <= half + w["size"] / 2 and (w["landed"] or w["body"].position.y > PIVOT[1]):
                tq += eff(w["v"]) * lx / P["referenceHalfLength"]
        tq += self.plank.vel / max(1e-6, P["degreesPerTorque"]) * 0.15  # anticipe le mouvement
        if self.wind.phase != "none":
            react = 1.0 if self.bot == "malin" else self.skill
            tq += react * self.wind.dir * self.wind.strength * self.plank.losing
        best, best_cost = 0.0, 1e9
        for i in range(31):
            x = -half * 0.9 + i * (half * 1.8 / 30)
            new_tq = tq + eff(self.current) * x / P["referenceHalfLength"]
            cost = abs(new_tq) * 1.0 + abs(x) * 0.3
            for w in self.weights.values():
                if w["v"] == self.current and abs(w["body"].position.x - x) < w["size"] * 0.5:
                    cost -= min(3.0, 1 + w["v"] * 0.1)
            cost += self.rng.uniform(0, noise)  # imprécision humaine
            if cost < best_cost:
                best, best_cost = x, cost
        return best

    @property
    def over(self):
        return self.plank.lost or (self.max_lives > 0 and self.lives <= 0)

    def run(self, max_drops=400):
        cooldown = 0.3
        while not self.over and self.drops < max_drops:
            cooldown -= DT
            if cooldown <= 0:
                x = self.choose_x()
                self.spawn(self.current, (x, SPAWN_Y), False)
                self.drops += 1
                self.chain = 0
                self.current, self.next = self.next, self.draw()
                # Temps de visée humain : un débutant hésite plus.
                cooldown = S["dropCooldown"] + (0.6 if self.bot != "joueur" else 0.5 + 0.9 * (1 - self.skill) * self.rng.random() + 0.3)
                until = P.get("shrinkUntilScore", 0)
                s_len = min(self.score, until) if until > 0 else self.score
                target = max(P["minLength"], P["startLength"] - P["shrinkPer1000Points"] * s_len / 1000)
                if abs(target - self.length) > 0.02:
                    self.set_length(target)
            self.time += DT
            self.merges()
            tq, load = self.torque()
            self.plank.sens = sensitivity(self.score)
            ratio = self.plank.angle / P["maxAngle"] * P["tiltDirection"]
            self.wind.update(DT, self.score, self.plank.danger, ratio, self.time < 3)
            self.plank.step(DT, tq + self.wind.torque(self.plank.losing), load)
            if int(self.time / DT) % 30 == 0:
                self.score_trace.append(self.score)
            d = self.plank.danger
            if d >= 0.85:
                self._armed, self._peak = True, max(self._peak, d)
            elif self._armed and d <= 0.5:
                self._armed = False
                self.best_save = max(self.best_save, self._peak)
                self._peak = 0
            # Planche cinématique : on lui donne la vitesse qui l'amène à l'angle voulu (comme MoveRotation).
            want = -math.radians(self.plank.angle)
            self.body.angular_velocity = (want - self.body.angle) / DT
            self.space.step(DT)
            for sh, w in list(self.weights.items()):
                p = w["body"].position
                if p.y < PIVOT[1] - 5.5 or abs(p.x) > HALF_WIDTH + 2:
                    self.space.remove(sh, w["body"])
                    del self.weights[sh]
                    if w["merging"]:
                        continue
                    self.lost_weights += 1
                    self.lost_by_value[w["v"]] = self.lost_by_value.get(w["v"], 0) + 1
                    self.score -= min(self.score, round(w["v"] * SC.get("lostWeightPenalty", 0)))
                    self.chain = 0
                    if self.max_lives > 0 and self.costs_life(w["v"]):
                        self.lives -= 1
                        if self.lives <= 0:
                            self.cause = "coeurs"
                            break
        return self

    @staticmethod
    def costs_life(v):
        return v >= SC.get("lifeMinValue", 1)


def high_score_report(n, max_drops):
    """Difficulté à haut score : part des joueurs encore en vie à chaque palier, et chute par tranche."""
    rows = []
    for bot, skill in (("malin", 1.0), ("joueur", 0.85), ("joueur", 0.7)):
        games = [Game(seed, bot, skill=skill).run(max_drops) for seed in range(n)]
        alive = lambda s: sum(1 for g in games if max(g.score_trace + [g.score]) >= s) / n
        gusts = statistics.mean(g.wind.count for g in games)
        reached = [max(g.score_trace + [g.score]) for g in games]
        rows.append((bot if bot == "malin" else f"joueur {skill}", statistics.median(reached), [alive(s) for s in (2000, 5000, 8000, 10000, 15000)], gusts,
                     sum(1 for g in games if not g.over)))
    print(f"\n{'bot':<12}{'méd. score':>11}  % encore en jeu à   2k    5k    8k   10k   15k   rafales/partie  non perdues")
    for name, med, al, gu, alive_end in rows:
        print(f"{name:<12}{med:>11.0f}  {'':>17}" + "".join(f"{a*100:>6.0f}" for a in al) + f"{gu:>16.1f}{alive_end:>12}")


def main():
    if len(sys.argv) > 1 and sys.argv[1] == "haut":
        n = int(sys.argv[2]) if len(sys.argv) > 2 else 30
        high_score_report(n, int(sys.argv[3]) if len(sys.argv) > 3 else 900)
        return
    n = int(sys.argv[1]) if len(sys.argv) > 1 else 20
    print(f"Réglages : {os.path.relpath(os.path.join(ROOT, 'Assets/Bascule/Resources/bascule_tuning.json'))}")
    for bot in ("hasard", "malin"):
        games = [Game(seed, bot).run() for seed in range(n)]
        drops = [g.drops for g in games]
        scores = [g.score for g in games]
        survived = sum(1 for g in games if not g.plank.lost)
        print(f"\n[{bot}] {n} parties")
        print(f"  poids lâchés avant la chute : médiane {statistics.median(drops):.0f}  (min {min(drops)}, max {max(drops)})")
        print(f"  score                       : médiane {statistics.median(scores):.0f}  (max {max(scores)})")
        print(f"  128 envolés (total)         : {sum(g.balloons for g in games)}")
        print(f"  poids perdus par partie     : {statistics.mean(g.lost_weights for g in games):.1f}")
        print(f"  sauvetages de justesse >=90%: {sum(1 for g in games if g.best_save >= 0.9)} parties")
        print(f"  parties non perdues (400 poids) : {survived}")


if __name__ == "__main__":
    main()
