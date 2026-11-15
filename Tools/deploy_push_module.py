"""
Construit et déploie le module Cloud Code « BasculePush » : la notification « Léo te défie en duel ! »
(docs/NOTIFICATIONS.md). À relancer après chaque modification de Server/BasculePush ou de Core/DuelPush.cs.

  python Tools/deploy_push_module.py          # tests, construction de Exports/BasculePush.ccm, puis déploiement
  python Tools/deploy_push_module.py --ccm    # tests et construction seulement (pour « ugs deploy Exports/BasculePush.ccm »)

Le déploiement utilise une clé de compte de service Unity (Unity Cloud > Administration > Comptes de service, rôle
« Cloud Code Editor » sur le projet), donnée par l'environnement, JAMAIS écrite dans le dépôt :
  PowerShell : $env:UNITY_KEY_ID="..."; $env:UNITY_KEY_SECRET="..."; python Tools/deploy_push_module.py
  Mac/Linux  : UNITY_KEY_ID=... UNITY_KEY_SECRET=... python3 Tools/deploy_push_module.py
Dans une session Claude Code dans le cloud, la clé peut être un « network secret » de l'environnement (hôte
services.api.unity.com, type Basic) : le proxy l'ajoute lui-même et aucune variable n'est nécessaire.
Il faut le SDK .NET 8 (ou plus récent) : https://dotnet.microsoft.com/download
"""
import base64
import json
import os
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.request
import uuid
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = "c644bd04-fe06-46bb-98bf-3a3296017e04"
ENV_ID = "90b9209d-d93b-47cc-8c79-94a50d5fa045"  # « production »
MODULE = "BasculePush"  # = Core/DuelPush.Module (le jeu appelle ce nom)
ADMIN = f"https://services.api.unity.com/cloud-code/v1/projects/{PROJECT}/environments/{ENV_ID}/modules"
OUT = os.path.join(ROOT, "Exports", MODULE)
CCM = os.path.join(ROOT, "Exports", MODULE + ".ccm")


def run(args):
    print("> " + " ".join(args))
    try:
        r = subprocess.run(args, cwd=ROOT)
    except FileNotFoundError:
        sys.exit(f"ÉCHEC : « {args[0]} » est introuvable. Installe le SDK .NET 8 (PowerShell : winget install "
                 "Microsoft.DotNet.SDK.8), ferme et rouvre PowerShell, puis relance ce script.")
    if r.returncode != 0:
        sys.exit("ÉCHEC : " + " ".join(args))


def check_key():
    """Avant de construire : une vraie clé de compte de service (pas le texte d'exemple de la doc)."""
    key_id, secret = os.environ.get("UNITY_KEY_ID", ""), os.environ.get("UNITY_KEY_SECRET", "")
    if not key_id and not secret and os.environ.get("HTTPS_PROXY"):
        return  # session Claude Code dans le cloud : la clé est un « network secret » ajouté par le proxy
    looks_like_id = len(key_id) == 36 and key_id.count("-") == 4
    if not looks_like_id or len(secret) < 16 or "COMPTE" in key_id.upper() or "COMPTE" in secret.upper():
        sys.exit("ÉCHEC : clé du compte de service absente ou pas remplacée. Dans PowerShell, mets entre les guillemets "
                 "le Key ID (de la forme xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx) et la Secret key affichés par Unity Cloud "
                 "à la création de la clé :\n  $env:UNITY_KEY_ID=\"...\"; $env:UNITY_KEY_SECRET=\"...\"")


def build():
    # Rien ne part si un test échoue (le module touche aux données des joueurs).
    run(["dotnet", "test", os.path.join("Server", "BasculePush.Tests"), "-c", "Release"])
    publish = os.path.join(OUT, "publish")
    shutil.rmtree(OUT, ignore_errors=True)
    run(["dotnet", "publish", os.path.join("Server", "BasculePush", "BasculePush.csproj"), "-c", "Release",
         "-r", "linux-x64", "--self-contained", "false", "-o", publish])
    files = sorted(f for f in os.listdir(publish) if os.path.isfile(os.path.join(publish, f)))
    if MODULE + ".dll" not in files:
        sys.exit("ÉCHEC : " + MODULE + ".dll absent de la publication")
    os.makedirs(os.path.dirname(CCM), exist_ok=True)
    # Un .ccm est un zip avec les fichiers de la publication à la racine (doc Cloud Code, « Package code »).
    with zipfile.ZipFile(CCM, "w", zipfile.ZIP_DEFLATED) as z:
        for f in files:
            z.write(os.path.join(publish, f), f)
    print(f"Module construit : {CCM} ({os.path.getsize(CCM) // 1024} Ko, {len(files)} fichiers)")


def basic_auth():
    key_id, secret = os.environ.get("UNITY_KEY_ID"), os.environ.get("UNITY_KEY_SECRET")
    # Sans variables : valeur factice, remplacée par le proxy qui détient la vraie clé (network secret).
    return base64.b64encode(f"{key_id}:{secret}".encode() if key_id and secret else b"proxy:proxy").decode()


def request(url, method, auth, fields=None):
    body, headers = None, {"Authorization": "Basic " + auth}
    if fields is not None:
        boundary = uuid.uuid4().hex
        parts = []
        for name, value in fields:
            if isinstance(value, tuple):  # fichier : (nom, octets)
                parts.append((f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"; filename="{value[0]}"\r\n'
                              f'Content-Type: application/zip\r\n\r\n').encode() + value[1] + b"\r\n")
            else:
                parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n{value}\r\n'.encode())
        body = b"".join(parts) + f"--{boundary}--\r\n".encode()
        headers["Content-Type"] = "multipart/form-data; boundary=" + boundary
    req = urllib.request.Request(url, data=body, method=method, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            return r.status, r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


def deploy():
    auth = basic_auth()
    data = open(CCM, "rb").read()
    status, text = request(f"{ADMIN}/{MODULE}", "GET", auth)
    if status == 200:
        status, text = request(f"{ADMIN}/{MODULE}", "PATCH", auth, [("file", (MODULE + ".ccm", data))])
        what = "mis à jour"
    elif status == 404:
        status, text = request(ADMIN, "POST", auth, [("name", MODULE), ("language", "CS"), ("file", (MODULE + ".ccm", data))])
        what = "créé"
    else:
        what = None
    if what is None or status >= 300:
        hint = {401: "clé de service absente ou fausse (UNITY_KEY_ID / UNITY_KEY_SECRET)",
                403: "la clé n'a pas le rôle « Cloud Code Editor » sur le projet, ou Cloud Code n'est pas activé dans le tableau de bord"}
        sys.exit(f"ÉCHEC du déploiement ({status}) : {hint.get(status, text[:300])}")
    print(f"Module {MODULE} {what} dans l'environnement production.")
    self_test()
    print("Ensuite (docs/NOTIFICATIONS.md, étapes 6 et 7) : interrupteur dans Unity, build, puis deux iPhone amis.")


def json_call(url, method="POST", body=None, headers=None):
    req = urllib.request.Request(url, method=method, data=json.dumps(body).encode() if body is not None else None)
    for k, v in (headers or {}).items():
        req.add_header(k, v)
    if body is not None:
        req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            raw = r.read()
            return r.status, json.loads(raw) if raw else {}
    except urllib.error.HTTPError as e:
        try:
            return e.code, json.loads(e.read() or b"{}")
        except Exception:
            return e.code, {}


def self_test():
    """Comme un joueur (compte anonyme temporaire, supprimé ensuite) : le module joint-il Apple avec la bonne clé ?"""
    st, auth = json_call("https://player-auth.services.api.unity.com/v1/authentication/anonymous", body={},
                         headers={"ProjectId": PROJECT, "UnityEnvironment": "production"})
    if st != 200:
        print(f"Auto-test impossible (connexion anonyme : HTTP {st}). Le relancer avec Tools/verif_publication.py.")
        return
    h = {"Authorization": "Bearer " + auth["idToken"], "ProjectId": PROJECT, "UnityEnvironment": "production"}
    try:
        # Le premier appel d'un module tout neuf peut être lent : quelques essais.
        for attempt in range(4):
            st, d = json_call(f"https://cloud-code.services.api.unity.com/v1/projects/{PROJECT}/modules/{MODULE}/DuelPushCheck",
                              body={"params": {}}, headers=h)
            if st == 200:
                break
            time.sleep(5)
        out = d.get("output", "") if st == 200 else f"HTTP {st} {str(d)[:200]}"
        meaning = {
            "ok": "OK : clé Apple, équipe, app et réseau bons.",
            "wait": "déjà testé il y a moins de 5 minutes : relancer Tools/verif_publication.py plus tard.",
            "nokey": "ÉCHEC : secrets APNS_KEY / APNS_KEY_ID / APNS_TEAM_ID absents, non autorisés pour Cloud Code, ou clé .p8 incomplète (étape 4).",
        }
        print("Auto-test de la liaison avec Apple : " + out + " → " + meaning.get(out, "ÉCHEC : voir le tableau Diagnostic de docs/NOTIFICATIONS.md."))
    finally:
        json_call(f"https://player-auth.services.api.unity.com/v1/users/{auth['userId']}", "DELETE", headers=h)


if __name__ == "__main__":
    if "--ccm" not in sys.argv[1:]:
        check_key()
    build()
    if "--ccm" in sys.argv[1:]:
        print(f"Pour déployer avec l'outil Unity : ugs deploy {os.path.relpath(CCM, ROOT)}")
    else:
        deploy()
