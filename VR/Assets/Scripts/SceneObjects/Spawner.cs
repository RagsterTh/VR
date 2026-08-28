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
    [Tooltip("Base time between spawns with a single player.")]
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
            yield return new WaitForSeconds(GetScaledSpawnTime());
            _enemyPool.CallObject(spawnRotation.RotateSpawn());
            StartCoroutine(Spawn());
        }
    }

    float GetScaledSpawnTime()
    {
        if (GameController.instance == null)
            return _timeToSpawn;

        int playerCount = Mathf.Max(1, GameController.instance.PlayerAvatar.Count);
        float difficultyScale = 1f + (playerCount - 1) * GameController.instance.DifficultyFactor;
        return _timeToSpawn / Mathf.Max(difficultyScale, 0.01f);
    }

    public void SetBool(bool setBool)
    {
        isTime = setBool;
    }

}
