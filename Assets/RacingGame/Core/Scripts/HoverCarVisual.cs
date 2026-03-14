using UnityEngine;

public class HoverCarVisual : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float hoverAmplitude = 0.08f;
    [SerializeField] private float hoverFrequency = 3.5f;
    [SerializeField] private float tiltStrength = 10f;
    [SerializeField] private float tiltSmoothness = 8f;

    private Vector3 _initialLocalPosition;
    private Vector3 _lastWorldPosition;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        _initialLocalPosition = visualRoot.localPosition;
        _lastWorldPosition = transform.position;
    }

    public void Configure(Transform configuredVisualRoot)
    {
        visualRoot = configuredVisualRoot != null ? configuredVisualRoot : transform;
        _initialLocalPosition = visualRoot.localPosition;
        _lastWorldPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (visualRoot == null)
            return;

        float bob = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        float horizontalSpeed = (transform.position.x - _lastWorldPosition.x) / Mathf.Max(Time.deltaTime, 0.0001f);
        float targetRoll = Mathf.Clamp(-horizontalSpeed * tiltStrength, -18f, 18f);
        float targetPitch = Mathf.Lerp(-2f, 6f, Mathf.InverseLerp(0f, 260f, DifficultyManager.CurrentSpeedKmh));

        Quaternion targetRotation = Quaternion.Euler(targetPitch, 0f, targetRoll);
        visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRotation, tiltSmoothness * Time.deltaTime);
        visualRoot.localPosition = _initialLocalPosition + new Vector3(0f, bob, 0f);

        _lastWorldPosition = transform.position;
    }
}
