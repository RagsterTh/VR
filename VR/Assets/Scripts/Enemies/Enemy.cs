using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
public abstract class Enemy : MonoBehaviour, IShootable
{

    protected Transform followingPlayer;
    List<Transform> _playersPos = new List<Transform>();
    PhotonView _phView;
    [SerializeField] private float damage;
    protected void Awake()
    {
        _phView = GetComponent<PhotonView>();
        gameObject.SetActive(false);
    }

    protected void OnEnable()
    {
        _playersPos.Clear();
        followingPlayer = null;
        if (SceneManager.GetActiveScene().name.Equals("Game"))
            foreach (var t in GameController.instance.PlayerAvatar)
            {
                _playersPos.Add(t.transform);
            }
        if (SceneManager.GetActiveScene().name.Equals("GloboV2"))
            foreach (var t in SimulationController.Instance.PlayerAvatar)
            {
                _playersPos.Add(t.transform);
            }
        StartCoroutine(FindClose(_playersPos.ToArray()));
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DealDamage();
        }
    }
    private void DealDamage()
    {
        if (followingPlayer)
        {
            ServiceLocator.Get<PlayersLifeBar>()?.TakeDamage(damage);
        }
    }

    IEnumerator FindClose(Transform[] players)
    {
        float distance = 50;
        for (int i = 0; i < players.Length; i++)
        {
            if (Vector3.Distance(players[i].position, transform.position) < distance)
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
        if (followingPlayer)
            if (agent)
            {
                agent.SetDestination(followingPlayer.position);
            }
        StartCoroutine(FollowPlayer(agent));
    }

    public virtual void Hit()
    {
        _phView.RPC("RPC_Hit", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_Hit()
    {
        gameObject.SetActive(false);
    }
}
