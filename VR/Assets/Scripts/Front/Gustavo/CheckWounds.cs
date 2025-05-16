using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckWounds : MonoBehaviour
{
    public MedicalEmergency[] woundsPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        woundsPrefab = GetComponentsInChildren<MedicalEmergency>();
    }

    // Update is called once per frame
    void Update()
    {
        if (woundsPrefab == null || woundsPrefab.All(w=>w==null))
        {
            SceneManager.LoadScene("Title");    
        }
        
    }
}
