using UnityEngine;

public class IntroScript : MonoBehaviour
{
    [Header("Objects to Delete")]
    [SerializeField] private GameObject[] objectsToDelete;

    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animationName = "YourAnimationName";

    [Header("Sound Settings")]
    [SerializeField] private AudioClip startSound;
    [SerializeField] private AudioClip completeSound;
    [SerializeField] private float startSoundVolume = 1f;
    [SerializeField] private float completeSoundVolume = 1f;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    private bool hasPlayed = false;
    private bool animationStarted = false;

    private void Start()
    {
        // If animator reference is not set, try to get it
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // If audio source is not set, try to get it from this object or create one
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // Make it 2D sound
            }
        }

        // Play animation on start if it hasn't played yet
        if (!hasPlayed && animator != null)
        {
            PlayIntroAnimation();
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

    public void PlayIntroAnimation()
    {
        if (!hasPlayed && animator != null)
        {
            // Play start sound
            PlaySound(startSound, startSoundVolume);

            // Play animation
            animator.Play(animationName);
            animationStarted = true;
        }
    }

    // Animation Event method (call this from Animation Events)
    public void OnAnimationComplete()
    {
        if (hasPlayed) return; // Prevent multiple calls

        hasPlayed = true;

        // Play completion sound
        PlaySound(completeSound, completeSoundVolume);

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

    // Optional: Animation Event method for start sound (if you want to time it precisely)
    public void PlayStartSoundEvent()
    {
        PlaySound(startSound, startSoundVolume);
    }

    // Optional: Animation Event method for completion sound (if you want to time it precisely)
    public void PlayCompleteSoundEvent()
    {
        PlaySound(completeSound, completeSoundVolume);
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
        else if (clip != null && audioSource == null)
        {
            Debug.LogWarning("AudioSource not found on " + gameObject.name + ". Cannot play sound.");
        }
    }

    // Optional: Public method to manually trigger animation if needed
    public void PlayAnimationOnce()
    {
        PlayIntroAnimation();
    }

    // Optional: Public method to trigger animation completion manually
    public void TriggerAnimationCompletion()
    {
        OnAnimationComplete();
    }
}