using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class TitleFunctions : MonoBehaviour
{
    [SerializeField]private GameObject _credits;
    [SerializeField]private GameObject _playBtn;
    [SerializeField]private GameObject _creditsBtn;
    [SerializeField]private GameObject _backBtn;

    [SerializeField] ConnectionManager _connectionManager;

    private void Awake()
    {
        _connectionManager = ConnectionManager.instance;
        Credits();
    }

    public void StartGame()
    {
        SceneManager.LoadScene("LoadingScene");
        _connectionManager.Connection();
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
