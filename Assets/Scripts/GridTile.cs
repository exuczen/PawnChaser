using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/GridTile")]
public class GridTile : Tile
{
    protected Transform _content = default;
    protected TextMesh _textMesh = default;

    public Transform Content { get => _content; set => _content = value; }
    public TextMesh TextMesh { get => _textMesh; set => _textMesh = value; }
}
