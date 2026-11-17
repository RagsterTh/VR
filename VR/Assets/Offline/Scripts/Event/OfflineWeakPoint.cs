using UnityEngine;

public sealed class OfflineWeakPoint : MonoBehaviour
{
    private const float Radius = 0.2f;
    private static readonly Color Core = new(1f, 0.85f, 0.2f);

    private static bool _hasShot;
    private static Vector3 _shotPosition;
    private static Vector3 _shotDirection;

    public static int Crits { get; private set; }

    private Transform _anchor;
    private Vector3 _localPoint;
    private int _hits;

    public static void ResetSession() => Crits = 0;

    public static void SetShot(Vector3 position, Vector3 velocity)
    {
        _hasShot = velocity.sqrMagnitude > 0.0001f;
        _shotPosition = position;
        _shotDirection = velocity.normalized;
    }

    public static bool ResolveHit(Enemy enemy)
    {
        var point = enemy.GetComponent<OfflineWeakPoint>();
        if (point == null)
            point = enemy.gameObject.AddComponent<OfflineWeakPoint>();

        bool hadShot = _hasShot;
        _hasShot = false;

        Vector3 core = point.WorldPoint;
        if (hadShot)
        {
            Vector3 toCore = core - _shotPosition;
            Vector3 closest = _shotPosition + _shotDirection * Vector3.Dot(toCore, _shotDirection);
            if (Vector3.Distance(closest, core) <= Radius)
            {
                Crits++;
                OfflineFx.FloatingText(core + Vector3.up * 0.45f, "CRÍTICO!", Core, 120f, 0.9f, 0.35f);
                OfflineFx.Burst(core, Core, 30, 3.5f, 0.06f, 0.5f, 0f, 0.05f);
                OfflineFx.HapticAll(0.6f, 0.08f);
                return true;
            }
        }

        point._hits++;
        if (point._hits >= OfflineDifficulty.BodyHits)
            return true;

        OfflineFx.Burst(core, new Color(0.6f, 0.85f, 1f), 12, 2f, 0.04f, 0.3f, 0.3f, 0.15f);
        OfflineFx.Run(OfflineFx.Punch(enemy.transform, 0.12f, 0.15f));
        int left = OfflineDifficulty.BodyHits - point._hits;
        OfflineFx.FloatingText(core + Vector3.up * 0.4f, left == 1 ? "MIRE NO NÚCLEO!" : $"BLINDADO  {left}", new Color(0.7f, 0.9f, 1f), 70f, 0.6f, 0.2f);
        return false;
    }

    private Vector3 WorldPoint => _anchor != null ? _anchor.TransformPoint(_localPoint) : transform.position;

    private void Awake()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.name == "Lens")
            {
                _anchor = r.transform;
                _localPoint = r.transform.InverseTransformPoint(r.bounds.center);
                return;
            }
        }

        Renderer biggest = null;
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer)
                continue;
            if (biggest == null || r.bounds.size.sqrMagnitude > biggest.bounds.size.sqrMagnitude)
                biggest = r;
        }
        _anchor = biggest != null ? biggest.transform : transform;
        Vector3 top = biggest != null ? biggest.bounds.center + Vector3.up * biggest.bounds.extents.y * 0.75f : transform.position;
        _localPoint = _anchor.InverseTransformPoint(top);
    }

    private void OnEnable() => _hits = 0;

    private void LateUpdate()
    {
        float pulse = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 9f + GetInstanceID());
        OfflineShotFx.Glow(WorldPoint, 0.45f * pulse, new Color(1f, 0.6f, 0.1f, 0.9f), 0.03f);
        OfflineShotFx.Glow(WorldPoint, 0.12f, new Color(1f, 1f, 0.8f, 1f), 0.03f);
    }
}
