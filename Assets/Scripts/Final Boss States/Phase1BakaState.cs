using UnityEngine;

public class Phase1BakaState : BakaBossBaseState
{
    private BakaBossHealth bossHealth;
    private BakaBossAttackManager lobsterAttackManager;

    public override void EnterState(BakaBossStateManager baka)
    {
        bossHealth = baka.GetComponent<BakaBossHealth>();
        lobsterAttackManager = baka.GetComponent<BakaBossAttackManager>();
        Debug.Log("Lesbian Moment");
    }

    public override void UpdateState(BakaBossStateManager baka)
    {
        if (bossHealth != null && bossHealth.currentHealth <= 50)
        {
            baka.SwitchState(baka.Phase2State);
        }
    }

    public override void ExitState(BakaBossStateManager baka)
    {
        // Clean up or prepare for state exit if needed
    }
}