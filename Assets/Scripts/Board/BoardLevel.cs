using MustHave.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//public enum CellContentType
//{
//    None,
//    PlayerPawn,
//    PlayerTarget,
//    EnemyPawn,
//    EnemyTarget
//}

[Serializable]
public class BoardLevel
{
    [SerializeField] private Vector2Int[] _playerPawnsXY = default;
    [SerializeField] private Vector2Int[] _playerTargetsXY = default;
    [SerializeField] private Vector2Int[] _enemyTargetsXY = default;
    [SerializeField] private EnemyPawnData[] _enemyPawnsData = default;
    [SerializeField] private int _playerMovesInTurn = 1;

    public const string FILENAME_PREFIX = "BoardLevel";
    public const string FILENAME_EXTENSION = ".json";
    public static readonly string EditorFolderPath = Path.Combine(Application.dataPath, "Resources");

    public Vector2Int[] PlayerPawnsXY { get => _playerPawnsXY; }
    public Vector2Int[] PlayerTargetsXY { get => _playerTargetsXY; }
    public Vector2Int[] EnemyTargetsXY { get => _enemyTargetsXY; }
    public EnemyPawnData[] EnemyPawnsData { get => _enemyPawnsData; }
    public int PlayerMovesInTurn { get => _playerMovesInTurn; }

    public BoardLevel(Board board, BoardTilemap tilemap)
    {
        List<Vector2Int> playerPawnsXY = new List<Vector2Int>();
        List<Vector2Int> playerTargetsXY = new List<Vector2Int>();
        List<Vector2Int> enemyTargetsXY = new List<Vector2Int>();
        List<EnemyPawnData> enemyPawnsData = new List<EnemyPawnData>();

        foreach (Transform child in tilemap.PlayerPawnsContainer)
        {
            playerPawnsXY.Add(tilemap.WorldToCell(child.position));
        }
        foreach (Transform child in tilemap.PlayerTargetsContainer)
        {
            playerTargetsXY.Add(tilemap.WorldToCell(child.position));
        }
        foreach (Transform child in tilemap.EnemyPawnsContainer)
        {
            enemyPawnsData.Add(new EnemyPawnData() {
                cell = tilemap.WorldToCell(child.position),
                targetCell = tilemap.WorldToCell(child.GetComponent<EnemyPawn>().TargetPosition)
            });
        }
        foreach (Transform child in tilemap.EnemyTargetsContainer)
        {
            enemyTargetsXY.Add(tilemap.WorldToCell(child.position));
        }
        _playerPawnsXY = playerPawnsXY.ToArray();
        _playerTargetsXY = playerTargetsXY.ToArray();
        _enemyTargetsXY = enemyTargetsXY.ToArray();
        _enemyPawnsData = enemyPawnsData.ToArray();
        _playerMovesInTurn = board.PlayerMovesInTurn;
    }

    public static string GetEditorFilePath(int levelIndex)
    {
        return Path.Combine(EditorFolderPath, GetFileName(levelIndex));
    }

    public static string GetFileName(int levelIndex)
    {
        return FILENAME_PREFIX + levelIndex + FILENAME_EXTENSION;
    }

    public void SaveToJson(int levelIndex)
    {
        if (!Directory.Exists(EditorFolderPath))
        {
            Directory.CreateDirectory(EditorFolderPath);
        }
        JsonUtils.SaveToJson(this, GetEditorFilePath(levelIndex));
    }

    public static BoardLevel LoadFromJson(int levelIndex)
    {
        return JsonUtils.LoadFromJsonFromResources<BoardLevel>(GetFileName(levelIndex));
    }
}
