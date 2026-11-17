using UnityEngine;

public sealed class OfflineSpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.8f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.9f, new Vector3(0.6f, 1.8f, 0.6f));
        Gizmos.DrawRay(transform.position + Vector3.up * 1.6f, transform.forward * 0.8f);
    }
}
