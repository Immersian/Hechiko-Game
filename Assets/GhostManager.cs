using TarodevGhost;
using UnityEngine;

public class GhostManager : MonoBehaviour
{
    [SerializeField] private Transform _recordTarget;
    [SerializeField] private GameObject _ghostPrefab;
    [SerializeField, Range(1, 10)] private int _captureEveryNFrames = 2;

    private ReplaySystem _system;

    private void Awake()
    {
        _system = new ReplaySystem(this);
    }

    private void OnEnable()
    {
        FinishLine.Crossed += OnFinishLineCrossed;
    }

    private void OnDisable()
    {
        FinishLine.Crossed -= OnFinishLineCrossed;
    }

    private void OnFinishLineCrossed(bool runStarting)
    {
        if (runStarting)
        {
            _system.StartRun(_recordTarget, _captureEveryNFrames);
            _system.PlayRecording(RecordingType.Best, Instantiate(_ghostPrefab));
        }
        else
        {
            _system.FinishRun();
            _system.StopReplay();
        }
    }
}