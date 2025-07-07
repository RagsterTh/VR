using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
public class TitleFunctions : MonoBehaviour
{
    [SerializeField]private GameObject _credits;
    [SerializeField]private GameObject _playBtn;
    [SerializeField]private GameObject _creditsBtn;
    [SerializeField]private GameObject _backBtn;

    [SerializeField] ConnectionManager _connectionManager;
    [SerializeField] UnityEvent OnCreditsFinish;
    private void Awake()
    {
        Credits();
    }

    public void StartGame()
    {
        OnCreditsFinish?.Invoke();
        //SceneManager.LoadScene("LoadingScene");
    }

    public void Credits(){

        _credits.transform.localPosition = new Vector3(0, -700, 0);

        StartCoroutine(CreditsAnimation());
    }
    IEnumerator CreditsAnimation(){
        int y = -700;
        bool isRunning = true;
        while (isRunning){
            _credits.transform.localPosition = new Vector3(0, y, 0);
            y += 5;
            yield return new WaitForSeconds(0.01f);
            if (y >= 5500)
            {
                isRunning = false;
                StartGame();
            }
        }
        print("Acabo");
    }
}
