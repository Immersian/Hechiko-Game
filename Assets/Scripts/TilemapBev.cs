using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBev : MonoBehaviour
{
    [Header("Phasable Tilemaps (Normal)")]
    [SerializeField] private Tilemap phasableOn;        // Starts visible (solid)
    [SerializeField] private Tilemap phasableOnOutline; // Starts hidden

    [Header("Phasable Tilemaps (Inverted)")]
    [SerializeField] private Tilemap phasableOff;       // Starts hidden
    [SerializeField] private Tilemap phasableOffOutline;// Starts visible

    private void Start()
    {
        // Set initial states
        SetTilemapState(phasableOn, true);
        SetTilemapState(phasableOnOutline, false);
        SetTilemapState(phasableOff, false);
        SetTilemapState(phasableOffOutline, true);
    }

    public void ToggleAllTilemaps()
    {
        ToggleTilemapPair(phasableOn, phasableOnOutline);
        ToggleTilemapPair(phasableOff, phasableOffOutline);
    }

    private void ToggleTilemapPair(Tilemap primary, Tilemap outline)
    {
        bool primaryVisible = primary.GetComponent<TilemapRenderer>().enabled;
        SetTilemapState(primary, !primaryVisible);
        SetTilemapState(outline, primaryVisible);
    }

    private void SetTilemapState(Tilemap tilemap, bool state)
    {
        if (tilemap == null) return;

        var renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer != null) renderer.enabled = state;

        var collider = tilemap.GetComponent<TilemapCollider2D>();
        if (collider != null) collider.enabled = state;
    }
}