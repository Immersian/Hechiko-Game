using SupanthaPaul;
using UnityEngine;

public class HealingAnimationEventHandler : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;

    private void Start()
    {
        // If playerController is not set, try to find it
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();

            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }
        }

        if (playerController == null)
        {
            Debug.LogError("HealingAnimationEventHandler: PlayerController reference is missing!");
        }
    }

    // This method will be called from the animation event
    public void OnHealAnimationComplete()
    {
        if (playerController != null)
        {
            //playerController.OnHealAnimationComplete();
        }
    }

    // Optional: You can also add other animation events here
    public void OnHealAnimationStart()
    {
        // You can add any start effects here if needed
    }

    public void OnHealAnimationInterrupted()
    {
        if (playerController != null)
        {
            playerController.InterruptHealing();
        }
    }
}