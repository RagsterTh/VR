using UnityEngine;

/// <summary>
/// Keeps the player's damage trigger (BoxCollider on the rig root, tag "Player") on the player's body:
/// under the headset and as tall as the head. Without it the box stays at the XR Origin, away from where
/// the player actually stands, and enemies/bullets rarely touch it.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class PlayerDamageCollider : MonoBehaviour
{
    [Tooltip("Headset camera. Empty = first Camera under this rig.")]
    [SerializeField] private Transform _head;
    [SerializeField] private float _width = 1f;
    [SerializeField] private float _minHeight = 1f;
    [Tooltip("Extra height above the headset.")]
    [SerializeField] private float _headMargin = 0.15f;

    private BoxCollider _box;

    private void Awake()
    {
        _box = GetComponent<BoxCollider>();
        if (_head == null)
        {
            Camera camera = GetComponentInChildren<Camera>(true);
            if (camera != null)
                _head = camera.transform;
        }
    }

    private void LateUpdate()
    {
        if (_head == null || !_head.gameObject.activeInHierarchy)
            return;

        Vector3 head = transform.InverseTransformPoint(_head.position);
        float height = Mathf.Max(_minHeight, head.y + _headMargin);
        _box.center = new Vector3(head.x, height * 0.5f, head.z);
        _box.size = new Vector3(_width, height, _width);
    }
}
