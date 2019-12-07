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

public static class BoardLevels
{
    public static readonly string FolderPath = Path.Combine(Application.dataPath, "Resources");
}

[Serializable]
public class BoardLevel
{
    [SerializeField] private Vector2Int[] _playerPawnsXY = default;
    [SerializeField] private Vector2Int[] _playerTargetsXY = default;
    [SerializeField] private Vector2Int[] _enemyPawnsXY = default;
    [SerializeField] private Vector2Int[] _enemyTargetsXY = default;

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
            PawnTarget pawnTarget = child.GetComponent<PlayerPawnTarget>();
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

    public void SaveToJson(string filename)
    {
        if (!Directory.Exists(BoardLevels.FolderPath))
        {
            Directory.CreateDirectory(BoardLevels.FolderPath);
        }
        JsonUtils.SaveToJson(this, Path.Combine(BoardLevels.FolderPath, filename));
    }
}
