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
- tests des règles et compilation iOS / Android avec tous les paquets ;
- **179 duels simulés** (`Tests/Bascule.Duel.Tests`) : le vrai code du duel sur un faux réseau (Wi-Fi, 4G lente,
  Wi-Fi + 4G, relais lent, liaison qui n'aboutit jamais, hôte parti 4 à 40 s dans Snap, Centre de contrôle pendant la
  partie, appli quittée, téléphone éteint, abandon, attente du score, fin au temps, revanche, mauvais code puis bon
  code, 120 duels tirés au hasard). Les deux téléphones doivent toujours finir avec le même résultat.

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
- [ ] **Un en Wi-Fi, l'autre en 4G** (couper le Wi-Fi d'un téléphone).
- [ ] En **4G des deux côtés**.
- [ ] L'hôte envoie le code **par Snap ou Messages**, y reste 30 s, revient dans le jeu ; l'ami touche le lien : le duel part.
- [ ] Invitation depuis la liste d'amis (l'ami la reçoit sur l'accueil).
- [ ] Pendant la partie, tirer le Centre de contrôle 2 s puis revenir : **pas de défaite**, la partie continue.
- [ ] Un joueur quitte l'appli pendant le duel (plus de 10 s) : l'autre gagne, celui qui est parti voit « défaite ».

**Duel classé et ligue (1.5 ; deux iPhone, le classement `bascule_ligue` créé : docs/LIGUES.md)**
- [ ] Les deux touchent « Duel classé » à quelques secondes d'écart : ils se trouvent en moins de 10 s, sans code.
- [ ] Les deux touchent **en même temps** : ils se trouvent quand même (moins de 20 s).
- [ ] Fin du duel : « +30 points de ligue » chez le gagnant, « +8 » chez le perdant ; pas de bouton Revanche, mais « Nouveau duel classé ».
- [ ] Écran Duel → Classement : les deux joueurs y sont, avec leurs points et leur division.
- [ ] Un seul téléphone cherche : au bout de 20 s, « Personne pour l'instant » et le fantôme de l'autre joueur est proposé (après un premier duel classé perdu par cet autre joueur).
- [ ] Duel contre le fantôme : étiquette « Fantôme de … », il bouge dans la petite vue, il lâche, le résultat s'affiche avec « +10 points de ligue ».
- [ ] Annuler pendant la recherche, puis relancer : ça repart normalement.
- [ ] Un duel entre amis (par code) ne donne **aucun** point de ligue.

**Amis**
- [ ] A ajoute B avec son code : B voit la fenêtre « A veut t'ajouter en ami » avec Accepter / Refuser ; après Accepter, chacun voit l'autre dans ses amis, même sans score.
- [ ] B refuse : A ne reçoit rien, la demande ne revient pas chez B.
- [ ] Toucher un joueur du classement mondial : son profil s'ouvre (tête, place, record), « Ajouter en ami » lui envoie une demande.

**Pubs et achats**
- [ ] Test Suite LevelPlay (7 + 7 touches sur la version) : toutes les régies sans point rouge.
- [ ] Une vidéo récompensée (coffre ou roue) donne bien la récompense.
- [ ] Coffre de fin de partie : le 1er coup le charge, les suivants font monter la rareté (étiquette sous le coffre), puis il s'ouvre ; un coffre légendaire donne une tête ; les chances sont lisibles en bas.
- [ ] Bannière (si le bloc est rempli dans `GameApp.AdIds`) : relancer l'appli, 1re partie sans bannière, 2e partie avec, en bas, sans toucher les pieds de Basile ; elle disparaît en pause, à la chute et sur l'écran de fin ; jamais en duel ; pas de pub imposée juste après cette partie.
- [ ] Restaurer mes achats.

Si une case ne passe pas : on ne soumet pas, on envoie une capture à Claude.

## 3. Après l'envoi

- [ ] Version publiée **manuellement** (pas automatiquement), le soir de la vidéo.
- [ ] Le lendemain : relancer la partie « Serveurs Unity » du script et regarder le classement dans le jeu.
