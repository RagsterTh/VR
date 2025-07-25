using UnityEngine;
using UnityEngine.AI;

public abstract class MovingEnemy : Enemy
{
    protected NavMeshAgent agent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected new void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
    }
    private new void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(FollowPlayer(agent));
    }

}
