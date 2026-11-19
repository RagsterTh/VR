using System.Collections;
using System.Reflection;
using UnityEngine;

public sealed class OfflineWaves : MonoBehaviour
{
    public const int WaveCount = 3;
    private static readonly float[] WaveEnds = { 0.27f, 0.6f };
    private static readonly float[] WaveSpeed = { 0.75f, 1.05f, 1.4f };

    private static OfflineWaves _instance;

    public static int Wave => _instance != null ? _instance._wave : 0;

    public static float SpawnRateMultiplier =>
        (_instance != null && _instance._wave > 0 ? WaveSpeed[Mathf.Clamp(_instance._wave - 1, 0, WaveCount - 1)] : WaveSpeed[0])
        * OfflineDifficulty.SpawnScale;

    private static readonly FieldInfo CountdownField = typeof(WaveTime).GetField("countdown", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo StartTimeField = typeof(WaveTime).GetField("startTime", BindingFlags.NonPublic | BindingFlags.Instance);

    private GameOverManager _gameOver;
    private WaveTime[] _clocks;
    private float _frozenClock = -1f;
    private int _cap;
    private int _wave;
    private bool _break;

    private void Awake() => _instance = this;

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public static void BattleStarted()
    {
        if (_instance == null || _instance._wave > 0)
            return;
        _instance._wave = 1;
        _instance.Announce();
    }

    private void Update()
    {
        if (_wave == 0 || _break || _wave >= WaveCount)
            return;

        if (_gameOver == null)
        {
            _gameOver = FindAnyObjectByType<GameOverManager>();
            FieldInfo field = typeof(GameOverManager).GetField("enemiesKilledCap", BindingFlags.NonPublic | BindingFlags.Instance);
            _cap = _gameOver != null && field != null ? (int)field.GetValue(_gameOver) : 0;
        }
        if (_gameOver == null || _cap <= 0)
            return;

        int end = Mathf.Max(1, Mathf.RoundToInt(_cap * WaveEnds[_wave - 1]));
        bool killsDone = _gameOver.EnemiesKilled >= end;
        bool timeDone = ClockFraction() <= 1f - (float)_wave / WaveCount;
        if ((killsDone || timeDone) && _gameOver.EnemiesKilled < _cap)
            StartCoroutine(Breather());
    }

    private float ClockFraction()
    {
        _clocks ??= FindObjectsByType<WaveTime>(FindObjectsSortMode.None);
        if (CountdownField == null || StartTimeField == null)
            return 1f;
        float fraction = 1f;
        foreach (WaveTime clock in _clocks)
        {
            if (clock == null)
                continue;
            float start = (int)StartTimeField.GetValue(clock);
            if (start > 0f)
                fraction = Mathf.Min(fraction, (float)CountdownField.GetValue(clock) / start);
        }
        return fraction;
    }

    private void LateUpdate()
    {
        if (_frozenClock < 0f || _clocks == null || CountdownField == null)
            return;
        foreach (WaveTime clock in _clocks)
            if (clock != null && (float)CountdownField.GetValue(clock) < (int)StartTimeField.GetValue(clock))
                CountdownField.SetValue(clock, _frozenClock);
    }

    private IEnumerator Breather()
    {
        _break = true;
        SetSpawners(false);
        _clocks ??= FindObjectsByType<WaveTime>(FindObjectsSortMode.None);
        if (CountdownField != null && StartTimeField != null)
        {
            float lowest = float.MaxValue;
            foreach (WaveTime clock in _clocks)
                if (clock != null) lowest = Mathf.Min(lowest, (float)CountdownField.GetValue(clock));
            _frozenClock = lowest < float.MaxValue ? lowest : -1f;
        }

        OfflineHelmetHud.SetObjective($"ONDA {_wave} CONCLUÍDA  -  ELIMINE OS RESTANTES", new Color(0.3f, 1f, 0.55f));
        Camera head = Camera.main;
        if (head != null)
            OfflineFx.FloatingText(head.transform.position + head.transform.forward * 1.6f, $"ONDA {_wave} CONCLUÍDA!", new Color(0.3f, 1f, 0.55f), 120f, 1.8f, 0.1f);
        OfflineFx.HapticAll(0.5f, 0.2f);

        float waited = 0f;
        while (OfflineCombatFeedback.AliveEnemies() > 0 && waited < 20f)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        for (int i = 3; i > 0; i--)
        {
            OfflineHelmetHud.SetObjective($"ONDA {_wave + 1} EM {i}...", new Color(1f, 0.8f, 0.25f));
            OfflineFx.HapticAll(0.2f, 0.05f);
            yield return new WaitForSeconds(1f);
        }

        if (_gameOver != null && _gameOver.EnemiesKilled >= _cap)
        {
            _frozenClock = -1f;
            yield break;
        }

        _frozenClock = -1f;
        _wave++;
        Announce();
        SetSpawners(true);
        _break = false;
    }

    private void Announce()
    {
        Color color = _wave == WaveCount ? new Color(1f, 0.35f, 0.25f) : new Color(0.4f, 0.9f, 1f);
        string title = _wave == WaveCount ? $"ONDA FINAL" : $"ONDA {_wave}";
        OfflineHelmetHud.SetObjective($"{title}  -  {OfflineDifficulty.Label(OfflineDifficulty.Current)}", color);
        Camera head = Camera.main;
        if (head != null)
        {
            Vector3 front = head.transform.position + head.transform.forward * 1.8f;
            OfflineFx.FloatingText(front, title, color, 170f, 1.8f, 0.15f);
            OfflineFx.Burst(front, color, 40, 2f, 0.05f, 1f, 0f, 0.4f);
        }
        OfflineFx.HapticAll(0.6f, 0.2f);
    }

    private static void SetSpawners(bool on)
    {
        foreach (Spawner spawner in FindObjectsByType<Spawner>(FindObjectsSortMode.None))
        {
            spawner.StopAllCoroutines();
            spawner.SetBool(!on);
            if (on)
                spawner.StartCoroutine("Spawn");
        }
    }
}
