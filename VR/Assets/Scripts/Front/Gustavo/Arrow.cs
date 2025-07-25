using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 2f; // Velocidade do movimento
    public float distance = 3f; // Distância máxima do movimento
    private Vector3 startPos; // Posição inicial local do objeto

    void Start()
    {
        // Armazena a posição inicial local do objeto
        startPos = transform.position;
    }

    // Update é chamado uma vez por frame
    void Update()
    {
        // Calcula o movimento usando Mathf.PingPong
        float movement = Mathf.PingPong(Time.time * speed, distance);
        // Movimento relativo à rotação local do objeto
        transform.position = startPos + transform.right * movement;
    }
}
