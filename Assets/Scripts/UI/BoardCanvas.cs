using MustHave.UI;
using UnityEngine;

public class BoardCanvas : UICanvas
{
    [SerializeField] BoardScreen _boardScreen = default;

    protected override void OnAppAwake(bool active)
    {
        ShowScreen(_boardScreen);
    }
}
