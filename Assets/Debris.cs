using UnityEngine;

public class Debris : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private string playerTag = "Player";

    private void OnEnable()
    {
        // Reset any state when debris is reused from pool
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & groundLayer) != 0 || other.CompareTag(playerTag))
        {
            ReturnToPool();
        }
    }

    private void OnBecameInvisible()
    {
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        // Instead of Destroy, deactivate for pool reuse
        gameObject.SetActive(false);
    }
}