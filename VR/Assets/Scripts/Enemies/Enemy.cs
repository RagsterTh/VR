using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : MonoBehaviour, IShootable
{
    //protected Transform[] players = new Transform[4];
    protected Transform followingPlayer;
    List<Transform> _playersPos = new List<Transform>();

    protected void Start()
    {
        foreach(var t in GameController.instance.PlayerAvatar)
        {
            _playersPos.Add(t.transform);
        }
        StartCoroutine(FindClose(_playersPos.ToArray()));
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

    

    protected virtual IEnumerator FollowPlayer(NavMeshAgent agent)
    {
        yield return new WaitForSeconds(.1f);
        if (agent)
        {
            agent.SetDestination(followingPlayer.position);
        }        
        StartCoroutine(FollowPlayer(agent));
    }

    public void Hit()
    {
        print("atirado");
        gameObject.SetActive(false);
    }
}
