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
    LimpezaEAntissepsia,           // Para escoriação
    CurativoCompressivo,           // Para corte
    Imobilizacao,                  // Para perfuração
    ResfriarComAguaCorrente,       // Para queimadura térmica
    IrrigacaoAbundanteComAgua,     // Para queimadura química
    RemoverFonteRadiacao,          // Para queimadura radioativa
    // Distratores
    AplicarPomadaAntibiotica,      // Distrator
    UsoDeGeloLocal,                // Distrator
    CompressaQuente                // Distrator
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