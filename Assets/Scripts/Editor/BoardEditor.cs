using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(Board))]
public class BoardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        Board board = target as Board;
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
    }
}
