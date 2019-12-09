using MustHave.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardScreen : ScreenScript
{
    [SerializeField] private Button _optionsButton = default;
    [SerializeField] private Button _undoMovesButton = default;
    [SerializeField] private BoardTouchHandler _boardTouchHandler = default;

    private Board _board = default;

    protected override void Awake()
    {
        _board = _boardTouchHandler.Board;
        _optionsButton.onClick.AddListener(OnOptionsButtonClick);
        _undoMovesButton.onClick.AddListener(OnUndoMovesButtonClick);
    }

    private void OnOptionsButtonClick()
    {
        Canvas.AlertPopup.SetText("")
            .SetButtons(ActionWithText.Create("Reset", () => {
                _board.ResetBoardLevel();
            }), ActionWithText.Create("Back", null))
            .Show();
    }

    private void OnUndoMovesButtonClick()
    {
        _board.SetPawnsPreviousPositions();
    }
}
