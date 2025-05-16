using Photon.Pun;
using StarterAssets;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
public class Gun : MonoBehaviour
{
    [SerializeField] float fireSpeed = 20;
    PhotonView _phView;
    [SerializeField] Transform _gunDirection;
    IShootable _target;

    //Temporário
    [SerializeField] ContinuousMoveProvider _continuesMoveProvider;
    [SerializeField] StarterAssetsInputs _starterAssetsInputs;
    [SerializeField] XROrigin _xrOrigin;
    [SerializeField]InputActionManager _inputActionManager;
    [SerializeField]ContinuousTurnProvider _continuousTurnProvider;
    [SerializeField]FirstPersonController _firstPersonController;
    [SerializeField]XRBodyTransformer _bodyTransformer;
    [SerializeField]LocomotionMediator _locomotionMediator;
    [SerializeField] PlayerInput _playerInput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _phView = GetComponentInParent<PhotonView>();
        if (!_phView.IsMine)
        {
            this.enabled = false;
            _continuesMoveProvider.enabled = false;
            _starterAssetsInputs.enabled = false;
            _xrOrigin.enabled = false;
            _inputActionManager.enabled = false;
            _continuousTurnProvider.enabled = false;
            _firstPersonController.enabled = false;
            _bodyTransformer.enabled = false;
            _locomotionMediator.enabled = false;
            _playerInput.enabled = false;
        }
    }


    void FixedUpdate()
    {
        //Aim();
    }
    void Aim()
    {
        if (Physics.Raycast(_gunDirection.position, _gunDirection.forward, out RaycastHit hit))
        {
            if(hit.collider.TryGetComponent(out IShootable target))
            {
                this._target = target;
            }
        }
        else
        {
            _target = null;
        }
    }
    public void Shoot()
    {
        _target?.Hit();
    }
    public void SetTarget(HoverEnterEventArgs value)
    {
        _target = value.interactableObject.transform.GetComponent<IShootable>();
    }
}
