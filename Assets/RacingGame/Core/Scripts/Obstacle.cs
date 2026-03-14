using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    private bool _triggered;

    private void Awake()
    {
        Collider obstacleCollider = GetComponent<Collider>();
        obstacleCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        _triggered = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.TryGetComponent<PlayerController>(out _))
            return;

        _triggered = true;
        GameEventBus.Dispatch_ObstacleHit();
    }
}
