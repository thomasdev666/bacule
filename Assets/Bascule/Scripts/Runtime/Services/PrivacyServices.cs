using System;
using System.Collections;
using UnityEngine;

namespace Bascule.Services
{
    /// <summary>Ce que la régie a le droit de faire pour ce joueur (Core/Privacy.cs).</summary>
    public struct AdPrivacy
    {
        /// <summary>Pubs personnalisées : 15 ans ou plus ET accord donné dans le jeu.</summary>
        public bool Personalized;
        /// <summary>Joueur de moins de 15 ans (ou âge inconnu) : traité comme un enfant par toutes les régies.</summary>
        public bool Child;

        public static AdPrivacy Safe => new AdPrivacy { Personalized = false, Child = true };
    }

    /// <summary>
    /// Demande de suivi d'iPhone (App Tracking Transparency). Sans elle, iOS ne donne pas l'identifiant publicitaire :
    /// les pubs personnalisées rapportent alors beaucoup moins. Elle n'est montrée qu'après l'accord du joueur dans
    /// le jeu (jamais à un enfant), une seule fois : iOS ne la remontre jamais, le joueur peut ensuite changer
    /// d'avis dans Réglages → Confidentialité → Suivi.
    /// </summary>
    public static class TrackingPermission
    {
#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int BasculeTrackingStatus();
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void BasculeTrackingRequest();
#endif

        /// <summary>0 = pas encore demandé, 1 = restreint, 2 = refusé, 3 = autorisé (ou sans objet hors iPhone).</summary>
        public static int Status
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                try { return BasculeTrackingStatus(); } catch (Exception) { return 3; }
#else
                return 3;
#endif
            }
        }

        public static bool CanAsk => Application.platform == RuntimePlatform.IPhonePlayer && Status == 0;

        /// <summary>Montre la demande d'iOS (si elle n'a jamais été faite) et appelle <paramref name="done"/> avec la réponse.</summary>
        public static void Request(MonoBehaviour host, Action<bool> done)
        {
            if (!CanAsk) { done?.Invoke(Status == 3); return; }
            host.StartCoroutine(Ask(done));
        }

        static IEnumerator Ask(Action<bool> done)
        {
#if UNITY_IOS && !UNITY_EDITOR
            BasculeTrackingRequest();
#endif
            // La fenêtre d'iOS ne rappelle pas Unity : on lit l'état jusqu'à la réponse (au plus 2 minutes).
            float limit = Time.realtimeSinceStartup + 120f;
            while (Status == 0 && Time.realtimeSinceStartup < limit) yield return new WaitForSecondsRealtime(0.25f);
            done?.Invoke(Status == 3);
        }
    }
}
