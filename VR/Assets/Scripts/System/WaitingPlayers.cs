using Photon.Pun;
using UnityEngine;

public class WaitingPlayers : MonoBehaviour
{
    private PhotonView _phView;
    private int _playersFinish;
    private int _vrPlayersAmount;
    [SerializeField] private GameObject _shooterGame;
    [SerializeField] private GameObject _globe;


    [SerializeField] bool isMedical;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponent<PhotonView>();
        foreach (var item in PhotonNetwork.PlayerList)
        {
            if (item.CustomProperties.TryGetValue("IsVR", out object isVR) && (bool)isVR)
            {
                _vrPlayersAmount++;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void Finish()
    {
        _phView.RPC("RPC_Finish", RpcTarget.AllBuffered);
    }
    [PunRPC]
    public void RPC_Finish()
    {
        _playersFinish++;
        if (_playersFinish >= _vrPlayersAmount)
        {
            //PhotonNetwork.LoadLevel(scene);            
            if (isMedical)
            {
                SendBackToMenu();
            }
            else
            {
                _globe.SetActive(true);
                _shooterGame.SetActive(true);
            }
        }
    }
    public void ActiveShooterGame()
    {
        _phView.RPC("RPC_ActiveShooterGame", RpcTarget.AllBuffered);
    }
    [PunRPC]
    private void RPC_ActiveShooterGame()
    {
        SimulationController.Instance.ActiveShootGame();

    }
    public void SendBackToMenu()
    {
        PhotonNetwork.LoadLevel(0);
    }

}
