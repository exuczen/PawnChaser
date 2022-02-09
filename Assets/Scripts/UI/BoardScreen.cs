using MustHave.UI;
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

    public override bool OnBack()
    {
        Canvas.AlertPopup.SetText("Are you sure you want to quit?")
            .SetButtons(
                AlertButtonData.Create(AlertPopup.BUTTON_NO, null),
                AlertButtonData.Create(AlertPopup.BUTTON_YES, ShowLevelsScreen, false)
            ).Show();
        return false;
    }

    private void OnOptionsButtonClick()
    {
        Canvas.AlertPopup.SetText("")
            .SetButtons(
                AlertButtonData.Create("Reset", () => {
                    _board.ResetLevel();
                }),
                AlertButtonData.Create("Menu", ShowLevelsScreen, false),
                AlertButtonData.Create("Back", null)
            ).Show();
    }

    private void ShowLevelsScreen()
    {
        Canvas.ShowScreenFromOtherScene<LevelsScreen, LevelsCanvas>(SceneName.LevelsScene, false, false);
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
                AlertButtonData.Create("Next level", () => {
                    _board.LoadNextLevel();
                }),
                AlertButtonData.Create("Menu", ShowLevelsScreen)
            ).Show();
    }

    public void SetPlayerMovesLeft(int count)
    {
        _playerMovesLeftText.text = count.ToString();
    }
}
