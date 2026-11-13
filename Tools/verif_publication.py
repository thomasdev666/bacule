"""
Vérification obligatoire AVANT chaque envoi à Apple (docs/AVANT_PUBLICATION.md). Elle compare le code, l'export iOS
qui va partir (dépôt bacule-ios) et les serveurs Unity, pour attraper ce qui a déjà cassé une version publiée :
- 1.4 : classements renommés par rapport à la version en ligne (les records des joueurs avaient disparu) ;
- 1.4 build 21 : régies en bibliothèque dynamique non embarquées (plantage au lancement) ;
- 1.4 : numéro de build déjà utilisé (envoi refusé par Apple) ;
- 1.4 : duel qui ne démarre pas (Wi-Fi + 4G, retour de Snap, connexion lente) : 179 scénarios simulés
  (Tests/Bascule.Duel.Tests, le vrai DuelController sur un faux réseau).

  python3 Tools/verif_publication.py ../bacule-ios          # tout, tests et compilation compris (~5 min)
  python3 Tools/verif_publication.py ../bacule-ios --rapide # sans les tests ni la compilation

Le script crée un joueur anonyme temporaire pour lire les serveurs, puis le supprime.
"""
import json
import os
import plistlib
import re
import subprocess
import sys
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IOS = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 and not sys.argv[1].startswith("-") else os.path.join(os.path.dirname(ROOT), "bacule-ios")
RAPIDE = "--rapide" in sys.argv
PROJECT = "c644bd04-fe06-46bb-98bf-3a3296017e04"
ENV = "production"
# Régies dont le SDK est une bibliothèque DYNAMIQUE (vérifié sur les binaires le 13/11/2026) : elles doivent être dans
# la cible Unity-iPhone du Podfile (Editor/IosPodfileFix.cs), sinon iOS ferme le jeu au lancement.
DYNAMIC = {"IronSourceInMobiAdapter": "InMobiSDK", "IronSourceAppLovinAdapter": "AppLovinSDK"}
# Clés Cloud Save qui doivent être indexées (recherche entre joueurs).
INDEXES = ["friend_code", "link1", "link2", "link3", "link4", "link5", "duel_to"]

results = []


def check(ok, label, detail=""):
    results.append((ok, label, detail))
    print(("  OK  " if ok else "  !!  ") + label + (" : " + detail if detail else ""), flush=True)
    return ok


def read(path):
    with open(path, encoding="utf-8", errors="replace") as f:
        return f.read()


def git(*args, cwd=IOS):
    return subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True).stdout


def http(url, method="GET", body=None, headers=None):
    req = urllib.request.Request(url, method=method, data=json.dumps(body).encode() if body is not None else None)
    for k, v in (headers or {}).items():
        req.add_header(k, v)
    if body is not None:
        req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, timeout=30) as r:
            raw = r.read()
            return r.status, json.loads(raw) if raw else {}
    except urllib.error.HTTPError as e:
        try:
            return e.code, json.loads(e.read() or b"{}")
        except Exception:
            return e.code, {}


def boards_in(text):
    return sorted(set(re.findall(r"bascule_(?:monde|jour)(?:_s\d+)?", text)))


print("\n== Code et export iOS ==")
setup = read(os.path.join(ROOT, "Assets/Bascule/Scripts/Editor/BasculeSetup.cs"))
version = re.search(r'Version = "([^"]+)"', setup).group(1)
lb = read(os.path.join(ROOT, "Assets/Bascule/Scripts/Runtime/Services/LeaderboardService.cs"))
code_boards = sorted(re.findall(r'public const string (?:World|Daily) = "([^"]+)"', lb))
check(len(code_boards) == 2, "Classements du code", ", ".join(code_boards))

if not os.path.isdir(os.path.join(IOS, ".git")):
    check(False, "Export iOS introuvable", IOS + " (cloner thomasdev666/bacule-ios)")
else:
    git("fetch", "-q", "--depth=50", "origin", "main")
    head = git("rev-parse", "--short", "origin/main").strip()
    plist = plistlib.loads(subprocess.run(["git", "show", "origin/main:Info.plist"], cwd=IOS, capture_output=True).stdout)
    check(plist.get("CFBundleShortVersionString") == version, "Version de l'export = version du code",
          f"export {plist.get('CFBundleShortVersionString')} (commit {head}), code {version}")
    meta = subprocess.run(["git", "show", "origin/main:Data/Managed/Metadata/global-metadata.dat"], cwd=IOS, capture_output=True).stdout.decode("latin-1")
    exp_boards = boards_in(meta)
    check(exp_boards == code_boards, "Classements dans l'export = ceux du code", ", ".join(exp_boards))
    # Classements de l'export précédent (la version probablement en ligne) : un changement efface les records affichés.
    prev = None
    for c in git("log", "--format=%h", "origin/main", "--", "Data/Managed/Metadata/global-metadata.dat").split()[1:]:
        b = boards_in(subprocess.run(["git", "show", f"{c}:Data/Managed/Metadata/global-metadata.dat"], cwd=IOS, capture_output=True).stdout.decode("latin-1"))
        if b:
            prev = (c, b)
            break
    if prev:
        check(prev[1] == exp_boards, "Mêmes classements que l'export précédent", f"{prev[0]} : {', '.join(prev[1])}"
              + ("" if prev[1] == exp_boards else " → les records des joueurs ne s'afficheront plus : copier les scores (Tools/copy_leaderboards.py) ou garder les anciens noms"))
    podfile = git("show", "origin/main:Podfile")
    main_target = podfile.split("target 'Unity-iPhone' do", 1)[1].split("end", 1)[0] if "target 'Unity-iPhone' do" in podfile else ""
    for adapter, sdk in DYNAMIC.items():
        if f"'{adapter}'" in podfile:
            check(f"pod '{sdk}'" in main_target, f"{sdk} (dynamique) embarqué dans l'app", "Podfile, cible Unity-iPhone")
    if "IronSourceAdMobAdapter" in podfile:
        check(bool(plist.get("GADApplicationIdentifier")), "Identifiant AdMob dans Info.plist", plist.get("GADApplicationIdentifier", "absent : plantage au lancement"))
    check(bool(plist.get("NSUserTrackingUsageDescription")), "Texte de la demande de suivi (ATT)")
    check(len(plist.get("SKAdNetworkItems", [])) > 50, "Identifiants SKAdNetwork", str(len(plist.get("SKAdNetworkItems", []))))
    cm = git("show", "origin/main:codemagic.yaml")
    check("100 + ${BUILD_NUMBER" in cm, "Numéro de build Codemagic unique", "100 + numéro du build")

print("\n== Serveurs Unity ==")
st, auth = http("https://player-auth.services.api.unity.com/v1/authentication/anonymous", "POST", {},
                {"ProjectId": PROJECT, "UnityEnvironment": ENV})
if check(st == 200, "Connexion anonyme (comme un joueur)", f"HTTP {st}"):
    tok, uid = auth["idToken"], auth["userId"]
    H = {"Authorization": "Bearer " + tok, "ProjectId": PROJECT, "UnityEnvironment": ENV}
    try:
        for b in code_boards:
            st, d = http(f"https://leaderboards.services.api.unity.com/v1/projects/{PROJECT}/leaderboards/{b}/scores?limit=1", headers=H)
            check(st == 200, f"Classement {b} existe", f"{d.get('total', '?')} joueurs" if st == 200 else f"HTTP {st}")
        for k in INDEXES:
            st, _ = http(f"https://cloud-save.services.api.unity.com/v1/data/projects/{PROJECT}/players/public/query", "POST",
                         {"fields": [{"key": k, "value": "zz", "op": "EQ", "asc": True}], "returnKeys": [k], "offset": 0, "limit": 1}, H)
            check(st == 200, f"Index Cloud Save « {k} »", "OK" if st == 200 else f"HTTP {st} : créer l'index (docs/INTEGRATIONS.md)")
    finally:
        http(f"https://player-auth.services.api.unity.com/v1/users/{uid}", "DELETE", headers=H)

print("\n== Réglages ==")
try:
    json.load(open(os.path.join(ROOT, "Assets/Bascule/Resources/bascule_tuning.json"), encoding="utf-8"))
    check(True, "bascule_tuning.json lisible")
except Exception as e:
    check(False, "bascule_tuning.json lisible", str(e))

if not RAPIDE:
    print("\n== Tests et compilation (quelques minutes) ==")
    r = subprocess.run(["dotnet", "test", "Tests/Bascule.Core.Tests"], cwd=ROOT, capture_output=True, text=True)
    m = re.search(r"(Passed!|Failed!).*", r.stdout)
    check(r.returncode == 0, "Tests des règles", m.group(0)[:120] if m else r.stdout[-200:])
    r = subprocess.run(["dotnet", "test", "Tests/Bascule.Duel.Tests"], cwd=ROOT, capture_output=True, text=True)
    m = re.search(r"(Passed!|Failed!).*", r.stdout)
    failed = re.findall(r"^\s+Failed (\S+)", r.stdout, re.M)
    check(r.returncode == 0, "Duel : scénarios simulés (Wi-Fi, 4G, Snap, pauses, abandons, revanche…)",
          (m.group(0)[:120] if m else r.stdout[-200:]) + (" ; en échec : " + ", ".join(failed[:6]) if failed else ""))
    for defs in ["", "BASCULE_STUBS;BASCULE_UNITYADS;BASCULE_IAP;BASCULE_NOTIFICATIONS;BASCULE_UGS;BASCULE_CLOUDSAVE;BASCULE_MULTIPLAYER;UNITY_IOS",
                 "BASCULE_STUBS;BASCULE_UNITYADS;BASCULE_IAP;BASCULE_NOTIFICATIONS;BASCULE_UGS;BASCULE_CLOUDSAVE;BASCULE_MULTIPLAYER;UNITY_ANDROID"]:
        r = subprocess.run(["dotnet", "build", "Tools/CompileCheck", "-p:ExtraDefines=" + defs.replace(";", "%3B")], cwd=ROOT, capture_output=True, text=True)
        check(r.returncode == 0 and " error " not in r.stdout, "Compilation " + (defs.split(";")[-1] if defs else "sans paquets"))

bad = [r for r in results if not r[0]]
print(f"\n{len(results) - len(bad)}/{len(results)} vérifications OK.")
if bad:
    print("À CORRIGER AVANT L'ENVOI À APPLE :")
    for _, label, detail in bad:
        print(" - " + label + (" : " + detail if detail else ""))
print("Ensuite : les tests sur iPhone de docs/AVANT_PUBLICATION.md (obligatoires, le script ne remplace pas deux vrais téléphones).")
sys.exit(1 if bad else 0)
