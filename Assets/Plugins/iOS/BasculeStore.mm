// Version de test ou version de l'App Store ? Une app installée par TestFlight (ou par Xcode) a un reçu nommé
// « sandboxReceipt » ; une app téléchargée sur l'App Store, un reçu nommé « receipt ». Sert seulement à proposer
// « Achats de test » dans la fenêtre cachée des réglages (StoreEnvironment.IsTest, GameApp.AskClearTestPurchases) :
// les achats faits dans TestFlight ne sont pas payés et Apple ne permet pas de les effacer.
#import <Foundation/Foundation.h>

extern "C"
{
    int BasculeIsSandboxReceipt(void)
    {
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
        NSURL *url = [[NSBundle mainBundle] appStoreReceiptURL];
#pragma clang diagnostic pop
        return (url != nil && [[url lastPathComponent] isEqualToString:@"sandboxReceipt"]) ? 1 : 0;
    }
}
