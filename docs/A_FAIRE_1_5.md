# Ce que Thomas doit faire pour la 1.5

Tout le code est sur la branche `v1.1-poids-pubs-achats`. Voici, dans l'ordre, ce qui ne peut se faire que de ton côté.

## 1. Une seule fois, dans les tableaux de bord (avant la sortie)

1. **Classement de la ligue** (10 min) : Unity Cloud → projet Bascule → **production** → Leaderboards → Create leaderboard, avec les réglages de `docs/LIGUES.md` (`bascule_ligue`, Descending, Keep best, remise à zéro tous les 7 jours le lundi 00:00 UTC, **archive cochée**). Sans lui, les ligues restent vides et pas de récompense du lundi.
2. **Bannière** : unité Banner créée (`358ljy0tx574p6cx`, 30 s) et mise dans le jeu. Reste à ajouter un emplacement bannière chez chaque réseau (AdMob, Unity Ads, Liftoff, InMobi, Chartboost ; ironSource est automatique) et à les mettre dans le groupe de médiation de la bannière (`docs/PUBS.md`). Sans eux, seul ironSource enchérit : la bannière marche, mais rapporte moins.
3. **Clé Unity exposée** : supprime la clé du compte de service `bascule-classement` qui était visible sur une capture (Unity Cloud → Administration → Service accounts).
4. **Site** : mets `docs/site/bascule/confidentialite.html` et `docs/site/bascule/duel.html` sur orpheex.fr (ligues, fantômes, liens d'invitation, notifications de duel).
5. **Notifications de duel** (facultatif, environ 30 min, gratuit) : « Léo te défie en duel » sur le téléphone de l'ami, même jeu fermé. Étapes 0 à 7 de `docs/NOTIFICATIONS.md` : clé Apple, Push sur l'identifiant de l'app, profil refait, trois secrets dans Unity, `python Tools\deploy_push_module.py`, puis **Bascule → Notifications de duel (iOS) → Activer** avant le build. Tant que l'interrupteur est éteint, rien ne change (les rappels marchent).

## 2. À chaque build

```
cd "$env:USERPROFILE\Documents\bacule"
git pull origin v1.1-poids-pubs-achats
robocopy "$env:USERPROFILE\Documents\bacule\Assets" "$env:USERPROFILE\Downloads\Bascule_V20.5\Bascule\Assets" /E
robocopy "$env:USERPROFILE\Documents\bacule\Assets\Bascule\Resources" "$env:USERPROFILE\Downloads\Bascule_V20.5\Bascule\Assets\Bascule\Resources" /MIR /XF *.meta
```

La 2e copie enlève du projet Unity les fichiers supprimés du dépôt (anciennes images, logo Samb'bach, ancienne musique du duel) : sans elle, ils restaient dans l'appli envoyée à Apple. Elle ne touche qu'au dossier `Resources` du jeu.

**Rappels sur le téléphone** : dans Unity, Window → Package Manager → Unity Registry → **Mobile Notifications** → Install. Le jeu a déjà tout le code (au plus un rappel par jour : cadeau du jour, roue prête, série en danger, fin de l'événement, Superstar, retour après 3 jours) ; sans ce paquet, il n'envoie aucune notification. La question « Un petit rappel ? » arrive après le premier coffre (et une fois pour les joueurs déjà installés). Gratuit (rien à payer : ni Apple, ni Unity). Le projet coupe tout seul la demande d'autorisation que le paquet poserait sinon dès le premier lancement (`Editor/NotificationsSetup.cs`). En cas de souci : désinstaller le paquet, le jeu redevient exactement comme avant. Les invitations au duel par notification demandent en plus l'étape 5 ci-dessus.

Dans Unity : **Bascule → Configurer le projet** (version 1.5, et nouveau réglage iOS « Behavior in Background : Custom » pour que le duel continue avec le Centre de contrôle), puis build iOS dans `Documents\bacule-ios`. Dans la console, après le build, deux lignes doivent apparaître (sinon, envoie-moi une capture) :
- **« Bascule : le jeu continue quand le Centre de contrôle est ouvert »** ;
- **« Bascule : SDK dynamiques ajoutés à la cible Unity-iPhone du Podfile »** (InMobi et AppLovin : sans eux, plantage au lancement).

Le build iOS doit **réussir** (pas d'erreur rouge dans la console) avant la copie ci-dessous. Le 14/11, l'envoi « Export iOS 1.5 » était en fait l'ancien export 1.4 recopié (le build Unity n'avait pas abouti) : il ne faut **pas** utiliser un build Codemagic fait à partir de cet envoi. Codemagic refuse maintenant un export ancien (étape « Vérifier l'export »).

```
cd "$env:USERPROFILE\Documents\bacule-ios-git"
git pull origin main
robocopy "$env:USERPROFILE\Documents\bacule-ios" "$env:USERPROFILE\Documents\bacule-ios-git" /MIR /XD .git Pods build /XF codemagic.yaml .gitattributes .gitignore
git add -A
git commit -m "Export iOS 1.5"
git push origin main
```

Puis Codemagic, et dis-moi que c'est poussé : je lance `Tools/verif_publication.py` (tout doit être OK).

## 3. Tests TestFlight (deux iPhone)

La liste complète est dans `docs/AVANT_PUBLICATION.md`. Les plus importants pour la 1.5 :
- duel classé (les deux en même temps, puis un seul : fantôme au bout de 20 s) ;
- Centre de contrôle + enregistrement de l'écran pendant un duel : le duel continue ;
- demande d'ami : rien dans le classement Amis avant l'acceptation ;
- coffre : la rareté monte tape après tape ;
- bannière (si l'identifiant est rempli) : pas à la 1re partie, oui à la 2e ;
- notifications de duel (si l'interrupteur est allumé) : test 7 de `docs/NOTIFICATIONS.md` (B ferme le jeu, A le défie, B reçoit la notification).

## 4. App Store Connect

Version **1.5**, choisir le build, et dans « Nouveautés » par exemple :

> Duel classé : affronte des joueurs du monde entier sans code, grimpe dans ta ligue chaque semaine (Légende, Élite, Challenger, Espoir) et gagne des pièces le lundi. Coffres à rareté, renfort après un 128, demandes d'ami, et plein de corrections.

Avec les notifications de duel allumées, ajouter : « Un ami te défie ? Tu reçois une notification, même jeu fermé (Réglages → Rappels et duels). »
