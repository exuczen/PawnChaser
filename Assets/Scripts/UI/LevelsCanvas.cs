using MustHave.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class LevelsCanvas : CanvasScript
{
    [SerializeField] LevelsScreen _levelsScreen = default;

    protected override void OnAppAwake(bool active)
    {
        ShowScreen(_levelsScreen);
    }
}
