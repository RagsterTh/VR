using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class TitleFunctions : MonoBehaviour
{
    [SerializeField]private GameObject _credits;
    [SerializeField]private GameObject _playBtn;
    [SerializeField]private GameObject _creditsBtn;
    [SerializeField]private GameObject _backBtn;

    public void StartGame()
    {
        SceneManager.LoadScene("LoadingScene");
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
        while(true){
            _credits.transform.localPosition = new Vector3(0, y, 0);
            y += 10;
            yield return new WaitForSeconds(0.01f);
        }
    }
}
