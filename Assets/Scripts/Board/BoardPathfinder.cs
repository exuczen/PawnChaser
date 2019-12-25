//#define DEBUG_SHOW_CELL_ENTER_RISK
//#define DEBUG_SHOW_DISTANCES
//#define DEBUG_DISTANCE_INT

using MustHave;
using MustHave.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathResult : Tuple<bool, List<Vector2Int>>
{
    public PathResult(bool item1, List<Vector2Int> item2) : base(item1, item2) { }

    public bool PathFound => Item1;
    public List<Vector2Int> Path => Item2;
}

public class BoardPathfinder : MonoBehaviour
{
    [SerializeField] private Transform _tileTextsContainer = default;
    [SerializeField] private TextMesh _tileTextMeshPrefab = default;
    [SerializeField] private Transform _pathSpritesContainer = default;
    [SerializeField] private SpriteRenderer _pathSpritePrefab = default;

    private readonly float SQRT2 = Mathf.Sqrt(2f);

    private BoardTilemap _tilemap = default;
    private bool _cellNodesEnterRisk = default;

    private static readonly Vector2Int[] _cellNgbrsDeltaXY = new Vector2Int[]
    {
        new Vector2Int(-1, 0), new Vector2Int(1, 0),new Vector2Int(0, -1), new Vector2Int(0, 1),
        new Vector2Int(-1, -1),  new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(1, 1)
    };

    public bool CellNodesEnterRisk { get => _cellNodesEnterRisk; set => _cellNodesEnterRisk = value; }

    private class CellNode
    {
        public bool locked = false;
        public bool @checked = false;
        public float distance = float.MaxValue;
        public bool[] ngbrsEnterRisk = new bool[8];

        public bool GetNgbrEnterRisk(int ngbrIndex)
        {
            return ngbrIndex >= 0 && ngbrIndex < 8 ? ngbrsEnterRisk[ngbrIndex] : false;
        }
    }

    private void Awake()
    {
        Board board = GetComponent<Board>();
        _tilemap = board?.Tilemap;
    }

    public void ClearSprites()
    {
        _tileTextsContainer.transform.DestroyAllChildren();
        _pathSpritesContainer.DestroyAllChildren();
    }

    private IEnumerator FindPathRoutine(Vector2Int begXY, Vector2Int endXY, Bounds2Int bounds, Action<PathResult> onEnd,
        List<PawnTransition> pawnTransitions, params Transform[] contentContainers)
    {
        if (begXY == endXY)
        {
            onEnd?.Invoke(new PathResult(true, new List<Vector2Int>()));
        }
        else
        {
            _tileTextsContainer.transform.DestroyAllChildren();
            begXY -= bounds.Min;
            endXY -= bounds.Min;
            var nodes = CreateCellNodes(begXY, endXY, bounds, pawnTransitions, contentContainers);

            HashSet<Vector2Int> nodesXY = new HashSet<Vector2Int>() { begXY };
            yield return UpdateCellNodesRoutine(nodes, bounds, begXY, endXY, targetReached => {
                ClearTilesMeshTexts(bounds);
                onEnd?.Invoke(new PathResult(targetReached, CreatePath(nodes, bounds, begXY, endXY)));
            });
        }
    }

    private PathResult FindPath(Vector2Int begXY, Vector2Int endXY, Bounds2Int bounds, List<PawnTransition> pawnTransitions, params Transform[] contentContainers)
    {
        if (begXY == endXY)
        {
            return new PathResult(true, new List<Vector2Int>());
        }
        else
        {
            _tileTextsContainer.transform.DestroyAllChildren();
            begXY -= bounds.Min;
            endXY -= bounds.Min;
            //Debug.Log(GetType() + ".FindPathToTarget: bounds: " + bounds.Min + " " + bounds.Max + " " + bounds.Size + " ");
            //Debug.Log(GetType() + ".FindPathToTarget: " + begXY + "->" + endXY);
            var nodes = CreateCellNodes(begXY, endXY, bounds, pawnTransitions, contentContainers);

            HashSet<Vector2Int> nodesXY = new HashSet<Vector2Int>() { begXY };
            bool targetReached = false;
            while (nodesXY.Count > 0)
            {
                nodesXY = UpdateCellNodes(nodes, bounds, nodesXY, endXY, out targetReached);
            }
            ClearTilesMeshTexts(bounds);
            return new PathResult(targetReached, CreatePath(nodes, bounds, begXY, endXY));
        }
    }

    public IEnumerator FindPathRoutine(TileContent begTileContent, TileContent endTileContent, Action<PathResult> onEnd,
        List<PawnTransition> pawnTransitions, params Transform[] contentContainers)
    {
        Vector2Int begXY = _tilemap.WorldToCell(begTileContent.transform.position);
        Vector2Int endXY = _tilemap.WorldToCell(endTileContent.transform.position);
        Bounds2Int bounds = _tilemap.GetTilesContentCellBounds(begXY, endXY, contentContainers);
        yield return FindPathRoutine(begXY, endXY, bounds, onEnd, pawnTransitions, contentContainers);
    }

    public PathResult FindPath(TileContent begTileContent, TileContent endTileContent,
        List<PawnTransition> pawnTransitions, params Transform[] contentContainers)
    {
        Vector2Int begXY = _tilemap.WorldToCell(begTileContent.transform.position);
        Vector2Int endXY = _tilemap.WorldToCell(endTileContent.transform.position);
        Bounds2Int bounds = _tilemap.GetTilesContentCellBounds(begXY, endXY, contentContainers);
        return FindPath(begXY, endXY, bounds, pawnTransitions, contentContainers);
    }

    public PathResult FindPathToBoundsMin(TileContent begTileContent, params Transform[] contentContainers)
    {
        _cellNodesEnterRisk = false;
        Vector2Int begXY = _tilemap.WorldToCell(begTileContent.transform.position);
        Bounds2Int bounds = _tilemap.GetTilesContentCellBounds(begXY, contentContainers);
        Vector2Int endXY = bounds.Min;
        return FindPath(begXY, endXY, bounds, null, contentContainers);
    }

    private List<Vector2Int> CreatePath(CellNode[] nodes, Bounds2Int bounds, Vector2Int begXY, Vector2Int endXY)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        CellNode endNode = nodes[GetCellNodeIndex(endXY, bounds.Size)];
        if (begXY != endXY && endNode.distance < float.MaxValue / 2f)
        {
            //Debug.Log(GetType() + ".GetPath: " + begXY + "->" + endXY);
            CellNode begNode = nodes[GetCellNodeIndex(begXY, bounds.Size)];
            bool begNodeLocked = begNode.locked;
            begNode.locked = false;

            Vector2Int nodeXY = endXY;
            path.Add(nodeXY);
            while (nodeXY != begXY)
            {
                nodeXY = GetMinDistanceNgbrCellNodeXY(nodes, nodeXY, bounds);
                path.Add(nodeXY);
            }
            if (path.Count > 1)
                path.RemoveAt(path.Count - 1);
            for (int i = 0; i < path.Count; i++)
            {
                path[i] += bounds.Min;
                //Debug.Log(GetType() + ".GetPath: " + path[i]);
            }
            begNode.locked = begNodeLocked;
        }
        return path;
    }

    private CellNode[] CreateCellNodes(Vector2Int begXY, Vector2Int endXY, Bounds2Int bounds, List<PawnTransition> pawnTransitions, params Transform[] contentContainers)
    {
        var nodes = new CellNode[bounds.Size.x * bounds.Size.y];
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i] = new CellNode();
        }
        foreach (Transform container in contentContainers)
        {
            foreach (Transform pawnTransform in container)
            {
                Vector2Int cell = _tilemap.WorldToCell(pawnTransform.position);
                int nodeIndex = GetCellNodeIndex(cell - bounds.Min, bounds.Size);
                nodes[nodeIndex].locked = true;
            }
        }
        if (pawnTransitions != null)
        {
            int nodeIndex;
            foreach (var pawnTransition in pawnTransitions)
            {
                nodeIndex = GetCellNodeIndex(pawnTransition.EndCell - bounds.Min, bounds.Size);
                nodes[nodeIndex].locked = true;
                nodeIndex = GetCellNodeIndex(pawnTransition.BegCell - bounds.Min, bounds.Size);
                nodes[nodeIndex].locked = false;
            }
        }
        CellNode begNode = nodes[GetCellNodeIndex(begXY, bounds.Size)];
        CellNode endNode = nodes[GetCellNodeIndex(endXY, bounds.Size)];
        begNode.distance = 0f;
        begNode.locked = false;
        endNode.locked = false;

        if (_cellNodesEnterRisk)
            SetCellNodesEnterRisks(nodes, bounds);

        return nodes;
    }

    private void SetCellNoneScndaryEnterRisks(int x, int y, CellNode cellNode, CellNode[] nodes, int columns)
    {
    }

    private void SetCellNonePrimaryEnterRisks(int x, int y, CellNode cellNode, CellNode[] nodes, int columns)
    {
        bool[] ngbrsEnterRisk = cellNode.ngbrsEnterRisk;
        for (int i = 0; i < 4; i++)
        {
            Vector2Int ngbrDeltaXY = _cellNgbrsDeltaXY[i];
            ngbrDeltaXY.x = -ngbrDeltaXY.x;
            ngbrDeltaXY.y = -ngbrDeltaXY.y;
            int dx = ngbrDeltaXY.x;
            int dy = ngbrDeltaXY.y;
            int absdx = Mathf.Abs(ngbrDeltaXY.x);
            int absdy = Mathf.Abs(ngbrDeltaXY.y);
            int lockedCount = 0;
            if (nodes[GetCellNodeIndex(x - absdy, y - absdx, columns)].locked &&
                nodes[GetCellNodeIndex(x + absdy, y + absdx, columns)].locked)
            {
                lockedCount = 2;
                if (absdx > absdy)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        lockedCount += nodes[GetCellNodeIndex(x - dx, y + j, columns)].locked ? 1 : 0;
                    }
                }
                else
                {
                    for (int j = -1; j < 2; j++)
                    {
                        lockedCount += nodes[GetCellNodeIndex(x + j, y - dy, columns)].locked ? 1 : 0;
                    }
                }
            }
            ngbrsEnterRisk[i] = lockedCount >= 4;
        }
        ngbrsEnterRisk[4] = ngbrsEnterRisk[0] || ngbrsEnterRisk[2];
        ngbrsEnterRisk[5] = ngbrsEnterRisk[1] || ngbrsEnterRisk[2];
        ngbrsEnterRisk[6] = ngbrsEnterRisk[0] || ngbrsEnterRisk[3];
        ngbrsEnterRisk[7] = ngbrsEnterRisk[1] || ngbrsEnterRisk[3];
    }

    private void SetCellNodesEnterRisks(CellNode[] nodes, Bounds2Int bounds)
    {
        Vector2Int size = bounds.Size;
        int columns = bounds.Size.x;
        for (int y = 1; y < size.y - 1; y++)
        {
            for (int x = 1; x < size.x - 1; x++)
            {
                CellNode cellNode = nodes[GetCellNodeIndex(x, y, columns)];
                if (!cellNode.locked)
                {
                    SetCellNonePrimaryEnterRisks(x, y, cellNode, nodes, columns);
                    SetCellNoneScndaryEnterRisks(x, y, cellNode, nodes, columns);
                }
#if DEBUG_SHOW_CELL_ENTER_RISK
                bool[] ngbrsEnterRisk = cellNode.ngbrsEnterRisk;
                Vector2Int cell = new Vector2Int(x + bounds.Min.x, y + bounds.Min.y);
                int[] risks = new int[ngbrsEnterRisk.Length];
                for (int i = 0; i < ngbrsEnterRisk.Length; i++)
                {
                    risks[i] = ngbrsEnterRisk[i] ? 1 : 0;
                }
                string risksText = risks[5] + "" + risks[2] + "" + risks[4] + "\n";
                risksText += risks[1] + "0" + risks[0] + "\n";
                risksText += risks[7] + "" + risks[3] + "" + risks[6];
                SetTileMeshText(cell, risksText);
#endif
            }
        }
    }

    private HashSet<Vector2Int> UpdateCellNodes(CellNode[] nodes, Bounds2Int bounds, HashSet<Vector2Int> nodesXY, Vector2Int targetXY, out bool targetReached)
    {
        HashSet<Vector2Int> nextNodesXY = new HashSet<Vector2Int>();
        targetReached = false;
        foreach (var nodeXY in nodesXY)
        {
            int nodeIndex = GetCellNodeIndex(nodeXY, bounds.Size);
            CellNode node = nodes[nodeIndex];
            node.@checked = true;
            nextNodesXY.Remove(nodeXY);
            for (int i = 0; i < _cellNgbrsDeltaXY.Length; i++)
            {
                Vector2Int ngbrDeltaXY = _cellNgbrsDeltaXY[i];
                Vector2Int ngbrXY = nodeXY + ngbrDeltaXY;
                if (CellNodeInBounds(ngbrXY, bounds.Size))
                {
                    int ngbrNodeIndex = GetCellNodeIndex(ngbrXY, bounds.Size);
                    CellNode ngbrNode = nodes[ngbrNodeIndex];
                    bool ngbrEnterRisk = _cellNodesEnterRisk && ngbrNode.GetNgbrEnterRisk(i);
                    if (!ngbrNode.locked && !ngbrEnterRisk && !ngbrNode.@checked)
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
#if DEBUG_SHOW_DISTANCES
                        SetTileMeshText(ngbrXY + bounds.Min, ngbrNode.distance.ToString("F1"));
#endif
                        if (ngbrXY == targetXY)
                        {
                            targetReached = true;
                            nextNodesXY.Clear();
                            return nextNodesXY;
                        }
                    }
                }
            }
        }
        return nextNodesXY;
    }

    private IEnumerator UpdateCellNodesRoutine(CellNode[] nodes, Bounds2Int bounds, Vector2Int begXY, Vector2Int endXY, Action<bool> onEnd)
    {
        HashSet<Vector2Int> nodesXY = new HashSet<Vector2Int> { begXY };
        bool targetReached = false;
        while (nodesXY.Count > 0)
        {
            nodesXY = UpdateCellNodes(nodes, bounds, nodesXY, endXY, out targetReached);
            yield return new WaitForSeconds(0.05f);
        }
        onEnd(targetReached);
    }

    private Vector2Int GetMinDistanceNgbrCellNodeXY(CellNode[] nodes, Vector2Int nodeXY, Bounds2Int bounds)
    {
        float minDistance = float.MaxValue;
        Vector2Int minDistNgbrXY = default;
        foreach (var ngbrDeltaXY in _cellNgbrsDeltaXY)
        {
            Vector2Int ngbrXY = nodeXY + ngbrDeltaXY;
            if (CellNodeInBounds(ngbrXY, bounds.Size))
            {
                int ngbrNodeIndex = GetCellNodeIndex(ngbrXY, bounds.Size);
                CellNode ngbrNode = nodes[ngbrNodeIndex];
                if (!ngbrNode.locked && ngbrNode.distance < minDistance)
                {
                    minDistance = ngbrNode.distance;
                    minDistNgbrXY = ngbrXY;
                }
            }
        }
        //Debug.Log(GetType() + ".GetMinDistanceCellNodeNgbrXY: " + minDistNgbrXY);
        return minDistNgbrXY;
    }

    public List<SpriteRenderer> CreatePathSprites(List<Vector2Int> path, int begOffset, int endOffset)
    {
        List<SpriteRenderer> sprites = new List<SpriteRenderer>();
        int beg = begOffset;
        int end = path.Count - endOffset - 1;
        for (int i = beg; i <= end; i++)
        {
            SpriteRenderer sprite = Instantiate(_pathSpritePrefab, _tilemap.GetCellCenterWorld(path[i]), Quaternion.identity, _pathSpritesContainer);
            sprites.Add(sprite);
        }
        return sprites;
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

    //private ref CellNode GetCollNodeRef(CellNode[] nodes, Vector2Int xy, Bounds2Int bounds)
    //{
    //    int nodeIndex = GetCellNodeIndex(xy, bounds.Size);
    //    return ref nodes[nodeIndex];
    //}

    private int GetCellNodeIndex(int x, int y, int columns)
    {
        return x + y * columns;
    }

    private int GetCellNodeIndex(Vector2Int cell, Vector2Int size)
    {
        return cell.x + cell.y * size.x;
    }

    private bool CellNodeInBounds(Vector2Int cell, Vector2Int size)
    {
        return cell.x >= 0 && cell.x < size.x && cell.y >= 0 && cell.y < size.y;
    }
}
