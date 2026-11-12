namespace Bascule.EditorTools
{
    /// <summary>
    /// Réglages des réseaux publicitaires de la médiation LevelPlay (docs/PUBS.md), utilisés au moment du build.
    /// </summary>
    public static class AdNetworkSettings
    {
        /// <summary>
        /// Identifiant d'application Google AdMob (« ca-app-pub-…~… », pas un secret). À remplir seulement si le réseau
        /// Google AdMob est activé dans LevelPlay : sans lui, le SDK de Google ferme l'application au lancement.
        /// Vide : rien n'est ajouté (et rien ne plante tant que l'adaptateur AdMob n'est pas installé).
        /// </summary>
        /// iPhone : l'app Bascule du compte AdMob d'ORPHÉE X (éditeur pub-3087780473555937). Android : vide tant que le
        /// jeu n'est pas sur Google Play (ne pas installer l'adaptateur AdMob pour un build Android avant de le remplir).
        public const string AdMobIosAppId = "ca-app-pub-3087780473555937~5965751815";
        public const string AdMobAndroidAppId = "";

        /// <summary>
        /// Identifiants SKAdNetwork (iPhone) : sans eux, un réseau ne peut pas mesurer ses pubs sur iOS 14+ et les paie
        /// beaucoup moins cher. Liste réunie à partir de celle de Google (exemples AdMob, novembre 2026) et de celle de
        /// la médiation ironSource / LevelPlay ; elle couvre Unity Ads, ironSource, AppLovin, Google, Meta, Mintegral,
        /// Liftoff (Vungle), Pangle, InMobi, Moloco, Chartboost, DT Exchange et la plupart des acheteurs de pub.
        /// Ajouter ceux d'un nouveau réseau ici (en minuscules) : les doublons sont ignorés.
        /// </summary>
        public static readonly string[] SkAdNetworkIds =
        {
            "22mmun2rn5.skadnetwork", "238da6jt44.skadnetwork", "24t9a8vw3c.skadnetwork", "252b5q8x7y.skadnetwork",
            "2fnua5tdw4.skadnetwork", "2u9pt9hc89.skadnetwork", "3qcr597p9d.skadnetwork", "3qy4746246.skadnetwork",
            "3rd42ekr43.skadnetwork", "3sh42y64q3.skadnetwork", "424m5254lk.skadnetwork", "4468km3ulz.skadnetwork",
            "44jx6755aq.skadnetwork", "44n7hlldy6.skadnetwork", "47vhws6wlr.skadnetwork", "488r3q3dtq.skadnetwork",
            "4dzt52r2t5.skadnetwork", "4fzdc2evr5.skadnetwork", "4pfyvq9l8r.skadnetwork", "578prtvx9j.skadnetwork",
            "5a6flpkh64.skadnetwork", "5l3tpt7t6e.skadnetwork", "5lm9lj6jb7.skadnetwork", "7rz58n8ntl.skadnetwork",
            "7ug5zh24hu.skadnetwork", "8c4e2ghe7u.skadnetwork", "8s468mfl3y.skadnetwork", "97r2b46745.skadnetwork",
            "9g2aggbj52.skadnetwork", "9rd848q2bz.skadnetwork", "9t245vhmpl.skadnetwork", "a2p9lx4jpn.skadnetwork",
            "av6w8kgt66.skadnetwork", "bvpn9ufa9b.skadnetwork", "c3frkrj4fj.skadnetwork", "c6k4g5qg8m.skadnetwork",
            "cg4yq2srnc.skadnetwork", "cp8zw746q7.skadnetwork", "cstr6suwn9.skadnetwork", "dzg6xy7pwj.skadnetwork",
            "e5fvkxwrpn.skadnetwork", "ecpz2srf59.skadnetwork", "ejvt5qm6ak.skadnetwork", "f38h382jlk.skadnetwork",
            "f73kdq92p3.skadnetwork", "ggvn48r87g.skadnetwork", "glqzh8vgby.skadnetwork", "gta9lk7p23.skadnetwork",
            "hdw39hrw9y.skadnetwork", "hs6bdukanm.skadnetwork", "k674qkevps.skadnetwork", "kbd757ywx3.skadnetwork",
            "kbmxgpxpgc.skadnetwork", "klf5c3l5u5.skadnetwork", "lr83yxwka7.skadnetwork", "ludvb6z3bs.skadnetwork",
            "m8dbw4sv7c.skadnetwork", "mlmmfzh3r3.skadnetwork", "mtkv5xtk9e.skadnetwork", "n38lu8286q.skadnetwork",
            "n9x2a789qt.skadnetwork", "nu4557a4je.skadnetwork", "p78axxw29g.skadnetwork", "ppxm28t8ap.skadnetwork",
            "prcb7njmu6.skadnetwork", "s39g8k73mm.skadnetwork", "su67r6k2v3.skadnetwork", "t38b2kh725.skadnetwork",
            "tl55sbb4fm.skadnetwork", "uw77j35x4d.skadnetwork", "v4nxqhlyqp.skadnetwork", "v72qych5uu.skadnetwork",
            "v79kvwwj4g.skadnetwork", "v9wttpbfk9.skadnetwork", "vutu7akeur.skadnetwork", "w9q455wk68.skadnetwork",
            "wg4vff78zm.skadnetwork", "wzmmz9fp6w.skadnetwork", "y45688jllp.skadnetwork", "y5ghdn5j9k.skadnetwork",
            "yclnxrl5pm.skadnetwork", "ydx93a7ass.skadnetwork", "zmvfpc5aq8.skadnetwork", "zq492l623r.skadnetwork",
        };
    }
}
