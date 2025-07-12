using UnityEngine;
using UnityEngine.Playables;

public class CutsceneTriggerBoss : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private BakaBossStateManager bossStateManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playableDirector.Play();
            GetComponent<BoxCollider2D>().enabled = false;

            // Register for the stopped event
            playableDirector.stopped += OnCutsceneFinished;
        }
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        // Unregister the event
        playableDirector.stopped -= OnCutsceneFinished;

        // Transition to Phase 1
        if (bossStateManager != null)
        {
            bossStateManager.SwitchState(bossStateManager.Phase1State);
        }
    }
}