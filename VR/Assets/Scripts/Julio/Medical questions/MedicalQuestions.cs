using Photon.Pun;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
        SetupButtons();
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

        int rand = Random.Range(0, medicalQuestionsData.medicalDatas.Length);
        currentData = medicalQuestionsData.medicalDatas[rand];

        displayText.text = $"Patient has a <b>{currentData.damageIntensity}</b> <b>{currentData.damageType}</b>.\nWhat is the correct treatment?";
        correctTreatment = currentData.treatmentType;

        buttonPanel.SetActive(true);
    }

    public void CheckAnswer(int answerID)
    {
        if ((int)correctTreatment == answerID)
        {
            Debug.Log("Correct treatment!");
            if (currentWound != null)
            {
                Destroy(currentWound.gameObject);
            }
        }
        else
        {
            Debug.Log("Incorrect treatment!");
        }

        displayText.text = "";
        buttonPanel.SetActive(false);

        if (currentWound != null)
        {
            currentWound.ClearLabel();
            currentWound = null;
        }
    }

    public void SetupButtons()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int id = i;
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => CheckAnswer(id));
        }
    }
}
