using UnityEngine;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;

public class InteractiveObject : MonoBehaviour
{
    [Header("References")]
    public CameraShake cameraShake;
    public CrossFade crossFade;
    public Animator animator;

    [Header("Settings")]
    public KeyCode activationKey = KeyCode.C;
    public float shakeIntensity = 2f;
    public float shakeDuration = 0.5f;
    public float fadeInDuration = 0.5f;
    public float fadeOutDelay = 1f; // Time screen stays black before fading out
    public float fadeOutDuration = 0.5f;

    private bool _playerInTrigger = false;
    private bool _isConquered = false;
    private GameObject _player;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !_isConquered)
        {
            _playerInTrigger = true;
            _player = other.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInTrigger = false;
            _player = null;
        }
    }

    private void Update()
    {
        if (_playerInTrigger && !_isConquered && Input.GetKeyDown(activationKey))
        {
            ActivateCheckpoint();
        }
    }

    private void ActivateCheckpoint()
    {
        _isConquered = true;

        // Trigger camera shake
        if (cameraShake != null)
        {
            cameraShake.ShakeCamera(shakeIntensity, shakeDuration);
        }

        // Play conquering animation
        if (animator != null)
        {
            animator.SetTrigger("Conquer");
        }
    }

    // Called by animation event at end of conquering animation
    public void OnConqueringComplete()
    {
        StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        // Fade in to black
        if (crossFade != null)
        {
            yield return crossFade.FadeIn(fadeInDuration);
        }

        // Switch to conquered idle animation
        if (animator != null)
        {
            animator.SetTrigger("Conquered");
        }

        // Wait while screen is black
        yield return new WaitForSeconds(fadeOutDelay);

        // Fade out from black
        if (crossFade != null)
        {
            yield return crossFade.FadeOut(fadeOutDuration);
        }
    }
}