using UnityEngine;

public class SkyboxJuice : MonoBehaviour
{
    [Header("Skybox")]
    [SerializeField] private float rotationSpeed = 0.05f;

    private Material skyboxMaterial;

    private void Start()
    {
        // Pega o material da Skybox atual
        skyboxMaterial = RenderSettings.skybox;

        if (skyboxMaterial == null)
        {
            Debug.LogWarning("Nenhuma Skybox foi encontrada!");
        }
    }

    private void Update()
    {
        if (skyboxMaterial == null)
            return;

        // Faz a Skybox girar suavemente
        float rotation = Time.time * rotationSpeed;

        skyboxMaterial.SetFloat("_Rotation", rotation);
    }
}