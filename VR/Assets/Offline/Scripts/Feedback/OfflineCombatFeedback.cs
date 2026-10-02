using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Offline combat "juice": gun recoil + bullet trails, kill explosions with hit-stop and "+1",
/// boss hit flashes, a kill counter and arrows pointing at enemies outside the player's view.
/// Called from Gun/Enemy/Boss offline branches; one instance per combat scene.
/// </summary>
public sealed class OfflineCombatFeedback : MonoBehaviour
{
    private static OfflineCombatFeedback _instance;

    [Tooltip("Distance of the head-locked HUD (same as OfflineHelmetHud).")]
    [SerializeField] private float _hudDistance = 1.3f;

    [Header("Where enemies come from")]
    [SerializeField] private int _maxArrows = 6;
    [Tooltip("Enemies inside this angle from the view center get no arrow/edge glow.")]
    [SerializeField] private float _visibleAngle = 30f;
    [Tooltip("Arrow distance from the view center (canvas units, 1 = 1 mm at 1.3 m).")]
    [SerializeField] private float _arrowRadius = 370f;
    [Tooltip("Red glow on the edge of the view, on the enemy's side.")]
    [SerializeField] private float _glowRadius = 620f;
    [Tooltip("Enemies closer than this glow at full strength; farther ones fade out.")]
    [SerializeField] private float _nearDistance = 4f;
    [SerializeField] private float _farDistance = 30f;
    [Tooltip("Seconds the edge blinks after an enemy appears.")]
    [SerializeField] private float _spawnAlertTime = 0.9f;

    private readonly List<Image> _arrows = new();
    private readonly List<Image> _glows = new();
    private readonly List<Transform> _enemies = new();
    private readonly Dictionary<Transform, float> _spawnTimes = new();
    private Camera _head;
    private RectTransform _hud;
    private int _kills;
    private float _nextScan;

    private static OfflineCombatFeedback Instance
    {
        get
        {
            if (_instance == null)
                _instance = new GameObject("[Offline] Combat Feedback").AddComponent<OfflineCombatFeedback>();
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
    }

    private IEnumerator Start()
    {
        // Arriving on a globe map: show the place name once the fade is over.
        if (!OfflineSession.IsMapScene)
            yield break;
        yield return new WaitForSecondsRealtime(1.5f);
        Camera head = Camera.main;
        if (head == null)
            yield break;
        string place = OfflineLobbyFeedback.MapLabel(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        Vector3 front = head.transform.position + Vector3.ProjectOnPlane(head.transform.forward, Vector3.up).normalized * 2f + Vector3.up * 0.3f;
        OfflineFx.FloatingText(front, place, new Color(0.75f, 0.95f, 1f), 150f, 3.5f, 0.2f);
        OfflineFx.Burst(front, new Color(0.35f, 0.85f, 1f), 50, 1.5f, 0.05f, 1.5f, 0f, 0.6f);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    // ---------- Events from gameplay ----------

    public static void Shot(Gun gun, GameObject bullet)
    {
        if (gun == null)
            return;

        OfflineScore.AddShot();
        OfflineRecoil recoil = gun.GetComponent<OfflineRecoil>();
        if (recoil == null)
            recoil = gun.gameObject.AddComponent<OfflineRecoil>();
        recoil.Kick();

        // Bullets leave along -up of the gun point (see PlayerBullet).
        Transform gunPoint = gun.transform.childCount > 0 ? gun.transform.GetChild(0) : gun.transform;
        OfflineShotFx.Muzzle(gunPoint.position, -gunPoint.up);

        // Slight pitch change per shot so rapid fire does not sound like a loop.
        var audio = gun.GetComponent<AudioSource>();
        if (audio != null)
            audio.pitch = Random.Range(0.93f, 1.08f);

        // Sharp kick followed by a softer tail on the firing hand.
        OfflineFx.HapticNear(gun.transform, 0.55f, 0.04f);
        Instance.StartCoroutine(HapticTail(gun.transform));

        if (bullet != null && bullet.GetComponent<OfflineBulletTrail>() == null)
            bullet.AddComponent<OfflineBulletTrail>();
    }

    private static IEnumerator HapticTail(Transform gun)
    {
        yield return new WaitForSecondsRealtime(0.05f);
        if (gun != null)
            OfflineFx.HapticNear(gun, 0.2f, 0.08f);
    }

    /// <summary>A player bullet hit something: sparks and flash at the point (bigger on enemies).</summary>
    public static void BulletImpact(Vector3 point, Vector3 bulletDirection, bool enemy)
    {
        if (enemy)
            OfflineScore.AddHit();
        OfflineShotFx.Impact(point, -bulletDirection.normalized, enemy);
    }

    /// <summary>An enemy appeared: blink the edge of the view on its side and vibrate that hand.</summary>
    public static void EnemySpawned(Transform enemy)
    {
        if (enemy == null)
            return;

        OfflineCombatFeedback feedback = Instance;
        feedback._spawnTimes[enemy] = Time.unscaledTime;
        if (!feedback._enemies.Contains(enemy))
            feedback._enemies.Add(enemy);
        if (enemy.GetComponent<OfflineEnemyMarker>() == null)
            enemy.gameObject.AddComponent<OfflineEnemyMarker>();
        enemy.GetComponent<OfflineEnemyMarker>().Alert();

        Camera head = Camera.main;
        if (head != null)
        {
            float side = head.transform.InverseTransformPoint(enemy.position).x;
            HapticSide(side < 0f ? "Left" : "Right", 0.35f, 0.12f);
        }
    }

    private static void HapticSide(string side, float amplitude, float duration)
    {
        bool sent = false;
        foreach (var haptic in FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics.HapticImpulsePlayer>(FindObjectsSortMode.None))
        {
            for (Transform t = haptic.transform; t != null; t = t.parent)
            {
                if (t.name.Contains(side))
                {
                    haptic.SendHapticImpulse(amplitude, duration);
                    sent = true;
                    break;
                }
            }
        }
        if (!sent)
            OfflineFx.HapticAll(amplitude * 0.6f, duration);
    }

    public static void EnemyKilled(Enemy enemy)
    {
        if (enemy == null)
            return;

        Vector3 center = Center(enemy.transform);
        OfflineFx.Burst(center, new Color(1f, 0.45f, 0.1f), 36, 3.2f, 0.09f, 0.7f, 0.4f, 0.15f);
        OfflineFx.Burst(center, new Color(1f, 0.95f, 0.7f), 14, 5f, 0.05f, 0.35f);
        OfflineFx.FloatingText(center + Vector3.up * 0.35f, "+1", new Color(1f, 0.85f, 0.2f), 110f, 0.9f, 0.35f);
        OfflineFx.HitStop(0.045f);
        OfflineFx.HapticAll(0.3f, 0.07f);
        Instance.AddKill();
    }

    public static void BossHit(Boss boss, bool killed)
    {
        if (boss == null)
            return;

        Vector3 center = Center(boss.transform);
        OfflineFx.Run(FlashRed(boss.transform));
        OfflineFx.Run(OfflineFx.Punch(boss.transform, 0.08f, 0.15f));
        OfflineFx.Burst(center, new Color(1f, 0.3f, 0.2f), 16, 2.5f, 0.07f, 0.45f);

        if (killed)
        {
            OfflineFx.Burst(center, new Color(1f, 0.55f, 0.1f), 90, 5f, 0.14f, 1.1f, 0.3f, 0.4f);
            OfflineFx.Burst(center, Color.white, 30, 7f, 0.07f, 0.5f);
            OfflineFx.FloatingText(center + Vector3.up * 0.8f, "CHEFE DERROTADO!", new Color(1f, 0.8f, 0.2f), 120f, 1.8f, 0.4f);
            OfflineFx.HitStop(0.12f);
            OfflineFx.HapticAll(0.8f, 0.3f);
            Instance.AddKill();
        }
    }

    // ---------- End of the battle ----------

    /// <summary>Enemies (and bosses) still alive in the scene.</summary>
    public static int AliveEnemies()
    {
        int alive = 0;
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (enemy.gameObject.activeInHierarchy) alive++;
        foreach (Boss boss in FindObjectsByType<Boss>(FindObjectsSortMode.None))
            if (boss.gameObject.activeInHierarchy) alive++;
        return alive;
    }

    public static void BattleTimeUp()
    {
        OfflineCombatFeedback feedback = Instance;
        feedback.EnsureHud();
        Camera head = Camera.main;
        if (head != null)
            OfflineFx.FloatingText(head.transform.position + head.transform.forward * 1.5f, "ÚLTIMA ONDA!", new Color(1f, 0.85f, 0.2f), 120f, 2f, 0.1f);
        OfflineFx.HapticAll(0.5f, 0.2f);
        feedback.SetObjective("ELIMINE OS INIMIGOS RESTANTES", new Color(1f, 0.4f, 0.3f));
    }

    public static void RemainingEnemies(int remaining)
    {
        Instance.SetObjective($"ELIMINE OS INIMIGOS RESTANTES:  {remaining}", new Color(1f, 0.4f, 0.3f));
    }

    public static void AreaCleared()
    {
        OfflineCombatFeedback feedback = Instance;
        feedback.SetObjective("ÁREA LIMPA!", new Color(0.3f, 1f, 0.5f));
        Camera head = Camera.main;
        if (head != null)
            OfflineFx.Burst(head.transform.position + head.transform.forward * 1.5f, new Color(0.3f, 1f, 0.5f), 60, 2.5f, 0.06f, 1.2f, 0.3f, 0.3f);
        OfflineFx.HapticAll(0.6f, 0.25f);
    }

    private void SetObjective(string text, Color color)
    {
        OfflineHelmetHud.SetObjective(text, color);
    }

    // ---------- HUD ----------

    private void AddKill()
    {
        OfflineScore.AddKill();
        _kills++;
        OfflineHelmetHud.SetKills(_kills);
    }

    private void EnsureHud()
    {
        Camera head = Camera.main;
        if (head == null)
            return;
        if (_hud != null && _head == head)
            return;

        if (_hud != null)
            Destroy(_hud.gameObject);

        _head = head;
        _hud = OfflineFx.CreateCanvas("[Offline] Combat HUD", head.transform, new Vector2(1000, 1000), 0.001f, 150);
        _hud.localPosition = new Vector3(0f, 0f, _hudDistance);
        _hud.localRotation = Quaternion.identity;

        _arrows.Clear();
        _glows.Clear();
        for (int i = 0; i < _maxArrows; i++)
        {
            Image glow = OfflineFx.AddImage(_hud, "Enemy Edge Glow", OfflineFx.Soft, new Color(1f, 0.05f, 0.05f, 0f), new Vector2(520, 520));
            glow.material = OfflineFx.OverlayMaterial;
            glow.enabled = false;
            _glows.Add(glow);

            // Chevron: a diamond with a darker notch reads as an arrow pointing outwards.
            Image arrow = OfflineFx.AddImage(_hud, "Enemy Arrow", OfflineFx.White, new Color(1f, 0.2f, 0.15f, 0.95f), new Vector2(60, 60));
            arrow.material = OfflineFx.OverlayMaterial;
            Image notch = OfflineFx.AddImage(arrow.transform, "Notch", OfflineFx.White, new Color(0.25f, 0f, 0f, 1f), new Vector2(42, 42), new Vector2(-18f, -18f));
            notch.material = OfflineFx.OverlayMaterial;
            arrow.enabled = false;
            notch.enabled = false;
            _arrows.Add(arrow);
        }
    }

    private void Update()
    {
        EnsureHud();
        if (_head == null || _hud == null)
            return;

        // Life, heal, mission and compass live on the helmet HUD; this canvas keeps only the enemy arrows.
        OfflineHelmetHud.Ensure();

        if (Time.unscaledTime >= _nextScan)
        {
            _nextScan = Time.unscaledTime + 0.25f;
            _enemies.Clear();
            foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                _enemies.Add(enemy.transform);
            foreach (Boss boss in FindObjectsByType<Boss>(FindObjectsSortMode.None))
                _enemies.Add(boss.transform);

            foreach (Gun gun in FindObjectsByType<Gun>(FindObjectsSortMode.None))
                if (gun.enabled && gun.GetComponent<OfflineGunLaser>() == null)
                    gun.gameObject.AddComponent<OfflineGunLaser>();
        }

        Transform head = _head.transform;
        float now = Time.unscaledTime;
        float pulse = 0.65f + 0.35f * Mathf.Sin(now * 8f);
        int used = 0;
        foreach (Transform enemy in _enemies)
        {
            if (used >= _arrows.Count)
                break;
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            Vector3 local = head.InverseTransformPoint(enemy.position);
            bool inView = local.z > 0f && Vector3.Angle(Vector3.forward, local) < _visibleAngle;
            bool alerting = _spawnTimes.TryGetValue(enemy, out float spawned) && now - spawned < _spawnAlertTime;
            if (inView)
                continue;

            Vector2 direction = new Vector2(local.x, local.y);
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.down;
            direction.Normalize();
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            float distance = local.magnitude;
            float proximity = 1f - Mathf.Clamp01((distance - _nearDistance) / Mathf.Max(0.1f, _farDistance - _nearDistance));
            // Three quick blinks right after the enemy appears.
            float blink = alerting ? (Mathf.Repeat((now - spawned) / _spawnAlertTime * 3f, 1f) < 0.55f ? 1f : 0.15f) : 0f;

            Image glow = _glows[used];
            glow.enabled = true;
            glow.rectTransform.anchoredPosition = direction * _glowRadius;
            float glowAlpha = Mathf.Max(0.12f + 0.45f * proximity * pulse, 0.85f * blink);
            glow.color = new Color(1f, 0.05f, 0.05f, glowAlpha);
            glow.rectTransform.localScale = Vector3.one * (0.8f + 0.5f * proximity + 0.4f * blink);

            Image arrow = _arrows[used];
            arrow.enabled = true;
            Image notch = arrow.transform.GetChild(0).GetComponent<Image>();
            notch.enabled = true;
            var rect = arrow.rectTransform;
            rect.anchoredPosition = direction * _arrowRadius;
            // The diamond's corner at +45deg points along the direction.
            rect.localRotation = Quaternion.Euler(0f, 0f, angle - 45f);
            rect.localScale = Vector3.one * (0.8f + 0.25f * pulse + 0.3f * proximity + 0.4f * blink);
            arrow.color = new Color(1f, 0.2f, 0.15f, 0.6f + 0.4f * Mathf.Max(pulse * (0.5f + 0.5f * proximity), blink));

            used++;
        }
        for (int i = used; i < _arrows.Count; i++)
        {
            _arrows[i].enabled = false;
            _arrows[i].transform.GetChild(0).GetComponent<Image>().enabled = false;
            _glows[i].enabled = false;
        }
    }

    // ---------- Helpers ----------

    private static Vector3 Center(Transform target)
    {
        Renderer renderer = target.GetComponentInChildren<Renderer>();
        return renderer != null ? renderer.bounds.center : target.position;
    }

    private static IEnumerator FlashRed(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        var block = new MaterialPropertyBlock();
        foreach (Renderer r in renderers)
        {
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", new Color(1f, 0.25f, 0.25f));
            block.SetColor("_Color", new Color(1f, 0.25f, 0.25f));
            r.SetPropertyBlock(block);
        }
        yield return new WaitForSecondsRealtime(0.1f);
        foreach (Renderer r in renderers)
        {
            if (r != null)
                r.SetPropertyBlock(null);
        }
    }
}

/// <summary>Visual kick of the gun when it fires: snaps back and up, then settles with a small spring overshoot.</summary>
public sealed class OfflineRecoil : MonoBehaviour
{
    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private float _time = 10f;
    private bool _captured;

    public void Kick()
    {
        if (!_captured)
        {
            _basePosition = transform.localPosition;
            _baseRotation = transform.localRotation;
            _captured = true;
        }
        _time = 0f;
    }

    private void LateUpdate()
    {
        if (!_captured)
            return;

        _time += Time.deltaTime;
        // Damped spring: 1 at the shot, crosses zero and overshoots a little before resting.
        float k = Mathf.Exp(-_time * 14f) * Mathf.Cos(_time * 26f);
        if (_time > 0.6f)
            k = 0f;

        transform.localPosition = _basePosition + _baseRotation * (Vector3.back * 0.045f * k);
        transform.localRotation = _baseRotation * Quaternion.Euler(-11f * k, 0f, 2.5f * k);
    }
}

/// <summary>
/// Energy look for pooled player bullets: a thin white core trail inside a wide blue one, plus a glowing head.
/// Trails are cleared on reuse so they never streak across the scene.
/// </summary>
public sealed class OfflineBulletTrail : MonoBehaviour
{
    [Tooltip("Hide the bullet's own mesh (red sphere) so the projectile is a pure energy bolt.")]
    [SerializeField] private bool _hideBulletMesh = true;

    private TrailRenderer _glow;
    private TrailRenderer _core;

    private void Awake()
    {
        if (_hideBulletMesh)
            foreach (MeshRenderer mesh in GetComponentsInChildren<MeshRenderer>())
                mesh.enabled = false;

        _glow = MakeTrail("Glow Trail", 0.09f, 0.16f, new Color(0.3f, 0.75f, 1f), new Color(0.1f, 0.3f, 1f), 0.75f);
        _core = MakeTrail("Core Trail", 0.03f, 0.1f, Color.white, new Color(0.6f, 0.95f, 1f), 1f);
    }

    private TrailRenderer MakeTrail(string trailName, float width, float time, Color start, Color end, float alpha)
    {
        var go = new GameObject(trailName);
        go.transform.SetParent(transform, false);
        var trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = OfflineFx.LineMaterial;
        trail.time = time;
        trail.minVertexDistance = 0.02f;
        trail.widthCurve = AnimationCurve.EaseInOut(0f, width, 1f, 0f);
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        return trail;
    }

    private void OnEnable()
    {
        if (_glow != null) _glow.Clear();
        if (_core != null) _core.Clear();
    }

    private void LateUpdate()
    {
        // Glowing head that travels with the bullet.
        OfflineShotFx.Glow(transform.position, 0.22f, OfflineShotFx.Energy, 0.05f);
        OfflineShotFx.Glow(transform.position, 0.1f, new Color(1f, 1f, 1f, 0.8f), 0.05f);
    }
}

/// <summary>Red diamond floating above an enemy, drawn on top of everything so it can be spotted behind cover.</summary>
public sealed class OfflineEnemyMarker : MonoBehaviour
{
    private RectTransform _canvas;
    private Image _diamond;
    private Image _halo;
    private float _alertUntil;
    private float _height = 1f;

    public void Alert() => _alertUntil = Time.unscaledTime + 1.2f;

    private void Start()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
            _height = renderer.bounds.max.y - transform.position.y + 0.35f;

        _canvas = OfflineFx.CreateCanvas("[Offline] Enemy Marker", null, new Vector2(400, 400), 0.001f, 130);
        _canvas.gameObject.AddComponent<OfflineBillboard>();
        _halo = OfflineFx.AddImage(_canvas, "Halo", OfflineFx.Soft, new Color(1f, 0.1f, 0.05f, 0.5f), new Vector2(260, 260));
        _halo.material = OfflineFx.OverlayMaterial;
        _diamond = OfflineFx.AddImage(_canvas, "Diamond", OfflineFx.White, new Color(1f, 0.25f, 0.15f, 1f), new Vector2(70, 70));
        _diamond.material = OfflineFx.OverlayMaterial;
        _diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private void LateUpdate()
    {
        if (_canvas == null)
            return;
        bool visible = gameObject.activeInHierarchy;
        _canvas.gameObject.SetActive(visible);
        if (!visible)
            return;

        Camera head = Camera.main;
        float distance = head != null ? Vector3.Distance(head.transform.position, transform.position) : 5f;
        float now = Time.unscaledTime;
        bool alerting = now < _alertUntil;
        float bob = Mathf.Sin(now * 3f) * 0.05f;
        _canvas.position = transform.position + Vector3.up * (_height + bob);
        // Constant apparent size: grows with distance.
        _canvas.localScale = Vector3.one * 0.001f * Mathf.Clamp(distance * 0.35f, 0.6f, 6f) * (alerting ? 1.4f : 1f);
        float pulse = 0.6f + 0.4f * Mathf.Sin(now * (alerting ? 16f : 6f));
        _halo.color = new Color(1f, 0.1f, 0.05f, 0.35f * pulse + (alerting ? 0.3f : 0f));
        _diamond.color = new Color(1f, 0.25f, 0.15f, 0.7f + 0.3f * pulse);
    }

    private void OnDisable()
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_canvas != null)
            Destroy(_canvas.gameObject);
    }
}
