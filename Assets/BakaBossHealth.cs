using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BakaBossHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;

    private SimpleFlash flashEffect;
    private Collider2D[] colliders;

    // Start is called before the first frame update
    void Start()
    {
        flashEffect = GetComponent<SimpleFlash>();
        colliders = GetComponentsInChildren<Collider2D>();
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {

        currentHealth -= damage;

        flashEffect.CallDFlash();


        if (currentHealth <= 0)
        {
            //Later
            isDead = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player Attack")) return;

        if (other.TryGetComponent<PlayerAttackHitbox>(out var attack))
        {
            Vector2 hitDirection = (transform.position - other.transform.position).normalized;
            TakeDamage(attack.damage, hitDirection);
        }
    }
}
