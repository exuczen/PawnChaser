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
        _board.BoardScreen = this;
        _optionsButton.onClick.AddListener(OnOptionsButtonClick);
        _undoMovesButton.onClick.AddListener(OnUndoMovesButtonClick);
    }

    private void OnOptionsButtonClick()
    {
        Canvas.AlertPopup.SetText("")
            .SetButtons(ActionWithText.Create("Reset", () => {
                _board.ResetLevel();
            }), ActionWithText.Create("Back", null))
            .Show();
    }

    private void OnUndoMovesButtonClick()
    {
        _board.SetPawnsPreviousPositions();
    }

    public void ShowFailPopup()
    {
        Canvas.AlertPopup.ShowWithConfirmButton("Fail", () => {
            _board.ResetLevel();
        });
    }

    public void ShowSuccessPopup()
    {
        Canvas.AlertPopup.SetText("Success")
            .SetButtons(
                ActionWithText.Create("Next level", () => {
                    _board.ResetLevel();
                }),
                ActionWithText.Create("Menu", () => {
                    _board.ResetLevel();
                }))
            .Show();
    }
}
