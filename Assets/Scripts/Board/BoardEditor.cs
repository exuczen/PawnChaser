#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(Board))]
public class BoardEditor : MonoBehaviour
{
    [SerializeField] private BoardTilemap _tilemap = default;
    [SerializeField] private TileContentButtons _tileContentButtons = default;

    private int _selectedTileContentButtonIndex = 0;
    private bool enabledOnUpdate = default;

    public BoardTilemap Tilemap { get => _tilemap; }
    public TileContentButtons TileContentButtons { get => _tileContentButtons; }
    public int SelectedTileContentButtonIndex { get => _selectedTileContentButtonIndex; set => _selectedTileContentButtonIndex = value; }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            Destroy(this);
        }
    }

    private void OnEnable()
    {
        Debug.Log(GetType() + ".OnEnable: enabledOnUpdate: " + enabledOnUpdate);
        if (!enabledOnUpdate)
        {
            _tilemap.SetTilesContent();
        }
        enabledOnUpdate = false;
    }

#if UNITY_EDITOR
    private void EnableOnUpdate()
    {
        enabledOnUpdate = true;
        enabled = true;
        EditorApplication.update -= EnableOnUpdate;
    }

    private void OnGUI()
    {
        //Debug.Log(GetType() + ".OnGUI: " + isActiveAndEnabled + " " + enabled + " " + EditorApplication.timeSinceStartup + " " + Event.GetEventCount());
        if (Event.GetEventCount() <= 1)
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
                _tilemap.SetTileContent(_tileContentButtons.GetTileContentType(_selectedTileContentButtonIndex), cell);
            }
        }
        else
        {
            enabled = false;
            EditorApplication.update += EnableOnUpdate;
        }
    }
#endif
}
