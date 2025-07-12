using UnityEngine;

public abstract class BakaBossBaseState
{
    public abstract void EnterState(BakaBossStateManager baka);
    public abstract void UpdateState(BakaBossStateManager baka);
    public abstract void ExitState(BakaBossStateManager baka);
}