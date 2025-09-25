using UnityEngine;
using UnityEngine.Playables;

public class DeathCutsceneBakaState : BakaBossBaseState
{
    private PlayableDirector deathCutsceneDirector;
    private BakaBossStateManager stateManager;
    private GameObject objectToEnable;
    private GameObject objectToDelete;
    private CutsceneTriggerBoss cutsceneTrigger;

    public override void EnterState(BakaBossStateManager baka)
    {
        stateManager = baka;
        deathCutsceneDirector = baka.deathCutscene;
        objectToEnable = baka.objectToEnableDuringCutscene;
        objectToDelete = baka.objectToDeleteAfterCutscene;
        cutsceneTrigger = Object.FindObjectOfType<CutsceneTriggerBoss>();

        // SET DEATH ANIMATION IMMEDIATELY
        baka.SetIsDead(true);

        var attackManager = baka.GetComponent<BakaBossAttackManager>();
        if (attackManager != null)
        {
            attackManager.StopAllAttacks();
        }

        if (objectToEnable != null)
        {
            objectToEnable.SetActive(true);
        }

        if (deathCutsceneDirector != null)
        {
            deathCutsceneDirector.Play();
            deathCutsceneDirector.stopped += OnDeathCutsceneFinished;
            Debug.Log("Started death cutscene");
        }
        else
        {
            Debug.LogWarning("No death cutscene assigned!");
            CleanUpAfterCutscene();
        }
    }

    public override void UpdateState(BakaBossStateManager baka)
    {
        // No updates needed during cutscene
    }

    public override void ExitState(BakaBossStateManager baka)
    {
        // Clean up event when exiting state
        if (deathCutsceneDirector != null)
        {
            deathCutsceneDirector.stopped -= OnDeathCutsceneFinished;
        }
    }

    private void OnDeathCutsceneFinished(PlayableDirector director)
    {
        Debug.Log("Death cutscene finished");
        CleanUpAfterCutscene();
    }

    private void CleanUpAfterCutscene()
    {
        // NEW: Fade out the health bar
        if (cutsceneTrigger != null)
        {
            cutsceneTrigger.StartFadeOutHealthBar();
        }

        // Disable the object that was only for the cutscene
        if (objectToEnable != null)
        {
            objectToEnable.SetActive(false);
        }

        // Delete the object that should be removed after cutscene
        if (objectToDelete != null)
        {
            Debug.Log($"Deleting object: {objectToDelete.name}");
            Object.Destroy(objectToDelete);
        }

        // Disable the border object using the state manager
        if (stateManager != null)
        {
            stateManager.DisableBorder();
        }

        // Disable or destroy the cutscene timeline itself if desired
        if (deathCutsceneDirector != null)
        {
            Debug.Log($"Disabling death cutscene director: {deathCutsceneDirector.name}");
            deathCutsceneDirector.gameObject.SetActive(false);
        }

        // Transition to death idle state
        if (stateManager != null)
        {
            stateManager.TriggerDeathIdle();
        }
    }
}