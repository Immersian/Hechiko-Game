using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterTriggerHandler : MonoBehaviour
{
    [SerializeField] private LayerMask _waterMask;
    [SerializeField] private GameObject _splashParticles;
    [SerializeField] private AudioClip[] _splashSounds; // Array of splash sound effects
    [SerializeField] private float _minPitch = 0.85f;    // Minimum pitch variation
    [SerializeField] private float _maxPitch = 1.15f;    // Maximum pitch variation

    private EdgeCollider2D _edgeColl;
    private InteractableWater _water;
    private AudioSource _audioSource;

    private void Awake()
    {
        _edgeColl = GetComponent<EdgeCollider2D>();
        _water = GetComponent<InteractableWater>();

        // Try to get AudioSource on this GameObject, or add one if it doesn't exist
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((_waterMask.value & (1 << collision.gameObject.layer)) > 0)
        {
            Rigidbody2D rb = collision.GetComponentInParent<Rigidbody2D>();
            if (rb != null)
            {
                // Find the highest point of the water surface from the EdgeCollider2D
                float highestPointY = float.MinValue;
                foreach (Vector2 point in _edgeColl.points)
                {
                    Vector2 worldPoint = transform.TransformPoint(point + _edgeColl.offset);
                    if (worldPoint.y > highestPointY)
                    {
                        highestPointY = worldPoint.y;
                    }
                }

                // Apply Y offset to position the splash perfectly at the surface
                Vector3 spawnPos = new Vector3(collision.transform.position.x,
                                              highestPointY + _water.SplashYOffset,
                                              0f);

                Instantiate(_splashParticles, spawnPos, Quaternion.identity);

                // Play random splash sound
                PlaySplashSound();

                int multiplier = 1;
                if (rb.velocity.y < 0)
                {
                    multiplier = -1;
                }
                else { multiplier = 1; }

                float vel = rb.velocity.y * _water.ForceMultiplier;
                vel = Mathf.Clamp(Mathf.Abs(vel), 0f, _water.MaxForce);
                vel *= multiplier;

                _water.Splash(collision, vel);
            }
        }
    }

    private void PlaySplashSound()
    {
        // Check if there are any sounds to play
        if (_splashSounds == null || _splashSounds.Length == 0)
        {
            Debug.LogWarning("No splash sounds assigned to WaterTriggerHandler!");
            return;
        }

        // Select a random sound from the array
        int randomIndex = Random.Range(0, _splashSounds.Length);
        AudioClip selectedClip = _splashSounds[randomIndex];

        // Apply random pitch variation
        float randomPitch = Random.Range(_minPitch, _maxPitch);
        _audioSource.pitch = randomPitch;

        // Play the sound
        _audioSource.PlayOneShot(selectedClip);

        // Reset pitch to default for other sounds
        // (Note: This is immediate, so it won't affect this specific playback)
    }
}