using UnityEngine;
using UnityEngine.SceneManagement;
public class TitleFunctions : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("LoadingScene");
    }
}
