using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TarodevGhost
{
    public class Recording
    {
        private readonly AnimationCurve _posXCurve = new AnimationCurve();
        private readonly AnimationCurve _posYCurve = new AnimationCurve();
        private readonly AnimationCurve _rotZCurve = new AnimationCurve();
        private readonly AnimationCurve _spriteIdCurve = new AnimationCurve();
        private readonly AnimationCurve _groundedCurve = new AnimationCurve();
        private readonly AnimationCurve _animatorParamsCurve = new AnimationCurve();
        private Dictionary<string, int> _animParamHashes = new Dictionary<string, int>();
        private Animator _targetAnimator;
        public float Duration { get; private set; }
        public Transform Target { get; private set; }

        private SpriteRenderer _targetRenderer;
        private Dictionary<int, Sprite> _spriteLibrary = new Dictionary<int, Sprite>();

        #region Recording

        public Recording(Transform target)
        {
            Target = target;
            _targetRenderer = target.GetComponent<SpriteRenderer>();
            _targetAnimator = target.GetComponent<Animator>();

            // Initialize with current sprite if available
            if (_targetRenderer != null && _targetRenderer.sprite != null)
            {
                RecordSprite(_targetRenderer.sprite);
            }
        }

        public void AddSnapshot(float elapsed, bool isGrounded)
        {
            Duration = elapsed;

            // Record position and rotation
            var pos = Target.position;
            var rot = Target.rotation.eulerAngles;

            UpdateCurve(_posXCurve, elapsed, pos.x);
            UpdateCurve(_posYCurve, elapsed, pos.y);
            UpdateCurve(_rotZCurve, elapsed, rot.z);
            UpdateCurve(_groundedCurve, elapsed, isGrounded ? 1 : 0);

            // Record current sprite if available
            if (_targetRenderer != null && _targetRenderer.sprite != null)
            {
                int spriteId = RecordSprite(_targetRenderer.sprite);
                UpdateCurve(_spriteIdCurve, elapsed, spriteId);
            }

            // Record animator parameters if available
            if (_targetAnimator != null)
            {
                float animatorState = 0;

                // Record facing direction (1 for right, -1 for left)
                var scale = Target.localScale;
                animatorState = scale.x > 0 ? 1 : -1;

                // You can add more parameters here as needed
                UpdateCurve(_animatorParamsCurve, elapsed, animatorState);
            }
        }

        private int RecordSprite(Sprite sprite)
        {
            int spriteId = sprite.GetInstanceID();
            if (!_spriteLibrary.ContainsKey(spriteId))
            {
                _spriteLibrary.Add(spriteId, sprite);
            }
            return spriteId;
        }

        private void UpdateCurve(AnimationCurve curve, float time, float val)
        {
            var count = curve.length;
            var kf = new Keyframe(time, val);

            // Optimize: Only add new key if value changed significantly
            if (count > 1 &&
                Mathf.Approximately(curve.keys[count - 1].value, curve.keys[count - 2].value) &&
                Mathf.Approximately(val, curve.keys[count - 1].value))
            {
                curve.MoveKey(count - 1, kf); // Update existing key
            }
            else
            {
                curve.AddKey(kf); // Add new key
            }
        }

        #endregion

        #region Playback

        public GhostPose EvaluatePoint(float elapsed)
        {
            return new GhostPose(
                new Vector3(
                    _posXCurve.Evaluate(elapsed),
                    _posYCurve.Evaluate(elapsed),
                    0f),
                Quaternion.Euler(0, 0, _rotZCurve.Evaluate(elapsed)),
                (int)_spriteIdCurve.Evaluate(elapsed),
                _animatorParamsCurve.Evaluate(elapsed),
                _groundedCurve.Evaluate(elapsed) > 0.5f
            );
        }

        public Sprite GetRecordedSprite(int spriteId)
        {
            _spriteLibrary.TryGetValue(spriteId, out Sprite sprite);
            return sprite;
        }

        #endregion

        #region Serialization

        private const char DATA_DELIMITER = '|';
        private const char CURVE_DELIMITER = '\n';

        public string Serialize()
        {
            var builder = new StringBuilder();

            // Serialize position/rotation curves
            SerializeCurve(_posXCurve);
            SerializeCurve(_posYCurve);
            SerializeCurve(_rotZCurve);
            SerializeCurve(_spriteIdCurve, false);

            // Serialize sprite library
            builder.Append(CURVE_DELIMITER);
            foreach (var entry in _spriteLibrary)
            {
                builder.Append($"{entry.Key},{entry.Value.name}");
                if (entry.Key != _spriteLibrary.Last().Key)
                    builder.Append(DATA_DELIMITER);
            }

            return builder.ToString();

            void SerializeCurve(AnimationCurve curve, bool addDelimiter = true)
            {
                for (int i = 0; i < curve.length; i++)
                {
                    var point = curve[i];
                    builder.Append($"{point.time:F3},{point.value:F0}");
                    if (i < curve.length - 1)
                        builder.Append(DATA_DELIMITER);
                }
                if (addDelimiter)
                    builder.Append(CURVE_DELIMITER);
            }
        }

        public void Deserialize(string data)
        {
            var components = data.Split(CURVE_DELIMITER);

            // Deserialize curves
            DeserializeCurve(_posXCurve, components[0]);
            DeserializeCurve(_posYCurve, components[1]);
            DeserializeCurve(_rotZCurve, components[2]);
            DeserializeCurve(_spriteIdCurve, components[3]);

            // Deserialize sprite library (if available)
            if (components.Length > 4)
            {
                var spriteEntries = components[4].Split(DATA_DELIMITER);
                foreach (var entry in spriteEntries)
                {
                    var parts = entry.Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int spriteId))
                    {
                        // Note: In a real implementation, you'd need to load the sprite
                        // from resources using the sprite name (parts[1])
                        // _spriteLibrary[spriteId] = LoadSprite(parts[1]);
                    }
                }
            }

            Duration = _posXCurve.keys.LastOrDefault().time;

            void DeserializeCurve(AnimationCurve curve, string curveData)
            {
                if (string.IsNullOrEmpty(curveData)) return;

                var points = curveData.Split(DATA_DELIMITER);
                foreach (var point in points)
                {
                    var values = point.Split(',');
                    if (values.Length == 2)
                    {
                        curve.AddKey(
                            float.Parse(values[0]),
                            float.Parse(values[1]));
                    }
                }
            }
        }

        #endregion
    }

    public struct GhostPose
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public int SpriteId;
        public float AnimatorState; // 1 for right, -1 for left
        public bool IsGrounded;

        public GhostPose(Vector3 position, Quaternion rotation, int spriteId, float animatorState, bool isGrounded)
        {
            Position = position;
            Rotation = rotation;
            SpriteId = spriteId;
            AnimatorState = animatorState;
            IsGrounded = isGrounded;
        }
    }
}