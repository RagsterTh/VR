using UnityEngine;
using UnityEngine.AI;


public class FlyingEnemy : MovingEnemy
{
    public new void Start()
    {
        base.Start();
        float value = Random.Range(2, 5);
        agent.height = value;
        agent.baseOffset = value;
    }
}
