using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(Board))]
public class BoardEditor : MonoBehaviour
{
    [SerializeField] private BoardTilemap _tilemap = default;
    [SerializeField] private BoardEditorTileContents _tileContents = default;

    public BoardTilemap Tilemap { get => _tilemap; }
    public BoardEditorTileContents TileContents { get => _tileContents; }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            Destroy(this);
        }
    }

    private void OnGUI()
    {
        Event currEvent = Event.current;
        if (currEvent.type == EventType.Layout || currEvent.type == EventType.Repaint)
        {
            EditorUtility.SetDirty(this); // this is important, if omitted, "Mouse down" will not be display
        }
        else if (currEvent.type == EventType.MouseDown)
        {
            Vector3 worldPos = _tilemap.GetTouchRayIntersection(Camera.main, currEvent.mousePosition);
            Vector2Int cell = _tilemap.WorldToCell(worldPos);
            cell.y = -cell.y - 1;
            Debug.Log(GetType() + ".OnGUI: cell:" + cell + " " + _tilemap.GetTile(cell).Content);
        }
    }
}
