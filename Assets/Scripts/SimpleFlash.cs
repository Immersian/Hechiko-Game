using System.Collections;
using UnityEngine;

public class SimpleFlash : MonoBehaviour
{
    [Header("Damage Flash")]
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float flashTime = 0.25f;

    [Header("Other Flash Colors")]
    [SerializeField] private Color dashRefreshColor = Color.cyan;

    private SpriteRenderer[] spriteRenderers;
    private Material[] materials;
    private Coroutine flashCoroutine;

    void Start()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        Init();
    }

    private void Init()
    {
        materials = new Material[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            materials[i] = spriteRenderers[i].material;
        }
    }

    // Default damage flash
    public void CallDFlash()
    {
        Flash(damageFlashColor);
    }

    // New method for dash refresh flash
    public void CallDashRefreshFlash()
    {
        Flash(dashRefreshColor);
    }

    // Generic flash method
    public void Flash(Color color)
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine(color));
    }

    private IEnumerator FlashRoutine(Color color)
    {
        SetFlashColor(color);

        float currentFlashAmount = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < flashTime)
        {
            elapsedTime += Time.deltaTime;
            currentFlashAmount = Mathf.Lerp(1f, 0f, (elapsedTime / flashTime));
            SetFlashAmount(currentFlashAmount);
            yield return null;
        }
    }

    private void SetFlashColor(Color color)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetColor("_FlashColor", color);
        }
    }

    private void SetFlashAmount(float amount)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetFloat("_FlashAmount", amount);
        }
    }
}