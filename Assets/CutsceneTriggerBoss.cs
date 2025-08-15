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
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float fadeDelay = 0.5f;

    private Color initialFillColor;
    private Color initialOutlineColor;

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
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
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

            playableDirector.Play();
            GetComponent<BoxCollider2D>().enabled = false;

            StartCoroutine(FadeInHealthBar());
            playableDirector.stopped += OnCutsceneFinished;
        }
    }

    private IEnumerator FadeInHealthBar()
    {
        if (healthBarFill == null && healthBarOutline == null)
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
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        playableDirector.stopped -= OnCutsceneFinished;

        if (bossStateManager != null)
        {
            bossStateManager.SwitchState(bossStateManager.Phase1State);
        }
    }
}