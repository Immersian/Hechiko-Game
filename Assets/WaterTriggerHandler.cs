using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterTriggerHandler : MonoBehaviour
{
    [SerializeField] private LayerMask _waterMask;
    [SerializeField] private GameObject _splashParticles;

    private EdgeCollider2D _edgeColl;

    private InteractableWater _water;

    private void Awake()
    {
        _edgeColl = GetComponent<EdgeCollider2D>();
        _water = GetComponent<InteractableWater>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((_waterMask.value & (1 << collision.gameObject.layer)) > 0)
        {
            Rigidbody2D rb = collision.GetComponentInParent<Rigidbody2D>();
            if (rb != null)
            {
                // Find the highest point of the water surface from the EdgeCollider2D
                float highestPointY = float.MinValue;
                foreach (Vector2 point in _edgeColl.points)
                {
                    Vector2 worldPoint = transform.TransformPoint(point + _edgeColl.offset);
                    if (worldPoint.y > highestPointY)
                    {
                        highestPointY = worldPoint.y;
                    }
                }

                // Apply Y offset to position the splash perfectly at the surface
                Vector3 spawnPos = new Vector3(collision.transform.position.x,
                                              highestPointY + _water.SplashYOffset,
                                              0f);

                Instantiate(_splashParticles, spawnPos, Quaternion.identity);

                int multiplier = 1;
                if (rb.velocity.y < 0)
                {
                    multiplier = -1;
                }
                else { multiplier = 1; }

                float vel = rb.velocity.y * _water.ForceMultiplier;
                vel = Mathf.Clamp(Mathf.Abs(vel), 0f, _water.MaxForce);
                vel *= multiplier;

                _water.Splash(collision, vel);
            }
        }
    }
}