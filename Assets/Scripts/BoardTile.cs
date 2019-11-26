using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/BoardTile")]
public class BoardTile : Tile
{
    private Transform _content = default;

    public Transform Content { get => _content; set => _content = value; }
}
