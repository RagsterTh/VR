using UnityEngine;

[System.Serializable]
public enum DamageType
{
    Escoriacao,
    Corte,
    Perfuracao,
    QueimaduraTermica,
    QueimaduraQuimica,
    QueimaduraRadioativa
}

public enum DamageIntensity
{
    Leve,
    Moderado,
    Grave
}

public enum TreatmentType
{
    LimpezaEAntissepsia,
    CurativoSimples,
    CurativoCompressivo,
    EstancarSangramento,
    Imobilizacao,
    IrrigacaoComSoro,
    NeutralizacaoQuimica,
    TratamentoComPomada,
    AtendimentoHospitalarImediato
}

[CreateAssetMenu(fileName = "MedicalData", menuName = "Data/Medical Data")]
public class MedicalData : ScriptableObject
{
    public DamageType damageType;
    public DamageIntensity damageIntensity;
    public TreatmentType treatmentType;
    [TextArea(3, 6)]
    public string treatmentDescription;
}