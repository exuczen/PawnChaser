using MustHave.UI;
using UnityEngine;

public class LevelsCanvas : UICanvas
{
    [SerializeField] LevelsScreen _levelsScreen = default;

    protected override void OnAppAwake(bool active)
    {
        ShowScreen(_levelsScreen);
    }
}
