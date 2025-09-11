using UnityEngine;

public class Phase1BakaState : BakaBossBaseState
{
    private BakaBossHealth bossHealth;
    private BakaBossAttackManager attackManager;

    public override void EnterState(BakaBossStateManager baka)
    {
        bossHealth = baka.GetComponent<BakaBossHealth>();
        attackManager = baka.GetComponent<BakaBossAttackManager>();
        Debug.Log("Entered Phase 1");
    }

    public override void UpdateState(BakaBossStateManager baka)
    {
        if (bossHealth != null && bossHealth.currentHealth <= 700)
        {
            // Stop any ongoing attacks before transitioning
            attackManager?.StopAllAttacks();

            // Transition to phase cutscene state
            baka.SwitchState(baka.phasecutsceneState);
        }
    }

    public override void ExitState(BakaBossStateManager baka)
    {
        Debug.Log("Exiting Phase 1");
    }
}