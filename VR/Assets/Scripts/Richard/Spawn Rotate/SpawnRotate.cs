using UnityEngine;

public class SpawnRotate : MonoBehaviour
{
    public Vector3 spawnPos;

    [SerializeField] float radius;

    public Vector3 RotateSpawn()
    {
        float i = Random.Range(0,361);
        print(i);
        float angle = i * Mathf.PI * 2/360;
        Vector3 center = Vector3.zero;
        float x = center.x + Mathf.Cos(angle) * radius;
        float z = center.z + Mathf.Sin(angle) * radius;
        spawnPos = new Vector3(x, gameObject.transform.position.y, z);
        return spawnPos;
    }
}
