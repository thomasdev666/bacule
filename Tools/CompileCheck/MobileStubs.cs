#if UNITY_ANDROID || UNITY_IOS
// Les assemblies de référence NuGet viennent d'un player PC : Handheld.Vibrate (mobile) en est absent.
// Ce bouchon n'existe que pour la vérification hors Unity ; il n'est jamais copié dans le projet Unity.
namespace UnityEngine
{
    public static class Handheld { public static void Vibrate() { } }
}
#endif

#if UNITY_IOS
namespace UnityEngine.iOS
{
    public static class Device { public static bool RequestStoreReview() => true; }
}
#endif
