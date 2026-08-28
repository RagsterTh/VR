using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.Events;

public class MedicalQuestions : MonoBehaviour
{
    [SerializeField] private MedicalQuestionsData medicalQuestionsData;
    private MedicalData currentData;

    [SerializeField] private TMP_Text displayText;
    [SerializeField] private Button[] answerButtons;
    [SerializeField] private GameObject buttonPanel;

    private MedicalEmergencyManager emergencyManager;

    private TreatmentType correctTreatment;
    private MedicalEmergency currentWound;
    [SerializeField] UnityEvent OnQuestionsDone;

    [SerializeField] UnityEvent OnCorrectAnswer;
    [SerializeField] UnityEvent OnWrongAnswer;

    private void Start()
    {
        emergencyManager = GetComponent<MedicalEmergencyManager>();
        buttonPanel.SetActive(false);
    }

    public void ShowTreatmentOptions(MedicalEmergency wound)
    {
        if (currentWound != null && currentWound != wound)
        {
            currentWound.ClearLabel();
        }

        currentWound = wound;
        wound.ShowLabel();

        if (!wound.HasAssignedData)
        {
            int rand = UnityEngine.Random.Range(0, medicalQuestionsData.medicalDatas.Length);
            MedicalData assignedData = medicalQuestionsData.medicalDatas[rand];
            wound.AssignMedicalData(assignedData);
        }

        currentData = wound.GetAssignedMedicalData();
        correctTreatment = currentData.treatmentType;

        string damageType = FormatEnum(currentData.damageType);
        string intensity = FormatEnum(currentData.damageIntensity);

        displayText.text = "<b>Relatório Clínico:</b>\n" +
                           $"O paciente apresenta uma lesão <b>{intensity.ToLower()}</b> do tipo <b>{damageType.ToLower()}</b>.\n\n" +
                           "Selecione o tratamento mais adequado:";

        buttonPanel.SetActive(true);
        SetupButtons();
    }

    private void SetupButtons()
    {
        // Obtenha todos os tratamentos possíveis
        TreatmentType[] allTreatments = (TreatmentType[])Enum.GetValues(typeof(TreatmentType));
        int correctIndex = Array.IndexOf(allTreatments, correctTreatment);

        // Escolha um tratamento aleatório diferente do correto
        int distractorIndex;
        do
        {
            distractorIndex = UnityEngine.Random.Range(0, allTreatments.Length);
        } while (distractorIndex == correctIndex);

        buttonPanel.GetComponent<MoveTween>().Move();

        // Crie um array com os dois tratamentos
        TreatmentType[] options = new TreatmentType[2];
        int correctButton = UnityEngine.Random.Range(0, 2); // 0 ou 1

        options[correctButton] = correctTreatment;
        options[1 - correctButton] = allTreatments[distractorIndex];

        for (int i = 0; i < answerButtons.Length; i++)
        {
            print(i);
            if (i >= 2)
            {
                answerButtons[i].gameObject.SetActive(false);
                continue;
            }
            int id = (options[i] == correctTreatment) ? (int)correctTreatment : (int)options[i];
            answerButtons[i].gameObject.SetActive(true);
            answerButtons[i].GetComponent<MoveTween>().Move();
            answerButtons[i].GetComponentInChildren<TMP_Text>().text = FormatEnum(options[i]);
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => CheckAnswer(id));

        }
    }

    public void CheckAnswer(int answerID)
    {
        if ((int)correctTreatment == answerID)
        {
            Debug.Log("Tratamento correto!");
            OnCorrectAnswer?.Invoke();
            if (currentWound != null)
            {
                Destroy(currentWound.gameObject);
            }
            // ATIVA O PRÓXIMO FERIMENTO
            if (emergencyManager != null)
            {
                print("AAAA");
                emergencyManager.ActivateNextWound();
            }
        }
        else
        {
            OnWrongAnswer?.Invoke();
            Debug.Log("Tratamento incorreto!");
        }

        displayText.text = "";
        buttonPanel.SetActive(false);

        if (currentWound != null)
        {
            currentWound.ClearLabel();
            currentWound = null;
        }
    }

    private string FormatEnum(Enum value)
    {
        string formatted = value.ToString();

        switch (formatted)
        {
            case "Escoriacao": return "Escoriação";
            case "Corte": return "Corte";
            case "Perfuracao": return "Perfuração";
            case "QueimaduraTermica": return "Queimadura Térmica";
            case "QueimaduraQuimica": return "Queimadura Química";
            case "QueimaduraRadioativa": return "Queimadura Radioativa";

            case "Leve": return "Leve";
            case "Moderado": return "Moderado";
            case "Grave": return "Grave";

            case "LimpezaEAntissepsia": return "Limpeza e Antissepsia";
            case "CurativoCompressivo": return "Curativo Compressivo";
            case "Imobilizacao": return "Imobilização";
            case "ResfriarComAguaCorrente": return "Resfriar com Água Corrente";
            case "IrrigacaoAbundanteComAgua": return "Irrigação Abundante com Água";
            case "RemoverFonteRadiacao": return "Remover Fonte de Radiação";
            case "AplicarPomadaAntibiotica": return "Aplicar Pomada Antibiótica";
            case "UsoDeGeloLocal": return "Uso de Gelo Local";
            case "CompressaQuente": return "Compressa Quente";

            default:
                return formatted;
        }

    }

    public void AllWoundsTreated()
    {
        Debug.Log("ACABOU");
        OnQuestionsDone?.Invoke();
    }
}
