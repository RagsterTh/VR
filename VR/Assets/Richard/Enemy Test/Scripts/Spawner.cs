using System.Collections;
using UnityEngine;

enum SpawnerType
{
    Terrestial, Aerial
}

public class Spawner : MonoBehaviour
{
    [SerializeField] float timeToSpawn;
    [SerializeField] SpawnerType typeOfEnemy;
    [SerializeField] GameObject[] enemies; 

    private void Start()
    {
        StartCoroutine(Spawn());
    }
    IEnumerator Spawn()
    {
        yield return new WaitForSeconds(timeToSpawn);
        switch (typeOfEnemy)
        {
            case SpawnerType.Terrestial:
                // Spawn
                break;

            case SpawnerType.Aerial:
                // Spawn
                break;
        }

    }
}
