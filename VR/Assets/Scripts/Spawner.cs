using System.Collections;
using UnityEngine;

enum SpawnerType
{
    Terrestial, Aerial
}

public class Spawner : MonoBehaviour
{
    [SerializeField] ObjectPool _enemyPool;
    [SerializeField] float _timeToSpawn;

    private void Start()
    {
        StartCoroutine(Spawn());
    }
    IEnumerator Spawn()
    {
        yield return new WaitForSeconds(_timeToSpawn);
        _enemyPool.CallObject(transform.position);

    }
}
