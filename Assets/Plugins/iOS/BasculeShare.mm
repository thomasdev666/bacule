// Feuille de partage native d'iOS (Messages, Snapchat, Instagram, TikTok, WhatsApp, Enregistrer l'image…).
// Appelée depuis C# (NativeShareService) : aucun paquet à installer.
#import <UIKit/UIKit.h>

extern UIViewController* UnityGetGLViewController(void);

extern "C" void BasculeShare(const char* text, const char* imagePath)
{
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
        while (top.presentedViewController != nil) top = top.presentedViewController;
        UIActivityViewController* vc = [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];
        // iPad : la feuille s'ouvre en bulle, ancrée au centre de l'écran.
        if (vc.popoverPresentationController != nil)
        {
            vc.popoverPresentationController.sourceView = top.view;
            vc.popoverPresentationController.sourceRect = CGRectMake(top.view.bounds.size.width / 2, top.view.bounds.size.height / 2, 1, 1);
            vc.popoverPresentationController.permittedArrowDirections = 0;
        }
        [top presentViewController:vc animated:YES completion:nil];
    });
}
