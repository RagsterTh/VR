using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class ObjectPool : MonoBehaviour
{
    [SerializeField]List<GameObject> _pooledObjects;
    [SerializeField]GameObject _objectToPool;
    [SerializeField]int _amountToPool;
    PhotonView _phView;

    void Awake()
    {
        _phView = GetComponent<PhotonView>();
        if (!PhotonNetwork.IsMasterClient)
            return;


        GameObject collection = new GameObject(_objectToPool.name + " Collection");
        _pooledObjects = new List<GameObject>();
        GameObject tmp;
        for (int i = 0; i < _amountToPool; i++)
        {
            tmp = PhotonNetwork.InstantiateRoomObject(_objectToPool.name, collection.transform.position, _objectToPool.transform.rotation);
            tmp.SetActive(false);
            _pooledObjects.Add(tmp);
        }

    }
    public GameObject GetPooledObject()
    {
        for (int i = 0; i < _amountToPool; i++)
        {
            if (!_pooledObjects[i].activeInHierarchy)
            {
                return _pooledObjects[i];
            }
        }
        return null;
    }
    public void CallObject(Vector3 origin)
    {
        GameObject bullet = GetPooledObject();
        _phView.RPC("RPC_CallObject", RpcTarget.AllBuffered, origin, bullet.GetPhotonView().ViewID);
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
}