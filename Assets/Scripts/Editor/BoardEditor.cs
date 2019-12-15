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
        Board board = target as Board;
        GUILayout.BeginHorizontal();
        GUILayout.Label("Board level index:");
        //board.LevelFileNamePrefix = GUILayout.TextField(board.LevelFileNamePrefix);
        int prevLevelIndex = board.LevelIndex;
        board.LevelIndex = EditorGUILayout.IntField(board.LevelIndex);
        GUILayout.EndHorizontal();
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
        DrawDefaultInspector();
        if (!EditorApplication.isPlaying)
        {
            bool dirty = false;
            dirty |= prevLevelIndex != board.LevelIndex;
            if (dirty)
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
