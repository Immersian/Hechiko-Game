using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    private GameObject player;
    private Rigidbody2D rb;

    public float force;
    [Header("Collision Settings")]
    public LayerMask groundLayer; // Assign this in the Inspector to the ground layer
    public LayerMask playerLayer; // Assign this in the Inspector to the player layer
    public float destroyDelay = 0.1f;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem impactParticlesPrefab; // Prefab for impact effect
    [SerializeField] private bool spawnParticlesOnDestroy = true;
    [SerializeField] private float particleLifetime = 1f;

    // Simple object pooling for particles (prevents GC spikes)
    private static List<ParticleSystem> particlePool = new List<ParticleSystem>();
    private static Transform particlePoolParent;
    private const int MAX_POOL_SIZE = 10; // Limit pool size to prevent memory issues

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            Vector3 direction = player.transform.position - transform.position;
            rb.velocity = direction.normalized * force;

            float rot = Mathf.Atan2(-direction.y, -direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, rot + 180);
        }
        else
        {
            DestroyBullet(); // No player found, destroy bullet
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Get the layer of the collided object
        int otherLayer = other.gameObject.layer;

        // Check if collided with player or ground using layers
        if ((groundLayer.value & (1 << otherLayer)) != 0 ||
            (playerLayer.value & (1 << otherLayer)) != 0)
        {
            // Play impact effect before destroying
            PlayImpactEffect(transform.position);

            // Destroy the bullet after a slight delay
            DestroyBullet();
        }
    }

    private void PlayImpactEffect(Vector3 position)
    {
        if (!spawnParticlesOnDestroy || impactParticlesPrefab == null)
            return;

        // Get particle system from pool or create new one
        ParticleSystem particles = GetPooledParticle();

        if (particles != null)
        {
            // Position and activate
            particles.transform.position = position;
            particles.gameObject.SetActive(true);

            // Play the particles
            particles.Play();

            // Return to pool after playing
            StartCoroutine(ReturnParticleToPoolAfterPlay(particles));
        }
        else
        {
            // Fallback: Instantiate and destroy (only if pool is full)
            ParticleSystem tempParticles = Instantiate(impactParticlesPrefab, position, Quaternion.identity);
            tempParticles.Play();
            Destroy(tempParticles.gameObject, particleLifetime);
        }
    }

    private ParticleSystem GetPooledParticle()
    {
        // Create pool parent if needed
        if (particlePoolParent == null)
        {
            GameObject poolObj = new GameObject("BulletParticlePool");
            particlePoolParent = poolObj.transform;
            DontDestroyOnLoad(poolObj); // Optional: keep between scenes
        }

        // Try to find an available particle system in the pool
        for (int i = 0; i < particlePool.Count; i++)
        {
            if (particlePool[i] != null && !particlePool[i].gameObject.activeSelf)
            {
                return particlePool[i];
            }
        }

        // If pool is not full, create a new one
        if (particlePool.Count < MAX_POOL_SIZE)
        {
            ParticleSystem newParticle = Instantiate(impactParticlesPrefab, particlePoolParent);
            newParticle.gameObject.SetActive(false);

            // Configure for pooling
            var main = newParticle.main;
            main.stopAction = ParticleSystemStopAction.None; // Don't destroy automatically

            particlePool.Add(newParticle);
            return newParticle;
        }

        // Pool is full, return null
        return null;
    }

    private IEnumerator ReturnParticleToPoolAfterPlay(ParticleSystem particleSystem)
    {
        if (particleSystem == null) yield break;

        // Wait for particle system to finish playing
        float duration = particleSystem.main.duration;
        float maxLifetime = particleSystem.main.startLifetime.constantMax;
        yield return new WaitForSeconds(duration + maxLifetime + 0.1f); // Small buffer

        // Return to pool (deactivate)
        if (particleSystem != null)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            particleSystem.Clear();
            particleSystem.gameObject.SetActive(false);
        }
    }

    private void DestroyBullet()
    {
        // Disable components first
        if (rb != null) rb.simulated = false;
        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
        if (GetComponent<SpriteRenderer>() != null) GetComponent<SpriteRenderer>().enabled = false;

        // Destroy after delay
        Destroy(gameObject, destroyDelay);
    }

    private void OnDestroy()
    {
        // If bullet is destroyed without hitting anything (e.g., out of bounds)
        if (spawnParticlesOnDestroy && impactParticlesPrefab != null)
        {
            PlayImpactEffect(transform.position);
        }
    }

    // Clean up pool when game quits
    private void OnApplicationQuit()
    {
        if (particlePoolParent != null)
        {
            Destroy(particlePoolParent.gameObject);
        }
        particlePool.Clear();
    }
}