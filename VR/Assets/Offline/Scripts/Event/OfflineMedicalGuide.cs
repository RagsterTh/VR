using System.Collections.Generic;

/// <summary>
/// First-aid explanations shown after each answer in the offline medical scene: why the right treatment is
/// right for that wound, and why the chosen one is not. Labels match MedicalQuestions' button texts.
/// </summary>
public static class OfflineMedicalGuide
{
    private static readonly Dictionary<TreatmentType, string> Labels = new()
    {
        { TreatmentType.LimpezaEAntissepsia, "Limpeza e Antissepsia" },
        { TreatmentType.CurativoCompressivo, "Curativo Compressivo" },
        { TreatmentType.Imobilizacao, "Imobilização" },
        { TreatmentType.ResfriarComAguaCorrente, "Resfriar com Água Corrente" },
        { TreatmentType.IrrigacaoAbundanteComAgua, "Irrigação Abundante com Água" },
        { TreatmentType.RemoverFonteRadiacao, "Remover Fonte de Radiação" },
        { TreatmentType.AplicarPomadaAntibiotica, "Aplicar Pomada Antibiótica" },
        { TreatmentType.UsoDeGeloLocal, "Uso de Gelo Local" },
        { TreatmentType.CompressaQuente, "Compressa Quente" },
    };

    /// <summary>Why this treatment is the right one (for the wound it belongs to).</summary>
    private static readonly Dictionary<TreatmentType, string> Why = new()
    {
        { TreatmentType.LimpezaEAntissepsia,
            "Escoriação é um ralado superficial. Lavar com água corrente e sabão remove a sujeira, e o antisséptico reduz o risco de infecção." },
        { TreatmentType.CurativoCompressivo,
            "No corte, a prioridade é parar o sangramento: pressão direta e firme com gaze ou pano limpo, mantida sem soltar." },
        { TreatmentType.Imobilizacao,
            "Na perfuração, não se retira o objeto cravado: ele pode estar contendo o sangramento. Imobilize-o no lugar até o atendimento." },
        { TreatmentType.ResfriarComAguaCorrente,
            "Na queimadura térmica, água corrente em temperatura ambiente por cerca de 20 minutos esfria a pele e impede que a lesão se aprofunde." },
        { TreatmentType.IrrigacaoAbundanteComAgua,
            "O produto químico continua agindo enquanto está na pele. Lavar com muita água corrente, por vários minutos, dilui e remove o agente." },
        { TreatmentType.RemoverFonteRadiacao,
            "Enquanto a exposição continua, a lesão aumenta. Primeiro afaste a vítima da fonte e retire as roupas contaminadas." },
    };

    /// <summary>Why a treatment that only looks right is wrong here.</summary>
    private static readonly Dictionary<TreatmentType, string> WhyNot = new()
    {
        { TreatmentType.AplicarPomadaAntibiotica,
            "Pomada não é o primeiro passo: aplicada antes de limpar ou resfriar, ela prende sujeira e calor na lesão." },
        { TreatmentType.UsoDeGeloLocal,
            "Gelo direto na pele pode causar lesão pelo frio e piorar o tecido já ferido." },
        { TreatmentType.CompressaQuente,
            "Calor aumenta o fluxo de sangue e a inflamação: piora sangramentos e queimaduras." },
    };

    private static readonly Dictionary<TreatmentType, string> MeantFor = new()
    {
        { TreatmentType.LimpezaEAntissepsia, "escoriações" },
        { TreatmentType.CurativoCompressivo, "cortes com sangramento" },
        { TreatmentType.Imobilizacao, "perfurações com objeto cravado" },
        { TreatmentType.ResfriarComAguaCorrente, "queimaduras térmicas" },
        { TreatmentType.IrrigacaoAbundanteComAgua, "queimaduras químicas" },
        { TreatmentType.RemoverFonteRadiacao, "queimaduras por radiação" },
    };

    public static string Label(TreatmentType treatment) => Labels.TryGetValue(treatment, out string label) ? label : treatment.ToString();

    public static bool TryParse(string label, out TreatmentType treatment)
    {
        foreach (KeyValuePair<TreatmentType, string> pair in Labels)
        {
            if (pair.Value == label)
            {
                treatment = pair.Key;
                return true;
            }
        }
        treatment = default;
        return false;
    }

    public static string Explain(TreatmentType right) => Why.TryGetValue(right, out string text) ? text : string.Empty;

    /// <summary>Why <paramref name="chosen"/> was not the answer.</summary>
    public static string ExplainWrong(TreatmentType chosen)
    {
        if (WhyNot.TryGetValue(chosen, out string text))
            return text;
        return MeantFor.TryGetValue(chosen, out string use)
            ? $"{Label(chosen)} é o cuidado indicado para {use}, não para esta lesão."
            : string.Empty;
    }
}
