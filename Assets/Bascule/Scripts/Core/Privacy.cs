using System;
using System.Collections.Generic;

namespace Bascule.Core
{
    /// <summary>Choix du joueur pour les publicités (écran du premier lancement, modifiable dans les réglages).</summary>
    public enum AdChoice
    {
        /// <summary>Pas encore demandé.</summary>
        Unknown = 0,
        /// <summary>Pubs adaptées à ses goûts (consentement RGPD donné).</summary>
        Personalized = 1,
        /// <summary>Pubs non personnalisées (refus : le jeu reste identique, seules les pubs changent).</summary>
        Standard = 2,
    }

    /// <summary>
    /// Âge et consentement aux pubs personnalisées (RGPD, loi française, COPPA, règles d'Apple et de Google).
    /// - L'âge est demandé de façon neutre (année de naissance), une seule fois.
    /// - Moins de 15 ans (majorité numérique en France) : jamais de pubs personnalisées, jamais de demande de suivi
    ///   iPhone (ATT), et les régies sont prévenues que le joueur est un enfant.
    /// - Avec seulement l'année, on ne sait pas si l'anniversaire est passé : on compte au plus bas (prudence).
    /// - Âge inconnu : traité comme un enfant (rien de personnalisé tant que le joueur n'a pas répondu).
    /// - Refuser est aussi simple qu'accepter, et le choix se change à tout moment dans les réglages.
    /// </summary>
    public static class PrivacyRules
    {
        /// <summary>Majorité numérique en France (article 45 de la loi Informatique et Libertés).</summary>
        public const int DigitalMajority = 15;
        public const int OldestYear = 1920;

        /// <summary>Âge sûr : celui qu'on est certain d'avoir atteint cette année (anniversaire peut-être pas encore passé).</summary>
        public static int SafeAge(int birthYear, int currentYear) => birthYear <= 0 ? -1 : currentYear - birthYear - 1;

        public static bool AgeKnown(Profile p) => p != null && p.birthYear > 0;

        public static bool IsMinor(Profile p, int currentYear) => !AgeKnown(p) || SafeAge(p.birthYear, currentYear) < DigitalMajority;

        /// <summary>Faut-il encore poser une question (âge, puis pubs) ?</summary>
        public static bool NeedsAsking(Profile p, int currentYear) =>
            !AgeKnown(p) || (!IsMinor(p, currentYear) && (AdChoice)p.adChoice == AdChoice.Unknown);

        /// <summary>Pubs personnalisées autorisées : adulte numérique ET consentement donné.</summary>
        public static bool Personalized(Profile p, int currentYear) =>
            AgeKnown(p) && !IsMinor(p, currentYear) && (AdChoice)p.adChoice == AdChoice.Personalized;

        /// <summary>Les régies doivent traiter le joueur comme un enfant (pas de ciblage, pas d'identifiant).</summary>
        public static bool ChildDirected(Profile p, int currentYear) => IsMinor(p, currentYear);

        /// <summary>Années proposées, de la plus récente à la plus ancienne.</summary>
        public static List<int> Years(int currentYear)
        {
            var list = new List<int>();
            for (int y = currentYear - 4; y >= OldestYear; y--) list.Add(y);
            return list;
        }

        /// <summary>Enregistre l'année de naissance (refuse une année impossible). Un enfant n'a pas de choix de pubs à faire.</summary>
        public static bool SetBirthYear(Profile p, int year, int currentYear)
        {
            if (p == null || year < OldestYear || year > currentYear) return false;
            p.birthYear = year;
            if (IsMinor(p, currentYear)) p.adChoice = (int)AdChoice.Standard;
            return true;
        }

        public static void SetChoice(Profile p, AdChoice choice, int currentYear)
        {
            if (p == null) return;
            // Un mineur ne peut pas consentir seul : on garde des pubs non personnalisées.
            if (choice == AdChoice.Personalized && IsMinor(p, currentYear)) choice = AdChoice.Standard;
            p.adChoice = (int)choice;
        }

        public static void Sanitize(Profile p)
        {
            if (p.birthYear != 0 && p.birthYear < OldestYear) p.birthYear = 0;
            if (p.adChoice < 0 || p.adChoice > 2) p.adChoice = 0;
        }
    }
}
