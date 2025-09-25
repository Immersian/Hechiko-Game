// DeathCutsceneManager.cs
using UnityEngine;
using UnityEngine.Playables;

public class DeathCutsceneManager : MonoBehaviour
{
    public PlayableDirector deathCutscene;
    public GameObject objectToEnableDuringCutscene;
    public GameObject objectToDeleteAfterCutscene;

    void Start()
    {
        // Initially disable the cutscene-only object
        if (objectToEnableDuringCutscene != null)
        {
            objectToEnableDuringCutscene.SetActive(false);
        }
    }

    public void PlayDeathCutscene()
    {
        if (deathCutscene != null)
        {
            // Enable the cutscene-only object
            if (objectToEnableDuringCutscene != null)
            {
                objectToEnableDuringCutscene.SetActive(true);
            }

            deathCutscene.Play();
            deathCutscene.stopped += OnCutsceneFinished;
        }
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        // Clean up after cutscene
        if (objectToEnableDuringCutscene != null)
        {
            objectToEnableDuringCutscene.SetActive(false);
        }

        if (objectToDeleteAfterCutscene != null)
        {
            Destroy(objectToDeleteAfterCutscene);
        }

        // Optional: disable or destroy the cutscene itself
        if (deathCutscene != null)
        {
            deathCutscene.gameObject.SetActive(false);
        }
    }


}