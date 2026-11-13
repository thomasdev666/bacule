# Avant chaque envoi à Apple : vérification obligatoire

Après la 1.4 (classement vide, plantage au lancement du build 21, duel qui coupe), plus aucune version ne part sans ces deux étapes. Compter 30 minutes.

## 1. Vérification automatique (Claude la lance, 5 minutes)

```
python3 Tools/verif_publication.py ../bacule-ios
```

Elle compare le code, l'export iOS qui va partir et les serveurs Unity :
- version de l'export = version du code ;
- **mêmes classements que la version précédente** (sinon les records des joueurs disparaissent) ;
- régies à bibliothèque dynamique embarquées (sinon plantage au lancement), identifiant AdMob, texte du suivi, SKAdNetwork ;
- numéro de build Codemagic unique ;
- classements et index Cloud Save présents sur le serveur ;
- tests des règles et compilation iOS / Android avec tous les paquets.

Tout doit être « OK ». Un « !! » se corrige avant d'aller plus loin.

## 2. Tests sur iPhone avec le build TestFlight (Thomas, 25 minutes)

Toujours sur le build qui sera soumis (même numéro), pas sur un autre.

**Mise à jour et lancement**
- [ ] Installer par-dessus la version de l'App Store : le jeu **s'ouvre**, record, têtes, pièces et niveau sont **conservés**.
- [ ] Classement → Monde : les **vrais joueurs** sont là, « Ta place » s'affiche sans rejouer.
- [ ] Une partie complète, puis le classement du jour.

**Duel (deux iPhone, les deux avec ce build)**
- [ ] Côte à côte, même Wi-Fi : « Défier un ami » → l'autre tape le code → départ en moins de 5 s → duel **jusqu'au résultat**.
- [ ] Revanche depuis l'écran de résultat.
- [ ] En **4G des deux côtés**.
- [ ] L'hôte envoie le code **par Snap ou Messages**, revient dans le jeu, l'ami rejoint par le lien.
- [ ] Invitation depuis la liste d'amis (l'ami la reçoit sur l'accueil).
- [ ] Un joueur quitte l'appli pendant le duel : l'autre gagne en quelques secondes.

**Amis**
- [ ] A ajoute B avec son code : B voit « A t'a ajouté en ami » et A apparaît dans ses amis, même sans score.

**Pubs et achats**
- [ ] Test Suite LevelPlay (7 + 7 touches sur la version) : toutes les régies sans point rouge.
- [ ] Une vidéo récompensée (coffre ou roue) donne bien la récompense.
- [ ] Restaurer mes achats.

Si une case ne passe pas : on ne soumet pas, on envoie une capture à Claude.

## 3. Après l'envoi

- [ ] Version publiée **manuellement** (pas automatiquement), le soir de la vidéo.
- [ ] Le lendemain : relancer la partie « Serveurs Unity » du script et regarder le classement dans le jeu.
