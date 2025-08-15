using UnityEngine;

public class BossTailEyeShooter : MonoBehaviour
{
    [Header("Basic Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;
    public float bulletSpeed = 10f;
    public float recoilStrength = 1f;

    [Header("Homing Projectile Settings (Phase 2)")]
    public GameObject homingBulletPrefab;
    public float homingFireRate = 1f;
    public float homingBulletSpeed = 7f;
    public float homingStrength = 2f;
    public LayerMask groundLayer;

    [Header("References")]
    public Animator bossAnimator;
    public Rigidbody2D bossRigidbody;
    public BakaBossStateManager stateManager;

    private Transform player;
    private float nextFireTime;
    private float nextHomingFireTime;
    private bool isActive;
    private bool isPhase2;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        SetShootingActive(false);
    }

    public void SetShootingActive(bool active)
    {
        isActive = active;
        isPhase2 = stateManager.currentStateName == "Phase2BakaState";

        if (active)
        {
            nextFireTime = Time.time;
            nextHomingFireTime = Time.time;
        }
    }

    private void Update()
    {
        if (!isActive || player == null) return;

        // Handle basic shooting
        if (Time.time >= nextFireTime)
        {
            ShootAtPlayer();
            nextFireTime = Time.time + fireRate;
        }

        // Handle homing shots in phase 2
        if (isPhase2 && Time.time >= nextHomingFireTime)
        {
            ShootHomingProjectile();
            nextHomingFireTime = Time.time + homingFireRate;
        }
    }

    private void ShootAtPlayer()
    {
        if (bulletPrefab == null || firePoint == null) return;

        Vector2 direction = (player.position - firePoint.position).normalized;
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = direction * bulletSpeed;
        }

        if (bossRigidbody != null)
        {
            bossRigidbody.AddForce(-direction * recoilStrength, ForceMode2D.Impulse);
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void ShootHomingProjectile()
    {
        if (homingBulletPrefab == null || firePoint == null) return;

        GameObject homingBullet = Instantiate(homingBulletPrefab, firePoint.position, Quaternion.identity);
        Vector2 direction = (player.position - firePoint.position).normalized;
        homingBullet.transform.right = direction;

        HomingProjectile homingScript = homingBullet.GetComponent<HomingProjectile>();
        if (homingScript != null)
        {
            // Initialize with turnRate instead of homingForce
            homingScript.Initialize(player, homingBulletSpeed, homingStrength, groundLayer);
        }
        else
        {
            Rigidbody2D bulletRb = homingBullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.velocity = direction * homingBulletSpeed;
            }
        }

        if (bossRigidbody != null)
        {
            bossRigidbody.AddForce(-direction * recoilStrength * 0.5f, ForceMode2D.Impulse);
        }
    }
}