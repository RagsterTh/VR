using UnityEngine;

[System.Serializable]
public enum DamageType
{
    escoria��o,
    cortes,
    perfura��o,
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
    escoria��o,
    cortes,
    perfura��o,
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
