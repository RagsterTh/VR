using UnityEngine;

[System.Serializable]
public enum DamageType
{
    escoriação,
    cortes,
    perfuração,
    queimadura,
    queimaduraQuimica,
    queimaduraRadioativa
}

public enum DamageIntensity
{
    danoLeve,
    danoModerado,
    danoExpressivo
}

public enum TreatmentType
{
    escoriação,
    cortes,
    perfuração,
    queimadura,
    queimaduraQuimica,
    queimaduraRadioativa
}

[CreateAssetMenu(fileName = "MedicalData", menuName = "Data/Medical Data")]
public class MedicalData : ScriptableObject
{
    public DamageType damageType;
    public DamageIntensity damageIntensity;
    public TreatmentType treatmentType;
}
