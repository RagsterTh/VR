using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
public class HandInteraction : MonoBehaviour
{
    PhotonView _phView;
    IShootable _target;
    HapticImpulsePlayer _haptic;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _phView = GetComponentInParent<PhotonView>();
        _haptic = GetComponentInParent<HapticImpulsePlayer>();
        if (!OfflineSession.IsOffline && !_phView.IsMine)
        {
            this.enabled = false;
        }
    }
    public void Shoot()
    {
        if (_target == null)
            return;
        _target.Hit();
        Pulse(0.28f, 0.07f);
    }
    public void SetTarget(HoverEnterEventArgs value)
    {
        _target = value.interactableObject.transform.GetComponent<IShootable>();
    }
    public void NullTarget()
    {
        _target = null;
    }
    public void HandTouch(HoverEnterEventArgs value)
    {
        if (value.interactableObject == null)
            return;
        IShootable target = value.interactableObject.transform.GetComponent<IShootable>();
        if (target == null)
            return;
        target.Hit();
        Pulse(0.28f, 0.07f);
    }
    public void HandUITouch(UIHoverEventArgs value)
    {
        if (value.uiObject == null)
            return;
        Button button = value.uiObject.GetComponentInParent<Button>();
        if (button != null && button.IsInteractable())
        {
            button.onClick.Invoke();
            Pulse(0.2f, 0.05f);
        }
    }

    private void Pulse(float amplitude, float duration)
    {
        if (OfflineSession.IsOffline && _haptic != null && _haptic.isActiveAndEnabled)
            _haptic.SendHapticImpulse(amplitude, duration);
    }
}
