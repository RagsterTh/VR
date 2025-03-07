using UnityEngine;

[CreateAssetMenu(fileName = "GunData", menuName = "Data/Gun Data")]
public class GunData : ScriptableObject
{
    public float damage;
    public float fireRate;
    public float ammoCapacity;
}
