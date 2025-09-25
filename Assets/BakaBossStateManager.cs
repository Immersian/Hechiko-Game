using UnityEngine;
using UnityEngine.Playables;

public class BakaBossStateManager : MonoBehaviour
{
    // All possible states
    public CutsceneBakaState cutsceneState = new CutsceneBakaState();
    public Phase1BakaState Phase1State = new Phase1BakaState();
    public PhaseTransitionState phasecutsceneState = new PhaseTransitionState();
    public Phase2BakaState Phase2State = new Phase2BakaState();
    public StunBakaState stunState = new StunBakaState();
    public DeathCutsceneBakaState deathCutsceneState = new DeathCutsceneBakaState();
    public DeathIdleBakaState deathIdleState = new DeathIdleBakaState();

    [Header("Phase Transition")]
    public PlayableDirector phaseTransitionCutscene;

    [Header("Death Cutscene")]
    public PlayableDirector deathCutscene;
    public GameObject objectToEnableDuringCutscene;
    public GameObject objectToDeleteAfterCutscene;

    [Header("UI Elements")]
    public GameObject borderObject; // Reference to the border object

    [Header("Animation Parameters")]
    public string isDeadParameter = "IsDead";

    [SerializeField]
    public string currentStateName;

    public BakaBossBaseState currentState;
    public BakaBossBaseState CurrentState => currentState;

    private Animator bossAnimator;

    void Start()
    {
        bossAnimator = GetComponent<Animator>();
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
        currentState.ExitState(this);
        currentState = state;
        state.EnterState(this);
        currentStateName = currentState.GetType().Name;
    }

    public void TriggerStun()
    {
        if (currentState is not StunBakaState)
        {
            SwitchState(stunState);
        }
    }

    public void TriggerDeathCutscene()
    {
        if (currentState is not DeathCutsceneBakaState && currentState is not DeathIdleBakaState)
        {
            SwitchState(deathCutsceneState);
        }
    }

    public void TriggerDeathIdle()
    {
        if (currentState is not DeathIdleBakaState)
        {
            SwitchState(deathIdleState);
        }
    }

    // Animation control methods
    public void SetIsDead(bool value)
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetBool(isDeadParameter, value);
        }
    }

    public void ResetAllAnimatorParameters()
    {
        if (bossAnimator != null)
        {
            // Reset common parameters you might have
            bossAnimator.SetBool(isDeadParameter, false);
            // Add any other parameters you want to reset here
        }
    }

    // NEW: Method to disable the border
    public void DisableBorder()
    {
        if (borderObject != null)
        {
            borderObject.SetActive(false);
            Debug.Log("Border disabled after death cutscene");
        }
    }

    // NEW: Method to enable the border (if needed)
    public void EnableBorder()
    {
        if (borderObject != null)
        {
            borderObject.SetActive(true);
            Debug.Log("Border enabled");
        }
    }
}