using SupanthaPaul;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TarodevGhost
{
    public class ReplaySystem
    {
        private readonly WaitForFixedUpdate _wait = new WaitForFixedUpdate();

        public ReplaySystem(MonoBehaviour runner)
        {
            runner.StartCoroutine(FixedUpdate());
            runner.StartCoroutine(Update());
        }

        private IEnumerator FixedUpdate()
        {
            while (true)
            {
                yield return _wait;
                AddSnapshot();
                _elapsedRecordingTime += Time.smoothDeltaTime;
            }
        }

        private IEnumerator Update()
        {
            while (true)
            {
                yield return null;
                _replaySmoothedTime += Time.smoothDeltaTime;
                UpdateReplay();
            }
        }

        #region Recording

        private readonly Dictionary<RecordingType, Recording> _runs = new Dictionary<RecordingType, Recording>();
        private Recording _currentRun;
        private float _elapsedRecordingTime;
        private int _snapshotEveryNFrames;
        private int _frameCount;
        private float _maxRecordingTimeLimit;

        public void StartRun(Transform target, int snapshotEveryNFrames = 2, float maxRecordingTimeLimit = 60)
        {
            _currentRun = new Recording(target);
            _elapsedRecordingTime = 0;
            _snapshotEveryNFrames = Mathf.Max(1, snapshotEveryNFrames);
            _frameCount = 0;
            _maxRecordingTimeLimit = maxRecordingTimeLimit;
        }

        private void AddSnapshot()
        {
            if (_currentRun == null) return;

            if (_frameCount++ % _snapshotEveryNFrames == 0)
            {
                // Get the PlayerController component to check grounded state
                PlayerController playerController = _currentRun.Target.GetComponent<PlayerController>();
                bool isGrounded = playerController != null && playerController.isGrounded;

                _currentRun.AddSnapshot(_elapsedRecordingTime, isGrounded);
            }

            if (_currentRun.Duration >= _maxRecordingTimeLimit) FinishRun();
        }
        public bool FinishRun(bool save = true)
        {
            if (_currentRun == null) return false;
            if (!save)
            {
                _currentRun = null;
                return false;
            }

            _runs[RecordingType.Last] = _currentRun;
            _currentRun = null;

            if (!GetRun(RecordingType.Best, out var best) || _runs[RecordingType.Last].Duration <= best.Duration)
            {
                _runs[RecordingType.Best] = _runs[RecordingType.Last];
                return true;
            }

            return false;
        }

        public void SetSavedRun(Recording run) => _runs[RecordingType.Saved] = run;

        public bool GetRun(RecordingType type, out Recording run)
        {
            return _runs.TryGetValue(type, out run);
        }

        #endregion

        #region Play Ghost

        private Recording _currentReplay;
        private GameObject _ghostObj;
        private bool _destroyOnComplete;
        private float _replaySmoothedTime;
        private Dictionary<float, Sprite> _spriteHashLookup = new Dictionary<float, Sprite>();
        private SpriteRenderer _ghostRenderer;

        public void PlayRecording(RecordingType type, GameObject ghostObj, bool destroyOnCompletion = true)
        {
            if (_ghostObj != null) Object.Destroy(_ghostObj);

            if (!GetRun(type, out _currentReplay))
            {
                Object.Destroy(ghostObj);
                return;
            }

            _ghostObj = ghostObj;
            var ghostController = _ghostObj.GetComponent<GhostController>();
            if (ghostController == null) ghostController = _ghostObj.AddComponent<GhostController>();

            ghostController.StartPlayback(_currentReplay);

            _destroyOnComplete = destroyOnCompletion;
        }

        private void UpdateReplay()
        {
            if (_currentReplay == null || _ghostObj == null) return;

            // Get the complete ghost pose
            GhostPose pose = _currentReplay.EvaluatePoint(_replaySmoothedTime);

            // Update transform
            _ghostObj.transform.SetPositionAndRotation(pose.Position, pose.Rotation);

            // Update sprite if we have a renderer
            if (_ghostRenderer != null)
            {
                Sprite currentSprite = _currentReplay.GetRecordedSprite(pose.SpriteId);
                if (currentSprite != null)
                {
                    _ghostRenderer.sprite = currentSprite;
                }
            }

            // Update facing direction based on animator state
            if (pose.AnimatorState != 0)
            {
                Vector3 newScale = _ghostObj.transform.localScale;
                if ((newScale.x > 0 && pose.AnimatorState < 0) || (newScale.x < 0 && pose.AnimatorState > 0))
                {
                    newScale.x *= -1;
                    _ghostObj.transform.localScale = newScale;
                }
            }

            // Update animator parameters if available
            var ghostAnimator = _ghostObj.GetComponent<Animator>();
            if (ghostAnimator != null)
            {
                // Calculate horizontal speed from position changes
                float prevTime = Mathf.Max(0, _replaySmoothedTime - Time.deltaTime);
                GhostPose prevPose = _currentReplay.EvaluatePoint(prevTime);
                float horizontalSpeed = Mathf.Abs(pose.Position.x - prevPose.Position.x) / Time.deltaTime;

                // Calculate if grounded (Y position hasn't changed)
                bool isGrounded = Mathf.Approximately(pose.Position.y, prevPose.Position.y);

                // Set animator parameters
                ghostAnimator.SetFloat("HorizontalSpeed", horizontalSpeed);
                ghostAnimator.SetBool("IsGrounded", isGrounded);
                // Add more parameters as needed
            }

            // Destroy when replay completes
            if (_replaySmoothedTime > _currentReplay.Duration)
            {
                _currentReplay = null;
                if (_destroyOnComplete) Object.Destroy(_ghostObj);
            }
        }

        public void StopReplay()
        {
            if (_ghostObj != null) Object.Destroy(_ghostObj);
            _currentReplay = null;
            _spriteHashLookup.Clear();
        }

        #endregion
    }

    public enum RecordingType
    {
        Last = 0,
        Best = 1,
        Saved = 2
    }
}