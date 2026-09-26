using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>VR defeat screen. Keeps the chosen session so retry returns to the same battle.</summary>
public sealed class OfflineDefeatScreen : MonoBehaviour
{
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _menuButton;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private Transform _panel;
    [SerializeField] private float _panelDistance = 1.3f;
    [SerializeField] private float _panelHeightOffset = -0.15f;

    private bool _leaving;

    private void Awake()
    {
        if (_panel != null && _panel.TryGetComponent(out RotateCanvas rotation))
            rotation.enabled = false;
        if (_retryButton != null)
            _retryButton.onClick.AddListener(Retry);
        if (_menuButton != null)
            _menuButton.onClick.AddListener(ReturnToMenu);
    }

    private IEnumerator Start()
    {
        float waited = 0f;
        OfflinePlayerRig rig = FindAnyObjectByType<OfflinePlayerRig>();
        while (rig != null && !rig.IsPlaced && waited < 3f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        Camera head = Camera.main;
        if (head != null && _panel != null)
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();
            _panel.position = head.transform.position + forward * _panelDistance + Vector3.up * _panelHeightOffset;
            _panel.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        if (_statusText != null)
            _statusText.text = "Sua vida chegou a zero. Selecione uma opção com o raio do controle e aperte o gatilho.";
    }

    public void Retry()
    {
        if (_leaving)
            return;
        _leaving = true;
        if (_statusText != null)
            _statusText.text = "Preparando nova tentativa...";
        OfflineSession.RetryBattle();
    }

    public void ReturnToMenu()
    {
        if (_leaving)
            return;
        _leaving = true;
        if (_statusText != null)
            _statusText.text = "Voltando à seleção de modo...";
        OfflineSession.ReturnToEntry();
    }
}
