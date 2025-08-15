using TarodevGhost;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class GhostController : MonoBehaviour
{
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private Recording _currentRecording;
    private float _playbackTime;
    private bool _isPlaying;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void StartPlayback(Recording recording)
    {
        _currentRecording = recording;
        _playbackTime = 0f;
        _isPlaying = true;
    }

    public void StopPlayback()
    {
        _isPlaying = false;
        _currentRecording = null;
    }

    private void Update()
    {
        if (!_isPlaying || _currentRecording == null) return;

        // Evaluate the recording at current time
        GhostPose pose = _currentRecording.EvaluatePoint(_playbackTime);

        // Update position and rotation
        transform.position = pose.Position;
        transform.rotation = pose.Rotation;

        // Update sprite if available
        if (_spriteRenderer != null && pose.SpriteId != 0)
        {
            Sprite sprite = _currentRecording.GetRecordedSprite(pose.SpriteId);
            if (sprite != null) _spriteRenderer.sprite = sprite;
        }

        // Update facing direction
        if (pose.AnimatorState != 0)
        {
            Vector3 scale = transform.localScale;
            float newXScale = Mathf.Abs(scale.x) * Mathf.Sign(pose.AnimatorState);
            if (!Mathf.Approximately(scale.x, newXScale))
            {
                scale.x = newXScale;
                transform.localScale = scale;
            }
        }

        // Update animator parameters
        UpdateAnimatorParameters(pose);

        // Advance playback time
        _playbackTime += Time.deltaTime;

        // Check if playback finished
        if (_playbackTime > _currentRecording.Duration)
        {
            StopPlayback();
        }
    }

    private void UpdateAnimatorParameters(GhostPose pose)
    {
        if (_animator == null) return;

        // Calculate movement speed (for blend trees)
        float horizontalSpeed = CalculateHorizontalSpeed(pose);
        _animator.SetFloat("Speed", horizontalSpeed);

        // Set grounded state
        _animator.SetBool("IsGrounded", pose.IsGrounded);

        // Set other parameters as needed
        // Example: _animator.SetBool("IsDashing", pose.IsDashing);
    }

    private float CalculateHorizontalSpeed(GhostPose pose)
    {
        // Get previous frame's position to calculate velocity
        float prevTime = Mathf.Max(0, _playbackTime - Time.deltaTime);
        GhostPose prevPose = _currentRecording.EvaluatePoint(prevTime);

        // Calculate horizontal movement speed
        float distance = Mathf.Abs(pose.Position.x - prevPose.Position.x);
        return distance / Time.deltaTime;
    }
}