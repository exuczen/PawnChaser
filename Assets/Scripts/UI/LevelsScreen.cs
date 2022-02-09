using MustHave.UI;
using UnityEngine;

public class LevelsScreen : ScreenScript
{
    public void ShowLevelPopup(int level)
    {
        Canvas.AlertPopup.ShowWithConfirmButton("Ready, Steady, Go!", () => {
            PlayerPrefs.SetInt(PlayerData.PLAYER_PREFS_LEVEL_INDEX, level - 1);
            Canvas.ShowScreenFromOtherScene<BoardScreen, BoardCanvas>(SceneName.MainScene, false, false);
        }, false, false);
    }
}

