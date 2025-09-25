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
    [SerializeField] private DebrisSpawnerPhase2 groundSlamSpawnerPhase2;

    [Header("Tail Eye Settings")]
    public float projectileIdleDuration = 2f;
    public string projectileStartTrigger = "TailEyeStart";
    public string projectileIdleBool = "IsTailEyeIdle";
    public string projectileEndTrigger = "TailEyeEnd";

    [Header("Tail Eye Shooting")]
    public BossTailEyeShooter tailEyeShooter;

    [Header("Claw Attack Settings")]
    public ClawSpawner leftClawSpawner;
    public ClawSpawner rightClawSpawner;
    public string clawAttackTrigger = "ClawAttack";

    private AttackType[] phase1Attacks = { AttackType.GroundSlam, AttackType.ClawAttack, AttackType.TailEye };
    private AttackType[] phase2Attacks = { AttackType.GroundSlam, AttackType.GroundSlamVariation, AttackType.ClawAttack, AttackType.TailEye };
    private Coroutine attackRoutine;
    private float lastAttackTime;
    private AttackType lastAttackUsed;

    private enum AttackType { GroundSlam, ClawAttack, TailEye, GroundSlamVariation }

    #region Attack Functions
    private void GroundSlamAttack()
    {
        if (BakaBossSM.currentStateName == "Phase2BakaState")
        {
            if (Random.Range(0, 2) == 0)
            {
                Debug.Log("Executing Ground Slam Variation 1");
                ResetTrigger("GroundSlamVariation1");
                SetTrigger("GroundSlamVariation1");
            }
            else
            {
                Debug.Log("Executing Ground Slam Variation 2");
                ResetTrigger("GroundSlamVariation2");
                SetTrigger("GroundSlamVariation2");
            }
        }
        else
        {
            Debug.Log("Executing Ground Slam Attack");
            ResetTrigger("GroundSlam");
            SetTrigger("GroundSlam");
        }
    }

    private void ClawAttack()
    {
        Debug.Log("Executing Claw Attack");
        ResetTrigger(clawAttackTrigger);
        SetTrigger(clawAttackTrigger);
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

    public void ActivateGroundSlamSpawner()
    {
        if (groundSlamSpawner != null)
        {
            groundSlamSpawner.enabled = true;
            Debug.Log("Phase 1 Ground Slam Spawner activated");
        }
    }

    public void ActivatePhase2GroundSlamSpawner()
    {
        if (groundSlamSpawnerPhase2 != null)
        {
            groundSlamSpawnerPhase2.enabled = true;
            Debug.Log("Phase 2 Ground Slam Spawner activated");
        }
    }

    private IEnumerator TailEyeAttackSequence()
    {
        ResetTrigger(projectileStartTrigger);
        SetTrigger(projectileStartTrigger);
        Debug.Log("Started TailEyeStart animation");

        yield return WaitForAnimationState("BossProjectileStart");
        Debug.Log("TailEyeStart animation completed");

        bossAnimator.SetBool(projectileIdleBool, true);
        ResetTrigger(projectileEndTrigger);

        if (tailEyeShooter != null)
        {
            tailEyeShooter.SetShootingActive(true);
        }
        Debug.Log("Set TailEyeIdle bool to true and enabled shooting");

        yield return new WaitUntil(() =>
            bossAnimator.GetCurrentAnimatorStateInfo(0).IsName("BossProjectileIdle"));
        Debug.Log("Entered BossProjectileIdle state");

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

        bossAnimator.SetBool(projectileIdleBool, false);
        SetTrigger(projectileEndTrigger);

        if (tailEyeShooter != null)
        {
            tailEyeShooter.SetShootingActive(false);
        }
        Debug.Log("Triggered TailEyeEnd and disabled shooting");

        yield return WaitForAnimationState("BossProjectileEnd");
        Debug.Log("TailEyeEnd animation completed");

        animationComplete = true;
    }

    private IEnumerator ExecuteAttack(AttackType attack)
    {
        lastAttackTime = Time.time;
        lastAttackUsed = attack;

        ResetTrigger("Idle");
        SetTrigger("Idle");

        yield return new WaitForSeconds(Random.Range(minIdleTime, maxIdleTime));

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

    #region Animation Event Methods
    // ANIMATION EVENT: This method will be called from the claw attack animation
    public void OnClawAttackAnimationEvent()
    {
        // Only execute claw spawning if we're in Phase 2
        if (BakaBossSM.currentStateName == "Phase2BakaState")
        {
            SpawnClawProjectiles();
        }
        else
        {
            Debug.Log("Claw attack animation event received, but boss is not in Phase 2. Ignoring.");
        }
    }

    private void SpawnClawProjectiles()
    {
        if (leftClawSpawner != null && rightClawSpawner != null)
        {
            // Left spawner shoots left
            leftClawSpawner.SetShootDirection(false, true);
            leftClawSpawner.SpawnClawProjectile();

            // Right spawner shoots right
            rightClawSpawner.SetShootDirection(true, false);
            rightClawSpawner.SpawnClawProjectile();

            Debug.Log("Phase 2 Claw Projectiles spawned!");
        }
        else
        {
            Debug.LogWarning("Claw spawners not assigned in BakaBossAttackManager!");
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
        ResetTrigger(clawAttackTrigger);
        ResetTrigger("TailEyeStart");
        ResetTrigger(projectileEndTrigger);
        bossAnimator.SetBool(projectileIdleBool, false);
    }
    #endregion

    public void StopAllAttacks()
    {
        StopAllCoroutines();
        ResetAllTriggers();

        bossAnimator.Play("Idle", 0, 0f);
        bossAnimator.Update(0f);

        bossAnimator.SetBool(projectileIdleBool, false);
        ResetTrigger(projectileEndTrigger);

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

        animationComplete = true;
        lastAttackTime = Time.time;

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