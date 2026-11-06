"""
Bibliothèque de parties simulées pour le modèle de joueurs (Tools/player_sim.py).

Fait jouer des parties complètes avec la vraie physique approchée (balance_sim.py, mêmes réglages que le jeu)
à plusieurs niveaux de compétence, et enregistre leur résultat dans Tools/sim_out/parties.json.

Usage : python3 Tools/game_library.py [parties_par_niveau]
"""
import json
import os
import sys
from multiprocessing import Pool

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import balance_sim as B  # noqa: E402

SKILLS = (0.1, 0.3, 0.5, 0.7, 0.9)
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "sim_out")


def play(args):
    skill, seed = args
    g = B.Game(seed, "joueur", skill).run(max_drops=600)
    return {
        "skill": skill, "seed": seed, "drops": g.drops, "seconds": round(g.time, 1), "score": g.score,
        "balloons": g.balloons, "bestChain": g.best_chain, "bestSave": round(g.best_save, 3),
        "lost": g.lost_weights, "cause": g.cause if g.over else "limite", "merges": g.merge_count,
        "maxValue": g.max_value, "made": {str(k): v for k, v in g.made.items()},
    }


def main():
    n = int(sys.argv[1]) if len(sys.argv) > 1 else 60
    jobs = [(k, 10000 + int(k * 1000) * 100 + i) for k in SKILLS for i in range(n)]
    with Pool(os.cpu_count()) as pool:
        games = pool.map(play, jobs, chunksize=4)
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "parties.json"), "w", encoding="utf-8") as f:
        json.dump({"tuning": B.T, "games": games}, f)
    print(f"{len(games)} parties -> {os.path.join(OUT, 'parties.json')}")


if __name__ == "__main__":
    main()
