using UnityEngine;
using System.Collections;

public class StunBakaState : BakaBossBaseState
{
    private BakaBossStateManager stateManager;
    private BakaBossAttackManager attackManager;
    private Animator bossAnimator;
    private float stunDuration = 5f;
    private BakaBossBaseState previousState;
    private bool hasExited;
    private Coroutine stunCoroutine;

    public override void EnterState(BakaBossStateManager baka)
    {
        stateManager = baka;
        attackManager = baka.GetComponent<BakaBossAttackManager>();
        bossAnimator = baka.GetComponent<Animator>();
        hasExited = false;

        // Store the previous state to return to after stun
        previousState = stateManager.CurrentState;
        Debug.Log($"Storing previous state: {previousState?.GetType().Name}");

        // Stop all ongoing attacks immediately
        if (attackManager != null)
        {
            attackManager.StopAllAttacks();
        }

        // Play stun animation
        if (bossAnimator != null)
        {
            bossAnimator.ResetTrigger("GotStunned");
            bossAnimator.ResetTrigger("GetBackUp");
            bossAnimator.SetBool("IsStunned", true);
            bossAnimator.SetTrigger("GotStunned");
        }

        Debug.Log("Entered Stun State");

        // Start the stun timer coroutine
        stunCoroutine = baka.StartCoroutine(StunTimerRoutine());
    }

    private IEnumerator StunTimerRoutine()
    {
        // Wait for stun duration
        yield return new WaitForSeconds(stunDuration);

        if (hasExited) yield break;

        // Get back up animation
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("IsStunned", false);
            bossAnimator.SetTrigger("GetBackUp");

            // Wait for get up animation to start playing
            yield return new WaitUntil(() =>
                bossAnimator.GetCurrentAnimatorStateInfo(0).IsName("GetBackUpAnimation") ||
                bossAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.1f);

            // Wait for get up animation to complete (or at least most of it)
            yield return new WaitForSeconds(1f);
        }

        if (hasExited) yield break;

        // Determine which state to return to based on current health
        BakaBossHealth bossHealth = stateManager.GetComponent<BakaBossHealth>();
        BakaBossBaseState targetState = GetAppropriateState(bossHealth);

        Debug.Log($"Returning to state: {targetState.GetType().Name}");
        stateManager.SwitchState(targetState);
    }

    private BakaBossBaseState GetAppropriateState(BakaBossHealth bossHealth)
    {
        if (bossHealth == null) return stateManager.Phase1State;

        // Check health to determine appropriate state
        if (bossHealth.currentHealth <= 50) // Phase 3 threshold
        {
            // Return phase 3 state if you have one, otherwise phase 2
            return stateManager.Phase2State; // Change this if you have Phase3State
        }
        else if (bossHealth.currentHealth <= 700) // Phase 2 threshold
        {
            return stateManager.Phase2State;
        }
        else // Phase 1
        {
            return stateManager.Phase1State;
        }
    }

    public override void UpdateState(BakaBossStateManager baka)
    {
        // Update logic can be handled in the coroutine
    }

    public override void ExitState(BakaBossStateManager baka)
    {
        hasExited = true;

        // Stop the coroutine if it's still running
        if (stunCoroutine != null)
        {
            baka.StopCoroutine(stunCoroutine);
        }

        // Clean up animation states
        if (bossAnimator != null)
        {
            bossAnimator.ResetTrigger("GotStunned");
            bossAnimator.ResetTrigger("GetBackUp");
            bossAnimator.SetBool("IsStunned", false);
        }
        Debug.Log("Exiting Stun State");
    }
}