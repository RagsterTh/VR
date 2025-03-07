using UnityEngine;

public class LocationConfirm : MonoBehaviour
{
    [SerializeField] GameObject _confirmPanel;

    public void Back()
    {
        _confirmPanel.SetActive(false);
    }
    public void Confirm()
    {
        Debug.Log("mudar de cena");
    }
}
