"""
Copie les records des classements de l'ancienne version (bascule_monde_s2, bascule_jour_s2) vers ceux de la 1.4
(bascule_monde, bascule_jour). À relancer tant que des joueurs ont encore l'ancienne version (elle écrit dans les _s2).
Ne supprime rien : le classement garde le meilleur score, et un joueur déjà mieux classé dans la 1.4 est laissé tel quel.

Clé de compte de service Unity Cloud (rôle « Leaderboards Admin ») dans l'environnement, jamais dans le dépôt :
  UNITY_KEY_ID=… UNITY_KEY_SECRET=… python3 Tools/copy_leaderboards.py
Dans une session Claude Code dans le cloud, la clé peut aussi être un « network secret » de l'environnement
(hôte services.api.unity.com, type Basic : identifiant = Key ID, mot de passe = Secret) : le proxy l'ajoute
lui-même à la demande de jeton et le script n'a besoin d'aucune variable.
"""
import base64
import json
import os
import urllib.request

PROJECT = "c644bd04-fe06-46bb-98bf-3a3296017e04"
ENV_ID = "90b9209d-d93b-47cc-8c79-94a50d5fa045"  # « production »
PAIRS = [("bascule_monde_s2", "bascule_monde"), ("bascule_jour_s2", "bascule_jour")]
LB = f"https://leaderboards.services.api.unity.com/v1/projects/{PROJECT}/leaderboards"


def call(url, method="GET", token=None, basic=None, body=None):
    req = urllib.request.Request(url, method=method, data=json.dumps(body).encode() if body is not None else None)
    if token:
        req.add_header("Authorization", "Bearer " + token)
    if basic:
        req.add_header("Authorization", "Basic " + basic)
    if body is not None:
        req.add_header("Content-Type", "application/json")
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read() or b"{}")


def read_all(board, token):
    """Tout le classement, par pages de 1000."""
    out, offset = [], 0
    while True:
        page = call(f"{LB}/{board}/scores?offset={offset}&limit=1000&includeMetadata=true", token=token)["results"]
        out += page
        if len(page) < 1000:
            return out
        offset += 1000


def main():
    key_id, secret = os.environ.get("UNITY_KEY_ID"), os.environ.get("UNITY_KEY_SECRET")
    # Sans variables : valeur factice, remplacée par le proxy qui détient la vraie clé (network secret).
    basic = base64.b64encode(f"{key_id}:{secret}".encode() if key_id and secret else b"proxy:proxy").decode()
    token = call(f"https://services.api.unity.com/auth/v1/token-exchange?projectId={PROJECT}&environmentId={ENV_ID}",
                 "POST", basic=basic)["accessToken"]
    for src, dst in PAIRS:
        have = {x["playerId"]: x["score"] for x in read_all(dst, token)}
        copied = []
        for x in read_all(src, token):
            if have.get(x["playerId"], -1) >= x["score"]:
                continue
            call(f"{LB}/{dst}/scores/players/{x['playerId']}", "POST", token=token,
                 body={"score": x["score"], "metadata": x.get("metadata") or {}})
            copied.append(x)
        print(f"{src} -> {dst} : {len(copied)} record(s) copié(s)")
        if copied:
            # Place obtenue dans le classement de la 1.4 (pseudos : pour l'éditeur seulement, ne pas publier).
            ranks = {x["playerId"]: x["rank"] + 1 for x in read_all(dst, token)}
            for x in sorted(copied, key=lambda e: -e["score"]):
                print(f"   {int(x['score']):>7}  {x.get('playerName', '?')}  -> place {ranks.get(x['playerId'], '?')}")


main()
