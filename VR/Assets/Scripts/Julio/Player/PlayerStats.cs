using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable, IHealable
{
    [SerializeField] PlayerData playerData;

    [SerializeField] TempGUI gui;

    public float _curLife;

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
        gui.AtualizeBar(playerData.maxLife, _curLife);
        if (_curLife <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        _curLife += healAmount;
        if(_curLife >= playerData.maxLife) 
        {
            _curLife = playerData.maxLife;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (true)
        {

        }
    }
}
