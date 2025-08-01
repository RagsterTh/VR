using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine;
using System.Collections;
enum SpawnerType
{
    Terrestial, Aerial
}

public class Spawner : MonoBehaviourPunCallbacks
{
    [SerializeField] ObjectPool _enemyPool;
    [SerializeField] float _timeToSpawn;

    [SerializeField] SpawnRotate spawnRotation;

    bool isTime;
    private void Start()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            gameObject.SetActive(false);
            return;
        }
        StartCoroutine(Spawn());
    }

    IEnumerator Spawn()
    {
        if (!isTime)
        {
            yield return new WaitForSeconds(_timeToSpawn);
            _enemyPool.CallObject(spawnRotation.RotateSpawn());
            StartCoroutine(Spawn());
        }
    }

    public void SetBool(bool setBool)
    {
        isTime = setBool;
    }

}
