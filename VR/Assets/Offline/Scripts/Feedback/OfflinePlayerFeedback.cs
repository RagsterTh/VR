using UnityEngine;

/// <summary>
/// Offline player feedback: red vignette + both controllers vibrating when taking damage, green flash when
/// healing, and a pulsing red edge while life is low. Reads the shared life bar, no gameplay change.
/// </summary>
public sealed class OfflinePlayerFeedback : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float _lowLifeThreshold = 0.3f;

    private PlayersLifeBar _lifeBar;
    private float _lastLife = -1f;

    private void Update()
    {
        if (_lifeBar == null || !_lifeBar.isActiveAndEnabled)
        {
            _lifeBar = GetComponentInChildren<PlayersLifeBar>();
            _lastLife = -1f;
            OfflineViewOverlay.SetLowLife(false);
            if (_lifeBar == null)
                return;
        }

        float life = _lifeBar.CurrentLife;
        if (_lastLife >= 0f)
        {
            if (life < _lastLife - 0.01f)
            {
                float hit = Mathf.Clamp01((_lastLife - life) / Mathf.Max(1f, _lifeBar.MaxLife) * 4f);
                OfflineViewOverlay.Flash(new Color(0.95f, 0.05f, 0.05f), 0.55f + 0.35f * hit, 0.4f);
                OfflineFx.HapticAll(0.5f + 0.4f * hit, 0.15f);
            }
            else if (life > _lastLife + 0.01f)
            {
                OfflineViewOverlay.Flash(new Color(0.2f, 1f, 0.45f), 0.55f, 0.6f);
            }
        }
        _lastLife = life;

        OfflineViewOverlay.SetLowLife(life > 0f && life <= _lifeBar.MaxLife * _lowLifeThreshold);
    }

    private void OnDestroy()
    {
        OfflineViewOverlay.SetLowLife(false);
    }
}
