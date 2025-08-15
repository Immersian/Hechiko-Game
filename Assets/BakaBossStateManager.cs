using UnityEngine;
using UnityEngine.Playables;

public class BakaBossStateManager : MonoBehaviour
{
    // All possible states
    public CutsceneBakaState cutsceneState = new CutsceneBakaState();
    public Phase1BakaState Phase1State = new Phase1BakaState();
    public PhaseTransitionState phasecutsceneState = new PhaseTransitionState(); // Added this line
    public Phase2BakaState Phase2State = new Phase2BakaState();

    [Header("Phase Transition")]
    public PlayableDirector phaseTransitionCutscene;

    [SerializeField]
    public string currentStateName;

    private BakaBossBaseState currentState;

    void Start()
    {
        currentState = cutsceneState;
        currentState.EnterState(this);
        currentStateName = currentState.GetType().Name;
    }

    void Update()
    {
        currentState.UpdateState(this);
    }

    public void SwitchState(BakaBossBaseState state)
    {
        currentState = state;
        state.EnterState(this);
        currentStateName = currentState.GetType().Name;
    }
}