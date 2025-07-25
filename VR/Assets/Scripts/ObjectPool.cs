using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class ObjectPool : MonoBehaviourPunCallbacks
{
    [SerializeField]List<int> _pooledObjects;
    [SerializeField]GameObject _objectToPool;
    [SerializeField]int _amountToPool;
    PhotonView _phView;

    void Awake()
    {
        _phView = GetComponent<PhotonView>();
        Initialize();

    }
    public override void OnJoinedRoom()
    {
        Initialize();
    }
    void Initialize()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        GameObject collection = new GameObject(_objectToPool.name + " Collection");
        _pooledObjects = new List<int>();
        GameObject tmp;
        for (int i = 0; i < _amountToPool; i++)
        {
            tmp = PhotonNetwork.InstantiateRoomObject(_objectToPool.name, collection.transform.position, _objectToPool.transform.rotation);
            _pooledObjects.Add(tmp.GetPhotonView().ViewID);
        }
        _phView.RPC("RPC_SetPool", RpcTarget.Others, _pooledObjects.ToArray());
    }
    public GameObject GetPooledObject()
    {
        for (int i = 0; i < _amountToPool; i++)
        {
            GameObject temp = PhotonNetwork.GetPhotonView(_pooledObjects[i]).gameObject;
            if (!temp.activeInHierarchy)
            {
                return temp;
            }
        }
        return null;
    }
    public GameObject CallObject(Vector3 origin)
    {
        GameObject bullet = GetPooledObject();
        _phView.RPC("RPC_CallObject", RpcTarget.AllBuffered, origin, bullet.GetPhotonView().ViewID);
        return bullet;
    }
    public void CallObject(Vector3 origin, Quaternion rotation)
    {
        GameObject bullet = GetPooledObject();
        _phView.RPC("RPC_CallObjectWithRotation", RpcTarget.AllBuffered, origin, rotation, bullet.GetPhotonView().ViewID);
    }
    [PunRPC]
    public void RPC_SetPool(int[] pool)
    {
        _pooledObjects = pool.ToList();
    }
    [PunRPC]
    public void RPC_CallObject(Vector3 origin, int photonID)
    {
        GameObject bullet = PhotonNetwork.GetPhotonView(photonID).gameObject;
        if (bullet != null)
        {
            bullet.transform.position = origin;
            bullet.SetActive(true);
        }
    }
    [PunRPC]
    public void RPC_CallObjectWithRotation(Vector3 origin, Quaternion rotation, int photonID)
    {
        GameObject bullet = PhotonNetwork.GetPhotonView(photonID).gameObject;
        if (bullet != null)
        {
            bullet.transform.position = origin;
            bullet.transform.rotation = rotation;
            bullet.SetActive(true);
        }
    }
}
