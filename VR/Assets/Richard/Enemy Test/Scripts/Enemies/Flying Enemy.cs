using System.Linq;
using System;
using UnityEngine;
using System.Collections;
using UnityEngine.AI;


public class FlyingEnemy : Enemy
{
    NavMeshAgent agent;
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 10;
        StartCoroutine(FollowPlayer(agent));
        print(agent.stoppingDistance + "Aerio");
    }
    protected override void TakeDamage()
    {

    }

}
