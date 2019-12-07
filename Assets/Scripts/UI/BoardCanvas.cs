using MustHave.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardCanvas : CanvasScript
{
    [SerializeField] BoardScreen _boardScreen = default;

    protected override void OnAppAwake(bool active)
    {
        ShowScreen(_boardScreen);
    }
}
