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
        _credits.SetActive(true);
        _playBtn.SetActive(false);
        _creditsBtn.SetActive(false);
        _backBtn.SetActive(true);

        _credits.transform.localPosition = new Vector3(0, -700, 0);

        StartCoroutine(CreditsAnimation());
    }
    public void ReturnMenu(){
        StopAllCoroutines();
        _playBtn.SetActive(true);
        _creditsBtn.SetActive(true);
        _backBtn.SetActive(false);
        _credits.SetActive(false);
    }
    IEnumerator CreditsAnimation(){
        int y = -700;
        bool isRunning = true;
        while (isRunning){
            _credits.transform.localPosition = new Vector3(0, y, 0);
            y += 5;
            yield return new WaitForSeconds(0.01f);
            if (y >= 5000)
            {
                isRunning = false;
            }
        }
        print("Acabo");
    }
}
