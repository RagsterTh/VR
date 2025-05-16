using UnityEngine;
using Photon.Pun;
public class PlayerPrefabNetwork : MonoBehaviour
{
    PhotonView _phView;
    [SerializeField] GameObject[] elements;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _phView = GetComponent<PhotonView>();
        if (!_phView.IsMine)
            return;

        foreach (var item in elements)
        {
            item.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
