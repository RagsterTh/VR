using UnityEngine;
using UnityEngine.AI;


public class FlyingEnemy : MovingEnemy
{
    void Start()
    {
        float value = Random.Range(2, 5);
        agent.height = value;
        agent.baseOffset = value;
    }
}
