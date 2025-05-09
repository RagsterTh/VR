using System;
using System.Collections;
using System.Collections.Generic;
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
        gameObject.SetActive(false);


    }
    protected void OnEnable()
    {
        foreach (var t in GameController.instance.PlayerAvatar)
        {
            _playersPos.Add(t.transform);
        }
        StartCoroutine(FindClose(_playersPos.ToArray()));
    }

    IEnumerator FindClose(Transform[] players)
    {
        float distance = 50;
        for (int i = 0; i < players.Length; i++)
        {
            if(Vector3.Distance(players[i].position, transform.position) < distance)
            {
                distance = Vector3.Distance(players[i].position, transform.position);
                followingPlayer = players[i];
            }

        }
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
    private void OnCollisionEnter(Collision collision)
    {
        print("Foi");
    }
}
