using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
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

        displayText.text = $"<b>Relat�rio Cl�nico:</b>\n" +
                           $"O paciente apresenta uma les�o <b>{intensity.ToLower()}</b> do tipo <b>{damageType.ToLower()}</b>.\n\n" +
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
            case "Escoriacao": return "Escoria��o";
            case "Corte": return "Corte";
            case "Perfuracao": return "Perfura��o";
            case "QueimaduraTermica": return "Queimadura T�rmica";
            case "QueimaduraQuimica": return "Queimadura Qu�mica";
            case "QueimaduraRadioativa": return "Queimadura Radioativa";

            case "Leve": return "Leve";
            case "Moderado": return "Moderado";
            case "Grave": return "Grave";

            case "LimpezaEAntissepsia": return "Limpeza e Antissepsia";
            case "CurativoSimples": return "Curativo Simples";
            case "CurativoCompressivo": return "Curativo Compressivo";
            case "EstancarSangramento": return "Estancar Sangramento";
            case "Imobilizacao": return "Imobiliza��o";
            case "IrrigacaoComSoro": return "Irriga��o com Soro";
            case "NeutralizacaoQuimica": return "Neutraliza��o Qu�mica";
            case "TratamentoComPomada": return "Tratamento com Pomada";
            case "AtendimentoHospitalarImediato": return "Atendimento Hospitalar Imediato";

            default:
                return formatted;
        }

    }

    void AllWoundsTreated()
    {
        Debug.Log("ACABOU");
        PhotonNetwork.LoadLevel("Credits");
    }
}
