using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BakaBossStateManager : MonoBehaviour
{

    BakaBossBaseState currentState;
    public CutsceneBakaState cutsceneState = new CutsceneBakaState();
    public Phase1BakaState Phase1State = new Phase1BakaState();
    public Phase2BakaState Phase2State = new Phase2BakaState();

    [SerializeField]
    public string currentStateName;

    // Start is called before the first frame update
    void Start()
    {
        currentState = cutsceneState;
        //currentState = damagedBState;
        //currentState = dizzyState;
        currentState.EnterState(this);
        currentStateName = currentState.GetType().Name;
        currentStateName = currentState.GetType().Name;
    }

    // Update is called once per frame 
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
