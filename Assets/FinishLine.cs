using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class FinishLine : MonoBehaviour
{
    public static event Action<bool> Crossed; // bool parameter: true = run starting, false = run ending

    [Header("Settings")]
    [SerializeField] private bool _isStartLine = true;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private float _cooldownTime = 0.5f;

    private bool _canTrigger = true;
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        if (_collider == null)
        {
            Debug.LogError("FinishLine requires a Collider component!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_canTrigger) return;

        // Check if the colliding object is on the player layer
        if (((1 << other.gameObject.layer) & _playerLayer) != 0)
        {
            TriggerFinishLine();
        }
    }

    private void TriggerFinishLine()
    {
        if (Crossed != null)
        {
            Crossed.Invoke(_isStartLine);
        }

        // Start cooldown to prevent multiple triggers
        StartCoroutine(Cooldown());
    }

    private IEnumerator Cooldown()
    {
        _canTrigger = false;
        yield return new WaitForSeconds(_cooldownTime);
        _canTrigger = true;
    }

    // Visual indicator in editor
    private void OnDrawGizmos()
    {
        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_collider == null) return;

        Gizmos.color = _isStartLine ? Color.green : Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
    }
}