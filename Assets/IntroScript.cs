using UnityEngine;

public class IntroScript : MonoBehaviour
{
    [Header("Objects to Delete")]
    [SerializeField] private GameObject[] objectsToDelete;

    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animationName = "YourAnimationName";

    private bool hasPlayed = false;
    private bool animationStarted = false;

    private void Start()
    {
        // If animator reference is not set, try to get it
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Play animation on start if it hasn't played yet
        if (!hasPlayed && animator != null)
        {
            animator.Play(animationName);
            animationStarted = true;
        }
    }

    private void Update()
    {
        // Check if animation has finished playing
        if (animationStarted && animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.normalizedTime >= 1f && !hasPlayed)
            {
                OnAnimationComplete();
            }
        }
    }

    // Animation Event method (call this from Animation Events)
    public void OnAnimationComplete()
    {
        hasPlayed = true;

        // Delete specified GameObjects
        foreach (GameObject obj in objectsToDelete)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        // Optional: Disable the animator to prevent replay
        if (animator != null)
        {
            animator.enabled = false;
        }

        animationStarted = false;
    }

    // Optional: Public method to manually trigger animation if needed
    public void PlayAnimationOnce()
    {
        if (!hasPlayed && animator != null)
        {
            animator.Play(animationName);
            animationStarted = true;
        }
    }
}