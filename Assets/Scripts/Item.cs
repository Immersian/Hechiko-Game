using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite sprite;
    [TextArea]
    [SerializeField] private string itemDescription;

    [Header("Collision")]
    [SerializeField] private bool useTrigger = true;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float pickupVolume = 1f;
    [SerializeField] private bool playAtPlayerPosition = true;

    private InventoryManager inventoryManager;
    private AudioSource audioSource;
    private Transform playerTransform;

    private void Start()
    {
        // Find the InventoryManager in the scene
        inventoryManager = FindObjectOfType<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogError("No InventoryManager found in scene!");
            return;
        }

        // Check if this item already exists in inventory
        if (inventoryManager.HasItem(itemName))
        {
            Destroy(gameObject);
            return;
        }

        // Try to get or create AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!useTrigger && collision.gameObject.CompareTag("Player"))
        {
            playerTransform = collision.transform;
            TryAddItemToInventory();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (useTrigger && other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            TryAddItemToInventory();
        }
    }

    private void TryAddItemToInventory()
    {
        if (inventoryManager == null)
        {
            // Try to find it again in case it wasn't available at Start
            inventoryManager = FindObjectOfType<InventoryManager>();
        }

        if (inventoryManager != null)
        {
            bool added = inventoryManager.AddItem(itemName, sprite, itemDescription);
            if (added)
            {
                PlayPickupSound();

                // Disable visuals immediately but delay destruction to allow sound to play
                DisableVisuals();

                // Destroy after sound plays (or after a short delay if no sound)
                if (pickupSound != null)
                {
                    Destroy(gameObject, pickupSound.length);
                }
                else
                {
                    Destroy(gameObject, 0.1f);
                }
            }
        }
        else
        {
            Debug.LogWarning("Tried to add item but no InventoryManager found");
        }
    }

    private void PlayPickupSound()
    {
        if (pickupSound == null) return;

        // Determine where to play the sound
        if (playAtPlayerPosition && playerTransform != null)
        {
            // Play at player position for better spatial awareness
            AudioSource.PlayClipAtPoint(pickupSound, playerTransform.position, pickupVolume);
        }
        else
        {
            // Play at item position
            audioSource.PlayOneShot(pickupSound, pickupVolume);
        }
    }

    private void DisableVisuals()
    {
        // Disable all renderers to make item invisible but keep it for sound
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }

        // Also disable colliders
        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // Optional: Disable particle systems if any
        var particles = GetComponentsInChildren<ParticleSystem>();
        foreach (var particle in particles)
        {
            particle.Stop();
        }
    }
}