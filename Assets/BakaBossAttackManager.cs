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

    private AttackType[] phase1Attacks = { AttackType.GroundSlam, AttackType.ClawAttack, AttackType.TailEye };
    private Coroutine attackRoutine;
    private float lastAttackTime;
    private AttackType lastAttackUsed;

    private enum AttackType { GroundSlam, ClawAttack, TailEye }

    #region Attack Functions
    private void GroundSlamAttack()
    {
        Debug.Log("Executing Ground Slam Attack");
        ResetTrigger("GroundSlam");
        SetTrigger("GroundSlam");
        // Add GroundSlam-specific logic here
    }

    private void ClawAttack()
    {
        Debug.Log("Executing Claw Attack");
        ResetTrigger("ClawAttack");
        SetTrigger("ClawAttack");
        // Add ClawAttack-specific logic here
    }

    private void TailEyeAttack()
    {
        Debug.Log("Executing Tail Eye Attack");
        ResetTrigger("TailEye");
        SetTrigger("TailEye");
        // Add TailEye-specific logic here
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
        if (phase1Attacks.Length == 1)
            return phase1Attacks[0];

        AttackType selectedAttack;
        do
        {
            selectedAttack = phase1Attacks[Random.Range(0, phase1Attacks.Length)];
        } while (selectedAttack == lastAttackUsed && phase1Attacks.Length > 1);

        return selectedAttack;
    }

    private bool CanAttack()
    {
        return BakaBossSM.currentStateName == "Phase1BakaState" &&
               Time.time - lastAttackTime >= attackCooldown;
    }

    private IEnumerator ExecuteAttack(AttackType attack)
    {
        lastAttackTime = Time.time;
        lastAttackUsed = attack;

        // Play idle animation
        ResetTrigger("Idle");
        SetTrigger("Idle");
        yield return new WaitForSeconds(Random.Range(minIdleTime, maxIdleTime));

        // Execute the selected attack
        switch (attack)
        {
            case AttackType.GroundSlam:
                GroundSlamAttack();
                break;
            case AttackType.ClawAttack:
                ClawAttack();
                break;
            case AttackType.TailEye:
                TailEyeAttack();
                break;
        }

        // Wait for animation completion
        yield return WaitForAnimationCompletion();
    }

    private IEnumerator WaitForAnimationCompletion()
    {
        float timeout = 5f;
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
    #endregion

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
        ResetTrigger("ClawAttack");
        ResetTrigger("TailEye");
    }
    #endregion
}