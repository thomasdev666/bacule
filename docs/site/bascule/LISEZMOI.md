# Pages du jeu pour orpheex.fr

Mettre ces trois fichiers sur le site, dans un dossier `bascule` :

- `https://orpheex.fr/bascule/` → `index.html` (aide et contact : c'est l'« URL d'assistance » demandée par les stores)
- `https://orpheex.fr/bascule/confidentialite` → `confidentialite.html` (l'« URL de politique de confidentialité ») ; si le site n'enlève pas l'extension, utiliser `https://orpheex.fr/bascule/confidentialite.html` et changer `GameApp.PrivacyUrl`.
- `style.css`
- `https://orpheex.fr/bascule/duel.html` → `duel.html` (page du lien d'invitation au duel : `…/duel.html?c=CODE`). Elle ouvre le jeu (`bascule://duel/CODE`), sinon l'App Store, et garde le code à copier. L'adresse est dans `Core/Duel.cs` (`DuelRules.InviteUrl`) : la changer là aussi si le dossier change.

Avant de publier, remplir les passages surlignés en jaune (adresse e-mail, forme juridique, adresse, SIREN, date, durée de conservation). Ce texte est un modèle sérieux mais pas un avis juridique : le faire relire si possible.
