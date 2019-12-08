using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Board))]
public class BoardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Board board = target as Board;
        GUILayout.BeginHorizontal();
        GUILayout.Label("Board level index:");
        //board.LevelFileNamePrefix = GUILayout.TextField(board.LevelFileNamePrefix);
        board.LevelIndex = EditorGUILayout.IntField(board.LevelIndex);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save board level"))
        {
            board.SaveBoardLevelToJson();
            AssetDatabase.Refresh();
        }
        if (GUILayout.Button("Load board level"))
        {
            board.LoadBoardLevelFromJson(board.LevelIndex);
        }
        GUILayout.EndHorizontal();
        DrawDefaultInspector();
    }
}
