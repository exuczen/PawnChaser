using MustHave.UI;
using UnityEngine;

public class LevelsCanvas : CanvasScript
{
    [SerializeField] LevelsScreen _levelsScreen = default;

    protected override void OnAppAwake(bool active)
    {
        ShowScreen(_levelsScreen);
    }
}
