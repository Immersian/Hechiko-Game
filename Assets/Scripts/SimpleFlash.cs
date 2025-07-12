using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashTime = 0.25f;

    private SpriteRenderer[] spriteRenderers;
    private Material[] materials;
    private Coroutine damageFlashCoroutine;
    // Start is called before the first frame update
    void Start()
    {
        //get all them sprite renderers
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        Init();
    }

    private void Init()
    {
        materials = new Material[spriteRenderers.Length];

        //loop that assigns sprite renderer materials to materials
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            materials[i] = spriteRenderers[i].material;
        }
    }
    public void CallDFlash()
    {
        damageFlashCoroutine = StartCoroutine(DamageFlashActivate());
    }

    private IEnumerator DamageFlashActivate()
    {
        //color set
        SetFlashColor();
        //lerp flash amount
        float currentFlashAmount = 0f;
        float elapsedTime = 0f;
        //loop for how long we want flas timw to occur
        while (elapsedTime < flashTime)
        {
            elapsedTime += Time.deltaTime;
            //to lerp the slider from 1 to 0
            currentFlashAmount = Mathf.Lerp(1f, 0f, (elapsedTime / flashTime));
            SetFlashAmount(currentFlashAmount);
            yield return null;
        }
    }

    private void SetFlashColor()
    {
        //in a loop to ensure every sprite renderer gets it
        //setting color
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetColor("_FlashColor", flashColor);
        }
    }

    private void SetFlashAmount(float amount)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetFloat("_FlashAmount", amount);
        }
    }
    // Update is called once per frame
    void Update()
    {

    }
}
