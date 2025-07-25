using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Data/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public float damage;
    public float movimentVelocity;

    public GameObject bullet;
}
