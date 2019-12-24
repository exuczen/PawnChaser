using MustHave.UI;
using MustHave.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif
using UnityEngine.Tilemaps;

public class Board : MonoBehaviour
{
    [SerializeField] private BoardTilemap _tilemap = default;
    [SerializeField] private Transform _playerPawnsContainer = default;
    [SerializeField] private Transform _enemyPawnsContainer = default;

    [Header("BOARD LEVEL")]
    [SerializeField] private int _levelIndex = default;
    [SerializeField] private int _playerMovesInTurn = default;

    private BoardPathfinder _pathfinder = default;
    private EnemyHandler _enemyHandler = default;
    private bool _pawnsPositionsSaved = default;
    private List<EnemyPawn> _enemyPawns = new List<EnemyPawn>();
    private List<PlayerPawn> _playerPawns = new List<PlayerPawn>();
    private List<int> _playerMovesLeftStack = new List<int>();
    private int _playerMovesLeft = default;
    private BoardScreen _boardScreen = default;

    public BoardTilemap Tilemap { get => _tilemap; }
    public int LevelIndex { get => _levelIndex; set => _levelIndex = value; }
    public int PlayerMovesInTurn { get => _playerMovesInTurn; }
    public BoardScreen BoardScreen { get => _boardScreen; set => _boardScreen = value; }

    private void Awake()
    {
        _pathfinder = GetComponent<BoardPathfinder>();
        _enemyHandler = GetComponent<EnemyHandler>();
    }

    private void Start()
    {
        LoadLevelFromJson(_levelIndex);
    }

    public IEnumerator MovePlayerPawnRoutine(PlayerPawn pawn, Vector2Int destCell, Action onEnd = null)
    {
        yield return pawn.MoveRoutine(_tilemap, destCell);
        SetPlayerMovesLeft(Mathf.Max(_playerMovesLeft - 1, 0));
        if (_playerMovesLeft > 0)
        {
            SaveBoardState();
            onEnd?.Invoke();
        }
        else
        {
            MoveEnemyPawns(enemiesMoved => {
                SetPlayerMovesLeft(enemiesMoved ? _playerMovesInTurn : 1);
                SaveBoardState();
                onEnd?.Invoke();
            });
        }
    }

    public void MoveEnemyPawns(Action<bool> onEnd)
    {
        StartCoroutine(_enemyHandler.MoveEnemyPawnsRoutine(onEnd, _boardScreen.ShowSuccessPopup, _boardScreen.ShowFailPopup));
    }

    private void SetPlayerMovesLeft(int count)
    {
        _playerMovesLeft = count;
        if (count > 0)
            _boardScreen.SetPlayerMovesLeft(count);
    }

    public void SkipPlayerMove()
    {
        EventSystem currentEventSystem = EventSystem.current;
        currentEventSystem.enabled = false;
        MoveEnemyPawns(enemiesMoved => {
            SetPlayerMovesLeft(_playerMovesInTurn);
            if (enemiesMoved)
                SaveBoardState();
            currentEventSystem.enabled = true;
        });
    }

    public void SaveBoardState()
    {
        foreach (PlayerPawn pawn in _playerPawns)
        {
            pawn.AddCellPositionToStack(_tilemap);
        }
        foreach (EnemyPawn pawn in _enemyPawns)
        {
            pawn.AddCellPositionToStack(_tilemap);
        }
        _playerMovesLeftStack.Add(_playerMovesLeft);
        _pawnsPositionsSaved = true;
    }

    public void SetPreviousBoardState()
    {
        if (_pawnsPositionsSaved)
        {
            _pawnsPositionsSaved = false;
            _pathfinder.ClearSprites();
        }
        //int movedPlayerPawnsCount = 0;
        foreach (PlayerPawn pawn in _playerPawns)
        {
            //movedPlayerPawnsCount += pawn.SetPreviousCellPosition(_tilemap) ? 1 : 0;
            pawn.SetPreviousCellPosition(_tilemap);
        }
        foreach (EnemyPawn pawn in _enemyPawns)
        {
            pawn.SetPreviousCellPosition(_tilemap);
        }
        //Debug.Log(GetType() + ".SetPawnsPreviousPositions: " + _playerPawns[0].CellsStackCount);

        if (_playerMovesLeftStack.Count > 1)
        {
            _playerMovesLeftStack.RemoveAt(_playerMovesLeftStack.Count - 1);
            SetPlayerMovesLeft(_playerMovesLeftStack[_playerMovesLeftStack.Count - 1]);
        }
        else
        {
            SetPlayerMovesLeft(_playerMovesInTurn);
        }
        //Debug.Log(GetType() + ".SetPawnsPreviousPositions:" + _playerMovesLeft);
    }

    public void SaveLevelToJson()
    {
        BoardLevel boardLevel = new BoardLevel(this, _tilemap);
        boardLevel.SaveToJson(_levelIndex);
    }

    public void LoadLevelFromJson(int levelIndex)
    {
        BoardLevel boardLevel = _tilemap.LoadLevelFromJson(levelIndex);
        if (boardLevel != null)
        {
            OnLevelLoaded(boardLevel);
        }
    }

    private void AddPawnsToLists()
    {
        _playerPawns.Clear();
        foreach (Transform pawn in _playerPawnsContainer)
        {
            _playerPawns.Add(pawn.GetComponent<PlayerPawn>());
        }
        _enemyPawns.Clear();
        foreach (Transform pawn in _enemyPawnsContainer)
        {
            _enemyPawns.Add(pawn.GetComponent<EnemyPawn>());
        }
        _enemyHandler.AddPawnToLists(_enemyPawns, true);

        //foreach (var enemyPawn in _enemyPawns)
        //{
        //    Debug.Log(GetType() + ".AddEnemyPawnsToList: " + enemyPawn.Target);
        //}
    }

    private void OnLevelLoaded(BoardLevel boardLevel)
    {
        if (boardLevel != null)
        {
            _playerMovesInTurn = boardLevel.PlayerMovesInTurn;
        }
        SetPlayerMovesLeft(_playerMovesInTurn);
        if (EditorApplicationUtils.ApplicationIsPlaying)
        {
            _playerMovesLeftStack.Clear();
            _pathfinder.ClearSprites();
            AddPawnsToLists();
            SaveBoardState();
        }
#if UNITY_EDITOR
        else
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
#endif
    }

    public void LoadNextLevel()
    {
        LoadLevelFromJson(_levelIndex = (_levelIndex + 1) % 5);
    }

    public void ResetLevel()
    {
        LoadLevelFromJson(_levelIndex);
    }
}
