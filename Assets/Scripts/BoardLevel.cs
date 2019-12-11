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
    [SerializeField] private Vector2Int[] _enemyPawnsXY = default;
    [SerializeField] private Vector2Int[] _enemyTargetsXY = default;

    public const string FILENAME_PREFIX = "BoardLevel";
    public const string FILENAME_EXTENSION = ".json";
    public static readonly string EditorFolderPath = Path.Combine(Application.dataPath, "Resources");

    public Vector2Int[] PlayerPawnsXY { get => _playerPawnsXY; }
    public Vector2Int[] PlayerTargetsXY { get => _playerTargetsXY; }
    public Vector2Int[] EnemyPawnsXY { get => _enemyPawnsXY; }
    public Vector2Int[] EnemyTargetsXY { get => _enemyTargetsXY; }

    public BoardLevel(Board board)
    {
        List<Vector2Int> playerPawnsXY = new List<Vector2Int>();
        List<Vector2Int> playerTargetsXY = new List<Vector2Int>();
        List<Vector2Int> enemyPawnsXY = new List<Vector2Int>();
        List<Vector2Int> enemyTargetsXY = new List<Vector2Int>();
        foreach (Transform child in board.PlayerPawnsContainer)
        {
            playerPawnsXY.Add(board.Tilemap.WorldToCell(child.position));
        }
        foreach (Transform child in board.PlayerTargetsContainer)
        {
            playerTargetsXY.Add(board.Tilemap.WorldToCell(child.position));
        }
        foreach (Transform child in board.EnemyPawnsContainer)
        {
            enemyPawnsXY.Add(board.Tilemap.WorldToCell(child.position));
        }
        foreach (Transform child in board.EnemyTargetsContainer)
        {
            enemyTargetsXY.Add(board.Tilemap.WorldToCell(child.position));
        }
        _playerPawnsXY = playerPawnsXY.ToArray();
        _playerTargetsXY = playerTargetsXY.ToArray();
        _enemyPawnsXY = enemyPawnsXY.ToArray();
        _enemyTargetsXY = enemyTargetsXY.ToArray();
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
