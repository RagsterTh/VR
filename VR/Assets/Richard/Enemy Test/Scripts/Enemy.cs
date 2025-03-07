using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] Transform[] Players = new Transform[4]; // array de players

    void Start()
    {
        FindClosestPlayer();
    }

    IEnumerator FindClosest()
    {
        float[] playerDistances = new float[4]; // array de distancias

        for (int i = 0; i < Players.Length; i++)
        {
            playerDistances[i] = Vector3.Distance(Players[i].transform.position, transform.position);
        }
        print(Players[Array.IndexOf(playerDistances, playerDistances.Min())]); // Coleta o index do array baseado no menor distancia
        yield return new WaitForSeconds(2.5f);
        StartCoroutine(FindClosest());
    }

    void FindClosestPlayer()
    {
        StartCoroutine(FindClosest());
    }
}
