using UnityEngine;

public class DeathIdleBakaState : BakaBossBaseState
{
    private BakaBossAttackManager attackManager;

    public override void EnterState(BakaBossStateManager baka)
    {
        attackManager = baka.GetComponent<BakaBossAttackManager>();

        Debug.Log("Entered Death Idle State");

        // Stop all attacks permanently
        if (attackManager != null)
        {
            attackManager.StopAllAttacks();
        }

        // Set the IsDead bool parameter to true forever using the state manager
        baka.SetIsDead(true);

        // Optional: Disable any combat-related components
        var health = baka.GetComponent<BakaBossHealth>();
        if (health != null)
        {
            health.enabled = false;
        }
    }

    public override void UpdateState(BakaBossStateManager baka)
    {
        // No updates needed - boss is permanently dead
    }

    public override void ExitState(BakaBossStateManager baka)
    {
        // This state should never exit since the boss is dead forever
        Debug.LogWarning("Exiting Death Idle State - this shouldn't normally happen!");
    }
}