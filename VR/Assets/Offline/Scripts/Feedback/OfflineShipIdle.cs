using UnityEngine;

/// <summary>
/// Idle "parked in space" animation for a ship: soft hover, wing roll, small yaw sway and, every few
/// seconds, a quick thruster correction nudge. Purely visual; animates around the start pose.
/// </summary>
public sealed class OfflineShipIdle : MonoBehaviour
{
    [SerializeField] private float _hoverHeight = 0.12f;
    [SerializeField] private float _hoverSpeed = 0.7f;
    [SerializeField] private float _rollDegrees = 4f;
    [SerializeField] private float _yawDegrees = 3f;
    [SerializeField] private float _nudgeDistance = 0.25f;
    [SerializeField] private Vector2 _nudgeInterval = new(4f, 8f);

    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private float _seed;
    private float _nextNudge;
    private float _nudge;
    private Vector3 _nudgeDirection;

    private void Start()
    {
        _basePosition = transform.localPosition;
        _baseRotation = transform.localRotation;
        _seed = Random.value * 10f;
        _nextNudge = Time.time + Random.Range(_nudgeInterval.x, _nudgeInterval.y);
    }

    private void Update()
    {
        float t = Time.time + _seed;

        if (Time.time >= _nextNudge)
        {
            _nextNudge = Time.time + Random.Range(_nudgeInterval.x, _nudgeInterval.y);
            _nudge = 1f;
            _nudgeDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.3f, 0.3f), Random.Range(-1f, 1f)).normalized;
        }
        _nudge = Mathf.MoveTowards(_nudge, 0f, Time.deltaTime * 0.8f);
        // Quick push out, slow settle back.
        float push = Mathf.Sin(Mathf.Clamp01(1f - _nudge) * Mathf.PI) * _nudgeDistance;

        Vector3 hover = new Vector3(
            Mathf.Sin(t * _hoverSpeed * 0.5f) * _hoverHeight * 0.5f,
            Mathf.Sin(t * _hoverSpeed) * _hoverHeight,
            Mathf.Cos(t * _hoverSpeed * 0.4f) * _hoverHeight * 0.4f);
        transform.localPosition = _basePosition + hover + _nudgeDirection * push;

        float roll = Mathf.Sin(t * _hoverSpeed * 0.8f) * _rollDegrees + _nudgeDirection.x * push * 20f;
        float pitch = Mathf.Sin(t * _hoverSpeed * 1.1f + 1f) * _rollDegrees * 0.4f;
        float yaw = Mathf.Sin(t * _hoverSpeed * 0.3f) * _yawDegrees;
        transform.localRotation = _baseRotation * Quaternion.Euler(pitch, yaw, roll);
    }
}
