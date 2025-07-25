using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingTextAnimation : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI _loadingText;
    // Update is called once per frame
    void Start()
    {
        StartCoroutine(TextAnimation());
    }

    IEnumerator TextAnimation()
    {
        while (true)
        {
            _loadingText.text = "Loading";
            yield return new WaitForSeconds(0.5f);
            _loadingText.text = "Loading.";
            yield return new WaitForSeconds(0.5f);
            _loadingText.text = "Loading..";
            yield return new WaitForSeconds(0.5f);
            _loadingText.text = "Loading...";
            yield return new WaitForSeconds(0.5f);
        }
    }
}
