using UnityEngine;

public class SpawnRotate : MonoBehaviour
{
    public Vector3 spawnPos;

    [SerializeField] float radius;

    public Vector3 RotateSpawn()
    {
        float angle = Random.Range(0f, 360f) * Mathf.PI * 2f / 360f;
        Vector3 center = gameObject.transform.position;
        float x = center.x + Mathf.Cos(angle) * radius;
        float z = center.z + Mathf.Sin(angle) * radius;
        spawnPos = new Vector3(x, center.y, z);
        return spawnPos;
    }
}
