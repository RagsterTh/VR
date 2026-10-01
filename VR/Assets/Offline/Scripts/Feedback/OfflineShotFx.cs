using UnityEngine;

/// <summary>
/// Energy-weapon look for the offline shots: muzzle flash + sparks, glowing projectile head, impact sparks/embers.
/// Uses a few shared particle systems driven by Emit() (no per-shot GameObjects, no extra lights) to stay cheap on Quest.
/// </summary>
public sealed class OfflineShotFx : MonoBehaviour
{
    public static readonly Color Energy = new(0.35f, 0.85f, 1f);
    private static readonly Color Hot = new(1f, 0.55f, 0.15f);

    private static OfflineShotFx _instance;

    private ParticleSystem _flash;   // soft round glows (muzzle, impact, projectile head, laser dot)
    private ParticleSystem _sparks;  // stretched streaks
    private ParticleSystem _embers;  // sparks that fall with gravity

    private static OfflineShotFx Instance
    {
        get
        {
            if (_instance == null)
                _instance = new GameObject("[Offline] Shot Fx").AddComponent<OfflineShotFx>();
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
        _flash = Make("Flash", stretch: false, gravity: 0f, max: 600);
        _sparks = Make("Sparks", stretch: true, gravity: 0f, max: 600);
        _embers = Make("Embers", stretch: true, gravity: 0.7f, max: 400);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private ParticleSystem Make(string systemName, bool stretch, float gravity, int max)
    {
        var go = new GameObject(systemName);
        go.transform.SetParent(transform, false);
        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = max;
        main.gravityModifier = gravity;
        main.startSpeed = 0f;

        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;

        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        var shrink = particles.sizeOverLifetime;
        shrink.enabled = true;
        shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = OfflineFx.ParticleMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (stretch)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 1.2f;
        }

        particles.Play();
        return particles;
    }

    private static void Emit(ParticleSystem system, Vector3 position, Vector3 velocity, float size, float lifetime, Color color)
    {
        if (system == null || OfflineFx.ParticleMaterial == null)
            return;
        var p = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = velocity,
            startSize = size,
            startLifetime = lifetime,
            startColor = color,
        };
        system.Emit(p, 1);
    }

    /// <summary>Single soft glow (projectile head, laser dot).</summary>
    public static void Glow(Vector3 position, float size, Color color, float lifetime)
    {
        Emit(Instance._flash, position, Vector3.zero, size, lifetime, color);
    }

    /// <summary>Flash at the barrel plus streaks flying forward.</summary>
    public static void Muzzle(Vector3 position, Vector3 direction)
    {
        OfflineShotFx fx = Instance;
        Emit(fx._flash, position, direction * 0.5f, 0.34f, 0.07f, Energy);
        Emit(fx._flash, position, direction * 0.5f, 0.11f, 0.08f, new Color(1f, 1f, 1f, 0.6f));
        Emit(fx._flash, position + direction * 0.12f, direction * 1.5f, 0.2f, 0.06f, Energy);

        for (int i = 0; i < 12; i++)
        {
            Vector3 spread = (direction + Random.insideUnitSphere * 0.35f).normalized;
            Emit(fx._sparks, position, spread * Random.Range(4f, 11f), Random.Range(0.012f, 0.03f), Random.Range(0.08f, 0.2f),
                i % 3 == 0 ? Color.white : Energy);
        }
    }

    /// <summary>Hit point: flash, sparks bouncing back along <paramref name="normal"/> and falling embers.</summary>
    public static void Impact(Vector3 position, Vector3 normal, bool enemy)
    {
        OfflineShotFx fx = Instance;
        float scale = enemy ? 1.4f : 1f;
        Color tint = enemy ? Hot : Energy;

        Emit(fx._flash, position, Vector3.zero, 0.5f * scale, 0.1f, tint);
        Emit(fx._flash, position, Vector3.zero, 0.22f * scale, 0.14f, Color.white);

        int sparks = enemy ? 22 : 14;
        for (int i = 0; i < sparks; i++)
        {
            Vector3 spread = (normal + Random.insideUnitSphere * 0.9f).normalized;
            Emit(fx._sparks, position, spread * Random.Range(3f, 9f) * scale, Random.Range(0.012f, 0.028f), Random.Range(0.1f, 0.28f),
                i % 2 == 0 ? Color.white : tint);
        }
        for (int i = 0; i < (enemy ? 10 : 6); i++)
        {
            Vector3 spread = (normal + Vector3.up * 0.5f + Random.insideUnitSphere * 0.8f).normalized;
            Emit(fx._embers, position, spread * Random.Range(1.5f, 4f), Random.Range(0.015f, 0.03f), Random.Range(0.4f, 0.9f), tint);
        }
    }
}

/// <summary>
/// Laser sight on a gun: thin beam along the barrel that ends where it hits, with a glowing dot.
/// Turns red when it is on something shootable, so the player feels the aim "lock".
/// </summary>
public sealed class OfflineGunLaser : MonoBehaviour
{
    private const float MaxDistance = 40f;
    private static readonly RaycastHit[] Hits = new RaycastHit[12];

    private Transform _gunPoint;
    private Transform _playerRoot;
    private LineRenderer _line;

    private void Start()
    {
        _gunPoint = transform.childCount > 0 ? transform.GetChild(0) : transform;
        var player = GetComponentInParent<PlayerPrefabNetwork>();
        _playerRoot = player != null ? player.transform : transform.root;

        var go = new GameObject("[Offline] Laser Sight");
        go.transform.SetParent(transform, false);
        _line = go.AddComponent<LineRenderer>();
        _line.sharedMaterial = OfflineFx.LineMaterial;
        _line.useWorldSpace = true;
        _line.positionCount = 2;
        _line.numCapVertices = 2;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.012f), new Keyframe(1f, 0.006f));
    }

    private void LateUpdate()
    {
        if (_line == null || _gunPoint == null)
            return;

        // Bullets leave along -up of the gun point (see PlayerBullet).
        Vector3 origin = _gunPoint.position;
        Vector3 direction = -_gunPoint.up;

        float distance = MaxDistance;
        bool onTarget = false;
        int count = Physics.RaycastNonAlloc(origin, direction, Hits, MaxDistance, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Collider c = Hits[i].collider;
            if (c == null || c.transform.IsChildOf(_playerRoot))
                continue;
            bool shootable = c.GetComponentInParent<IShootable>() != null;
            // Trigger volumes that are not targets (zones, limits) do not stop the beam.
            if (c.isTrigger && !shootable)
                continue;
            if (Hits[i].distance < distance)
            {
                distance = Hits[i].distance;
                onTarget = shootable;
            }
        }

        Vector3 end = origin + direction * distance;
        _line.SetPosition(0, origin);
        _line.SetPosition(1, end);

        Color beam = onTarget ? new Color(1f, 0.25f, 0.15f, 0.75f) : new Color(0.35f, 0.85f, 1f, 0.35f);
        _line.startColor = beam;
        _line.endColor = new Color(beam.r, beam.g, beam.b, beam.a * 0.4f);

        if (distance < MaxDistance)
        {
            float pulse = onTarget ? 1f + 0.3f * Mathf.Sin(Time.unscaledTime * 18f) : 1f;
            OfflineShotFx.Glow(end - direction * 0.02f, (onTarget ? 0.09f : 0.045f) * pulse * Mathf.Clamp(distance * 0.15f, 1f, 3f),
                onTarget ? new Color(1f, 0.3f, 0.15f) : OfflineShotFx.Energy, 0.03f);
        }
    }
}
