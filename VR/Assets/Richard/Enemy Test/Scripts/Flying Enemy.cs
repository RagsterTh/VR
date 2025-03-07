using System.Linq;
using System;
using UnityEngine;
using System.Collections;

public class FlyingEnemy : Enemy
{
    protected override IEnumerator FindClose(Transform[] players)
    {
        float[] playerDistances = new float[4]; // array de distancias
        
        for (int i = 0; i < players.Length; i++)
        {
            playerDistances[i] = Vector3.Distance(players[i].transform.position, transform.position);
        }
        followingPlayer = players[Array.IndexOf(playerDistances, playerDistances.Min())]; // Coleta o index do array baseado no menor distancia
        print(followingPlayer.name + ", Aerial");
        yield return new WaitForSeconds(1f);
        StartCoroutine(FindClose(players));
    }
}
