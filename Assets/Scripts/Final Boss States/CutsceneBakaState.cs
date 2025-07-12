using UnityEngine;
using UnityEngine.Playables;

public class CutsceneBakaState : BakaBossBaseState
{
    private PlayableDirector cutsceneDirector;

    public override void EnterState(BakaBossStateManager baka)
    {
        // Get reference to the cutscene director
        cutsceneDirector = baka.GetComponent<PlayableDirector>();

        // Start the cutscene automatically when entering this state
        if (cutsceneDirector != null)
        {
            cutsceneDirector.Play();
            cutsceneDirector.stopped += OnCutsceneFinished;
        }
    }

    public override void UpdateState(BakaBossStateManager baka)
    {
        // No updates needed during cutscene
    }

    public override void ExitState(BakaBossStateManager baka)
    {
        // Clean up event when exiting state
        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped -= OnCutsceneFinished;
        }
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        // This will be called when the cutscene finishes
        // Transition to phase 1 through the state manager
        if (cutsceneDirector != null && cutsceneDirector.GetComponent<BakaBossStateManager>() != null)
        {
            cutsceneDirector.GetComponent<BakaBossStateManager>().SwitchState(
                cutsceneDirector.GetComponent<BakaBossStateManager>().Phase1State);
        }
    }
}