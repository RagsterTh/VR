using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class MedicalEmergencyManager : MonoBehaviour
{
    [SerializeField]private MedicalEmergency[] allWounds;
    [SerializeField] private int numberToActivate = 3;
    [SerializeField] UnityEvent OnSceneLoad;
    private void Start()
    {
        //allWounds = GetComponentsInChildren<MedicalEmergency>();
        ActivateRandomWounds();
        OnSceneLoad?.Invoke();
    }

    private void ActivateRandomWounds()
    {
        foreach (var wound in allWounds)
        {
            wound.gameObject.SetActive(false);
        }

        int activated = 0;
        while (activated < numberToActivate)
        {
            int rand = Random.Range(0, allWounds.Length);
            if (!allWounds[rand].gameObject.activeSelf)
            {
                allWounds[rand].gameObject.SetActive(true);
                activated++;
            }
        }
    }
}
