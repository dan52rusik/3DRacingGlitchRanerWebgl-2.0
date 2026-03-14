using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Lanes")]
    [SerializeField] private float laneOffset = 2.4f;
    [SerializeField] private float laneChangeSpeed = 10f;

    [Header("Bounds")]
    [SerializeField] private float fixedZ = -2f;
    [SerializeField] private float rideHeight = 0.75f;

    [Header("Input")]
    [SerializeField] private float minSwipeDistance = 40f;

    private Camera _mainCamera;
    private int _currentLane = 1;
    private bool _active = true;
    private Vector2 _swipeStart;
    private bool _trackingSwipe;

    private void Awake()
    {
        _mainCamera = Camera.main;

        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        var colliderRef = GetComponent<BoxCollider>();
        colliderRef.size = new Vector3(0.9f, 1.4f, 0.9f);
        colliderRef.center = new Vector3(0f, 0.7f, 0f);

        GameEventBus.OnGameOver += HandleGameOver;
        GameEventBus.OnGameRestart += HandleGameRestart;
    }

    private void OnDestroy()
    {
        GameEventBus.OnGameOver -= HandleGameOver;
        GameEventBus.OnGameRestart -= HandleGameRestart;
    }

    private void Update()
    {
        if (!_active)
            return;

        HandleKeyboardInput();
        HandleSwipeInput();
        UpdatePosition();
        UpdateCamera();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            _currentLane = Mathf.Max(0, _currentLane - 1);

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            _currentLane = Mathf.Min(2, _currentLane + 1);
    }

    private void HandleSwipeInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                _trackingSwipe = true;
                _swipeStart = touch.position;
            }
            else if (_trackingSwipe && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                ConsumeSwipe(touch.position);
                _trackingSwipe = false;
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            _trackingSwipe = true;
            _swipeStart = Input.mousePosition;
        }
        else if (_trackingSwipe && Input.GetMouseButtonUp(0))
        {
            ConsumeSwipe(Input.mousePosition);
            _trackingSwipe = false;
        }
    }

    private void ConsumeSwipe(Vector2 endPosition)
    {
        float deltaX = endPosition.x - _swipeStart.x;
        if (Mathf.Abs(deltaX) < minSwipeDistance)
            return;

        _currentLane = deltaX > 0f
            ? Mathf.Min(2, _currentLane + 1)
            : Mathf.Max(0, _currentLane - 1);
    }

    private void UpdatePosition()
    {
        float targetX = (_currentLane - 1) * laneOffset;
        Vector3 targetPosition = new Vector3(targetX, rideHeight, fixedZ);
        transform.position = Vector3.Lerp(transform.position, targetPosition, laneChangeSpeed * Time.deltaTime);
    }

    private void UpdateCamera()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
            return;

        Vector3 current = _mainCamera.transform.position;
        float targetX = transform.position.x * 0.35f;
        _mainCamera.transform.position = Vector3.Lerp(
            current,
            new Vector3(targetX, 6.8f, -16.5f),
            5f * Time.deltaTime);
        _mainCamera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
    }

    private void HandleGameOver()
    {
        _active = false;
    }

    private void HandleGameRestart()
    {
        _active = true;
        _currentLane = 1;
        transform.position = new Vector3(0f, rideHeight, fixedZ);
    }
}
