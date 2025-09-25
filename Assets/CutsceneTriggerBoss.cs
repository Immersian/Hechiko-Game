using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CutsceneTriggerBoss : MonoBehaviour
{
    [Header("Cutscene Settings")]
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private BakaBossStateManager bossStateManager;

    [Header("Health Bar Settings")]
    [SerializeField] private Image healthBarFill; // Main health bar fill
    [SerializeField] private Image healthBarOutline; // Health bar outline as separate Image
    [SerializeField] private Image extraImage; // Your extra image that should fade with health bar
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float fadeDelay = 0.5f;

    [Header("Border Settings")]
    [SerializeField] private GameObject borderObject; // The border that should be enabled during cutscene
    [SerializeField] private bool disableBorderAfterCutscene = true; // Whether to disable border after cutscene

    private Color initialFillColor;
    private Color initialOutlineColor;
    private Color initialExtraImageColor;

    private void Awake()
    {
        // Store initial colors and disable visuals
        if (healthBarFill != null)
        {
            initialFillColor = healthBarFill.color;
            healthBarFill.gameObject.SetActive(false);
        }

        if (healthBarOutline != null)
        {
            initialOutlineColor = healthBarOutline.color;
            healthBarOutline.gameObject.SetActive(false);
        }

        if (extraImage != null)
        {
            initialExtraImageColor = extraImage.color;
            extraImage.gameObject.SetActive(false);
        }

        // Ensure border is disabled initially
        if (borderObject != null)
        {
            borderObject.SetActive(false);
        }
    }

    private void Start()
    {
        // Double-check that border is disabled on start
        if (borderObject != null && borderObject.activeSelf)
        {
            Debug.LogWarning("Border was enabled on start, disabling it");
            borderObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Enable border before cutscene starts
            if (borderObject != null)
            {
                borderObject.SetActive(true);
                Debug.Log("Border enabled for cutscene");
            }

            // Activate with full transparency
            if (healthBarFill != null)
            {
                healthBarFill.gameObject.SetActive(true);
                healthBarFill.color = new Color(initialFillColor.r, initialFillColor.g, initialFillColor.b, 0f);
            }

            if (healthBarOutline != null)
            {
                healthBarOutline.gameObject.SetActive(true);
                healthBarOutline.color = new Color(
                    initialOutlineColor.r,
                    initialOutlineColor.g,
                    initialOutlineColor.b,
                    0f
                );
            }

            if (extraImage != null)
            {
                extraImage.gameObject.SetActive(true);
                extraImage.color = new Color(
                    initialExtraImageColor.r,
                    initialExtraImageColor.g,
                    initialExtraImageColor.b,
                   0f
                );
            }

            playableDirector.Play();
            GetComponent<BoxCollider2D>().enabled = false;

            StartCoroutine(FadeInHealthBar());
            playableDirector.stopped += OnCutsceneFinished;
        }
    }

    private IEnumerator FadeInHealthBar()
    {
        if (healthBarFill == null && healthBarOutline == null && extraImage == null)
            yield break;

        yield return new WaitForSeconds(fadeDelay);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);

            // Fade in fill
            if (healthBarFill != null)
            {
                healthBarFill.color = new Color(
                    initialFillColor.r,
                    initialFillColor.g,
                    initialFillColor.b,
                    alpha * initialFillColor.a
                );
            }

            // Fade in outline image
            if (healthBarOutline != null)
            {
                healthBarOutline.color = new Color(
                    initialOutlineColor.r,
                    initialOutlineColor.g,
                    initialOutlineColor.b,
                    alpha * initialOutlineColor.a
                );
            }

            // Fade in extra image
            if (extraImage != null)
            {
                extraImage.color = new Color(
                    initialExtraImageColor.r,
                    initialExtraImageColor.g,
                    initialExtraImageColor.b,
                    alpha * initialExtraImageColor.a
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure full visibility
        if (healthBarFill != null)
        {
            healthBarFill.color = initialFillColor;
        }

        if (healthBarOutline != null)
        {
            healthBarOutline.color = initialOutlineColor;
        }

        if (extraImage != null)
        {
            extraImage.color = initialExtraImageColor;
        }
    }

    // CHANGED: Make this public so it can be called from other scripts
    public IEnumerator FadeOutHealthBar()
    {
        if (healthBarFill == null && healthBarOutline == null && extraImage == null)
            yield break;

        float elapsed = 0f;
        Color currentFillColor = healthBarFill != null ? healthBarFill.color : Color.clear;
        Color currentOutlineColor = healthBarOutline != null ? healthBarOutline.color : Color.clear;
        Color currentExtraColor = extraImage != null ? extraImage.color : Color.clear;

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            if (healthBarFill != null)
            {
                healthBarFill.color = new Color(
                    currentFillColor.r,
                    currentFillColor.g,
                    currentFillColor.b,
                    alpha * currentFillColor.a
                );
            }

            if (healthBarOutline != null)
            {
                healthBarOutline.color = new Color(
                    currentOutlineColor.r,
                    currentOutlineColor.g,
                    currentOutlineColor.b,
                    alpha * currentOutlineColor.a
                );
            }

            if (extraImage != null)
            {
                extraImage.color = new Color(
                    currentExtraColor.r,
                    currentExtraColor.g,
                    currentExtraColor.b,
                    alpha * currentExtraColor.a
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Disable objects after fading out
        if (healthBarFill != null) healthBarFill.gameObject.SetActive(false);
        if (healthBarOutline != null) healthBarOutline.gameObject.SetActive(false);
        if (extraImage != null) extraImage.gameObject.SetActive(false);

        Debug.Log("Health bar faded out");
    }

    // NEW: Public method to start the fade out coroutine from other scripts
    public void StartFadeOutHealthBar()
    {
        StartCoroutine(FadeOutHealthBar());
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        playableDirector.stopped -= OnCutsceneFinished;

        // Disable border after cutscene if configured to do so
        if (disableBorderAfterCutscene && borderObject != null)
        {
            borderObject.SetActive(false);
            Debug.Log("Border disabled after cutscene");
        }

        if (bossStateManager != null)
        {
            bossStateManager.SwitchState(bossStateManager.Phase1State);
        }
    }

    // Optional: Public method to manually disable border if needed
    public void DisableBorder()
    {
        if (borderObject != null && borderObject.activeSelf)
        {
            borderObject.SetActive(false);
        }
    }

    // Optional: Public method to manually enable border if needed
    public void EnableBorder()
    {
        if (borderObject != null && !borderObject.activeSelf)
        {
            borderObject.SetActive(true);
        }
    }
}