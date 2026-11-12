// Demande de suivi d'iPhone (App Tracking Transparency), pour les pubs personnalisées.
// Montrée seulement à un joueur de 15 ans ou plus qui a d'abord accepté les pubs personnalisées dans le jeu
// (Core/Privacy.cs). Le jeu lit ensuite la réponse avec BasculeTrackingStatus (0 = pas encore demandé,
// 1 = restreint, 2 = refusé, 3 = autorisé), sans rappel vers Unity : il interroge l'état à chaque image.
#import <Foundation/Foundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

extern "C"
{
    int BasculeTrackingStatus(void)
    {
        if (@available(iOS 14, *)) return (int)[ATTrackingManager trackingAuthorizationStatus];
        return 3; // avant iOS 14 : pas de demande, l'identifiant suit le réglage « Suivi publicitaire limité »
    }

    void BasculeTrackingRequest(void)
    {
        if (@available(iOS 14, *))
        {
            dispatch_async(dispatch_get_main_queue(), ^{
                [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {}];
            });
        }
    }
}
