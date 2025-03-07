using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField]List<GameObject> _pooledObjects;
    [SerializeField]GameObject _objectToPool;
    [SerializeField]int _amountToPool;


    void Awake()
    {
        GameObject collection = new GameObject();
        collection.name = _objectToPool.name + " Collection";
        _pooledObjects = new List<GameObject>();
        GameObject tmp;
        for (int i = 0; i < _amountToPool; i++)
        {
            tmp = Instantiate(_objectToPool, collection.transform);
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
    public GameObject CallObject(Vector3 origin)
    {
        GameObject bullet = GetPooledObject();
        if (bullet != null)
        {
            bullet.transform.position = origin;
            bullet.SetActive(true);
        }
        return bullet;
    }
}