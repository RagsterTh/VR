using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private GameObject _door1;
    [SerializeField] private GameObject _door2;

    [SerializeField] private Vector3 _door1Closed;
    [SerializeField] private Vector3 _door1Opened;
    [SerializeField] private Vector3 _door2Closed;
    [SerializeField] private Vector3 _door2Opened;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Collider col = GetComponent<Collider>();

            StartCoroutine(InterpolatePos(_door1Closed, _door1Opened, 1f, _door1));
            StartCoroutine(InterpolatePos(_door2Closed, _door2Opened, 1f, _door2));
            col.enabled = false;
        }
    }

    IEnumerator InterpolatePos(Vector3 start, Vector3 end, float time, GameObject obj)
    {
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            obj.transform.localPosition = Vector3.Lerp(start, end, elapsedTime / time);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        obj.transform.localPosition = end;
    }
}
