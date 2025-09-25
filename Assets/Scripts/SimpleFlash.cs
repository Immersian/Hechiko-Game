using System.Collections;
using UnityEngine;

public class SimpleFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private float flashTime = 0.25f;

    [Header("Damage Flash")]
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private Color dashedFlashColor = Color.cyan;

    [Header("Dash Colors")]
    [SerializeField] private Color dashFlashColor = Color.white;
    [SerializeField] private Color noDashColor = new Color(0.243f, 0.749f, 0.769f); // #3ebfc4
    [SerializeField] private Color defaultColor = new Color(0.878f, 0.094f, 0.051f); // E0180D

    [Header("Material References")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private Material paletteMaterial;

    private SpriteRenderer[] spriteRenderers;
    private Material[] originalMaterials;
    private Coroutine flashCoroutine;
    private Coroutine dashColorRoutine;
    private Color currentFlashColor;

    void Start()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        StoreOriginalMaterials();
        RegularColour();
    }

    private void StoreOriginalMaterials()
    {
        originalMaterials = new Material[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalMaterials[i] = spriteRenderers[i].material;
        }
    }

    // Damage flash effect (red)
    public void CallHurtFlash()
    {
        if (flashMaterial == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        currentFlashColor = damageFlashColor;
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    public void CallDashFlash()
    {
        if (flashMaterial == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        currentFlashColor = dashedFlashColor;
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    // Dash transition (white flash)
    public void DashingTrans()
    {
        if (paletteMaterial == null) return;

        if (dashColorRoutine != null)
            StopCoroutine(dashColorRoutine);

        currentFlashColor = dashFlashColor;
        dashColorRoutine = StartCoroutine(DashFlashRoutine());
    }

    // When player can't dash
    public void NoDash()
    {
        if (paletteMaterial == null) return;
        SetPaletteColor(noDashColor);
    }

    // Return to regular color
    public void RegularColour()
    {
        if (paletteMaterial == null) return;
        SetPaletteColor(defaultColor);
    }

    private IEnumerator FlashRoutine()
    {
        if (flashMaterial == null) yield break;

        // Apply flash material to all renderers
        foreach (var renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                renderer.material = flashMaterial;
                renderer.material.SetColor("_FlashColor", currentFlashColor);
            }
        }

        // Animate flash
        float elapsedTime = 0f;
        while (elapsedTime < flashTime)
        {
            elapsedTime += Time.deltaTime;
            float flashAmount = Mathf.Lerp(1f, 0f, elapsedTime / flashTime);
            foreach (var renderer in spriteRenderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.SetFloat("_FlashAmount", flashAmount);
                }
            }
            yield return null;
        }

        // Restore original materials
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && originalMaterials[i] != null)
            {
                spriteRenderers[i].material = originalMaterials[i];
            }
        }
    }

    private IEnumerator DashFlashRoutine()
    {
        if (paletteMaterial == null) yield break;

        // Store current palette color
        Color currentColor = paletteMaterial.GetColor("_DashUsed");

        // Flash with specified color
        SetPaletteColor(currentFlashColor);
        yield return new WaitForSeconds(0.1f);

        // Restore color
        SetPaletteColor(currentColor);
    }

    private void SetPaletteColor(Color color)
    {
        if (paletteMaterial == null) return;

        // Update the palette material
        paletteMaterial.SetColor("_DashUsed", color);

        // Update all renderers using the palette material
        foreach (var renderer in spriteRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                if (renderer.material == paletteMaterial ||
                    renderer.material.HasProperty("_DashUsed"))
                {
                    renderer.material.SetColor("_DashUsed", color);
                }
            }
        }
    }
}