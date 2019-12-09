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
        foreach (Transform child in board.PawnsContainer)
        {
            Pawn pawn = child.GetComponent<Pawn>();
            List<Vector2Int> pawnsListXY = null;
            if (pawn is PlayerPawn)
            {
                pawnsListXY = playerPawnsXY;
            }
            else if (pawn is EnemyPawn)
            {
                pawnsListXY = enemyPawnsXY;
            }
            if (pawnsListXY != null)
                pawnsListXY.Add(board.Tilemap.WorldToCell(child.position));
        }
        foreach (Transform child in board.TargetsContainer)
        {
            PawnTarget pawnTarget = child.GetComponent<PawnTarget>();
            List<Vector2Int> targetsListXY = null;
            if (pawnTarget is PlayerPawnTarget)
            {
                targetsListXY = playerTargetsXY;
            }
            else if (pawnTarget is EnemyPawnTarget)
            {
                targetsListXY = enemyTargetsXY;
            }
            if (targetsListXY != null)
                targetsListXY.Add(board.Tilemap.WorldToCell(child.position));
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
