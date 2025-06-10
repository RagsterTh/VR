using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable, IHealable
{
    [SerializeField] PlayerData playerData;

    float _curLife;

    private void Start()
    {
        _curLife = playerData.maxLife;
    }
    public void Die()
    {
        Debug.Log("Player Dead");
    }

    public void TakeDamage(float dmg)
    {
        _curLife -= dmg;
    }

    public void Heal(float healAmount)
    {
        _curLife += healAmount;
        if(_curLife >= playerData.maxLife) 
        {
            _curLife = playerData.maxLife;
        }
    }
}
