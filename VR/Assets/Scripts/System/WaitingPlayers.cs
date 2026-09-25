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
        if (OfflineSession.IsOffline)
        {
            _vrPlayersAmount = 1;
            return;
        }

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
        if (OfflineSession.IsOffline)
            RPC_Finish();
        else
            _phView.RPC(nameof(RPC_Finish), RpcTarget.AllBuffered);
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
                // GloboV2 leaves _globe empty; without the check the exception stopped the combat area from opening.
                if (_globe != null)
                    _globe.SetActive(true);
                if (_shooterGame != null)
                    _shooterGame.SetActive(true);
            }
        }
    }
    public void ActiveShooterGame()
    {
        if (OfflineSession.IsOffline)
            RPC_ActiveShooterGame();
        else
            _phView.RPC(nameof(RPC_ActiveShooterGame), RpcTarget.AllBuffered);
    }
    [PunRPC]
    private void RPC_ActiveShooterGame()
    {
        SimulationController.Instance.ActiveShootGame();

    }
    public void SendBackToMenu()
    {
        if (OfflineSession.IsOffline)
            OfflineSession.ReturnToEntry();
        else
            PhotonNetwork.LoadLevel(0);
    }

}
