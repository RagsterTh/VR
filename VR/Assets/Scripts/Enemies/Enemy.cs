using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
public abstract class Enemy : MonoBehaviour, IShootable
{

    protected Transform followingPlayer;
    List<Transform> _playersPos = new List<Transform>();
    PhotonView _phView;
    protected void Awake()
    {
        _phView = GetComponent<PhotonView>();
        foreach (var t in GameController.instance.PlayerAvatar)
        {
            _playersPos.Add(t.transform);
        }

    }
    protected void OnEnable()
    {
        StartCoroutine(FindClose(_playersPos.ToArray()));
    }

    IEnumerator FindClose(Transform[] players)
    {
        float[] playerDistances = new float[4]; // array de distancias

        for (int i = 0; i < players.Length; i++)
        {
            playerDistances[i] = Vector3.Distance(players[i].transform.position, transform.position);
        }
        followingPlayer = players[Array.IndexOf(playerDistances, playerDistances.Min()) - 1];
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
        _phView.RPC("RPC_Hit", RpcTarget.All);
    }
    [PunRPC]
    public void RPC_Hit()
    {
        gameObject.SetActive(false);
    }
}
