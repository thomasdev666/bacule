using System;
using System.Collections.Generic;

namespace Bascule.Core
{
    /// <summary>Textes du jeu en français et en anglais. Ajouter une langue = ajouter une colonne.</summary>
    public static class Loc
    {
        public static string Language { get; set; } = "fr";

        static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            // Accueil
            ["title"] = new[] { "BASCULE", "BASCULE" },
            // Nom de l'éditeur tel qu'il s'affiche dans le jeu (le site reste orpheex.fr, l'identifiant fr.orpheex.bascule).
            ["publisher"] = new[] { "ORPHÉE X", "ORPHÉE X" },
            ["random_head"] = new[] { "Au hasard", "Random" },
            ["random_head_on"] = new[] { "Une tête au hasard à chaque partie !", "A random head every game!" },
            // Écran de démarrage (une phrase au hasard sous le niveau à bulle)
            ["boot_tip_0"] = new[] { "Basile s'échauffe…", "Basile is warming up…" },
            ["boot_tip_1"] = new[] { "On cire la planche…", "Waxing the plank…" },
            ["boot_tip_2"] = new[] { "Deux poids pareils = un poids double !", "Two equal weights = one double weight!" },
            ["boot_tip_3"] = new[] { "Un 128 s'envole : gros bonus !", "A 128 lifts off: big bonus!" },
            ["boot_tip_4"] = new[] { "Garde Basile en équilibre !", "Keep Basile balanced!" },
            ["play"] = new[] { "JOUER", "PLAY" },
            ["resume"] = new[] { "Reprendre la partie", "Resume game" },
            ["daily"] = new[] { "Défi du jour", "Daily challenge" },
            ["daily_done"] = new[] { "Défi du jour fait - meilleur : {0}", "Daily done - best: {0}" },
            ["streak"] = new[] { "Série : {0} jour(s)", "Streak: {0} day(s)" },
            ["best"] = new[] { "Record : {0}", "Best: {0}" },
            ["heads"] = new[] { "Têtes", "Heads" },
            ["leaderboard"] = new[] { "Classement", "Leaderboard" },
            ["settings"] = new[] { "Réglages", "Settings" },
            ["back"] = new[] { "Retour", "Back" },
            ["daily_rule"] = new[] { "Le même parcours de poids pour tout le monde aujourd'hui. Une seule partie compte pour le classement du jour.", "The same weight sequence for everyone today. One game counts for today's leaderboard." },
            ["daily_best"] = new[] { "Ton meilleur aujourd'hui : {0}", "Your best today: {0}" },
            ["daily_streak"] = new[] { "Série : {0} jour(s)", "Streak: {0} day(s)" },
            ["daily_again"] = new[] { "Rejouer", "Play again" },
            ["duel"] = new[] { "Duel", "Duel" },
            ["duel_code"] = new[] { "J'ai un code défi", "I have a challenge code" },
            ["duel_prompt"] = new[] { "Code défi de ton ami", "Your friend's challenge code" },
            ["duel_invalid"] = new[] { "Code invalide", "Invalid code" },
            ["thanks"] = new[] { "Merci !", "Thank you!" },
            ["soon"] = new[] { "Bientôt disponible", "Coming soon" },

            // Partie
            ["score"] = new[] { "{0}", "{0}" },
            ["next"] = new[] { "Suivant", "Next" },
            ["combo"] = new[] { "COMBO ×{0}", "COMBO ×{0}" },
            ["balloon"] = new[] { "ENVOLÉ ! +{0}", "LIFT OFF! +{0}" },
            ["save"] = new[] { "OUF ! Sauvé à {0} %", "PHEW! Saved at {0}%" },
            ["lost_weight"] = new[] { "Perdu !", "Lost!" },
            ["music"] = new[] { "Musique", "Music" },
            ["hint"] = new[] { "Glisse pour viser, relâche pour lâcher", "Slide to aim, release to drop" },
            ["hint_short"] = new[] { "Garde Basile en équilibre !", "Keep Basile balanced!" },
            ["score_label"] = new[] { "SCORE", "SCORE" },
            ["best_combo"] = new[] { "Meilleur combo : ×{0}", "Best combo: ×{0}" },
            ["next_head_left"] = new[] { "Encore {0} pts pour débloquer cette tête !", "{0} more points to unlock this head!" },
            ["pause"] = new[] { "Pause", "Pause" },
            ["pause_score"] = new[] { "Score : {0}   ·   Record : {1}", "Score: {0}   ·   Best: {1}" },
            ["continue"] = new[] { "Continuer", "Continue" },
            ["restart"] = new[] { "Recommencer", "Restart" },
            ["home"] = new[] { "Accueil", "Home" },

            // Fin de partie
            ["game_over"] = new[] { "{0} a lâché !", "{0} let go!" },
            ["new_best"] = new[] { "NOUVEAU RECORD !", "NEW BEST!" },
            ["revive"] = new[] { "Revivre (pub)", "Revive (ad)" },
            ["replay"] = new[] { "Rejouer", "Play again" },
            ["share"] = new[] { "Partager", "Share" },
            ["new_head"] = new[] { "Nouvelle tête : {0} !", "New head: {0}!" },
            ["next_head"] = new[] { "Prochaine tête à {0} points", "Next head at {0} points" },
            ["near_miss"] = new[] { "Sauvetage de justesse : {0} %", "Closest save: {0}%" },
            ["ad_playing"] = new[] { "Publicité…", "Ad playing…" },
            ["ad_simulated"] = new[] { "(publicité simulée — régie à brancher)", "(simulated ad — network not wired yet)" },
            ["ad_unavailable"] = new[] { "Pas de publicité disponible", "No ad available" },
            ["lb_report_btn"] = new[] { "Signaler", "Report" },
            ["mod_note"] = new[] { "Ce pseudo est choquant ou insultant ? Signale-le : on le vérifie sous 24 h. Tu peux aussi bloquer ce joueur.", "Is this name offensive? Report it: we check within 24 hours. You can also block this player." },
            ["mod_report"] = new[] { "Signaler le pseudo", "Report name" },
            ["mod_block"] = new[] { "Bloquer ce joueur", "Block player" },
            ["mod_reported"] = new[] { "Merci ! Signalement envoyé, ce joueur est masqué", "Thanks! Report sent, player hidden" },
            ["mod_blocked"] = new[] { "Joueur bloqué : tu ne le verras plus", "Player blocked: you won't see them again" },
            ["mod_unblock_all"] = new[] { "Débloquer les joueurs", "Unblock players" },
            ["mod_unblocked"] = new[] { "Tous les joueurs sont débloqués", "All players unblocked" },
            ["mod_mail_subject"] = new[] { "Bascule : signalement d'un pseudo", "Bascule: name report" },
            ["mod_mail_body"] = new[] { "Pseudo signalé : {0}\nIdentifiant du joueur : {1}\nMon identifiant : {2}\n\nPourquoi (facultatif) : ", "Reported name: {0}\nPlayer ID: {1}\nMy ID: {2}\n\nWhy (optional): " },
            ["pseudo_rules"] = new[] { "Visible par tous : pas d'insulte, ni d'info perso. Un pseudo signalé peut être supprimé.", "Visible to everyone: nothing rude, no personal info. Reported names may be removed." },
            ["store_unavailable"] = new[] { "Boutique indisponible : vérifie ta connexion", "Store unavailable: check your connection" },

            // Carte de score
            ["card_score"] = new[] { "points", "points" },
            // Partage (docs/PARTAGE.md) : titres, résumé « à la Wordle », derniers mots de Basile, mode vidéo
            ["title_express"] = new[] { "Chute express", "Express fall" },
            ["title_tightrope"] = new[] { "Funambule de l'extrême", "Extreme tightrope walker" },
            ["title_combo"] = new[] { "Roi du combo", "Combo king" },
            ["title_balloon"] = new[] { "Pilote de montgolfière", "Balloon pilot" },
            ["title_butter"] = new[] { "Mains de beurre", "Butterfingers" },
            ["title_survivor"] = new[] { "Increvable", "Unbreakable" },
            ["title_strong"] = new[] { "Haltérophile", "Weightlifter" },
            ["title_apprentice"] = new[] { "Apprenti équilibriste", "Apprentice balancer" },
            ["title_balancer"] = new[] { "Équilibriste", "Balancer" },
            ["title_master"] = new[] { "Maître de la bascule", "Seesaw master" },
            ["title_legend"] = new[] { "Légende de la bascule", "Seesaw legend" },
            ["title_label"] = new[] { "« {0} »", "\u201C{0}\u201D" },
            ["brag_head"] = new[] { "BASCULE ⚖️", "BASCULE ⚖️" },
            ["brag_head_daily"] = new[] { "BASCULE ⚖️ Défi du jour n°{0}", "BASCULE ⚖️ Daily #{0}" },
            ["brag_score"] = new[] { "{0} points · {1}", "{0} points · {1}" },
            ["brag_record"] = new[] { "🏆 Nouveau record !", "🏆 New best!" },
            ["brag_close"] = new[] { "😤 À {0} points de mon record…", "😤 {0} points short of my best…" },
            ["brag_save"] = new[] { "😱 Sauvé à {0} % de la chute", "😱 Saved at {0}% from falling" },
            ["brag_combo"] = new[] { "🔥 Combo ×{0}", "🔥 Combo ×{0}" },
            ["brag_balloon"] = new[] { "🎈 ×{0}", "🎈 ×{0}" },
            ["brag_challenge"] = new[] { "Tu crois faire mieux ? 😏 Joue exactement la même partie que moi avec le code {0}", "Think you can beat it? 😏 Play the exact same run with code {0}" },
            ["brag_daily_call"] = new[] { "Même partie pour tout le monde aujourd'hui : à toi de jouer ! 😏", "Same run for everyone today: your turn! 😏" },
            ["brag_hook"] = new[] { "⚖️ J'ai tenu {0} points sur Bascule !", "⚖️ I held on for {0} points in Bascule!" },
            ["brag_hook_record"] = new[] { "🏆 Nouveau record sur Bascule : {0} points !", "🏆 New best in Bascule: {0} points!" },
            ["brag_hook_close"] = new[] { "😤 {0} points sur Bascule… à {1} points de mon record !", "😤 {0} points in Bascule… {1} short of my best!" },
            ["brag_hook_daily"] = new[] { "⚖️ Défi du jour n°{0} sur Bascule : {1} points", "⚖️ Bascule daily #{0}: {1} points" },
            ["brag_hook_daily_record"] = new[] { "🏆 Défi du jour n°{0} sur Bascule : {1} points, mon record !", "🏆 Bascule daily #{0}: {1} points, my best!" },
            ["brag_story"] = new[] { "Mon titre : « {0} »", "My title: “{0}”" },
            ["brag_story_moment"] = new[] { "Mon titre : « {0} », avec {1}", "My title: “{0}”, with {1}" },
            ["brag_moment_save"] = new[] { "un sauvetage à {0} % de la chute 😱", "a save at {0}% from falling 😱" },
            ["brag_moment_balloon"] = new[] { "{0} montgolfière(s) envolée(s) 🎈", "{0} balloon(s) flown 🎈" },
            ["brag_moment_combo"] = new[] { "un combo ×{0} 🔥", "a ×{0} combo 🔥" },
            ["close_to_best"] = new[] { "À {0} points du record !", "{0} points from your best!" },
            ["share_panel_title"] = new[] { "Partage ta partie !", "Share your game!" },
            ["share_save"] = new[] { "Enregistrer", "Save" },
            ["share_saved"] = new[] { "Enregistrée !", "Saved!" },
            ["share_copy"] = new[] { "Copier le texte", "Copy the text" },
            ["share_copied"] = new[] { "Texte copié !", "Text copied!" },
            ["share_close"] = new[] { "Fermer", "Close" },
            ["share_hint"] = new[] { "Astuce : appuie longuement sur l'image pour l'enregistrer dans tes photos.", "Tip: long-press the image to save it to your photos." },
            ["caption_copied"] = new[] { "Légende copiée : colle-la sous ta vidéo", "Caption copied: paste it under your video" },
            ["last_0"] = new[] { "MA PLANCHE !", "MY PLANK!" },
            ["last_1"] = new[] { "Je reviendrai…", "I'll be back…" },
            ["last_2"] = new[] { "C'était le vent !", "It was the wind!" },
            ["last_3"] = new[] { "Qui a mis ce poids ?!", "Who put that there?!" },
            ["last_4"] = new[] { "Pas mon jour…", "Not my day…" },
            ["last_5"] = new[] { "J'y étais presque !", "So close!" },
            ["last_6"] = new[] { "Aïe aïe aïe !", "Ouch ouch ouch!" },
            ["last_7"] = new[] { "Au revoir le record…", "Bye-bye, record…" },
            ["creator_mode"] = new[] { "Mode vidéo", "Video mode" },
            ["creator_hint"] = new[] { "Nom du jeu et code défi à l'écran : filme et partage !", "Game name and code on screen: record and share!" },
            ["creator_mark"] = new[] { "BASCULE · défi {0}", "BASCULE · code {0}" },
            ["creator_mark_daily"] = new[] { "BASCULE · défi du jour n°{0}", "BASCULE · daily #{0}" },
            ["card_daily"] = new[] { "Défi du jour n°{0} : même partie pour tous", "Daily #{0}: same game for everyone" },
            ["card_challenge"] = new[] { "Tu fais mieux ? Défie-moi !", "Can you beat me?" },
            ["share_text"] = new[] { "J'ai fait {0} points sur Bascule avec {1} ! Tu fais mieux ? Code défi : {2}", "I scored {0} on Bascule with {1}! Can you beat it? Challenge code: {2}" },

            // Collection
            ["collection"] = new[] { "Collection : {0}/{1}", "Collection: {0}/{1}" },
            ["unlock_score"] = new[] { "{0} pts", "{0} pts" },
            ["unlock_streak"] = new[] { "{0} jours de défi", "{0}-day streak" },
            ["unlock_nearmiss"] = new[] { "Sauvé à {0} %", "Save at {0}%" },
            ["unlock_balloon"] = new[] { "Envoler un 128", "Lift a 128" },
            ["unlock_combo"] = new[] { "Combo ×{0}", "Combo ×{0}" },
            ["unlock_world"] = new[] { "Top {0} mondial", "World top {0}" },
            ["unlock_premium"] = new[] { "Boutique", "Shop" },
            ["unlock_level"] = new[] { "Niveau {0}", "Level {0}" },
            ["unlock_legends"] = new[] { "Pack Légendes", "Legends pack" },
            ["unlock_starter"] = new[] { "Offre de départ", "Starter offer" },
            ["sec_score"] = new[] { "Records", "High scores" },
            ["sec_level"] = new[] { "Niveaux", "Levels" },
            ["sec_special"] = new[] { "Exploits", "Feats" },
            ["sec_shop"] = new[] { "Boutique", "Shop" },
            ["sec_premium"] = new[] { "Légendes", "Legends" },
            ["equipped"] = new[] { "Portée", "Worn" },
            ["worn_head"] = new[] { "Tête portée", "Worn head" },
            ["next_head"] = new[] { "Prochaine", "Next" },
            ["collection_short"] = new[] { "{0} / {1} têtes", "{0} / {1} heads" },
            ["rarity_Common"] = new[] { "Commune", "Common" },
            ["rarity_Rare"] = new[] { "Rare", "Rare" },
            ["rarity_Epic"] = new[] { "Épique", "Epic" },
            ["rarity_Legendary"] = new[] { "Légendaire", "Legendary" },

            // Classement
            ["lb_world"] = new[] { "Monde", "World" },
            ["lb_daily"] = new[] { "Jour", "Today" },
            ["lb_mine"] = new[] { "Mes scores", "My scores" },
            ["lb_friends"] = new[] { "Amis", "Friends" },
            ["friends_empty"] = new[] { "Ajoute tes amis avec leur code ami pour comparer vos records.", "Add friends with their friend code to compare your best scores." },
            ["friend_code_btn"] = new[] { "Mon code ami", "My friend code" },
            ["friend_add"] = new[] { "Ajouter un ami", "Add a friend" },
            ["friend_prompt"] = new[] { "Colle le code ami (ou le message reçu)", "Paste the friend code (or the message you got)" },
            ["friend_added"] = new[] { "Ami ajouté !", "Friend added!" },
            ["friend_invalid"] = new[] { "Code ami introuvable", "Friend code not found" },
            ["friend_already"] = new[] { "Déjà dans tes amis", "Already your friend" },
            ["friend_share_text"] = new[] { "Ajoute-moi sur Bascule ! Mon code ami : {0}", "Add me on Bascule! My friend code: {0}" },
            ["friend_copied"] = new[] { "Code ami copié !", "Friend code copied!" },
            ["friend_offline"] = new[] { "Connecte-toi à Internet pour les amis.", "Go online to see friends." },
            ["pseudo_refused"] = new[] { "Ce pseudo n'est pas autorisé (3 à 16 lettres, pas de grossièreté)", "This name is not allowed (3 to 16 letters, nothing rude)" },
            ["lb_offline"] = new[] { "Hors ligne : le classement se synchronisera plus tard.", "Offline: the leaderboard will sync later." },
            ["lb_empty"] = new[] { "Pas encore de score.", "No scores yet." },
            ["lb_loading"] = new[] { "Chargement…", "Loading…" },
            ["pseudo_title"] = new[] { "Ton pseudo pour le classement", "Your name for the leaderboard" },
            ["pseudo_ok"] = new[] { "Valider", "OK" },
            ["pseudo_skip"] = new[] { "Plus tard", "Later" },

            // Réglages
            ["sound"] = new[] { "Son", "Sound" },
            ["vibration"] = new[] { "Vibrations", "Vibration" },
            ["language"] = new[] { "Langue", "Language" },
            ["text_size"] = new[] { "Taille du texte", "Text size" },
            ["reduced_motion"] = new[] { "Animations réduites", "Reduced motion" },
            ["high_fps"] = new[] { "Fluidité 120 images/s", "Smoothness 120 fps" },
            ["on"] = new[] { "Oui", "On" },
            ["off"] = new[] { "Non", "Off" },
            ["remove_ads"] = new[] { "Retirer les publicités", "Remove ads" },
            ["restore"] = new[] { "Restaurer mes achats", "Restore purchases" },
            ["privacy"] = new[] { "Confidentialité", "Privacy" },
            ["support"] = new[] { "Aide et contact", "Help and contact" },
            ["delete_data"] = new[] { "Supprimer mes données en ligne", "Delete my online data" },
            ["delete_title"] = new[] { "Supprimer tes données en ligne ?", "Delete your online data?" },
            ["delete_body"] = new[] { "Ton pseudo, tes scores en ligne et ta liste d'amis seront effacés. Ta progression sur ce téléphone (niveau, pièces, têtes) est gardée.", "Your name, online scores and friends list will be erased. Your progress on this phone is kept." },
            ["delete_ok"] = new[] { "Supprimer", "Delete" },
            ["delete_done"] = new[] { "Données en ligne supprimées", "Online data deleted" },
            ["delete_offline"] = new[] { "Connexion à Internet nécessaire", "Internet connection needed" },
            ["proto"] = new[] { "Prototype", "Prototype" },
            // Économie (docs/ECONOMIE.md) : tout est cosmétique, rien ne change le score.
            ["see_coins"] = new[] { "Voir les pièces", "See coins" },
            ["price_coins"] = new[] { "Prix : {0} pièces", "Price: {0} coins" },
            ["cosmetic_note"] = new[] { "Rien que pour le style : ça ne change ni le score ni la difficulté.", "Just for style: it changes neither the score nor the difficulty." },
            ["planks_title"] = new[] { "Planches", "Planks" },
            ["planks_sub"] = new[] { "Change l'allure de la planche de Basile.", "Change the look of Basile's plank." },
            ["plank_equipped"] = new[] { "Équipée", "Equipped" },
            ["plank_choose"] = new[] { "Choisir", "Choose" },
            ["buy_plank"] = new[] { "Planche « {0} »", "\u201C{0}\u201D plank" },
            ["bought_plank"] = new[] { "Planche « {0} » !", "\u201C{0}\u201D plank!" },
            ["plank_equipped_detail"] = new[] { "Elle est déjà sous les mains de Basile.", "It's already in Basile's hands." },
            ["surprise_title"] = new[] { "Machine à surprises", "Surprise machine" },
            ["surprise_desc"] = new[] { "Une tête, une planche ou des pièces, au hasard. Jamais de doublon !", "A head, a plank or coins, at random. Never a duplicate!" },
            ["surprise_none"] = new[] { "Tu as toutes les têtes de la boutique, bravo !", "You own every shop head, well done!" },
            ["surprise_left"] = new[] { "{0} lots à gagner", "{0} prizes to win" },
            ["surprise_tickets"] = new[] { "{0} ouverture(s) gratuite(s)", "{0} free open(s)" },
            ["surprise_free_button"] = new[] { "Gratuit", "Free" },
            ["surprise_confirm"] = new[] { "{0} pièces pour une tête au hasard parmi celles que tu n'as pas. Les plus chères sont plus rares.", "{0} coins for a random head you don't have yet. Pricier ones are rarer." },
            ["surprise_odds"] = new[] { "Chances :", "Odds:" },
            ["surprise_chances"] = new[] { "Tes chances pour chaque lot", "Your odds for each prize" },
            ["surprise_spinning"] = new[] { "Ça tourne…", "Spinning…" },
            ["surprise_new"] = new[] { "Nouvelle tête !", "New head!" },
            ["surprise_new_plank"] = new[] { "Nouvelle planche !", "New plank!" },
            ["surprise_coins"] = new[] { "Des pièces !", "Coins!" },
            ["surprise_open_free"] = new[] { "Ouvrir · gratuit", "Open · free" },
            ["prize_plank"] = new[] { "Planche", "Plank" },
            ["prize_jackpot"] = new[] { "JACKPOT !", "JACKPOT!" },
            ["surprise_open"] = new[] { "Ouvrir · {0}", "Open · {0}" },
            ["surprise_won"] = new[] { "Tu gagnes : {0} !", "You win: {0}!" },
            ["surprise_value"] = new[] { "Elle vaut {0} pièces dans la boutique.", "It's worth {0} coins in the shop." },
            ["wear"] = new[] { "La porter", "Wear it" },
            ["later"] = new[] { "Plus tard", "Later" },
            ["shield_title"] = new[] { "Protège-série", "Streak shield" },
            ["shield_desc"] = new[] { "Tu oublies un jour ? Ta série de cadeaux continue quand même. Il s'utilise tout seul.", "Forgot a day? Your gift streak keeps going. Used automatically." },
            ["shield_stock"] = new[] { "Protège-séries en réserve : {0}/{1}", "Streak shields in stock: {0}/{1}" },
            ["shield_full"] = new[] { "Ta réserve est pleine !", "Your stock is full!" },
            ["shield_used"] = new[] { "Série sauvée !", "Streak saved!" },
            ["shield_full_short"] = new[] { "Complet", "Full" },
            ["coins_title"] = new[] { "Pièces", "Coins" },
            ["sec_sound"] = new[] { "Son et vibrations", "Sound and vibration" },
            ["sec_display"] = new[] { "Affichage", "Display" },
            ["sec_notif"] = new[] { "Rappels", "Reminders" },
            ["sec_account"] = new[] { "Compte et achats", "Account and purchases" },
            ["sec_help"] = new[] { "Aide", "Help" },
            ["version"] = new[] { "Version {0}", "Version {0}" },
            ["tilt_dir"] = new[] { "Sens de bascule", "Tilt direction" },
            ["tilt_file"] = new[] { "Fichier", "File" },
            ["tilt_natural"] = new[] { "Vers le lourd", "Toward heavy" },
            ["tilt_inverse"] = new[] { "Vers le léger", "Toward light" },
            ["reload_tuning"] = new[] { "Recharger les réglages", "Reload tuning" },
            ["debug_coins"] = new[] { "Test : +1 000 pièces", "Test: +1,000 coins" },
            ["debug_newday"] = new[] { "Test : passer au jour suivant", "Test: skip to next day" },

            // Progression, pièces
            ["level"] = new[] { "Niv. {0}", "Lv. {0}" },
            ["level_up"] = new[] { "Niveau {0} ! +{1} pièces", "Level {0}! +{1} coins" },
            ["coins_gain"] = new[] { "+{0}", "+{0}" },
            ["coins_double"] = new[] { "x2", "x2" },
            ["coins_doubled"] = new[] { "Pièces doublées !", "Coins doubled!" },
            ["missing_coins"] = new[] { "Il te manque {0} pièces", "You need {0} more coins" },
            ["buy_head"] = new[] { "Acheter {0} ?", "Buy {0}?" },
            ["can_buy_head"] = new[] { "Tu peux t'offrir {0} !", "You can get {0}!" },
            ["see_head"] = new[] { "Voir", "See" },
            ["buy"] = new[] { "Acheter", "Buy" },
            ["cancel"] = new[] { "Annuler", "Cancel" },
            ["bought_head"] = new[] { "{0} rejoint ta collection !", "{0} joins your collection!" },
            ["next_shop_head"] = new[] { "Encore {0} pièces pour {1}", "{0} more coins for {1}" },
            ["next_level_head"] = new[] { "Niveau {0} : nouvelle tête", "Level {0}: new head" },
            ["xp_gain"] = new[] { "+{0} XP", "+{0} XP" },

            // Missions
            ["missions"] = new[] { "Missions", "Missions" },
            ["missions_title"] = new[] { "Missions du jour", "Daily missions" },
            ["missions_bonus"] = new[] { "Les 3 missions : +{0} pièces", "All 3 missions: +{0} coins" },
            ["missions_bonus_done"] = new[] { "Bonus des 3 missions gagné !", "All-missions bonus earned!" },
            ["missions_new_in"] = new[] { "Nouvelles missions dans {0}", "New missions in {0}" },
            ["mission_title"] = new[] { "Mission réussie !", "Mission complete!" },
            ["missions_bonus_detail"] = new[] { "Les 3 missions du jour sont faites", "All 3 daily missions done" },
            ["level_title"] = new[] { "Niveau {0} !", "Level {0}!" },
            ["level_detail"] = new[] { "Tu progresses : continue comme ça", "You're improving: keep going" },
            ["mission_done"] = new[] { "Mission réussie : +{0} pièces", "Mission complete: +{0} coins" },
            ["mission_reroll"] = new[] { "Changer", "Swap" },
            ["m_PlayGames"] = new[] { "Joue {0} parties", "Play {0} games" },
            ["m_ScoreInGame"] = new[] { "Fais {0} points en une partie", "Score {0} in one game" },
            ["m_TotalScore"] = new[] { "Cumule {0} points", "Score {0} in total" },
            ["m_Merges"] = new[] { "Fais {0} fusions", "Make {0} merges" },
            ["m_MakeValue"] = new[] { "Fabrique un {0}", "Make a {0}" },
            ["m_Balloon"] = new[] { "Fais s'envoler un 128", "Lift off a 128" },
            ["m_Combo"] = new[] { "Fais un combo ×{0}", "Get a ×{0} combo" },
            ["m_NearMiss"] = new[] { "Sauve Basile à {0} % de la chute", "Save Basile at {0}% of the fall" },
            ["m_PlayDaily"] = new[] { "Joue le défi du jour", "Play the daily challenge" },
            ["m_NoHeartLost"] = new[] { "Fais {0} points en une partie", "Score {0} in one game" },

            // Cadeau du jour
            ["gift_title"] = new[] { "Cadeau du jour", "Daily gift" },
            ["gift_day"] = new[] { "Jour {0}", "Day {0}" },
            ["gift_claim"] = new[] { "Récupérer {0}", "Collect {0}" },
            ["ok"] = new[] { "OK", "OK" },
            ["soon_tag"] = new[] { "Bientôt", "Soon" },
            ["duel_soon_title"] = new[] { "Les duels arrivent bientôt !", "Duels are coming soon!" },
            ["duel_soon_body"] = new[] { "Bientôt, tu pourras défier tes amis sur exactement la même partie. Reviens vite !", "Soon you'll be able to challenge your friends on the exact same run. Stay tuned!" },
            ["coins_word"] = new[] { "pièces", "coins" },
            ["gift_sub"] = new[] { "Jour {0} de ta série · reviens demain pour la suite !", "Day {0} of your streak · come back tomorrow!" },
            ["gift_big"] = new[] { "Jour 7 · Le gros cadeau", "Day 7 · The big gift" },
            ["gift_item_chest"] = new[] { "Un coffre", "A chest" },
            ["gift_item_ticket"] = new[] { "Surprise gratuite", "Free surprise" },
            ["gift_item_plank"] = new[] { "Planche {0}", "{0} plank" },
            ["gift_tile_chest"] = new[] { "Coffre", "Chest" },
            ["gift_tile_ticket"] = new[] { "Surprise", "Surprise" },
            ["gift_today"] = new[] { "Aujourd'hui", "Today" },
            ["gift_tomorrow"] = new[] { "Demain", "Tomorrow" },
            ["gift_claim_now"] = new[] { "Récupérer", "Collect" },
            ["gift_chest_title"] = new[] { "Coffre du jour !", "Daily chest!" },
            ["gift_ticket_title"] = new[] { "Surprise gratuite !", "Free surprise!" },
            ["gift_plank_title"] = new[] { "Nouvelle planche : {0} !", "New plank: {0}!" },
            ["gift_notif_coins"] = new[] { "{0} pièces", "{0} coins" },
            ["gift_notif_chest"] = new[] { "un coffre", "a chest" },
            ["gift_notif_ticket"] = new[] { "une surprise gratuite", "a free surprise" },
            ["gift_notif_plank"] = new[] { "une nouvelle planche", "a new plank" },
            ["gift_hint"] = new[] { "Reviens chaque jour : le cadeau grossit !", "Come back every day: the gift grows!" },
            ["gift_double"] = new[] { "x2 en vidéo", "x2 with a video" },

            // Boutique
            ["shop"] = new[] { "Boutique", "Shop" },
            ["p_starter"] = new[] { "Offre de départ", "Starter offer" },
            ["p_starter_desc"] = new[] { "Plus de pubs imposées + 2 000 pièces + Basile doré", "No forced ads + 2,000 coins + Golden Basile" },
            ["p_removeads"] = new[] { "Sans publicité", "No ads" },
            ["p_removeads_desc"] = new[] { "Plus aucune pub entre les parties, et la seconde chance sans vidéo", "No more ads between games, and second chances without videos" },
            ["p_coins_s"] = new[] { "600 pièces", "600 coins" },
            ["p_coins_m"] = new[] { "2 200 pièces", "2,200 coins" },
            ["p_coins_l"] = new[] { "6 000 pièces", "6,000 coins" },
            ["p_coins_desc"] = new[] { "Pour des têtes, des planches et la machine à surprises", "For heads, planks and the surprise machine" },
            ["p_legends"] = new[] { "Pack Légendes", "Legends pack" },
            ["p_legends_desc"] = new[] { "5 têtes exclusives : Astronaute, Robot, Licorne, Diablotin, Ange", "5 exclusive heads: Astronaut, Robot, Unicorn, Imp, Angel" },
            ["badge_starter"] = new[] { "-60 %", "-60%" },
            ["badge_plus20"] = new[] { "+20 %", "+20%" },
            ["badge_plus40"] = new[] { "+40 %", "+40%" },
            ["owned"] = new[] { "Acheté", "Owned" },
            ["free_coins"] = new[] { "{0} pièces gratuites", "{0} free coins" },
            ["free_coins_left"] = new[] { "Encore {0} aujourd'hui", "{0} left today" },
            ["free_coins_none"] = new[] { "Reviens demain !", "Come back tomorrow!" },
            ["offer_ends"] = new[] { "Se termine dans {0}", "Ends in {0}" },
            ["later"] = new[] { "Plus tard", "Later" },
            ["purchase_failed"] = new[] { "Achat annulé", "Purchase cancelled" },
            ["restored"] = new[] { "Achats restaurés", "Purchases restored" },
            ["removeads_hint_title"] = new[] { "Marre des pubs ?", "Tired of ads?" },
            ["removeads_hint_body"] = new[] { "Retire toutes les pubs entre les parties, pour toujours. Tu soutiens aussi le jeu !", "Remove all ads between games, forever. You also support the game!" },
            ["revive_free"] = new[] { "Revivre (gratuit)", "Revive (free)" },
            ["hours_minutes"] = new[] { "{0} h {1:00}", "{0}h {1:00}" },
            ["minutes_seconds"] = new[] { "{0} min {1:00}", "{0}m {1:00}s" },

            // Notifications
            ["notif_ask_title"] = new[] { "Un petit rappel ?", "A little reminder?" },
            ["notif_ask_body"] = new[] { "Basile peut te prévenir quand ton cadeau du jour et tes nouvelles missions sont prêts. Une fois par jour au maximum.", "Basile can let you know when your daily gift and new missions are ready. At most once a day." },
            ["notif_yes"] = new[] { "Oui, préviens-moi", "Yes, remind me" },
            ["notif_gift_title"] = new[] { "Ton cadeau du jour est prêt !", "Your daily gift is ready!" },
            ["notif_gift_body"] = new[] { "Basile t'attend avec {0} et 3 nouvelles missions.", "Basile is waiting with {0} and 3 new missions." },
            ["notif_streak_title"] = new[] { "Ta série de {0} jours !", "Your {0}-day streak!" },
            ["notif_streak_body"] = new[] { "Joue le défi du jour avant minuit pour la garder.", "Play today's challenge before midnight to keep it." },
            ["notif_comeback_title"] = new[] { "Basile s'ennuie…", "Basile is bored…" },
            ["notif_comeback_body"] = new[] { "Ta planche t'attend. Tu bats ton record de {0} ?", "Your plank is waiting. Can you beat your {0} best?" },
            ["notifications"] = new[] { "Rappels", "Reminders" },

            // Note sur le store
            ["rate_title"] = new[] { "Tu aimes Bascule ?", "Enjoying Bascule?" },
            ["rate_body"] = new[] { "Une petite note sur le store aide énormément le jeu à se faire connaître. Merci !", "A quick rating on the store helps the game a lot. Thank you!" },
            ["rate_yes"] = new[] { "Oui, je note !", "Yes, rate it!" },
            ["rate_no"] = new[] { "Pas maintenant", "Not now" },

            // Tutoriel
            ["tuto_aim"] = new[] { "Glisse le doigt pour viser,\nlâche pour poser le poids", "Slide to aim,\nlet go to drop the weight" },
            ["tuto_merge"] = new[] { "Deux poids pareils qui se touchent\nfusionnent : 2 + 2 = 4 !", "Two equal weights that touch\nmerge: 2 + 2 = 4!" },
            ["tuto_balance"] = new[] { "Ça penche ! Pose le suivant\nde l'autre côté", "It's tilting! Drop the next one\non the other side" },
            ["tuto_lost"] = new[] { "Un poids qui tombe fait perdre des points\net casse le combo !", "A fallen weight costs points\nand breaks the combo!" },
            ["special_now"] = new[] { "Partie spéciale !", "Special run!" },
            ["special_DoubleCoins"] = new[] { "Pièces doublées pendant cette partie", "Double coins this run" },
            ["special_DoubleXp"] = new[] { "XP doublée pendant cette partie", "Double XP this run" },
            ["special_ChestBoost"] = new[] { "Le coffre avance de 2 cases", "The chest fills 2 slots" },
            ["special_short_DoubleCoins"] = new[] { "Pièces x2 !", "Coins x2!" },
            ["special_short_DoubleXp"] = new[] { "XP x2 !", "XP x2!" },
            ["special_short_ChestBoost"] = new[] { "Coffre +2 !", "Chest +2!" },
            ["goal_label"] = new[] { "Objectif : {0}", "Goal: {0}" },
            ["goal_reached"] = new[] { "Objectif atteint !", "Goal reached!" },
            ["chest_title"] = new[] { "Coffre ouvert !", "Chest opened!" },
            ["chest_detail"] = new[] { "Un nouveau coffre toutes les 3 parties", "A new chest every 3 runs" },
            ["chest_head"] = new[] { "Nouvelle tête : {0}", "New head: {0}" },
            ["chest_progress"] = new[] { "Coffre : {0}/{1}", "Chest: {0}/{1}" },
            ["chest_open"] = new[] { "Ouvrir le coffre !", "Open the chest!" },
            ["chest_opened"] = new[] { "Coffre ouvert : +{0} pièces", "Chest opened: +{0} coins" },
            ["chest_next_1"] = new[] { "Coffre à la prochaine partie !", "Chest on your next run!" },
            ["chest_next_n"] = new[] { "Prochain coffre dans {0} parties", "Next chest in {0} runs" },
            ["chest_won"] = new[] { "Coffre gagné !", "Chest won!" },
            ["chest_tap"] = new[] { "Tapote le coffre pour l'ouvrir !", "Tap the chest to open it!" },
            ["chest_tap_more"] = new[] { "Encore ! Il bouge…", "Again! It's moving…" },
            ["chest_tap_last"] = new[] { "Encore une fois !", "One more time!" },
            ["chest_ok"] = new[] { "Super !", "Great!" },
            ["missions_done_n"] = new[] { "{0} missions réussies", "{0} missions complete" },
            ["run_rewards"] = new[] { "Récompenses de la partie", "Run rewards" },
            ["free_coins_title"] = new[] { "Pièces gratuites", "Free coins" },
            ["tuto_good"] = new[] { "Bravo ! À toi de jouer", "Well done! Your turn" },
            ["milestone"] = new[] { "{0} !", "{0}!" },
            ["decor_new"] = new[] { "Nouveau décor : {0}", "New scenery: {0}" },
            ["decor_0"] = new[] { "le matin", "morning" },
            ["decor_1"] = new[] { "plein soleil", "high noon" },
            ["decor_2"] = new[] { "l'heure dorée", "golden hour" },
            ["decor_3"] = new[] { "le couchant", "sunset" },
            ["decor_4"] = new[] { "la nuit étoilée", "starry night" },
            ["tip_128"] = new[] { "Un 128 s'envole : gros bonus de points, mais ta planche se rééquilibre d'un coup !", "A 128 lifts off: big point bonus, but your plank rebalances all at once!" },

            // Statistiques
            ["stats"] = new[] { "Mes statistiques", "My stats" },
            ["stat_games"] = new[] { "Parties : {0}", "Games: {0}" },
            ["stat_time"] = new[] { "Temps de jeu : {0}", "Play time: {0}" },
            ["stat_merges"] = new[] { "Fusions : {0}", "Merges: {0}" },
            ["stat_balloons"] = new[] { "128 envolés : {0}", "128s lifted: {0}" },
            ["stat_days"] = new[] { "Jours joués : {0}", "Days played: {0}" },

        };

        public static string Get(string key)
        {
            if (!Table.TryGetValue(key, out var row)) return key;
            int i = Language == "en" ? 1 : 0;
            return row[i];
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);

        /// <summary>Le jeu est publié en français uniquement (décision du porteur de projet) ; l'anglais reste écrit pour plus tard.</summary>
        public static bool FrenchOnly = true;

        public static string Resolve(string preferred, string systemLanguage)
        {
            if (FrenchOnly) return "fr";
            if (preferred == "fr" || preferred == "en") return preferred;
            return systemLanguage == "French" || systemLanguage == "fr" ? "fr" : (string.IsNullOrEmpty(systemLanguage) ? "fr" : "en");
        }

        public static bool HasKey(string key) => Table.ContainsKey(key);

        public static IEnumerable<string> Keys => Table.Keys;

        /// <summary>Texte de la condition de déblocage d'une tête.</summary>
        public static string UnlockText(HeadDef h)
        {
            switch (h.Unlock)
            {
                case UnlockKind.Score: return Format("unlock_score", FormatScore(h.UnlockValue));
                case UnlockKind.DailyStreak: return Format("unlock_streak", h.UnlockValue);
                case UnlockKind.NearMiss: return Format("unlock_nearmiss", h.UnlockValue);
                case UnlockKind.Balloon: return Get("unlock_balloon");
                case UnlockKind.Combo: return Format("unlock_combo", h.UnlockValue);
                case UnlockKind.WorldTop: return Format("unlock_world", h.UnlockValue);
                case UnlockKind.Premium: return Get(h.Product == Shop.Starter ? "unlock_starter" : "unlock_legends");
                case UnlockKind.Level: return Format("unlock_level", h.UnlockValue);
                case UnlockKind.Shop: return FormatScore(h.UnlockValue);
                default: return "";
            }
        }

        public static string MissionText(Mission m) => Format("m_" + m.kind, m.kind == MissionKind.ScoreInGame || m.kind == MissionKind.TotalScore || m.kind == MissionKind.NoHeartLost ? (object)FormatScore(m.target) : m.target);

        /// <summary>Durée courte : « 5 h 03 » ou « 12 min 04 ».</summary>
        public static string Duration(double seconds)
        {
            int s = (int)Math.Max(0, seconds);
            return s >= 3600 ? Format("hours_minutes", s / 3600, (s % 3600) / 60) : Format("minutes_seconds", s / 60, s % 60);
        }

        /// <summary>12 500 en français, 12,500 en anglais.</summary>
        public static string FormatScore(int score)
        {
            string s = score.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            return Language == "en" ? s : s.Replace(",", " ");
        }
    }
}
