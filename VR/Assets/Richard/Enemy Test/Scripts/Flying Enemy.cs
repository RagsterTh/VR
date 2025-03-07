using System.Linq;
using System;
using UnityEngine;
using System.Collections;

public class FlyingEnemy : Enemy
{
    RaycastHit hit;
    protected override IEnumerator FindClose(Transform[] players)
    {
        float[] playerDistances = new float[4]; // array de distancias
        
        for (int i = 0; i < players.Length; i++)
        {
            playerDistances[i] = Vector3.Distance(players[i].transform.position, transform.position);
        }
        followingPlayer = players[Array.IndexOf(playerDistances, playerDistances.Min())]; // Coleta o index do array baseado no menor distancia
        print(followingPlayer.name + ", Aerial");

        if (Physics.Linecast(transform.position, followingPlayer.position, out hit))
        {
            
            if (hit.collider.tag == "Player")
            {
                print("cumpensa");
                print(hit.collider.gameObject.name);
            }
            else
            {
                float[] TempDistances = playerDistances;
                print("num cumpensa");
                followingPlayer = players[Array.IndexOf(playerDistances, playerDistances.Min())];
            }
        }
        
        yield return new WaitForSeconds(1f);
        StartCoroutine(FindClose(players));
    }
}
