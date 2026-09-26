using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using Photon.Pun;

public class PlayersLifeBar : MonoBehaviourPun
{
    [SerializeField] private GameObject[] _lifeBar;
    [SerializeField] float _maxLife;
    [Header("Feedback de vida")]
    [SerializeField] private TMP_Text _lifeText;
    [SerializeField] private Image _damageFlash;
    [SerializeField] private Color _healthyColor = new(0.18f, 0.95f, 0.67f, 1f);
    [SerializeField] private Color _warningColor = new(1f, 0.66f, 0.18f, 1f);
    [SerializeField] private Color _criticalColor = new(1f, 0.24f, 0.2f, 1f);

    float currentLife;
    private float _flashRemaining;

    public float CurrentLife { get => currentLife; set => currentLife = value; }
    public GameObject[] LifeBar { get => _lifeBar; set => _lifeBar = value; }
    public float MaxLife => _maxLife;
    public bool IsFull => currentLife >= _maxLife;

    void Awake()
    {
        ServiceLocator.Register(this);
        EnsureFeedbackUI();
    }

    void Start()
    {
        CurrentLife = _maxLife;
        Debug.Log($"[PlayersLifeBar] {name} Start(): maxLife={_maxLife}, currentLife={currentLife}");
        UpdateVisual();

        var gameOverManager = ServiceLocator.Get<GameOverManager>();
        if (gameOverManager != null)
        {
            gameOverManager.RegisterLifeBar(this);
            Debug.Log($"[PlayersLifeBar] {name} registered in GameOverManager (total registered: {gameOverManager.PlayersLifeBars.Count})");
        }
        else
        {
            Debug.LogWarning($"[PlayersLifeBar] {name} Start(): no GameOverManager found in ServiceLocator yet.");
        }
    }

    void OnDestroy()
    {
        var gameOverManager = ServiceLocator.Get<GameOverManager>();
        if (gameOverManager != null)
            gameOverManager.UnregisterLifeBar(this);
    }

    private void Update()
    {
        if (_damageFlash == null || _flashRemaining <= 0f)
            return;

        _flashRemaining = Mathf.Max(0f, _flashRemaining - Time.unscaledDeltaTime);
        Color color = _damageFlash.color;
        color.a = _flashRemaining / 0.35f * 0.7f;
        _damageFlash.color = color;
    }

    public void TakeDamage(float amount)
    {
        if (OfflineSession.IsOffline)
        {
            ApplyDamage(amount);
            return;
        }

        Debug.Log($"[PlayersLifeBar] {name} TakeDamage({amount}) called, dispatching RPC_TakeDamage to all clients.");
        // The RPC lives on PlayerPrefabNetwork (on the prefab root, same GameObject as the PhotonView) because
        // PUN only looks for [PunRPC] methods on components attached to the exact GameObject the PhotonView sits
        // on - it does not search children. This script lives on a nested "Image" child, so its own RPCs would
        // never be found.
        PlayerPrefabNetwork playerNetwork = GetComponentInParent<PlayerPrefabNetwork>();
        if (playerNetwork != null)
            playerNetwork.TakeDamage(amount);
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f)
            return;
        var gameOverManager = ServiceLocator.Get<GameOverManager>();
        IReadOnlyList<PlayersLifeBar> targets;
        if (gameOverManager != null && gameOverManager.PlayersLifeBars.Count > 0)
        {
            targets = gameOverManager.PlayersLifeBars;
        }
        else
        {
            Debug.LogWarning($"[PlayersLifeBar] {name} ApplyDamage: no GameOverManager/registered life bars found, applying damage only to self.");
            targets = new List<PlayersLifeBar> { this };
        }

        foreach (var lifeBar in targets)
        {
            float before = lifeBar.CurrentLife;
            lifeBar.CurrentLife = Mathf.Max(0, lifeBar.CurrentLife - amount);
            Debug.Log($"[PlayersLifeBar] {lifeBar.name} life {before} -> {lifeBar.CurrentLife} (damage={amount})");
            lifeBar.UpdateVisual();
            if (lifeBar.CurrentLife < before)
                lifeBar.ShowDamageFeedback();
        }

        if (gameOverManager != null)
            gameOverManager.VerifyLose();
    }

    public void Heal(float amount)
    {
        if (OfflineSession.IsOffline)
        {
            ApplyHeal(amount);
            return;
        }

        // Same route as damage: the RPC lives on PlayerPrefabNetwork, next to the PhotonView.
        PlayerPrefabNetwork playerNetwork = GetComponentInParent<PlayerPrefabNetwork>();
        if (playerNetwork != null)
            playerNetwork.Heal(amount);
    }

    public void ApplyHeal(float amount)
    {
        if (amount <= 0f)
            return;
        // Life is shared like damage: every registered life bar is healed, capped at its max life.
        var gameOverManager = ServiceLocator.Get<GameOverManager>();
        IReadOnlyList<PlayersLifeBar> targets = gameOverManager != null && gameOverManager.PlayersLifeBars.Count > 0
            ? gameOverManager.PlayersLifeBars
            : new List<PlayersLifeBar> { this };

        foreach (var lifeBar in targets)
        {
            lifeBar.CurrentLife = Mathf.Min(lifeBar._maxLife, lifeBar.CurrentLife + amount);
            lifeBar.UpdateVisual();
        }
    }

    void UpdateVisual()
    {
        float fraction = _maxLife > 0f ? Mathf.Clamp01(CurrentLife / _maxLife) : 0f;
        Color stateColor = fraction <= 0.25f ? _criticalColor :
            fraction <= 0.5f ? _warningColor : _healthyColor;
        if (_lifeText != null)
        {
            _lifeText.text = $"VIDA  {Mathf.CeilToInt(CurrentLife)} / {Mathf.CeilToInt(_maxLife)}";
            _lifeText.color = stateColor;
        }

        if (LifeBar == null || LifeBar.Length == 0)
        {
            Debug.LogWarning($"{nameof(PlayersLifeBar)}: LifeBar is not assigned on {name}.", this);
            return;
        }

        foreach (GameObject lifeBar in LifeBar)
        {
            if (lifeBar == null || !lifeBar.TryGetComponent(out Image lifeBarImg))
                continue;
            lifeBarImg.fillAmount = fraction;
            lifeBarImg.color = stateColor;
        }
    }

    private void ShowDamageFeedback()
    {
        if (!OfflineSession.IsOffline)
            return;

        _flashRemaining = 0.35f;
        if (_damageFlash != null)
        {
            Color color = _damageFlash.color;
            color.a = 0.7f;
            _damageFlash.color = color;
        }

        PlayerPrefabNetwork player = GetComponentInParent<PlayerPrefabNetwork>();
        if (player == null)
            return;
        foreach (HapticImpulsePlayer haptic in player.GetComponentsInChildren<HapticImpulsePlayer>(true))
        {
            if (haptic != null && haptic.isActiveAndEnabled)
                haptic.SendHapticImpulse(0.45f, 0.12f);
        }
    }

    private void EnsureFeedbackUI()
    {
        RectTransform bar = transform as RectTransform;
        Image barImage = GetComponent<Image>();
        if (bar == null || barImage == null)
            return;

        if (bar.parent != null && bar.parent.Find("LifeFrame") == null)
        {
            var frameObject = new GameObject("LifeFrame", typeof(RectTransform));
            var frame = (RectTransform)frameObject.transform;
            frame.SetParent(bar.parent, false);
            frame.anchoredPosition = bar.anchoredPosition;
            frame.sizeDelta = bar.sizeDelta + new Vector2(24f, 24f);
            frame.localScale = bar.localScale;
            frame.localRotation = bar.localRotation;
            var background = frameObject.AddComponent<Image>();
            background.color = new Color(0.015f, 0.035f, 0.05f, 0.92f);
            background.raycastTarget = false;
            frame.SetSiblingIndex(bar.GetSiblingIndex());
        }

        if (_lifeText == null)
        {
            Transform existing = bar.Find("LifeValue");
            if (existing != null)
                _lifeText = existing.GetComponent<TMP_Text>();
            if (_lifeText == null)
            {
                var textObject = new GameObject("LifeValue", typeof(RectTransform));
                var rect = (RectTransform)textObject.transform;
                rect.SetParent(bar, false);
                rect.anchoredPosition = new Vector2(0f, 89f);
                rect.sizeDelta = new Vector2(980f, 86f);
                rect.localRotation = Quaternion.Inverse(bar.parent.localRotation);
                var value = textObject.AddComponent<TextMeshProUGUI>();
                value.fontSize = 45f;
                value.fontStyle = FontStyles.Bold;
                value.alignment = TextAlignmentOptions.Center;
                value.raycastTarget = false;
                _lifeText = value;
            }
        }

        if (_damageFlash == null)
        {
            Transform existing = bar.Find("DamageFlash");
            if (existing != null)
                _damageFlash = existing.GetComponent<Image>();
            if (_damageFlash == null)
            {
                var flashObject = new GameObject("DamageFlash", typeof(RectTransform));
                var rect = (RectTransform)flashObject.transform;
                rect.SetParent(bar, false);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = bar.sizeDelta;
                _damageFlash = flashObject.AddComponent<Image>();
                _damageFlash.sprite = barImage.sprite;
                _damageFlash.type = Image.Type.Filled;
                _damageFlash.fillMethod = Image.FillMethod.Horizontal;
                _damageFlash.color = new Color(1f, 1f, 1f, 0f);
                _damageFlash.raycastTarget = false;
                rect.SetSiblingIndex(0);
            }
        }
    }
}
