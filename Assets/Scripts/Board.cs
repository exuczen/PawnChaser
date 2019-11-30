using MustHave;
using MustHave.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private BoardTilemap _tilemap = default;
    [SerializeField] private Transform _pawnsContainer = default;
    [SerializeField] private Transform _targetsContainer = default;

    private EnemyPawnTarget _enemyTarget = default;
    private EnemyPawn _enemyPawn = default;
    private BoardPathfinder _pathfinder = default;

    public Transform PawnsContainer { get => _pawnsContainer; }
    public Transform TargetsContainer { get => _targetsContainer; }
    public BoardTilemap Tilemap { get => _tilemap; set => _tilemap = value; }

    private void Start()
    {
        _enemyPawn = _pawnsContainer.GetComponentInChildren<EnemyPawn>();
        _enemyTarget = _targetsContainer.GetComponentInChildren<EnemyPawnTarget>();
        _pathfinder = GetComponent<BoardPathfinder>();
    }

    public void FindEnemyPathToTarget()
    {
        _pathfinder.FindPath(_enemyPawn, _enemyTarget);
    }
}
