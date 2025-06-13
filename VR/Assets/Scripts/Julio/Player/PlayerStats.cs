using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [SerializeField] PlayerData playerData;

    public int damagesTaken;

    public void TakeDamage()
    {
        damagesTaken++;
    }
}
