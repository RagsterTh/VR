using Unity.VisualScripting;
using UnityEngine;

public class MedicalEmergencyManager : MonoBehaviour
{
    private MedicalEmergency[] allWounds;
    [SerializeField] private int numberToActivate = 3;

    private void Start()
    {
        //allWounds = GetComponentsInChildren<MedicalEmergency>();
        ActivateRandomWounds();
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
