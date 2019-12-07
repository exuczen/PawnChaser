using MustHave.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardScreen : ScreenScript
{
    [SerializeField] private Button _optionsButton = default;

    protected override void Awake()
    {
        _optionsButton.onClick.AddListener(() => {
            Canvas.AlertPopup.SetText("Pick your evil").Show();
        });
    }
}
