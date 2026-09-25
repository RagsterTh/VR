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
        if (!OfflineSession.IsOffline && !PhotonNetwork.IsMasterClient)
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
            if (_enemyPool != null && spawnRotation != null)
                _enemyPool.CallObject(spawnRotation.RotateSpawn());

            StartCoroutine(Spawn());
        }
    }

    float GetScaledSpawnTime()
    {
        int playerCount = 1;
        float difficultyFactor = 0.5f;

        if (GameController.instance != null)
        {
            return _timeToSpawn / Mathf.Max(GameController.instance.GetSpawnRateScale(), 0.01f);
        }
        else if (SimulationController.Instance != null)
        {
            playerCount = Mathf.Max(1, SimulationController.Instance.PlayerAvatar.Count);
        }
        else
        {
            return _timeToSpawn;
        }

        float difficultyScale = 1f + (playerCount - 1) * difficultyFactor;
        return _timeToSpawn / Mathf.Max(difficultyScale, 0.01f);
    }

    public void SetBool(bool setBool)
    {
        isTime = setBool;
    }

}
