using UnityEngine.AI;

public class TerrestrialEnemy : Enemy
{
    NavMeshAgent agent;
    private void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 5;
        StartCoroutine(FollowPlayer(agent));
        print(agent.stoppingDistance + "Terestre");
    }
}
