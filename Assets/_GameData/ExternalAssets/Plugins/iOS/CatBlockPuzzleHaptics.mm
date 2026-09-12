#import <UIKit/UIKit.h>

extern "C"
{
    void CatBlockPuzzleHapticLight()
    {
        if (@available(iOS 10.0, *))
        {
            UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
            [generator prepare];
            [generator impactOccurred];
        }
    }

    void CatBlockPuzzleHapticHeavy()
    {
        if (@available(iOS 10.0, *))
        {
            UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
            [generator prepare];
            [generator impactOccurred];
        }
    }

    void CatBlockPuzzleHapticSuccess()
    {
        if (@available(iOS 10.0, *))
        {
            UINotificationFeedbackGenerator *generator = [[UINotificationFeedbackGenerator alloc] init];
            [generator prepare];
            [generator notificationOccurred:UINotificationFeedbackTypeSuccess];
        }
    }
}
