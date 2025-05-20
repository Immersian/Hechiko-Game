using SupanthaPaul;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollowObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _playerTransform;

    [Header("Follow Settings")]
    [SerializeField] private float _flipRotationTime = 0.2f;
    [SerializeField] private float _followSmoothness = 5f;
    [SerializeField] private Vector3 _rightOffset = new Vector3(2f, 0, 0);
    [SerializeField] private Vector3 _leftOffset = new Vector3(-2f, 0, 0);

    [Header("Look Settings")]
    [SerializeField] private float _maxUpOffset = 1.5f; // Max height when looking fully up
    [SerializeField] private float _maxDownOffset = -1.5f; // Max depth when looking fully down
    [SerializeField] private float _lookOffsetSmoothness = 3f;
    [SerializeField] private float _lookDeadzone = 0.1f; // Ignore tiny stick movements

    [Header("Look Toggle")]
    [SerializeField] private bool _lookEnabled = true;

    // Input System
    private Controller _controls;
    private InputAction _lookAction;

    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private Vector3 _currentVelocity;
    private float _currentVerticalOffset;
    private float _verticalOffsetVelocity;

    private void Awake()
    {
        _controls = new Controller();
        _lookAction = _controls.Gameplay.Look; // Should be a Vector2 input
    }

    private void OnEnable()
    {
        _controls.Enable();
        _lookAction.Enable();
    }

    private void OnDisable()
    {
        _controls.Disable();
        _lookAction.Disable();
    }

    private void LateUpdate()
    {
        bool facingRight = _playerTransform.GetComponent<PlayerController>().m_facingRight;
        Vector3 baseOffset = facingRight ? _rightOffset : _leftOffset;

        // Only process look input if enabled
        float targetVerticalOffset = 0f;
        if (_lookEnabled)
        {
            Vector2 lookInput = _lookAction.ReadValue<Vector2>();
            float verticalLook = lookInput.y;

            if (Mathf.Abs(verticalLook) < _lookDeadzone)
                verticalLook = 0f;

            if (verticalLook > 0)
                targetVerticalOffset = verticalLook * _maxUpOffset;
            else if (verticalLook < 0)
                targetVerticalOffset = verticalLook * -_maxDownOffset;
        }

        _currentVerticalOffset = Mathf.SmoothDamp(
            _currentVerticalOffset,
            targetVerticalOffset,
            ref _verticalOffsetVelocity,
            _lookOffsetSmoothness * Time.deltaTime
        );

        Vector3 finalOffset = baseOffset + new Vector3(0f, _currentVerticalOffset, 0f);
        _targetPosition = _playerTransform.position + finalOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            _targetPosition,
            ref _currentVelocity,
            _followSmoothness * Time.deltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _targetRotation,
            Time.deltaTime * _flipRotationTime
        );
    }
    public void EnableLookUpDown() => _lookEnabled = true;
    public void DisableLookUpDown() => _lookEnabled = false;
    public void ToggleLookUpDown() => _lookEnabled = !_lookEnabled;
    public void SetLookUpDown(bool state) => _lookEnabled = state;

    public void CallTurn()
    {
        bool facingRight = _playerTransform.GetComponent<PlayerController>().m_facingRight;
        _targetRotation = Quaternion.Euler(0, facingRight ? 0 : 180, 0);
    }
}