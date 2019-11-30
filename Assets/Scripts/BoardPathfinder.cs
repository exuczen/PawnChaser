//#define DEBUG_DISTANCE_INT

using MustHave;
using MustHave.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardPathfinder : MonoBehaviour
{
    [SerializeField] private Transform _tileTextsContainer = default;
    [SerializeField] private TextMesh _tileTextMeshPrefab = default;

    private readonly float SQRT2 = Mathf.Sqrt(2f);

    private Transform _pawnsContainer = default;
    private BoardTilemap _tilemap = default;
    //private Coroutine _pathfindingRoutine = default;

    private struct CellNode
    {
        public bool locked;
        public bool @checked;
        public float distance;
    }

    private void Awake()
    {
        Board board = GetComponentInParent<Board>();
        _pawnsContainer = board.PawnsContainer;
        _tilemap = board.Tilemap;
    }

    public void FindPath(Pawn pawn, PawnTarget target)
    {
        Bounds2Int bounds = _tilemap.GetPawnsCellBounds();
        var nodes = new CellNode[bounds.Size.x * bounds.Size.y];
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].distance = float.MaxValue;
        }
        foreach (Transform pawnTransform in _pawnsContainer)
        {
            Vector2Int cell = _tilemap.WorldToCell(pawnTransform.position);
            Vector2Int xy = cell - bounds.Min;
            int nodeIndex = GetCellNodeIndex(xy, bounds.Size);
            nodes[nodeIndex].locked = true;
        }
        Vector2Int begXY = _tilemap.WorldToCell(pawn.transform.position) - bounds.Min;
        Vector2Int endXY = _tilemap.WorldToCell(target.transform.position) - bounds.Min;

        //Debug.Log(GetType() + ".FindPathToTarget: bounds: " + bounds.Min + " " + bounds.Max + " " + bounds.Size + " ");
        Debug.Log(GetType() + ".FindPathToTarget: " + begXY + "->" + endXY);

        ref CellNode begNode = ref nodes[GetCellNodeIndex(begXY, bounds.Size)];
        begNode.distance = 0f;

        _tileTextsContainer.transform.DestroyAllChildren();
        //if (_pathfindingRoutine != null)
        //{
        //    StopCoroutine(_pathfindingRoutine);
        //    ClearTilesMeshTexts(bounds);
        //}
        //_pathfindingRoutine = StartCoroutine(UpdateCellNodesRoutine(nodes, bounds, new HashSet<Vector2Int> { begXY }, endXY));

        HashSet<Vector2Int> nodesXY = new HashSet<Vector2Int>() { begXY };
        while (nodesXY.Count > 0)
        {
            nodesXY = UpdateCellNodes(nodes, bounds, nodesXY, endXY);
        }
        ClearTilesMeshTexts(bounds);
    }

    private HashSet<Vector2Int> UpdateCellNodes(CellNode[] nodes, Bounds2Int bounds, HashSet<Vector2Int> nodesXY, Vector2Int targetXY)
    {
        Vector2Int[] ngbrsDeltaXY = new Vector2Int[] {
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
            new Vector2Int(-1, 0), new Vector2Int(1, 0),
            new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
        };
        HashSet<Vector2Int> nextNodesXY = new HashSet<Vector2Int>();

        foreach (var nodeXY in nodesXY)
        {
            int nodeIndex = GetCellNodeIndex(nodeXY, bounds.Size);
            ref CellNode node = ref nodes[nodeIndex];
            node.@checked = true;
            nextNodesXY.Remove(nodeXY);
            foreach (var ngbrDeltaXY in ngbrsDeltaXY)
            {
                Vector2Int ngbrXY = nodeXY + ngbrDeltaXY;
                if (CellNodeInBounds(ngbrXY, bounds.Size))
                {
                    int ngbrNodeIndex = GetCellNodeIndex(ngbrXY, bounds.Size);
                    ref CellNode ngbrNode = ref nodes[ngbrNodeIndex];
                    if (!ngbrNode.locked && !ngbrNode.@checked)
                    {
                        if (!nextNodesXY.Contains(ngbrXY))
                        {
                            nextNodesXY.Add(ngbrXY);
                        }
#if DEBUG_DISTANCE_INT
                        float delta = 1f;
#else
                        float delta = ngbrDeltaXY.x == 0 || ngbrDeltaXY.y == 0 ? 1f : SQRT2;
#endif
                        ngbrNode.distance = Mathf.Min(ngbrNode.distance, node.distance + delta);

                        SetTileMeshText(ngbrXY + bounds.Min, ngbrNode.distance.ToString("F1"));

                        if (ngbrXY == targetXY)
                        {
                            nodesXY.Clear();
                            return nodesXY;
                        }
                    }
                }
            }
        }
        return nextNodesXY;
    }

    private IEnumerator UpdateCellNodesRoutine(CellNode[] nodes, Bounds2Int bounds, HashSet<Vector2Int> nodesXY, Vector2Int targetXY)
    {
        HashSet<Vector2Int> nextNodesXY = UpdateCellNodes(nodes, bounds, nodesXY, targetXY);
        if (nextNodesXY.Count > 0)
        {
            //yield return new WaitForSeconds(0.05f);
            yield return UpdateCellNodesRoutine(nodes, bounds, nextNodesXY, targetXY);
        }
        else
        {
            ClearTilesMeshTexts(bounds);
        }
    }

    private void SetTileMeshText(Vector2Int cell, string text)
    {
        BoardTile tile = _tilemap.GetTile(cell);
        Vector3 tilePos = _tilemap.GetCellCenterWorld(cell) - Vector3.forward;
        if (tile)
        {
            if (!tile.TextMesh)
            {
                tile.TextMesh = Instantiate(_tileTextMeshPrefab, tilePos, Quaternion.identity, _tileTextsContainer);
            }
            tile.TextMesh.text = text;
        }
    }

    private void ClearTilesMeshTexts(Bounds2Int bounds)
    {
        for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
        {
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            {
                BoardTile tile = _tilemap.GetTile(new Vector2Int(x, y));
                if (tile)
                    tile.TextMesh = null;
            }
        }
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
}
