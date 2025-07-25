using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
public class HandInteraction : MonoBehaviour
{
    PhotonView _phView;
    IShootable _target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _phView = GetComponentInParent<PhotonView>();
        if (!_phView.IsMine)
        {
            this.enabled = false;
        }
    }
    public void Shoot()
    {
        //print("atirei"+_target);    
        _target?.Hit();
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

        print(value.interactableObject.transform.name);
        value.interactableObject.transform.GetComponent<IShootable>().Hit();
    }
    public void HandUITouch(UIHoverEventArgs value)
    {
        value.uiObject.GetComponent<Button>().onClick.Invoke();
    }
}
