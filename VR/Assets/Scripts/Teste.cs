using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teste : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameController.instance.OnGameSceneLoad.Invoke();   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
