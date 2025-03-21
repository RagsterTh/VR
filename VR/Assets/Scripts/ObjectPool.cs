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
        _phView.RPC("RPC_CallObject", RpcTarget.AllBuffered, origin);
    }

    [PunRPC]
    public GameObject RPC_CallObject(Vector3 origin)
    {
        GameObject bullet = GetPooledObject();
        if (bullet != null)
        {
            bullet.transform.position = origin;
            bullet.SetActive(true);
        }
        return bullet;
    }
    /*
    [PunRPC]
    public void RPC_InitializePool()
    {
        GameObject collection = new GameObject();
        collection.name = _objectToPool.name + " Collection";
        _pooledObjects = new List<GameObject>();
        GameObject tmp;
        for (int i = 0; i < _amountToPool; i++)
        {
            tmp = Instantiate(_objectToPool, collection.transform);
            //tmp = PhotonNetwork.InstantiateRoomObject(_objectToPool.name, collection.transform.position, _objectToPool.transform.rotation);
            tmp.SetActive(false);
            _pooledObjects.Add(tmp);
        }
    }
    */
}