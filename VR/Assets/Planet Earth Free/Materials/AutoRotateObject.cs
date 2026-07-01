using UnityEngine;

public class AutoRotateObject : MonoBehaviour
{
    [SerializeField] Vector3 rotateSpeed;


    private void Update()
    {

        transform.localRotation *= Quaternion.Euler(rotateSpeed * Time.deltaTime);
    }

}
