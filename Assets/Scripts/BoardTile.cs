using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/BoardTile")]
public class BoardTile : Tile
{
    private Transform _content = default;
    private TextMesh _textMesh = default;

    public Transform Content { get => _content; set => _content = value; }
    public TextMesh TextMesh { get => _textMesh; set => _textMesh = value; }
}
