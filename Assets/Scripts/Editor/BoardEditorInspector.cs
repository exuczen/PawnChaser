using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoardEditor))]
public class BoardEditorInspector : Editor
{
    private int _selectedTileButtonIndex = 0;
    private bool _defaultInspectorEnabled = default;

    private void OnSceneGUI()
    {
        Event currEvent = Event.current;
        if (currEvent.type == EventType.MouseDown && currEvent.button == 1)
        {
            BoardTilemap tilemap = (target as BoardEditor).Tilemap;
            Vector3 worldPos = tilemap.GetTouchRayIntersection(SceneView.currentDrawingSceneView.camera, currEvent.mousePosition);
            Debug.Log(GetType() + ".OnSceneGUI: cell:" + tilemap.WorldToCell(worldPos));
        }
    }

    public override void OnInspectorGUI()
    {
        BoardEditor boardEditor = target as BoardEditor;
        BoardTilemap tilemap = boardEditor.Tilemap;
        BoardEditorTileContents tileContents = boardEditor.TileContents;
        if (tileContents)
        {
            if (tileContents.Textures == null)
                tileContents.AssignButtonTextures();
            Texture[] buttonTextures = tileContents.Textures;
            GUILayout.BeginVertical("Box");
            int prevTileButtonIndex = _selectedTileButtonIndex;
            _selectedTileButtonIndex = GUILayout.SelectionGrid(_selectedTileButtonIndex, buttonTextures, 4);
            if (prevTileButtonIndex != _selectedTileButtonIndex)
            {
                Debug.Log("You chose " + tileContents.GetTileContentType(_selectedTileButtonIndex));
            }
            if (GUILayout.Button("Assign buttons"))
            {
                tileContents.AssignButtonTextures();
            }
            GUILayout.EndVertical();
        }
        Board board = boardEditor.GetComponent<Board>();
        board.LevelIndex = EditorGUILayout.IntField("Level Index:", board.LevelIndex);
        board.PlayerMovesInTurn = EditorGUILayout.IntField("Player Moves In Turn:", board.PlayerMovesInTurn);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save board level"))
        {
            board.SaveLevelToJson();
            AssetDatabase.Refresh();
        }
        if (GUILayout.Button("Load board level"))
        {
            board.LoadLevelFromJson(board.LevelIndex);
        }
        GUILayout.EndHorizontal();
        _defaultInspectorEnabled = GUILayout.Toggle(_defaultInspectorEnabled, "Default Inspector");
        if (_defaultInspectorEnabled)
        {
            DrawDefaultInspector();
        }
    }
}
