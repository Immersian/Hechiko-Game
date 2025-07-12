using UnityEngine;

public class Phase2BakaState : BakaBossBaseState
{
    private BakaBossHealth bossHealth;
    private BakaBossAttackManager attackManager;
    private float timer;
    private float attackInterval = 3f; // Time between attacks in seconds

    public override void EnterState(BakaBossStateManager baka)
    {
        bossHealth = baka.GetComponent<BakaBossHealth>();
        attackManager = baka.GetComponent<BakaBossAttackManager>();

        Debug.Log("Entered Phase 2 - Angry Lobster Mode");

        // Initialize phase-specific values
        timer = 0f;

        // Trigger any phase 2 entry effects
        // Example: Change boss appearance, play sound, etc.
    }

    public override void UpdateState(BakaBossStateManager baka)
    {
        // Check for phase transition to phase 3
        if (bossHealth != null && bossHealth.currentHealth <= 0)
        {
            return;
        }

        // Phase 2 behavior
        timer += Time.deltaTime;
    }

    public override void ExitState(BakaBossStateManager baka)
    {
        // Clean up phase 2 specific things
        Debug.Log("Exiting Phase 2");
    }
}