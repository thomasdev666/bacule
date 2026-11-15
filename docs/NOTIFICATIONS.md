# Notifications : rappels et invitations au duel (1.5)

Deux sortes de notifications, réglées par le même interrupteur du jeu (Réglages → Notifications → **« Rappels et duels »**) :

| | Rappels | Invitation au duel |
|---|---|---|
| Exemple | « Ta roue est prête ! » | « Léo te défie en duel. Viens vite ! » |
| Qui l'envoie | le téléphone lui-même (programmée en quittant le jeu) | Apple, à la demande du serveur du jeu (module Cloud Code `BasculePush`) |
| Fréquence | 1 par jour au plus | 10 par jour au plus, jamais entre 21 h et 8 h |
| iPhone / Android | les deux | iPhone seulement (Android : il faudrait Firebase) |
| Prix | gratuit | gratuit (Apple : inclus dans le compte développeur ; Cloud Code : 1 million d'appels gratuits par mois) |
| Code | `GameApp.ScheduleNotifications` | `Core/DuelPush.cs`, `Server/BasculePush`, `GameApp.SyncPush` / `SendDuelPush` |

La question « Un petit rappel ? » arrive après le premier coffre et parle des deux. Le jeu ne pose jamais la question d'iOS tout seul au lancement (`Editor/NotificationsSetup.cs`).

## Comment marche l'invitation

1. **Le joueur B accepte les rappels.** Son jeu demande à iOS le jeton de notification du téléphone et le range dans **ses données privées** Cloud Save (lui seul et le serveur peuvent les lire, jamais les autres joueurs) avec la liste de **ses amis acceptés** et son **fuseau horaire** (`push_token`, `push_friends`, `push_tz`). Mis à jour tout seul quand ses amis changent ; effacé dès qu'il coupe les rappels (ou les coupe dans les Réglages de l'iPhone), et avant la suppression de son compte.
2. **Le joueur A invite B** (liste d'amis → Défier). Le jeu écrit l'invitation comme avant (données publiques `duel_to`…), puis appelle le module `BasculePush` (fonction `DuelInvite`, paramètre `targetId`).
3. **Le module vérifie tout**, avec le jeton du serveur, avant d'envoyer quoi que ce soit :
   - l'invitation existe vraiment, vers B, et date de moins de 2 minutes ;
   - B a un jeton, et **A fait partie des amis acceptés par B** (une demande pas encore acceptée ou un joueur bloqué ne peut rien envoyer) ;
   - il n'est pas entre 21 h et 8 h chez B ;
   - A n'insiste pas : 10 s entre deux envois, 2 min avant de relancer le même ami, 5 par jour au même ami, 20 par jour en tout ;
   - B n'est pas inondé : 10 notifications de duel par jour au plus, tous amis confondus.
4. **Envoi chez Apple** (HTTP/2, clé `.p8` lue dans le Secret Manager de Unity). Le pseudo de A passe par le même filtre que le classement (`PseudoFilter`) ; refusé, il devient « Un ami ». Si B a le jeu ouvert, rien ne s'affiche par-dessus : l'invitation apparaît déjà dans le jeu. Si B touche la notification, le jeu s'ouvre et l'invitation s'affiche (elle reste valable 5 minutes).

Rien de tout ça ne bloque le duel : sans module, sans clé ou sans réseau, l'invitation marche exactement comme en 1.4 (B la voit en ouvrant le jeu).

## Ce que Thomas doit faire (une fois, environ 30 minutes)

L'interrupteur **reste éteint** tant que les étapes 1 à 5 ne sont pas faites : la 1.5 peut partir sans, rien ne casse (les rappels marchent).

### 0. Paquet Mobile Notifications (si ce n'est pas déjà fait)

Unity → Window → Package Manager → Unity Registry → **Mobile Notifications** → Install. Sans lui, ni rappels ni invitations.

### 1. Clé Apple (5 min)

[developer.apple.com/account](https://developer.apple.com/account) → Certificates, Identifiers & Profiles → **Keys** → « + » :
- nom : `Bascule Push` ;
- cocher **Apple Push Notifications service (APNs)** → Configure : environnement **Sandbox & Production**, restriction **Team Scoped (All Topics)** → Save ;
- Continue → Register → **Download** : le fichier `AuthKey_XXXXXXXXXX.p8` ne se télécharge **qu'une fois**, le garder en lieu sûr (jamais dans le dépôt, jamais dans une conversation) ;
- noter le **Key ID** (10 caractères, affiché sur la page de la clé) ;
- noter le **Team ID** (10 caractères) : page **Membership details** du compte.

### 2. Notifications sur l'identifiant de l'app (2 min)

Certificates, Identifiers & Profiles → **Identifiers** → `fr.orpheex.bascule` → cocher **Push Notifications** → Save → Confirm. Ne pas créer de certificat : la clé de l'étape 1 suffit.

### 3. Profil de l'app (5 min)

Le profil App Store de l'app ne contient pas encore les notifications (Apple l'affiche « Invalid » après l'étape 2).
- Apple → **Profiles** → le profil App Store de `fr.orpheex.bascule` → Edit → Save (il est refait avec Push).
- Codemagic → Teams → **Code signing identities** → iOS provisioning profiles : retirer l'ancien et récupérer le nouveau (« Fetch profiles »).

Si on l'oublie, Codemagic s'arrête à l'étape « Signature iOS » (message sur `aps-environment` ou Push Notifications) : rien n'est envoyé à Apple, il suffit de faire cette étape et de relancer.

### 4. Unity Cloud : Cloud Code et les trois secrets (10 min)

[cloud.unity.com](https://cloud.unity.com) → projet Bascule → environnement **production** :
- **Cloud Code** : l'ouvrir une fois (activer le service s'il le propose).
- **Secret Manager** (Administration → Secrets, selon la version du tableau de bord) : créer trois secrets, chacun **autorisé pour Cloud Code** :
  - `APNS_KEY` : tout le contenu du fichier `.p8` (ouvert avec le Bloc-notes, lignes BEGIN et END comprises) ;
  - `APNS_KEY_ID` : le Key ID ;
  - `APNS_TEAM_ID` : le Team ID.

### 5. Déployer le module (5 min)

- Compte de service : Unity Cloud → Administration → **Service accounts** → en créer un (`bascule-push`) avec le rôle **Cloud Code Editor** sur le projet → Keys → créer une clé (Key ID + Secret).
- Sur le PC (SDK .NET 8 installé : [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)), dans PowerShell :

```
cd "$env:USERPROFILE\Documents\bacule"
$env:UNITY_KEY_ID="…"; $env:UNITY_KEY_SECRET="…"
python Tools\deploy_push_module.py
```

Le script lance les tests du module, le construit (`Exports/BasculePush.ccm`) et le déploie dans l'environnement production. Il doit finir par « Module BasculePush créé » (ou « mis à jour »). En cas de refus 403 : rôle manquant, ou Cloud Code pas encore activé (étape 4).

Autre possibilité, avec l'outil en ligne de commande de Unity : `python Tools\deploy_push_module.py --ccm` puis `ugs deploy Exports\BasculePush.ccm`.

À refaire seulement si `Server/BasculePush` ou `Core/DuelPush.cs` changent.

### 6. Allumer l'interrupteur et faire le build

Unity → **Bascule → Notifications de duel (iOS) → Activer** (la fenêtre rappelle les étapes 1 à 3). Puis build iOS, copie vers `bacule-ios`, Codemagic, comme d'habitude (`docs/A_FAIRE_1_5.md`). Dans la console du build : « Notifications de duel : ACTIVÉES ».

`python3 Tools/verif_publication.py ../bacule-ios` vérifie ensuite l'export (environnement de production d'Apple, inscription du téléphone, rien par-dessus le jeu ouvert) et que le module répond.

### 7. Test sur deux iPhone (TestFlight)

1. A et B amis (demande envoyée **et acceptée**).
2. Sur B : accepter « Un petit rappel ? » (ou Réglages → Notifications → Rappels et duels), puis rester une minute sur l'accueil (le jeton part en ligne), puis **fermer le jeu**.
3. Sur A : Défier B. En quelques secondes, B reçoit « A te défie en duel. Viens vite ! ».
4. B touche la notification : le jeu s'ouvre, l'invitation s'affiche, le duel démarre.
5. B coupe « Rappels et duels » dans le jeu ; A réinvite deux minutes plus tard : plus de notification (l'invitation dans le jeu marche toujours).

Pas de notification ? Voir le diagnostic ci-dessous.

## Diagnostic

Le module garde le résultat du dernier envoi dans les **données protégées de l'invitant** : Unity Cloud → Player Management → chercher le joueur A → Cloud Save → Protected → `push_status` (« 2026-11-20 16:02:11Z sent »).

| Résultat | Signification | Que faire |
|---|---|---|
| `sent` | Apple a accepté la notification | si rien n'arrive : notifications du jeu coupées dans les Réglages de l'iPhone de B, ou mode Concentration |
| `off` (non gardé) | B n'a pas de jeton | B doit accepter les rappels, avoir une version avec l'interrupteur allumé, puis ouvrir le jeu en ligne |
| `notfriend` | B n'a pas (encore) accepté A comme ami | accepter la demande d'ami |
| `quiet` | il est entre 21 h et 8 h chez B | normal |
| `limit` | trop d'envois (A) ou de réceptions (B) aujourd'hui | normal, ça repart au bout de 24 h |
| `stale` | pas d'invitation récente de A vers B | ne devrait pas arriver : m'envoyer la date et l'heure |
| `nokey` | un des trois secrets manque ou la clé `.p8` est illisible | refaire l'étape 4 (contenu complet du fichier, secrets autorisés pour Cloud Code) |
| `apns:403:InvalidProviderToken` | Apple refuse la clé | vérifier `APNS_KEY_ID` et `APNS_TEAM_ID` (et que la clé n'a pas été révoquée) |
| `apns:400:TopicDisallowed` | la clé ne couvre pas l'app | clé « Team Scoped (All Topics) » de la même équipe que l'app |
| `gone` | le jeton de B ne vaut plus rien (app supprimée…) : effacé | rien : B en republie un en rouvrant le jeu |
| `busy` | Apple demande d'attendre | rien |
| `error` | Cloud Save ou réseau indisponible | réessayer ; si ça dure, m'envoyer l'heure |

Dans le jeu, la console Xcode affiche aussi « [Bascule] Notification de duel : … » pour tout résultat autre que `sent` ou `off`, et l'événement `duel_push` est compté dans les statistiques.

## Revenir en arrière

Unity → Bascule → Notifications de duel (iOS) → **Désactiver**, puis build : l'app n'a plus la capacité Push, les rappels continuent. Le module peut rester déployé (il ne fait rien sans jeton).

## Confidentialité

- Données ajoutées : jeton de notification (identifiant technique donné par Apple pour ce téléphone et cette app), liste des amis acceptés, fuseau horaire : privées (le joueur et le serveur seulement) ; dates des notifications des dernières 24 h et résultat du dernier envoi : protégées (écrites par le serveur seulement).
- Politique de confidentialité mise à jour (`docs/site/bascule/confidentialite.html`, section Notifications) : la remettre sur orpheex.fr.
- App Store Connect → Confidentialité de l'app : ajouter **Identifiants → Identifiant de l'appareil** pour « Fonctionnalité de l'app », lié à l'utilisateur, pas de suivi (`docs/APP_STORE.md`).
