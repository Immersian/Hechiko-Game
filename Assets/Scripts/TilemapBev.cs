using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBev : MonoBehaviour
{
    [Header("Tilemap References")]
    [SerializeField] private Tilemap solidTilemap;
    [SerializeField] private Tilemap phasedTilemap;

    public void ToggleTilemaps()
    {
        if (solidTilemap != null && phasedTilemap != null)
        {
            bool isSolidActive = solidTilemap.gameObject.activeSelf;

            // Toggle active states
            solidTilemap.gameObject.SetActive(!isSolidActive);
            phasedTilemap.gameObject.SetActive(isSolidActive);

            // Force update the tilemap (prevents rendering artifacts)
            solidTilemap.RefreshAllTiles();
            phasedTilemap.RefreshAllTiles();
        }
    }
}