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

    [SerializeField] private GameObject[] possibleWounds;

    [SerializeField] private TMP_Text displayText;
    [SerializeField] private Button[] answerButtons;
    [SerializeField] private GameObject buttonPanel;

    private TreatmentType correctTreatment;
    private MedicalEmergency currentWound;
    [SerializeField] UnityEvent OnQuestionsDone;

    private void Start()
    {

        buttonPanel.SetActive(false);
        ChooseWound();

        for (int i = 0; i < possibleWounds.Length; i++)
        {
            if (!possibleWounds[i].activeSelf)
            {
                Destroy(possibleWounds[i]);
            }
        }

    }

    public void ChooseWound() 
    {
        int randomNum = UnityEngine.Random.Range(0,possibleWounds.Length);
        possibleWounds[randomNum].SetActive(true);
        Debug.Log("AAAA");
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

        SetupButtons();
        buttonPanel.SetActive(true);
    }

    private void SetupButtons()
    {
        TreatmentType[] allTreatments = (TreatmentType[])Enum.GetValues(typeof(TreatmentType));

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < allTreatments.Length)
            {
                int id = i;
                TreatmentType treatment = allTreatments[i];

                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponentInChildren<TMP_Text>().text = FormatEnum(treatment);
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => CheckAnswer(id));
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void CheckAnswer(int answerID)
    {
        if ((int)correctTreatment == answerID)
        {
            Debug.Log("Tratamento correto!");
            if (currentWound != null)
            {
                Destroy(currentWound.gameObject);
            }
        }
        else
        {
            Debug.Log("Tratamento incorreto!");
        }

        displayText.text = "";
        buttonPanel.SetActive(false);

        if (currentWound != null)
        {
            currentWound.ClearLabel();
            currentWound = null;
        }

        StartCoroutine(CheckIfAllWoundsTreatedNextFrame());
    }

    private IEnumerator CheckIfAllWoundsTreatedNextFrame()
    {
        yield return new WaitForEndOfFrame();

        bool hasWoundsLeft = false;
        foreach (GameObject wound in possibleWounds)
        {
            if (wound != null && wound.activeInHierarchy)
            {
                hasWoundsLeft = true;
                break;
            }
        }

        if (!hasWoundsLeft)
        {
            AllWoundsTreated();
        }
        else
        {
            ChooseWound();
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

    void AllWoundsTreated()
    {
        Debug.Log("ACABOU");
        OnQuestionsDone?.Invoke();
    }
}
