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
        // Set initial states for normal phasable
        SetTilemapState(phasableOn, true);
        SetTilemapState(phasableOnOutline, false);

        // Set initial states for inverted phasable
        SetTilemapState(phasableOff, false);
        SetTilemapState(phasableOffOutline, true);
    }

    private void Update()
    {
        if (InputManager.instance.inputControl.Gameplay.ShockwaveTest.WasPressedThisFrame())
        {
            ToggleAllTilemaps();
        }
    }

    private void ToggleAllTilemaps()
    {
        // Toggle normal phasable pair
        ToggleTilemapPair(phasableOn, phasableOnOutline);

        // Toggle inverted phasable pair
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
        // Set renderer state
        tilemap.GetComponent<TilemapRenderer>().enabled = state;

        // Set collider state if exists
        var collider = tilemap.GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            collider.enabled = state;
        }
    }
}