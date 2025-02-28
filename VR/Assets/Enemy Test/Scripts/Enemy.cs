using System.Collections;
using System.Linq;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField]Transform[] Players = new Transform[4];

    void Start()
    {
        StartCoroutine(FindClosest());
    }

    IEnumerator FindClosest()
    {
        float[] playerDistances = new float[4];
        for (int i = 0; i < Players.Length; i++)
        {
            playerDistances[i] = Vector3.Distance(Players[i].transform.position, transform.position);
            //print(playerDistances[i] + $", {i}");
        }
        print(playerDistances.Min());
        yield return new WaitForSeconds(2.5f);
        StartCoroutine(FindClosest());
    }

}
