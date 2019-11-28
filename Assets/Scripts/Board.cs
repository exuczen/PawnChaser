using MustHave;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    private struct CellNode
    {
        public bool locked;
        public bool @checked;
        public int distance;
    }

    [SerializeField] private BoardTilemap _tilemap = default;
    [SerializeField] private Transform _pawnsContainer = default;
    [SerializeField] private Transform _targetsContainer = default;

    private EnemyPawnTarget _enemyTarget = default;
    private EnemyPawn _enemyPawn = default;

    public Transform PawnsContainer { get => _pawnsContainer; }
    public Transform TargetsContainer { get => _targetsContainer; }

    CellNode[] debugNodes = new CellNode[2];
    private void Start()
    {
        _enemyPawn = _pawnsContainer.GetComponentInChildren<EnemyPawn>();
        _enemyTarget = _targetsContainer.GetComponentInChildren<EnemyPawnTarget>();
    }

    private void FindPathToTarget(Pawn pawn, PawnTarget target)
    {
        Bounds2Int bounds = _tilemap.GetPawnsCellBounds();
        var nodes = new CellNode[bounds.Size.x * bounds.Size.y];
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].distance = -1;
        }
        Debug.Log(GetType() + ".FindPathToTarget: " + bounds.Min + " " + bounds.Max + " " + bounds.Size);
        foreach (Transform pawnTransform in _pawnsContainer)
        {
            Vector2Int cell = _tilemap.WorldToCell(pawnTransform.position);
            Vector2Int xy = cell - bounds.Min;
            int nodeIndex = GetCellNodeIndex(xy, bounds.Size);
            nodes[nodeIndex].locked = true;
        }

        Vector2Int begXY = _tilemap.WorldToCell(pawn.transform.position) - bounds.Min;
        Vector2Int endXY = _tilemap.WorldToCell(pawn.transform.position) - bounds.Min;

        ref CellNode begNode = ref nodes[GetCellNodeIndex(begXY, bounds.Size)];
        begNode.distance = 0;

        PathfindUpdateNgbrCellNodes(nodes, bounds.Size, begXY, endXY);
        PrintCellNodes(nodes, bounds.Size);
    }

    private int GetCellNodeIndex(int x, int y, int columns)
    {
        return x + y * columns;
    }


    private int GetCellNodeIndex(Vector2Int xy, Vector2Int size)
    {
        return xy.x + xy.y * size.x;
    }

    private bool CellNodeInBounds(Vector2Int xy, Vector2Int size)
    {
        return xy.x >= 0 && xy.x < size.x && xy.y >= 0 && xy.y < size.y;
    }

    private void PathfindUpdateNgbrCellNodes(CellNode[] nodes, Vector2Int size, Vector2Int xy, Vector2Int targetXY)
    {
        Vector2Int[] ngbrsDeltaXY = new Vector2Int[] {
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
            new Vector2Int(-1, 0), new Vector2Int(1, 0),
            new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
        };
        List<Vector2Int> ngbrsXYToCheck = new List<Vector2Int>();
        ref CellNode centerNode = ref nodes[GetCellNodeIndex(xy, size)];
        centerNode.@checked = true;
        foreach (var ngbrDeltaXY in ngbrsDeltaXY)
        {
            Vector2Int ngbrXY = xy + ngbrDeltaXY;
            if (CellNodeInBounds(ngbrXY, size))
            {
                int nodeIndex = GetCellNodeIndex(ngbrXY, size);
                ref CellNode node = ref nodes[nodeIndex];
                if (!node.locked && !node.@checked)
                {
                    ngbrsXYToCheck.Add(ngbrXY);
                    if (node.distance > 0)
                        node.distance = Mathf.Min(node.distance, centerNode.distance + 1);
                    else
                        node.distance = centerNode.distance + 1;
                    //if (ngbrXY == targetXY)
                    //{
                    //    return;
                    //}
                }
            }
        }
        foreach (var ngbrXY in ngbrsXYToCheck)
        {
            PathfindUpdateNgbrCellNodes(nodes, size, ngbrXY, targetXY);
        }
    }

    private void PrintCellNodes(CellNode[] nodes, Vector2Int size)
    {
        string s = "\n";
        for (int y = size.y - 1; y >= 0; y--)
        {
            for (int x = 0; x < size.x; x++)
            {
                CellNode node = nodes[GetCellNodeIndex(x, y, size.x)];
                int distance = node.distance;
                s += "[" + (distance < 0 ? "" : distance < 10 ? " " : "") + distance + "]";
                //bool @checked = node.@checked;
                //s += "[" + (@checked ? "x" : "_") + "]";
            }
            s += "\n";
        }
        Debug.Log(GetType() + ".PrintCellNodes: " + s);
    }


    public void FindEnemyPathToTarget()
    {
        FindPathToTarget(_enemyPawn, _enemyTarget);
    }
}
