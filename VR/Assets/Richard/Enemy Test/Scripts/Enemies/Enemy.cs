using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : MonoBehaviour
{
    public Transform[] players = new Transform[4];
    public Transform followingPlayer;

    private void Awake()
    {
        StartCoroutine(FindClose(players));
    }

    IEnumerator FindClose(Transform[] players)
    {
        float[] playerDistances = new float[4]; // array de distancias

        for (int i = 0; i < players.Length; i++)
        {
            playerDistances[i] = Vector3.Distance(players[i].transform.position, transform.position);
        }
        followingPlayer = players[Array.IndexOf(playerDistances, playerDistances.Min())];
        yield return new WaitForSeconds(.2f);
        StartCoroutine(FindClose(players));
    }

    protected abstract void TakeDamage();

    protected virtual IEnumerator FollowPlayer(NavMeshAgent agent)
    {
        yield return new WaitForSeconds(.1f);
        if (agent)
        {
            agent.SetDestination(followingPlayer.position);
        }        
        StartCoroutine(FollowPlayer(agent));
    }
}
