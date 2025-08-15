using UnityEngine;
using UnityEngine.Playables;

public class PhaseTransitionState : BakaBossBaseState
{
    private PlayableDirector cutscene;
    private BakaBossStateManager stateManager;

    public override void EnterState(BakaBossStateManager baka)
    {
        stateManager = baka;
        cutscene = baka.phaseTransitionCutscene;

        // Reset animator to idle
        baka.GetComponent<Animator>()?.Play("Idle", 0, 0f);

        if (cutscene != null)
        {
            cutscene.Play();
            cutscene.stopped += OnCutsceneFinished;
            Debug.Log("Playing phase transition cutscene");
        }
        else
        {
            Debug.LogWarning("No cutscene assigned! Moving directly to phase 2");
            stateManager.SwitchState(stateManager.Phase2State);
        }
    }

    public override void UpdateState(BakaBossStateManager baka) { }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        stateManager.SwitchState(stateManager.Phase2State);
    }

    public override void ExitState(BakaBossStateManager baka)
    {
        if (cutscene != null)
        {
            cutscene.stopped -= OnCutsceneFinished;
        }
    }
}