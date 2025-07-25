using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [SerializeField] EnemyData enemyData;

    float _damage;
    

    private void Start()
    {
        _damage = enemyData.damage;
    }

}
