using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 2f; // Velocidade do movimento
    public float distance = 3f; // Distância máxima do movimento

    // Update é chamado uma vez por frame
    void Update()
    {
        // Calcula o movimento usando Mathf.PingPong
        float movement = Mathf.PingPong(Time.time * speed, distance);
        transform.position = new Vector3(movement, transform.position.y, transform.position.z);
    }
}
