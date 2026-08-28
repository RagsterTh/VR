using UnityEngine;
using UnityEngine.AI;

public abstract class MovingEnemy : Enemy
{
    protected NavMeshAgent agent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(FollowPlayer(agent));
    }

}
