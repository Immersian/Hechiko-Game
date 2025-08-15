using System.Collections;
using UnityEngine;

public class BakaBossAttackManager : MonoBehaviour
{
    [Header("References")]
    public BakaBossStateManager BakaBossSM;
    public Animator bossAnimator;
    public Transform BakaBossTransform;

    [Header("Timing")]
    public float minIdleTime = 1f;
    public float maxIdleTime = 2f;
    public float attackCooldown = 3f;

    [Header("Ground Slam Settings")]
    [SerializeField] private DebrisSpawner groundSlamSpawner;
    [SerializeField] private DebrisSpawnerPhase2 groundSlamSpawnerPhase2; // New spawner for phase 2

    [Header("Tail Eye Settings")]
    public float projectileIdleDuration = 2f;
    public string projectileStartTrigger = "TailEyeStart";
    public string projectileIdleBool = "IsTailEyeIdle";
    public string projectileEndBool = "IsTailEyeEnd";

    [Header("Tail Eye Shooting")]
    public BossTailEyeShooter tailEyeShooter;

    private AttackType[] phase1Attacks = { AttackType.GroundSlam, AttackType.ClawAttack, AttackType.TailEye };
    private AttackType[] phase2Attacks = { AttackType.GroundSlam, AttackType.GroundSlamVariation, AttackType.ClawAttack, AttackType.TailEye };
    private Coroutine attackRoutine;
    private float lastAttackTime;
    private AttackType lastAttackUsed;

    private enum AttackType { GroundSlam, ClawAttack, TailEye, GroundSlamVariation }

    #region Attack Functions
    // In GroundSlamAttack() method:
    private void GroundSlamAttack()
    {
        if (BakaBossSM.currentStateName == "Phase2BakaState")
        {
            // Randomly choose between two ground slam variations in phase 2
            if (Random.Range(0, 2) == 0)
            {
                Debug.Log("Executing Ground Slam Variation 1");
                ResetTrigger("GroundSlamVariation1");
                SetTrigger("GroundSlamVariation1");

                // Force enable spawner if animation event isn't working
                if (groundSlamSpawnerPhase2 != null)
                {
                    groundSlamSpawnerPhase2.enabled = true;
                }
            }
            else
            {
                Debug.Log("Executing Ground Slam Variation 2");
                ResetTrigger("GroundSlamVariation2");
                SetTrigger("GroundSlamVariation2");

                if (groundSlamSpawnerPhase2 != null)
                {
                    groundSlamSpawnerPhase2.enabled = true;
                }
            }
        }
        else
        {
            Debug.Log("Executing Ground Slam Attack");
            ResetTrigger("GroundSlam");
            SetTrigger("GroundSlam");

            if (groundSlamSpawner != null)
            {
                groundSlamSpawner.enabled = true;
            }
        }
    }

    private void ClawAttack()
    {
        Debug.Log("Executing Claw Attack");
        ResetTrigger("ClawAttack");
        SetTrigger("ClawAttack");
    }

    private void TailEyeAttack()
    {
        Debug.Log("Executing Tail Eye Attack");
        ResetTrigger("TailEye");
        StartCoroutine(TailEyeAttackSequence());
    }
    #endregion

    #region Core System
    private void OnEnable()
    {
        lastAttackUsed = (AttackType)(-1);
        StartAttackCycle();
    }

    private void OnDisable()
    {
        StopAttackCycle();
    }

    private void StartAttackCycle()
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(AttackCycle());
    }

    private void StopAttackCycle()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        ResetAllTriggers();
    }

    private IEnumerator AttackCycle()
    {
        while (true)
        {
            if (CanAttack())
            {
                AttackType randomAttack = GetRandomAttack();
                yield return ExecuteAttack(randomAttack);
            }
            yield return null;
        }
    }

    private AttackType GetRandomAttack()
    {
        AttackType[] availableAttacks = BakaBossSM.currentStateName == "Phase2BakaState" ?
            phase2Attacks : phase1Attacks;

        if (availableAttacks.Length == 1)
            return availableAttacks[0];

        AttackType selectedAttack;
        do
        {
            selectedAttack = availableAttacks[Random.Range(0, availableAttacks.Length)];
        } while (selectedAttack == lastAttackUsed && availableAttacks.Length > 1);

        return selectedAttack;
    }

    private bool CanAttack()
    {
        return (BakaBossSM.currentStateName == "Phase1BakaState" ||
               BakaBossSM.currentStateName == "Phase2BakaState") &&
               Time.time - lastAttackTime >= attackCooldown;
    }

    // Called from animation event for phase 1 ground slam
    public void ActivateGroundSlamSpawner()
    {
        if (groundSlamSpawner != null)
        {
            groundSlamSpawner.enabled = true;
            Debug.Log("Phase 1 Ground Slam Spawner activated");
        }
    }

    // Called from animation event for phase 2 ground slam variation
    public void ActivatePhase2GroundSlamSpawner()
    {
        if (groundSlamSpawnerPhase2 != null)
        {
            groundSlamSpawnerPhase2.enabled = true;
            Debug.Log("Phase 2 Ground Slam Spawner activated");

            // You can add specific phase 2 spawner patterns here
            // For example: groundSlamSpawnerPhase2.SetPattern(2); // Different pattern
        }
    }

    private IEnumerator TailEyeAttackSequence()
    {
        // 1. Play start animation
        ResetTrigger(projectileStartTrigger);
        SetTrigger(projectileStartTrigger);
        Debug.Log("Started TailEyeStart animation");

        // Wait for start animation to complete
        yield return WaitForAnimationState("BossProjectileStart");
        Debug.Log("TailEyeStart animation completed");

        // 2. Transition to idle state and enable shooting
        bossAnimator.SetBool(projectileIdleBool, true);
        bossAnimator.SetBool(projectileEndBool, false);

        if (tailEyeShooter != null)
        {
            tailEyeShooter.SetShootingActive(true);
        }
        Debug.Log("Set TailEyeIdle bool to true and enabled shooting");

        // Wait until we enter the projectile idle state
        yield return new WaitUntil(() =>
            bossAnimator.GetCurrentAnimatorStateInfo(0).IsName("BossProjectileIdle"));
        Debug.Log("Entered BossProjectileIdle state");

        // 3. Wait in idle state for the specified duration
        float timer = 0f;
        while (timer < projectileIdleDuration)
        {
            if (!bossAnimator.GetCurrentAnimatorStateInfo(0).IsName("BossProjectileIdle"))
            {
                Debug.LogWarning("Left BossProjectileIdle state prematurely!");
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"Completed idle duration: {timer} seconds");

        // 4. Transition to end state and disable shooting
        bossAnimator.SetBool(projectileIdleBool, false);
        bossAnimator.SetBool(projectileEndBool, true);

        if (tailEyeShooter != null)
        {
            tailEyeShooter.SetShootingActive(false);
        }
        Debug.Log("Started TailEyeEnd animation and disabled shooting");

        // Wait for end animation to complete
        yield return WaitForAnimationState("BossProjectileEnd");
        Debug.Log("TailEyeEnd animation completed");

        // 5. Reset end state
        bossAnimator.SetBool(projectileEndBool, false);
        Debug.Log("Reset TailEyeEnd bool");

        // Mark attack as complete
        animationComplete = true;
    }

    private IEnumerator ExecuteAttack(AttackType attack)
    {
        lastAttackTime = Time.time;
        lastAttackUsed = attack;

        // Play idle animation (same for both phases)
        ResetTrigger("Idle");
        SetTrigger("Idle");

        yield return new WaitForSeconds(Random.Range(minIdleTime, maxIdleTime));

        // Execute the selected attack
        switch (attack)
        {
            case AttackType.GroundSlam:
            case AttackType.GroundSlamVariation:
                GroundSlamAttack();
                yield return WaitForAnimationCompletion();
                break;
            case AttackType.ClawAttack:
                ClawAttack();
                yield return WaitForAnimationCompletion();
                break;
            case AttackType.TailEye:
                yield return StartCoroutine(TailEyeAttackSequence());
                break;
        }
    }
    #endregion

    private IEnumerator WaitForAnimationCompletion()
    {
        float timeout = 10f;
        float timer = 0f;
        while (!animationComplete && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (timer >= timeout)
            Debug.LogWarning("Animation timed out!");

        animationComplete = false;
    }

    private IEnumerator WaitForAnimationState(string stateName)
    {
        while (!bossAnimator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
        {
            yield return null;
        }

        while (bossAnimator.GetCurrentAnimatorStateInfo(0).IsName(stateName) &&
               bossAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }
    }

    #region Animation Control
    [Header("Animation")]
    public bool animationComplete = false;

    private void ResetTrigger(string trigger) => bossAnimator.ResetTrigger(trigger);
    private void SetTrigger(string trigger) => bossAnimator.SetTrigger(trigger);

    public void OnAnimationComplete() => animationComplete = true;

    private void ResetAllTriggers()
    {
        ResetTrigger("Idle");
        ResetTrigger("GroundSlam");
        ResetTrigger("GroundSlamVariation1");
        ResetTrigger("GroundSlamVariation2");
        ResetTrigger("ClawAttack");
        ResetTrigger("TailEyeStart");
        bossAnimator.SetBool(projectileIdleBool, false);
        bossAnimator.SetBool(projectileEndBool, false);
    }
    #endregion

    public void StopAllAttacks()
    {
        // Stop all coroutines immediately
        StopAllCoroutines();

        // Reset all animation parameters
        ResetAllTriggers();

        // Force immediate transition to idle state
        bossAnimator.Play("Idle", 0, 0f);
        bossAnimator.Update(0f); // Force immediate update

        // Reset all Tail Eye specific states
        bossAnimator.SetBool(projectileIdleBool, false);
        bossAnimator.SetBool(projectileEndBool, false);

        // Stop any active attack components
        if (tailEyeShooter != null)
        {
            tailEyeShooter.SetShootingActive(false);
        }

        if (groundSlamSpawner != null)
        {
            groundSlamSpawner.enabled = false;
        }

        if (groundSlamSpawnerPhase2 != null)
        {
            groundSlamSpawnerPhase2.enabled = false;
        }

        // Reset attack cycle
        animationComplete = true;
        lastAttackTime = Time.time;

        // Restart attack cycle if in a combat state
        if (BakaBossSM.currentStateName == "Phase1BakaState" ||
            BakaBossSM.currentStateName == "Phase2BakaState")
        {
            StartCoroutine(DelayedAttackRestart(0.5f));
        }
    }

    private IEnumerator DelayedAttackRestart(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartAttackCycle();
    }
}