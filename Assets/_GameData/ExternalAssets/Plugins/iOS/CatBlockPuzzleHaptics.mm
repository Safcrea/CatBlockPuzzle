#import <UIKit/UIKit.h>

// Native feedback for HapticsController (iOS). Each call prepares its generator first
// so the tap is not delayed on its first use.
static void CatImpact(UIImpactFeedbackStyle style)
{
    if (@available(iOS 10.0, *))
    {
        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
        [generator prepare];
        [generator impactOccurred];
    }
}

static void CatNotify(UINotificationFeedbackType type)
{
    if (@available(iOS 10.0, *))
    {
        UINotificationFeedbackGenerator *generator = [[UINotificationFeedbackGenerator alloc] init];
        [generator prepare];
        [generator notificationOccurred:type];
    }
}

extern "C"
{
    void CatBlockPuzzleHapticLight() { CatImpact(UIImpactFeedbackStyleLight); }
    void CatBlockPuzzleHapticMedium() { CatImpact(UIImpactFeedbackStyleMedium); }
    void CatBlockPuzzleHapticHeavy() { CatImpact(UIImpactFeedbackStyleHeavy); }

    void CatBlockPuzzleHapticSoft()
    {
        if (@available(iOS 13.0, *)) CatImpact(UIImpactFeedbackStyleSoft);
        else CatImpact(UIImpactFeedbackStyleLight);
    }

    void CatBlockPuzzleHapticRigid()
    {
        if (@available(iOS 13.0, *)) CatImpact(UIImpactFeedbackStyleRigid);
        else CatImpact(UIImpactFeedbackStyleMedium);
    }

    void CatBlockPuzzleHapticSelection()
    {
        if (@available(iOS 10.0, *))
        {
            UISelectionFeedbackGenerator *generator = [[UISelectionFeedbackGenerator alloc] init];
            [generator prepare];
            [generator selectionChanged];
        }
    }

    void CatBlockPuzzleHapticSuccess() { CatNotify(UINotificationFeedbackTypeSuccess); }
    void CatBlockPuzzleHapticWarning() { CatNotify(UINotificationFeedbackTypeWarning); }
    void CatBlockPuzzleHapticError() { CatNotify(UINotificationFeedbackTypeError); }
}
