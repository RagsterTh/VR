using Photon.Pun;
using UnityEngine;

public class WaitingPlayers : MonoBehaviour
{
    [SerializeField] private GameObject _starBattleBtn;
    private PhotonView _phView;
    private int _playersFinish;
    private int _vrPlayersAmount;
    [SerializeField] private GameObject _shooterGame;

    [SerializeField] bool isMedical;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponent<PhotonView>();
        foreach (var item in PhotonNetwork.PlayerList)
        {
            if ((bool)item.CustomProperties["IsVR"])
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
        if(_playersFinish >= _vrPlayersAmount)
        {
            //PhotonNetwork.LoadLevel(scene);            
            if (isMedical)
            {
                SendBackToMenu();
            } else
            {
                if(PhotonNetwork.IsMasterClient)
                    _starBattleBtn.SetActive(true);
                gameObject.SetActive(false);
                _shooterGame.SetActive(true);
                SimulationController.Instance.ActiveShootGame();
            }
        }
    }

    public void SendBackToMenu() 
    {
        PhotonNetwork.LoadLevel(0);
    }

}
