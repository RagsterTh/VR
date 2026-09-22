using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectPool : MonoBehaviourPunCallbacks
{
    [SerializeField] private List<int> _pooledObjects;
    [SerializeField] private GameObject _objectToPool;
    [SerializeField] private int _amountToPool;

    private readonly List<GameObject> _offlinePooledObjects = new();
    private PhotonView _phView;
    private bool _initialized;

    private void Awake()
    {
        _phView = GetComponent<PhotonView>();
        Initialize();
    }

    public override void OnJoinedRoom()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_initialized || _objectToPool == null)
            return;

        if (OfflineSession.IsOffline)
        {
            InitializeOfflinePool();
            _initialized = true;
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
            return;

        GameObject collection = new GameObject(_objectToPool.name + " Collection (Network)");
        collection.transform.SetParent(transform, false);
        _pooledObjects = new List<int>();

        for (int i = 0; i < _amountToPool; i++)
        {
            GameObject instance = PhotonNetwork.InstantiateRoomObject(
                _objectToPool.name,
                collection.transform.position,
                _objectToPool.transform.rotation);
            _pooledObjects.Add(instance.GetPhotonView().ViewID);
        }

        _phView.RPC(nameof(RPC_SetPool), RpcTarget.Others, _pooledObjects.ToArray());
        _initialized = true;
    }

    private void InitializeOfflinePool()
    {
        GameObject collection = new GameObject(_objectToPool.name + " Collection (Offline)");
        collection.transform.SetParent(transform, false);

        for (int i = 0; i < _amountToPool; i++)
        {
            GameObject instance = Instantiate(
                _objectToPool,
                collection.transform.position,
                _objectToPool.transform.rotation,
                collection.transform);
            instance.SetActive(false);
            _offlinePooledObjects.Add(instance);
        }
    }

    public GameObject GetPooledObject()
    {
        if (OfflineSession.IsOffline)
            return _offlinePooledObjects.FirstOrDefault(item => item != null && !item.activeInHierarchy);

        if (_pooledObjects == null)
            return null;

        foreach (int viewId in _pooledObjects)
        {
            PhotonView pooledView = PhotonNetwork.GetPhotonView(viewId);
            if (pooledView != null && !pooledView.gameObject.activeInHierarchy)
                return pooledView.gameObject;
        }

        return null;
    }

    public GameObject CallObject(Vector3 origin)
    {
        GameObject pooledObject = GetPooledObject();
        if (pooledObject == null)
        {
            Debug.LogWarning($"[ObjectPool] Pool '{name}' has no available object.", this);
            return null;
        }

        if (OfflineSession.IsOffline)
        {
            ActivateObject(pooledObject, origin, pooledObject.transform.rotation);
            return pooledObject;
        }

        _phView.RPC(nameof(RPC_CallObject), RpcTarget.AllBuffered, origin, pooledObject.GetPhotonView().ViewID);
        return pooledObject;
    }

    public void CallObject(Vector3 origin, Quaternion rotation)
    {
        GameObject pooledObject = GetPooledObject();
        if (pooledObject == null)
        {
            Debug.LogWarning($"[ObjectPool] Pool '{name}' has no available object.", this);
            return;
        }

        if (OfflineSession.IsOffline)
        {
            ActivateObject(pooledObject, origin, rotation);
            return;
        }

        _phView.RPC(nameof(RPC_CallObjectWithRotation), RpcTarget.AllBuffered, origin, rotation, pooledObject.GetPhotonView().ViewID);
    }

    private static void ActivateObject(GameObject pooledObject, Vector3 origin, Quaternion rotation)
    {
        pooledObject.transform.SetPositionAndRotation(origin, rotation);
        pooledObject.SetActive(true);
    }

    [PunRPC]
    public void RPC_SetPool(int[] pool)
    {
        _pooledObjects = pool.ToList();
        _initialized = true;
    }

    [PunRPC]
    public void RPC_CallObject(Vector3 origin, int photonID)
    {
        PhotonView pooledView = PhotonNetwork.GetPhotonView(photonID);
        if (pooledView != null)
            ActivateObject(pooledView.gameObject, origin, pooledView.transform.rotation);
    }

    [PunRPC]
    public void RPC_CallObjectWithRotation(Vector3 origin, Quaternion rotation, int photonID)
    {
        PhotonView pooledView = PhotonNetwork.GetPhotonView(photonID);
        if (pooledView != null)
            ActivateObject(pooledView.gameObject, origin, rotation);
    }
}
