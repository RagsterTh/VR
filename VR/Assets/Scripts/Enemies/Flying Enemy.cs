using UnityEngine.AI;


public class FlyingEnemy : Enemy
{
    NavMeshAgent agent;
    private void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 10;
        StartCoroutine(FollowPlayer(agent));
        print(agent.stoppingDistance + "Aerio");
    }

}
