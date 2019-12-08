using MustHave.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardScreen : ScreenScript
{
    [SerializeField] private Button _optionsButton = default;
    [SerializeField] private BoardTouchHandler _boardTouchHandler = default;

    private Board _board = default;

    protected override void Awake()
    {
        _optionsButton.onClick.AddListener(ShowMenuDialog);
        _board = _boardTouchHandler.Board;
    }

    private void ShowMenuDialog()
    {
        Canvas.AlertPopup.SetText("")
            .SetButtons(ActionWithText.Create("Reset", () => {
                _board.ResetBoardLevel();
            }), ActionWithText.Create("Back", null))
            .Show();
    }
}
