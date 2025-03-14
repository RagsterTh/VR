using System.Security.Cryptography;
using UnityEngine;

public class Balcony : MonoBehaviour
{
    [SerializeField] private bool _isInConstruction;
    [SerializeField] private GameObject _warningPanel;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_isInConstruction)
            {
                _warningPanel.SetActive(true);
            }
            else
            {
                Debug.Log("interact");
            }
        }
    }
}
