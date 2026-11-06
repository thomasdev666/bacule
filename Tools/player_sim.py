"""
Modèle de 1000 joueurs pour Bascule : rétention, temps de jeu, pubs, achats.

Ce n'est PAS une mesure : c'est un modèle à base d'agents qui sert à repérer les problèmes de structure
(objectifs inatteignables, sessions trop courtes, pubs trop fréquentes...) et à comparer deux versions du jeu
toutes choses égales par ailleurs. Les chiffres absolus (rétention, revenus) dépendent d'hypothèses tirées des
moyennes publiques du jeu mobile « casual » ; elles sont toutes regroupées dans HYP ci-dessous.
Les vraies valeurs viendront des statistiques d'un lancement test (voir docs/RAPPORT.md).

Les parties elles-mêmes ne sont pas inventées : chaque partie jouée par un joueur simulé est tirée de la
bibliothèque produite par Tools/game_library.py (vraie physique approchée, vrais réglages du jeu), puis ajustée
selon le talent du joueur et son apprentissage.

Usage :
  python3 Tools/game_library.py 80      # une fois, ou après un changement de réglages
  python3 Tools/player_sim.py [joueurs] [jours]
"""
import json
import math
import os
import random
import statistics as st
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "sim_out")

# ---------------------------------------------------------------------------------------------- hypothèses
HYP = {
    # Comportement (moyennes du casual mobile : J1 25-40 %, J7 8-15 %, J30 2-6 %).
    "e0_a": 2.0, "e0_b": 4.5,       # envie de départ (moyenne 0,31)
    "reward_scale": 1.0,            # force de toutes les récompenses
    "habituation": 6.0,             # après 6 récompenses du même type, l'effet est divisé par 2
    "decay_per_day": 0.88,          # l'envie s'use d'elle-même chaque jour
    "missed_day_decay": 0.84,       # un jour sans ouvrir l'appli : l'habitude se perd
    "notif_optin": 0.5,             # part des joueurs qui acceptent les notifications
    "notif_bonus": 0.03,            # chance de revenir en plus un jour donné grâce au rappel
    "gift_bonus": 0.015,            # cadeau du jour en attente (et série à ne pas perdre)
    "missions_bonus": 0.01,         # missions du jour à faire
    "core_fun_max": 0.07,           # plaisir du jeu lui-même, qui ne s'use pas (chance de revenir, au plus)
    "payer_share": 0.05,            # joueurs prêts à payer un jour (casual : 1 à 3 % paient vraiment)
    # Pubs (France, pubs non personnalisées : environ 30 à 50 % moins chères que la moyenne).
    "ecpm_interstitial": 4.0,       # € pour 1000 pubs affichées
    "ecpm_rewarded": 8.0,
    "fill_rate": 0.92,              # part des demandes de pub réellement servies
    # Achats : prix TTC -> net développeur (TVA 20 %, commission store 15 %).
    "iap_net": 0.85 / 1.2,
}

PRICES = {"removeads": 2.99, "starter": 3.99, "coins_s": 0.99, "coins_m": 2.99, "coins_l": 6.99, "legends": 4.99}

# ---------------------------------------------------------------------------------------------- versions du jeu
OLD_SCORE = [s for s in range(500, 5001, 500)] + list(range(6000, 15001, 1000)) + list(range(17500, 35001, 2500)) + list(range(40000, 60001, 5000))
NEW_SCORE = [250, 500, 750, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 5000, 6000, 7000, 8000, 10000, 12500, 15000, 20000, 30000, 50000]
NEW_LEVEL = [3, 5, 7, 10, 13, 16, 20, 24, 28, 32, 36, 40, 45]
SHOP_PRICES = [400, 500, 600, 750, 900, 1000, 1200, 1400, 1600, 1800, 2000, 2400, 2800, 3200, 3600, 4000]

DESIGNS = {
    "avant": {
        "score_heads": OLD_SCORE, "level_heads": [], "levels": False, "coins": False, "missions": False,
        "gift": False, "notifications": False, "interstitial": None, "rewarded": {"revive"}, "shop": False,
        "iap": {"removeads"}, "tutorial": False,
    },
    "apres": {
        "score_heads": NEW_SCORE, "level_heads": NEW_LEVEL, "levels": True, "coins": True, "missions": True,
        "gift": True, "notifications": True,
        "interstitial": {"first_after_games": 6, "every_games": 3, "min_seconds": 180, "after_rewarded": 120, "skip_on_best": True},
        "rewarded": {"revive", "double", "gift", "reroll", "free_coins"}, "shop": True,
        "iap": {"removeads", "starter", "coins_s", "coins_m", "coins_l", "legends"}, "tutorial": True,
    },
}
# V10 : réponses aux trois grandes causes de départ (Core/Engagement.cs) : coffre toutes les 3 parties,
# objectif de partie (60 % du record), une partie spéciale sur 4 (pièces x2, XP x2, coffre +2).
DESIGNS["v10"] = dict(DESIGNS["apres"], chest=True, goal=True, specials=True)
# V17 : économie complète (Core/Cosmetics.cs) : planches à collectionner, machine à surprises (tête sans doublon),
# protège-série du cadeau du jour. Tout est cosmétique : le score ne change pas.
DESIGNS["v17"] = dict(DESIGNS["v10"], economy=True)
PLANK_PRICES = [500, 800, 1100, 1500, 2000, 5000]
SURPRISE_PRICE, SHIELD_PRICE, SHIELD_MAX = 600, 300, 2
# V18 : têtes plus rares (Core/HeadCatalog.cs, Core/Engagement.cs, Core/Cosmetics.cs).
# Compromis retenu (« début doux, puis rare ») après comparaison de 4 réglages (docs/ECONOMIE.md).
RARE_SCORE = [250, 500, 1000, 1500, 2000, 3000, 4000, 5000, 6500, 8000, 10000, 12500, 15000, 17500, 20000, 25000, 30000, 40000, 50000, 75000]
RARE_LEVEL = [3, 6, 9, 12, 16, 20, 25, 30, 35, 40, 45, 50, 60]
RARE_SHOP = [600, 800, 1000, 1300, 1600, 2000, 2400, 2900, 3400, 4000, 4600, 5200, 5800, 6500, 7200, 8000]
DESIGNS["v18"] = dict(DESIGNS["v17"], score_heads=RARE_SCORE, level_heads=RARE_LEVEL, rare=True)


def xp_for_level(n):
    """XP pour passer du niveau n au niveau n+1 (Core/Economy.cs : Levels.XpToNext)."""
    return 75 + 50 * n


def game_xp(score):
    return 10 + score // 10


def game_coins(score):
    return score // 25


# ---------------------------------------------------------------------------------------------- joueur
class Player:
    def __init__(self, rng, design, games_by_skill, skills):
        self.rng = rng
        self.d = design
        self.talent = math.exp(rng.gauss(0, 0.35))
        # Le niveau de compétence de la bibliothèque le plus proche du talent.
        q = min(0.999, max(0.0, 0.5 + 0.5 * math.erf(math.log(self.talent) / (0.35 * math.sqrt(2)))))
        self.pool = games_by_skill[skills[min(len(skills) - 1, int(q * len(skills)))]]
        self.E = rng.betavariate(HYP["e0_a"], HYP["e0_b"])
        self.aversion = rng.uniform(0.3, 1.7)
        self.payer = rng.random() < HYP["payer_share"]
        self.wtp = math.exp(rng.gauss(1.2, 0.8)) if self.payer else 0
        self.notif = design["notifications"] and rng.random() < HYP["notif_optin"]
        self.max_sessions = 1 + min(3, int(rng.expovariate(1.2)))
        self.core_fun = rng.uniform(0, HYP["core_fun_max"]) ** 1.5 / HYP["core_fun_max"] ** 0.5
        # état
        self.games = 0
        self.best = 0
        self.heads = 0
        self.level = 1
        self.xp = 0
        self.coins = 0
        self.shop_owned = 0
        self.ads_removed = False
        self.spent = 0.0
        self.purchases = []
        self.inter = 0
        self.rewarded = 0
        self.inter_since = 99
        self.last_inter_t = -1e9
        self.last_rew_t = -1e9
        self.clock = 0.0
        self.unlocked_score = set()
        self.unlocked_level = set()
        self.special = set()
        self.days_active = []
        self.minutes = 0.0
        self.sessions = 0
        self.gift_day = -1
        self.gift_streak = 0
        self.starter_seen = False
        self.legends = False
        self.inter_seen_total = 0
        self.dry_sessions = 0
        self.seen = {}
        # Pourquoi l'envie baisse (pour comprendre les départs) : pertes d'envie par cause, jour par jour.
        self.day_blame = {}
        self.history = []
        self.chest_games = 0
        self.chests = 0
        self.planks = 0
        self.shields = 0
        rare = design.get("rare")
        self.shop_prices = RARE_SHOP if rare else SHOP_PRICES
        self.surprise_price = 1000 if rare else SURPRISE_PRICE
        self.chest_head = 0.08 if rare else 0.16
        self.spent_coins = 0

    # --- une partie
    def special_kind(self):
        """Core/Engagement.cs SpecialRun.Next."""
        if not self.d.get("specials"):
            return None
        n = self.games + 1
        if n <= 4 or n % 4:
            return None
        return ("coins", "xp", "chest")[(n // 4) % 3]

    def play_game(self, day):
        special = self.special_kind()
        goal = max(300, round(0.6 * self.best / 50) * 50) if self.d.get("goal") else 0
        g = self.rng.choice(self.pool)
        learn = 0.6 + 0.4 * min(1.0, self.games / 40)
        if self.d["tutorial"] and self.games < 3:
            learn += 0.1  # le tutoriel évite les premières parties ratées sans comprendre
        mult = self.talent * learn * self.rng.uniform(0.85, 1.15)
        score = int(g["score"] * mult)
        seconds = g["seconds"] * math.sqrt(max(0.3, mult))
        ev = {"best": False, "head": 0, "level": 0, "seconds": seconds, "score": score, "coins": 0, "goal": False, "chest": False, "special": special}

        # Seconde chance : pub récompensée (ou gratuite sans pub) en fin de partie.
        revive_p = 0.16 + (0.3 if score >= 0.7 * max(1, self.best) else 0.0) - 0.08 * self.aversion
        if self.ads_removed:
            revive_p += 0.25
        if self.rng.random() < max(0.02, revive_p):
            if not self.ads_removed:
                self.watch_rewarded()
            extra = self.rng.choice(self.pool)
            score += int(extra["score"] * mult * 0.35)
            seconds += extra["seconds"] * 0.35
            ev["score"], ev["seconds"] = score, seconds

        self.games += 1
        self.clock += seconds + 8
        self.minutes += (seconds + 8) / 60
        if score > self.best:
            ev["best"] = self.best > 0
            self.best = score
        for t in self.d["score_heads"]:
            if score >= t and t not in self.unlocked_score:
                self.unlocked_score.add(t)
                ev["head"] += 1
        for key, ok in (("balloon", g["balloons"] > 0), ("combo", g["bestChain"] >= 4), ("save", g["bestSave"] >= 0.95)):
            if ok and key not in self.special:
                self.special.add(key)
                ev["head"] += 1

        if goal and score >= goal:
            ev["goal"] = True
            if self.d["coins"]:
                self.coins += 25
        if self.d.get("chest"):
            self.chest_games += 2 if special == "chest" else 1
            if self.chest_games >= 3:
                self.chest_games = 0
                self.chests += 1
                ev["chest"] = True
                self.coins += 60 + 10 * int(self.rng.random() * 15)
                if ((self.chests == 2 and not self.d.get("rare")) or self.rng.random() < self.chest_head) and self.shop_owned < len(self.shop_prices):
                    self.shop_owned += 1
                    ev["head"] += 1
        if self.d["levels"]:
            self.xp += game_xp(score) * (2 if special == "xp" else 1)
            while self.xp >= xp_for_level(self.level):
                self.xp -= xp_for_level(self.level)
                self.level += 1
                ev["level"] += 1
                if self.d["coins"]:
                    self.coins += 50 + 10 * self.level
                if self.level in self.d["level_heads"]:
                    self.unlocked_level.add(self.level)
                    ev["head"] += 1
        if self.d["coins"]:
            c = game_coins(score) * (2 if special == "coins" else 1)
            ev["coins"] = c
            self.coins += c
            # x2 en vidéo : surtout quand on économise pour une tête.
            if "double" in self.d["rewarded"] and c >= 20:
                p = 0.14 + (0.18 if self.next_shop_price() and self.coins < self.next_shop_price() else 0) - 0.06 * self.aversion
                if self.rng.random() < max(0.02, p) and self.watch_rewarded():
                    self.coins += c
        self.heads += ev["head"]
        return ev

    def blame(self, reason, amount):
        self.day_blame[reason] = self.day_blame.get(reason, 0.0) + amount

    def reward(self, kind, amount):
        """Effet d'une récompense sur l'envie de jouer. Il s'use : la 20e mission fait moins d'effet que la première."""
        n = self.seen.get(kind, 0)
        self.seen[kind] = n + 1
        self.E = min(0.95, self.E + amount * HYP["reward_scale"] / (1 + n / HYP["habituation"]))

    def next_shop_price(self):
        if not self.d["shop"]:
            return None
        if self.d.get("economy"):
            # Ce que l'on vise : la chose la moins chère pas encore à soi (tête, planche), ou la planche en or au bout.
            options = []
            if self.shop_owned < len(self.shop_prices):
                options.append(min(self.surprise_price, self.shop_prices[self.shop_owned]))
            if self.planks < len(PLANK_PRICES):
                options.append(PLANK_PRICES[self.planks])
            return min(options) if options else None
        if self.shop_owned >= len(self.shop_prices):
            return None
        return self.shop_prices[self.shop_owned]

    def spend_coins(self):
        """Économie V17 : surprise (moins chère que la tête suivante), planche, sinon tête achetée directement."""
        r = self.rng.random()
        heads_left = self.shop_owned < len(self.shop_prices)
        head_price = self.shop_prices[self.shop_owned] if heads_left else None
        plank_price = PLANK_PRICES[self.planks] if self.planks < len(PLANK_PRICES) else None
        if heads_left and self.coins >= self.surprise_price and head_price > self.surprise_price and r < 0.55:
            self.coins -= self.surprise_price
            self.spent_coins += self.surprise_price
            self.open_surprise()
            return True
        if plank_price and self.coins >= plank_price and (not head_price or plank_price <= head_price or r < 0.4):
            self.coins -= plank_price
            self.spent_coins += plank_price
            self.planks += 1
            self.reward("planche", 0.028)
            return True
        if head_price and self.coins >= head_price:
            self.coins -= head_price
            self.spent_coins += head_price
            self.shop_owned += 1
            self.heads += 1
            self.reward("achat_tete", 0.03)
            return True
        return False

    def open_surprise(self):
        """Machine à surprises V20 (Core/Cosmetics.cs SurpriseBox) : tête 55 %, planche 15 %, pièces 30 %
        (400 : 20 %, 1 200 : 8 %, jackpot 3 000 : 2 %). Sans doublon : s'il ne reste rien d'une famille, on retire."""
        x = self.rng.random()
        heads_left = self.shop_owned < len(self.shop_prices)
        planks_left = self.planks < len(PLANK_PRICES)
        if x < 0.55 and heads_left:
            self.shop_owned += 1
            self.heads += 1
            self.reward("surprise", 0.035)
        elif x < 0.70 and planks_left:
            self.planks += 1
            self.reward("surprise", 0.03)
        else:
            y = self.rng.random()
            self.coins += 3000 if y < 0.02 / 0.30 else 1200 if y < 0.10 / 0.30 else 400
            self.reward("surprise", 0.02)

    def watch_rewarded(self):
        if self.rng.random() > HYP["fill_rate"]:
            return False
        self.rewarded += 1
        self.last_rew_t = self.clock
        return True

    def maybe_interstitial(self, ev):
        pol = self.d["interstitial"]
        if pol is None or self.ads_removed:
            return False
        self.inter_since += 1
        if self.games < pol["first_after_games"] or self.inter_since < pol["every_games"]:
            return False
        if self.clock - self.last_inter_t < pol["min_seconds"] or self.clock - self.last_rew_t < pol["after_rewarded"]:
            return False
        if pol["skip_on_best"] and (ev["best"] or ev["head"]):
            return False
        if self.rng.random() > HYP["fill_rate"]:
            return False
        self.inter += 1
        self.inter_seen_total += 1
        self.inter_since = 0
        self.last_inter_t = self.clock
        self.clock += 15
        self.E = max(0.0, self.E - 0.011 * self.aversion)
        self.blame("pubs", 0.011 * self.aversion)
        return True

    def buy(self, product):
        self.spent += PRICES[product]
        self.purchases.append(product)

    # --- une session
    def session(self, day):
        self.sessions += 1
        gained_goal = False
        if self.d["gift"] and self.gift_day != day and self.games >= 1:
            gap = day - self.gift_day
            if self.d.get("economy") and self.gift_day >= 0 and 1 < gap <= self.shields + 1:
                self.shields -= gap - 1  # protège-série : la série continue
                gap = 1
            self.gift_streak = self.gift_streak + 1 if gap == 1 else 1
            if self.d.get("economy") and self.shields < SHIELD_MAX and self.coins >= SHIELD_PRICE + 200 and self.gift_streak >= 3 and self.rng.random() < 0.12:
                self.coins -= SHIELD_PRICE
                self.spent_coins += SHIELD_PRICE
                self.shields += 1
            self.gift_day = day
            # Cadeau du jour V20 (bascule_tuning.json : dailyGift et dailyGiftItems) : pièces, coffre, surprise
            # gratuite, planche au jour 7 (sinon 600 pièces).
            k = (self.gift_streak - 1) % 7
            gift = [50, 0, 120, 0, 200, 0, 300][k]
            if k in (1, 5):
                gift += 60 + 10 * int(self.rng.random() * 15)
                if self.rng.random() < 0.08 and self.shop_owned < len(self.shop_prices):
                    self.shop_owned += 1
                    self.heads += 1
            elif k == 3 and self.d.get("economy"):
                self.open_surprise()
            elif k == 6:
                if self.d.get("economy") and self.planks < len(PLANK_PRICES):
                    self.planks += 1
                else:
                    gift += 600
            self.coins += gift
            if "gift" in self.d["rewarded"] and self.rng.random() < 0.32 - 0.08 * self.aversion and self.watch_rewarded():
                self.coins += gift
            self.reward("cadeau", 0.01)
        if "free_coins" in self.d["rewarded"] and self.rng.random() < 0.12 and self.watch_rewarded():
            self.coins += 60
        missions = self.d["missions"]
        mission_left = 3 if missions and self.days_active[-1:] != [day] else 0
        if "reroll" in self.d["rewarded"] and mission_left and self.rng.random() < 0.08:
            self.watch_rewarded()

        n = 0
        while True:
            ev = self.play_game(day)
            n += 1
            # Partie ratée loin de son record : petite frustration.
            if self.best > 0 and not ev["best"] and ev["score"] < 0.5 * self.best:
                self.E = max(0.0, self.E - 0.006)
                self.blame("parties_ratees", 0.006)
            if ev["seconds"] < 40:
                self.blame("parties_trop_courtes", 0.004)
                self.E = max(0.0, self.E - 0.004)
            if ev["best"]:
                self.reward("record", 0.025)
            if ev["head"]:
                self.reward("tete", 0.035 * min(2, ev["head"]))
                gained_goal = True
            if ev["level"]:
                self.reward("niveau", 0.012)
                gained_goal = True
            if ev["goal"]:
                self.reward("objectif", 0.006)
            if ev["chest"]:
                self.reward("coffre", 0.02)
                gained_goal = True
            if ev["special"]:
                self.reward("speciale", 0.012)
            if mission_left and self.rng.random() < 0.38:
                mission_left -= 1
                self.coins += 60
                self.xp += 40
                self.reward("mission", 0.022 if mission_left == 0 else 0.008)
                gained_goal = True
            # Boutique : on achète la tête suivante dès qu'on peut.
            if self.d.get("economy"):
                if self.spend_coins():
                    gained_goal = True
            else:
                price = self.next_shop_price()
                if price and self.coins >= price:
                    self.coins -= price
                    self.shop_owned += 1
                    self.heads += 1
                    self.reward("achat_tete", 0.03)
                    gained_goal = True
            self.iap_moment(day, ev)
            inter = self.maybe_interstitial(ev)

            # Rejouer ?
            momentum = 0.0
            if ev["best"]:
                momentum += 0.12
            nxt = self.next_goal_gap()
            if nxt is not None and nxt < 0.2:
                momentum += 0.1
            if ev["seconds"] < 40:
                momentum -= 0.05
            if inter:
                momentum -= 0.07 * self.aversion
            if mission_left:
                momentum += 0.04
            # « Encore une » : le coffre presque plein, ou la prochaine partie est spéciale.
            if self.d.get("chest") and self.chest_games == 2:
                momentum += 0.05
            if self.special_kind():
                momentum += 0.05
            p = min(0.9, max(0.05, 0.3 + 0.55 * self.E + momentum - 0.03 * n))
            if n >= 15 or self.rng.random() > p:
                break
        # Ennui : rien gagné et plus rien d'atteignable.
        if not gained_goal:
            self.dry_sessions += 1
            self.E = max(0.0, self.E - 0.02)
            self.blame("rien_de_neuf", 0.02)
            if self.no_reachable_goal():
                self.E = max(0.0, self.E - 0.03)
                self.blame("objectif_trop_loin", 0.03)

    def next_goal_gap(self):
        """Écart relatif entre le record et la prochaine tête de score."""
        for t in self.d["score_heads"]:
            if t not in self.unlocked_score:
                return (t - self.best) / t if self.best else 1.0
        return None

    def no_reachable_goal(self):
        if self.d["levels"] or self.d["shop"]:
            return False  # les niveaux et la boutique progressent toujours avec le temps de jeu
        gap = self.next_goal_gap()
        p90 = sorted(x["score"] for x in self.pool)[int(len(self.pool) * 0.95)] * self.talent
        nxt = next((t for t in self.d["score_heads"] if t not in self.unlocked_score), None)
        return nxt is None or (gap is not None and nxt > p90 * 1.2)

    def iap_moment(self, day, ev):
        if not self.payer:
            return
        iap = self.d["iap"]
        r = self.rng.random
        if "starter" in iap and not self.starter_seen and self.games >= 8 and not self.ads_removed:
            self.starter_seen = True
            if r() < 0.22 * min(2, self.wtp / 3):
                self.buy("starter")
                self.ads_removed = True
                self.coins += 2000
                self.heads += 1
                return
        if "removeads" in iap and not self.ads_removed:
            pressure = self.inter_seen_total / 6 if self.d["interstitial"] else 0.05
            if r() < 0.004 * min(3, pressure) * min(2, self.wtp / 3):
                self.buy("removeads")
                self.ads_removed = True
        if "coins_m" in iap:
            price = self.next_shop_price()
            if price and self.coins < price and price - self.coins < 2200 and r() < 0.0035 * min(2, self.wtp / 3):
                self.buy("coins_m" if self.wtp > 2.5 else "coins_s")
                self.coins += 2200 if self.wtp > 2.5 else 600
        if "legends" in iap and not self.legends and self.level >= 8 and r() < 0.0015 * min(2, self.wtp / 3):
            self.legends = True
            self.buy("legends")
            self.heads += 5

    # --- une journée
    def live_day(self, day):
        if day > 0:
            p = self.E + self.core_fun
            if self.notif:
                p += HYP["notif_bonus"]
            if self.d["gift"]:
                p += HYP["gift_bonus"]
            if self.d["missions"]:
                p += HYP["missions_bonus"]
            if self.rng.random() > p:
                self.blame("habitude_perdue", self.E * (1 - HYP["missed_day_decay"]))
                self.E *= HYP["missed_day_decay"]
                return
        self.days_active.append(day)
        self.day_blame = {}
        s = 0
        while True:
            self.session(day)
            s += 1
            if s >= self.max_sessions or self.rng.random() > self.E * 0.6:
                break
        self.blame("lassitude", self.E * (1 - HYP["decay_per_day"]))
        self.E *= HYP["decay_per_day"]
        self.history.append((day, self.day_blame))
        self.day_blame = {}


# ---------------------------------------------------------------------------------------------- simulation
def simulate(design_name, n_players, days, seed, games_by_skill, skills, hyp=None, design=None):
    saved = dict(HYP)
    if hyp:
        HYP.update(hyp)
    rng = random.Random(seed)
    d = design or DESIGNS[design_name]
    players = [Player(random.Random(rng.random()), d, games_by_skill, skills) for _ in range(n_players)]
    for p in players:
        for day in range(days):
            p.live_day(day)
    res = summarize(players, days)
    HYP.clear()
    HYP.update(saved)
    return res


def summarize(players, days):
    n = len(players)

    def ret(day):
        return sum(1 for p in players if day in p.days_active) / n

    dau = sum(len(p.days_active) for p in players)
    ad_rev = sum(p.inter for p in players) * HYP["ecpm_interstitial"] / 1000 + sum(p.rewarded for p in players) * HYP["ecpm_rewarded"] / 1000
    iap_rev = sum(p.spent for p in players) * HYP["iap_net"]
    minutes = [p.minutes for p in players]
    sessions = sum(p.sessions for p in players)
    payers = sum(1 for p in players if p.spent > 0)
    buys = {}
    for p in players:
        for b in p.purchases:
            buys[b] = buys.get(b, 0) + 1
    first_session_games = []
    return {
        "joueurs": n,
        "J1": ret(1), "J3": ret(3), "J7": ret(7), "J14": ret(14), "J30": ret(min(30, days - 1)),
        "courbe": [ret(d) for d in range(days)],
        "minutes_par_joueur": st.mean(minutes),
        "minutes_mediane": st.median(minutes),
        "minutes_par_jour_actif": sum(minutes) / max(1, dau),
        "sessions_par_jour_actif": sessions / max(1, dau),
        "minutes_par_session": sum(minutes) / max(1, sessions),
        "parties_par_session": sum(p.games for p in players) / max(1, sessions),
        "tetes_mediane": st.median(p.heads for p in players),
        "tetes_joueurs_30j": st.median([p.heads for p in players if (days - 1) in p.days_active or 29 in p.days_active] or [0]),
        "seances_sans_progres": sum(p.dry_sessions for p in players) / max(1, sessions),
        "inter_par_jour_actif": sum(p.inter for p in players) / max(1, dau),
        "recomp_par_jour_actif": sum(p.rewarded for p in players) / max(1, dau),
        "ARPDAU_pub": ad_rev / max(1, dau),
        "ARPDAU": (ad_rev + iap_rev) / max(1, dau),
        "revenu_pub": ad_rev, "revenu_achats": iap_rev,
        "LTV": (ad_rev + iap_rev) / n,
        "payeurs": payers / n,
        "achats": buys,
        "record_median": st.median(p.best for p in players),
    }


LABELS = {
    "lassitude": ("Toujours la même chose", "« Au bout d'un moment, chaque partie se ressemble. »"),
    "rien_de_neuf": ("Plus rien à gagner en jouant", "« J'ai joué, je n'ai rien débloqué, rien gagné de nouveau. »"),
    "objectif_trop_loin": ("Objectifs hors de portée", "« La prochaine tête demande un score que je ne ferai jamais. »"),
    "parties_ratees": ("Trop de parties ratées", "« Je n'arrive plus à approcher mon record, ça me décourage. »"),
    "parties_trop_courtes": ("Défaites trop rapides", "« Je perds au bout de 30 secondes sans comprendre. »"),
    "pubs": ("Trop de publicités", "« Encore une pub… »"),
    "habitude_perdue": ("Oublié l'appli", "« J'ai sauté un jour, puis j'ai oublié le jeu. »"),
}


def churn_report(players, days):
    """Raison principale du départ de chaque joueur parti (inactif sur la dernière semaine)."""
    reasons = {}
    churned = 0
    for p in players:
        if not p.days_active or p.days_active[-1] >= days - 7:
            continue
        churned += 1
        merged = {}
        for _, b in p.history[-3:]:
            for k, v in b.items():
                merged[k] = merged.get(k, 0) + v
        # L'oubli après le dernier jour compte aussi.
        if merged:
            top = max(merged, key=merged.get)
            reasons[top] = reasons.get(top, 0) + 1
    return churned, reasons


def load_library():
    path = os.path.join(OUT, "parties.json")
    if not os.path.exists(path):
        sys.exit("Lancer d'abord : python3 Tools/game_library.py 80")
    games = json.load(open(path, encoding="utf-8"))["games"]
    by = {}
    for g in games:
        by.setdefault(g["skill"], []).append(g)
    return by, sorted(by)


def pct(x):
    return f"{100 * x:.1f} %"


def main():
    n = int(sys.argv[1]) if len(sys.argv) > 1 else 1000
    days = int(sys.argv[2]) if len(sys.argv) > 2 else 60
    by, skills = load_library()
    if len(sys.argv) > 3 and sys.argv[3] == "economie":
        # Économie V17 contre V10 : mêmes joueurs, mêmes parties.
        keys = ("J1", "J7", "J30", "minutes_par_joueur", "seances_sans_progres", "payeurs", "ARPDAU", "LTV")
        res = {}
        for name in ("v10", "v17", "v18"):
            runs = [simulate(name, n, days, 7 + k, by, skills) for k in range(3)]
            res[name] = {k: st.mean(r[k] for r in runs) for k in keys}
            res[name]["achats"] = {k: sum(r["achats"].get(k, 0) for r in runs) / len(runs) for k in set().union(*[r["achats"] for r in runs])}
        for k in keys:
            print(f"{k:24}" + "".join(f"{res[d][k]:>14.4f}" for d in res))
        print("Achats :", {d: {k: round(v, 1) for k, v in res[d]["achats"].items()} for d in res})
        os.makedirs(OUT, exist_ok=True)
        with open(os.path.join(OUT, "economie.json"), "w", encoding="utf-8") as f:
            json.dump({"joueurs": n, "jours": days, "versions": res}, f, ensure_ascii=False, indent=1)
        return
    if len(sys.argv) > 3 and sys.argv[3] == "departs":
        # Les 1000 joueurs de la version actuelle : pourquoi ceux qui partent sont partis.
        out = {}
        for name in ("apres", "v10"):
            rng = random.Random(11)
            players = [Player(random.Random(rng.random()), DESIGNS[name], by, skills) for _ in range(n)]
            for p in players:
                for day in range(days):
                    p.live_day(day)
            churned, reasons = churn_report(players, days)
            print(f"[{name}] {churned} joueurs sur {n} ont arrêté de jouer avant la fin des {days} jours.")
            for k, v in sorted(reasons.items(), key=lambda kv: -kv[1]):
                label, quote = LABELS.get(k, (k, ""))
                print(f"  {100 * v / max(1, churned):4.0f} %  {label:34} {quote}")
            print()
            out[name] = {"partis": churned, "raisons": reasons}
        os.makedirs(OUT, exist_ok=True)
        with open(os.path.join(OUT, "departs.json"), "w", encoding="utf-8") as f:
            json.dump({"joueurs": n, "jours": days, "versions": out}, f, ensure_ascii=False, indent=1)
        return
    results = {}
    for name in DESIGNS:
        runs = [simulate(name, n, days, 7 + k, by, skills) for k in range(3)]
        avg = {k: (st.mean(r[k] for r in runs) if isinstance(runs[0][k], (int, float)) else runs[0][k]) for k in runs[0]}
        avg["courbe"] = [st.mean(r["courbe"][i] for r in runs) for i in range(days)]
        avg["achats"] = {k: sum(r["achats"].get(k, 0) for r in runs) / len(runs) for k in set().union(*[r["achats"] for r in runs])}
        results[name] = avg

    # Pression publicitaire : plusieurs règles d'interstitiel, toutes choses égales par ailleurs.
    pressure = {}
    base = DESIGNS["apres"]
    for label, every, secs, first in (("aucun", None, 0, 0), ("toutes les 2 parties, 90 s", 2, 90, 3), ("toutes les 3 parties, 180 s", 3, 180, 6), ("toutes les 5 parties, 300 s", 5, 300, 8), ("après chaque partie", 1, 0, 1)):
        d = dict(base)
        d["interstitial"] = None if every is None else {"first_after_games": first, "every_games": every, "min_seconds": secs, "after_rewarded": 120 if every else 0, "skip_on_best": every is not None and every > 1}
        r = [simulate("apres", n, days, 70 + k, by, skills, design=d) for k in range(2)]
        pressure[label] = {k: st.mean(x[k] for x in r) for k in ("J7", "J30", "minutes_par_joueur", "inter_par_jour_actif", "ARPDAU", "LTV")}

    # Sensibilité : et si les récompenses (missions, niveaux, cadeau…) motivaient deux fois moins que prévu ?
    weak = {}
    for name in DESIGNS:
        r = [simulate(name, n, days, 50 + k, by, skills, hyp={"reward_scale": 0.5, "gift_bonus": HYP["gift_bonus"] / 2, "missions_bonus": HYP["missions_bonus"] / 2, "notif_bonus": HYP["notif_bonus"] / 2}) for k in range(2)]
        weak[name] = {k: st.mean(x[k] for x in r) for k in ("J1", "J7", "J30", "minutes_par_joueur", "LTV")}

    # Sensibilité des revenus aux prix des pubs.
    sens = {}
    for label, h in (("pubs bon marché", {"ecpm_interstitial": 2.5, "ecpm_rewarded": 5}), ("hypothèse", {}), ("pubs chères", {"ecpm_interstitial": 7, "ecpm_rewarded": 14})):
        r = simulate("apres", n, days, 99, by, skills, hyp=h)
        sens[label] = {"ARPDAU": r["ARPDAU"], "LTV": r["LTV"]}

    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "joueurs.json"), "w", encoding="utf-8") as f:
        json.dump({"hypotheses": HYP, "resultats": results, "pression_pub": pressure, "sensibilite": sens, "recompenses_faibles": weak, "joueurs": n, "jours": days}, f, ensure_ascii=False, indent=1)

    keys = [("J1", pct), ("J7", pct), ("J30", pct), ("minutes_par_joueur", lambda v: f"{v:.0f} min"), ("minutes_par_jour_actif", lambda v: f"{v:.1f} min"),
            ("minutes_par_session", lambda v: f"{v:.1f} min"), ("parties_par_session", lambda v: f"{v:.1f}"), ("tetes_mediane", lambda v: f"{v:.0f}"),
            ("seances_sans_progres", pct), ("inter_par_jour_actif", lambda v: f"{v:.2f}"), ("recomp_par_jour_actif", lambda v: f"{v:.2f}"),
            ("ARPDAU", lambda v: f"{v:.3f} €"), ("LTV", lambda v: f"{v:.3f} €"), ("payeurs", pct), ("record_median", lambda v: f"{v:.0f}")]
    print(f"{n} joueurs, {days} jours (moyenne de 3 tirages)\n")
    print(f"{'':28}" + "".join(f"{k:>14}" for k in results))
    for k, fmt in keys:
        print(f"{k:28}" + "".join(f"{fmt(results[d][k]):>14}" for d in results))
    print("\nAchats (pour", n, "joueurs) :", {d: {k: round(v, 1) for k, v in results[d]["achats"].items()} for d in results})
    print("\nPression publicitaire :")
    for k, v in pressure.items():
        print(f"  {k:30} J7 {pct(v['J7'])}  J30 {pct(v['J30'])}  {v['minutes_par_joueur']:.0f} min/joueur  {v['inter_par_jour_actif']:.2f} inter/jour  LTV {v['LTV']:.3f} €")
    print("\nSensibilité :", {k: f"LTV {v['LTV']:.3f} €" for k, v in sens.items()})
    print("Récompenses deux fois moins efficaces :", {k: f"J7 {pct(v['J7'])} J30 {pct(v['J30'])} {v['minutes_par_joueur']:.0f} min" for k, v in weak.items()})


if __name__ == "__main__":
    main()
