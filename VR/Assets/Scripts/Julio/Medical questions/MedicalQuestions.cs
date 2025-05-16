using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class MedicalQuestions : MonoBehaviour
{
    [SerializeField] private MedicalQuestionsData medicalQuestionsData;
    private MedicalData currentData;

    [SerializeField] private TMP_Text displayText;
    [SerializeField] private Button[] answerButtons;
    [SerializeField] private GameObject buttonPanel;

    private TreatmentType correctTreatment;
    private MedicalEmergency currentWound;

    private void Start()
    {
        buttonPanel.SetActive(false);
        PhotonNetwork.Disconnect();
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

        displayText.text = $"<b>Relatório Clínico:</b>\n" +
                           $"O paciente apresenta uma lesão <b>{intensity.ToLower()}</b> do tipo <b>{damageType.ToLower()}</b>.\n\n" +
                           $"Selecione o tratamento mais adequado:";

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
    }

    private string FormatEnum(Enum value)
    {
        string formatted = value.ToString();
        formatted = System.Text.RegularExpressions.Regex.Replace(formatted, "([a-z])([A-Z])", "$1 $2");
        formatted = formatted.Replace("E", " e ");
        return formatted;
    }
}
