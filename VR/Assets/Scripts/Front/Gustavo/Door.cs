using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private GameObject _door1;
    [SerializeField] private GameObject _door2;

    private Vector3 _door1Closed;
    private Vector3 _door1Opened;
    private Vector3 _door2Closed;
    private Vector3 _door2Opened;
    [SerializeField] private float distance;


    void Start()
    {
        _door1Closed = _door1.transform.localPosition;
        _door2Closed = _door2.transform.localPosition;
        _door1Opened = _door1.transform.localPosition;
        _door2Opened = _door2.transform.localPosition;
        _door1Opened.z = 0;
        _door2Opened.z = 0;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Collider col = GetComponent<Collider>();

            StartCoroutine(InterpolatePos(_door1Closed, _door1Opened, 1f, _door1, true));
            StartCoroutine(InterpolatePos(_door2Closed, _door2Opened, 1f, _door2, false));
            col.enabled = false;
        }
    }

    // IEnumerator InterpolatePos(Vector3 start, Vector3 end, float time, GameObject obj)
    // {
    //     float elapsedTime = 0f;
    //     while (elapsedTime < time)
    //     {
    //         obj.transform.localPosition = Vector3.Lerp(start, end, elapsedTime / time);
    //         elapsedTime += Time.deltaTime;
    //         yield return null;
    //     }
    //     obj.transform.localPosition = end;
    // }
    IEnumerator InterpolatePos(Vector3 start, Vector3 end, float time, GameObject obj, bool toLeft){
        float elapsedTime = 0f;
        float zindex;
        if (toLeft) zindex = -1; else zindex = 1;
        while (elapsedTime < time)
        {
            obj.transform.localPosition = Vector3.Lerp(start, new Vector3(start.x, end.y, start.z-zindex*distance ), elapsedTime / time);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
