using UnityEngine;

public class ChildTriggerForwarder : MonoBehaviour
{
       [HideInInspector] public RespawnPoint respawnPoint;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                respawnPoint?.TriggerRespawn(collision.transform);
            }
        }
}