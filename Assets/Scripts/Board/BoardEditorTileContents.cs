using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MustHave.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public enum TileContentType
{
    Empty,
    PlayerPawn,
    PlayerPawnTarget,
    EnemyPawn,
    EnemyPawnTarget,
}

[Serializable]
public class TileContentButton
{
    [SerializeField] private TileContentType _contentType = default;
    [SerializeField] private Texture _texture = default;

    public Texture Texture { get => _texture; }
    public TileContentType ContentType { get => _contentType; }
}

//[CreateAssetMenu(menuName = "BoardEditor/TileContents")]
public class BoardEditorTileContents : ScriptableObject
{
    [ArrayElementTitle("_contentType")]
    [SerializeField] private TileContentButton[] _buttons = default;

    private Dictionary<Texture, TileContentType> _typesDict = default;
    private Texture[] _textures = default;

    public Texture[] Textures { get => _textures; }

    public void AssignButtonTextures()
    {
        _typesDict = new Dictionary<Texture, TileContentType>();
        _textures = new Texture[_buttons.Length];
        for (int i = 0; i < _buttons.Length; i++)
        {
            TileContentButton button = _buttons[i];
            if (button.Texture && !_typesDict.ContainsKey(button.Texture))
            {
                _typesDict.Add(button.Texture, button.ContentType);
            }
            _textures[i] = button.Texture;
        }
    }

    public TileContentType GetTileContentType(int textureIndex)
    {
        if (_textures == null || _typesDict == null)
        {
            AssignButtonTextures();
        }
        if (textureIndex >= 0 && textureIndex < _textures.Length && _typesDict.ContainsKey(_textures[textureIndex]))
        {
            return _typesDict[_textures[textureIndex]];
        }
        return default;
    }
}
