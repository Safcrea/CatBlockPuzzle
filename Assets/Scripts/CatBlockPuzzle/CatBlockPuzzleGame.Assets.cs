using System;
using UnityEngine;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private void LoadBakedUiAssets()
        {
            if (uiAssets == null)
                uiAssets = Resources.Load<CatPuzzleUiAssets>("CatBlockPuzzle/AuthoredUI/UiAssets");
            if (uiAssets == null || uiAssets.Portraits.Length != 24 || uiAssets.Icons.Length != 6 || uiAssets.Sounds.Length != 4)
                throw new InvalidOperationException("Missing or incomplete authored UI assets. Bake them in Edit Mode.");
            whiteSprite = uiAssets.White;
            roundedBoxSprite = uiAssets.RoundedBox;
            circleSprite = uiAssets.Circle;
            coinSprite = uiAssets.Coin;
            catHeadSprite = uiAssets.CatHead;
            mouthSprite = uiAssets.Mouth;
            tailSprite = uiAssets.Tail;
            pawSprite = uiAssets.Paw;
            starSprite = uiAssets.Star;
            starOutlineSprite = uiAssets.StarOutline;
            backIconSprite = uiAssets.Icons[0];
            pauseIconSprite = uiAssets.Icons[1];
            settingsIconSprite = uiAssets.Icons[2];
            hintIconSprite = uiAssets.Icons[3];
            resetIconSprite = uiAssets.Icons[4];
            closeIconSprite = uiAssets.Icons[5];
            buttonClip = uiAssets.Sounds[0];
            snapClip = uiAssets.Sounds[1];
            wrongClip = uiAssets.Sounds[2];
            winClip = uiAssets.Sounds[3];
            for (int mood = 0; mood < 3; mood++)
                for (int i = 0; i < 8; i++)
                    catPortraitSprites[mood, i] = uiAssets.Portraits[mood * 8 + i];
        }
    }
}
