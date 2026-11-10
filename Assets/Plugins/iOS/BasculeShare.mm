// Feuille de partage native d'iOS (Messages, Snapchat, Instagram, TikTok, WhatsApp, Enregistrer l'image…).
// Appelée depuis C# (NativeShareService) : aucun paquet à installer.
#import <UIKit/UIKit.h>

// Fonction de Unity (UnityAppController.mm), déclarée en C : sans « extern "C" », l'édition de liens échoue
// (le fichier est compilé en Objective-C++).
extern "C" UIViewController* UnityGetGLViewController(void);

extern "C" void BasculeShare(const char* text, const char* imagePath)
{
    // Les chaînes venues de C# ne vivent que le temps de l'appel : on les copie tout de suite.
    NSMutableArray* items = [NSMutableArray array];
    if (imagePath != NULL && strlen(imagePath) > 0)
    {
        NSString* path = [NSString stringWithUTF8String:imagePath];
        if ([[NSFileManager defaultManager] fileExistsAtPath:path])
            [items addObject:[NSURL fileURLWithPath:path]];
    }
    if (text != NULL && strlen(text) > 0)
        [items addObject:[NSString stringWithUTF8String:text]];
    if (items.count == 0) return;

    dispatch_async(dispatch_get_main_queue(), ^{
        UIViewController* root = UnityGetGLViewController();
        if (root == nil) return;
        UIViewController* top = root;
        while (top.presentedViewController != nil && !top.presentedViewController.isBeingDismissed) top = top.presentedViewController;
        // Une feuille de partage est déjà ouverte (double toucher) : on n'en empile pas une seconde.
        if ([top isKindOfClass:[UIActivityViewController class]]) return;
        UIActivityViewController* vc = [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];
        // iPad : la feuille s'ouvre en bulle, ancrée au centre de l'écran.
        if (vc.popoverPresentationController != nil)
        {
            vc.popoverPresentationController.sourceView = top.view;
            vc.popoverPresentationController.sourceRect = CGRectMake(top.view.bounds.size.width / 2, top.view.bounds.size.height / 2, 1, 1);
            vc.popoverPresentationController.permittedArrowDirections = (UIPopoverArrowDirection)0;
        }
        [top presentViewController:vc animated:YES completion:nil];
    });
}
