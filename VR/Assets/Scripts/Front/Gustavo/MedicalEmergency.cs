using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class MedicalEmergency : MonoBehaviour
{
    [Header("Scale Effect")]
    [SerializeField] private Vector3 hoverScale = new Vector3(1.2f, 1.2f, 1.2f);
    [SerializeField] private Vector3 originalScale = Vector3.one;
    [SerializeField] private float scaleDuration = 0.2f;

    [Header("Wound Info")]
    public string woundName;
    [TextArea] public string woundDescription;

    [Header("UI")]
    [SerializeField] private TMP_Text hoverLabel;
    public MedicalQuestions medicalSystem;

    private bool isSelected;

    private void OnMouseEnter()
    {
        StartCoroutine(ScaleLerp(transform.localScale, hoverScale, scaleDuration));
        if (!isSelected && hoverLabel != null)
            hoverLabel.text = woundName;
    }

    private void OnMouseExit()
    {
        StartCoroutine(ScaleLerp(transform.localScale, originalScale, scaleDuration));
        if (!isSelected && hoverLabel != null)
            hoverLabel.text = "";
    }

    private void OnMouseDown()
    {
        isSelected = true;
        if (medicalSystem != null)
        {
            medicalSystem.ShowTreatmentOptions(this);
        }
    }

    public void ShowLabel()
    {
        if (hoverLabel != null)
        {
            hoverLabel.text = woundName;
        }
        isSelected = true;
    }

    public void ClearLabel()
    {
        if (hoverLabel != null)
        {
            hoverLabel.text = "";
        }
        isSelected = false;
    }

    IEnumerator ScaleLerp(Vector3 start, Vector3 end, float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            transform.localScale = Vector3.Lerp(start, end, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = end;
    }
}
