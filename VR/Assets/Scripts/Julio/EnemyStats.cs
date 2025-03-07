using UnityEngine;

public class EnemyStats : MonoBehaviour,IShootable
{
    [SerializeField] EnemyData enemyData;

    float _damage;

    private void Start()
    {
        _damage = enemyData.damage;
    }
    public void Hit()
    {
        gameObject.SetActive(false);
    }

}
