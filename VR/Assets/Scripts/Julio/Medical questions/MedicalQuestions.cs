using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MedicalQuestions : MonoBehaviour
{
    [SerializeField] MedicalQuestionsData medicalQuestionsData;
    [SerializeField] int questionQt;
    MedicalData _medicalData;

    [SerializeField] TMP_Text displayText;
    int _treatmentID;

    [SerializeField] Button[] _questions;

    private void Start()
    {
        SetButtons();
        ArrangeQuestion();
    }

    public void SetID(int id) 
    {
        if(id == _treatmentID) 
        {
            Debug.Log("Tratamento certo");
            ArrangeQuestion();
        }
        else
            Debug.Log("Tratamento falso");

    }

    public void ArrangeQuestion() 
    {
        if (questionQt <= 0)
        {
            Debug.Log("Acabou as Perguntas");
            displayText.text = ":)";
            _treatmentID = 10;
            return;
        }
        else
            questionQt--;

        int randomMedicalData = Random.Range(0, medicalQuestionsData.medicalDatas.Length);
        _medicalData = medicalQuestionsData.medicalDatas[randomMedicalData];

        displayText.text = _medicalData.treatmentDescription;
        _treatmentID = (int)_medicalData.treatmentType;
    }

    public void SetButtons() 
    {
        _questions[0].onClick.AddListener(delegate
        {
            SetID(0);
        });
        _questions[1].onClick.AddListener(delegate
        {
            SetID(1);
        });
        _questions[2].onClick.AddListener(delegate
        {
            SetID(2);
        });
        _questions[3].onClick.AddListener(delegate
        {
            SetID(3);
        });
        _questions[4].onClick.AddListener(delegate
        {
            SetID(4);
        });
        _questions[5].onClick.AddListener(delegate
        {
            SetID(5);
        });

    }
}
