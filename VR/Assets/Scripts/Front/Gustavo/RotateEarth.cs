using UnityEngine;

public class RotateEarth : MonoBehaviour
{
    [SerializeField] float _rotationSpeed = 5f;
    private float lastMouseX;

    private void OnMouseDrag()
    {
        float deltaX = Input.GetAxis("Mouse X");
        transform.Rotate(Vector3.down, deltaX * _rotationSpeed);
    }
}
