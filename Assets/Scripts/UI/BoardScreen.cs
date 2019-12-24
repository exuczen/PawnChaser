﻿using MustHave.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardScreen : ScreenScript
{
    [SerializeField] private Button _optionsButton = default;
    [SerializeField] private Button _undoMovesButton = default;
    [SerializeField] private Button _skipMoveButton = default;
    [SerializeField] private BoardTouchHandler _boardTouchHandler = default;
    [SerializeField] private Text _playerMovesLeftText = default;

    private Board _board = default;

    protected override void Awake()
    {
        _board = _boardTouchHandler.Board;
        _board.BoardScreen = this;
        _optionsButton.onClick.AddListener(OnOptionsButtonClick);
        _undoMovesButton.onClick.AddListener(_board.SetPreviousBoardState);
        _skipMoveButton.onClick.AddListener(_board.SkipPlayerMove);
    }

    private void OnOptionsButtonClick()
    {
        Canvas.AlertPopup.SetText("")
            .SetButtons(ActionWithText.Create("Reset", () => {
                _board.ResetLevel();
            }), ActionWithText.Create("Back", null))
            .Show();
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
                    _board.LoadNextLevel();
                }),
                ActionWithText.Create("Menu", () => {
                    _board.ResetLevel();
                }))
            .Show();
    }

    public void SetPlayerMovesLeft(int count)
    {
        _playerMovesLeftText.text = count.ToString();
    }
}
