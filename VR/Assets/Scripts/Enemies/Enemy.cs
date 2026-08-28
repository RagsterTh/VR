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
    PhotonView _phView;
    [SerializeField] private float damage;
    [SerializeField] private bool isTerrestrian;
    protected virtual void Awake()
    {
        _phView = GetComponent<PhotonView>();
        gameObject.SetActive(false);
    }

    protected virtual void OnEnable()
    {
        followingPlayer = null;
        StartCoroutine(FindClose());
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Enemy] {name} OnTriggerEnter with '{other.name}' (tag={other.tag})");
        if (other.CompareTag("Player"))
        {
            DealDamage();
        }
    }
    private void DealDamage()
    {
        Debug.Log($"[Enemy] {name} DealDamage() called. followingPlayer={(followingPlayer ? followingPlayer.name : "null")}, damage={damage}");
        if (followingPlayer)
        {
            var lifeBar = ServiceLocator.Get<PlayersLifeBar>();
            if (lifeBar == null)
            {
                Debug.LogWarning($"[Enemy] {name} could not find a PlayersLifeBar via ServiceLocator. No damage applied.");
            }
            else
            {
                Debug.Log($"[Enemy] {name} calling TakeDamage({damage}) on {lifeBar.name}");
                lifeBar.TakeDamage(damage);
            }
        }
        if (isTerrestrian)
        {
            Debug.Log($"[Enemy] {name} is terrestrial, returning to pool.");
            _phView.RPC(nameof(RPC_Despawn), RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_Despawn()
    {
        gameObject.SetActive(false);
    }

    List<Transform> GetPlayerTransforms()
    {
        List<Transform> players = new List<Transform>();
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Equals("Game") && GameController.instance != null)
        {
            foreach (var avatar in GameController.instance.PlayerAvatar)
                players.Add(avatar.transform);
        }
        else if (sceneName.Equals("GloboV2") && SimulationController.Instance != null)
        {
            foreach (var avatar in SimulationController.Instance.PlayerAvatar)
                players.Add(avatar.transform);
        }
        return players;
    }

    IEnumerator FindClose()
    {
        WaitForSeconds wait = new WaitForSeconds(.2f);
        while (true)
        {
            List<Transform> players = GetPlayerTransforms();
            float distance = 50;
            Transform closest = null;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == null)
                    continue;

                float d = Vector3.Distance(players[i].position, transform.position);
                if (d < distance)
                {
                    distance = d;
                    closest = players[i];
                }
            }
            if (closest != null)
                followingPlayer = closest;

            yield return wait;
        }
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

        var gameOverManager = ServiceLocator.Get<GameOverManager>();
        if (gameOverManager == null)
        {
            Debug.LogWarning($"[Enemy] {name} RPC_Hit: no GameOverManager registered in ServiceLocator. Kill not counted.");
            return;
        }

        gameOverManager.EnemiesKilled++;
        Debug.Log("Enemy Killed: " + gameOverManager.EnemiesKilled);
        gameOverManager.VerifyWin();
    }
}
