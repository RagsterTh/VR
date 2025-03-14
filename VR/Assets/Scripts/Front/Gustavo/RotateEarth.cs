using UnityEngine;

public class RotateEarth : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _maxRotationSpeed = 10f;
    [SerializeField] private float _deceleration = 2f;
    private float _currentRotationSpeed = 0f;
    private bool _isDragging = false;

    private void Update()
    {
        if (!_isDragging && Mathf.Abs(_currentRotationSpeed) > 0.01f)
        {
            _currentRotationSpeed = Mathf.MoveTowards(_currentRotationSpeed, 0, _deceleration * Time.deltaTime);
        }
        else if (!_isDragging)
        {
            _currentRotationSpeed = 0f;
        }

        transform.Rotate(Vector3.down, _currentRotationSpeed * Time.deltaTime * 50f);
    }

    private void OnMouseDrag()
    {
        float deltaX = Input.GetAxis("Mouse X");
        _currentRotationSpeed += deltaX * _rotationSpeed;
        _currentRotationSpeed = Mathf.Clamp(_currentRotationSpeed, -_maxRotationSpeed, _maxRotationSpeed);
        _isDragging = true;
    }

    private void OnMouseUp()
    {
        _isDragging = false;
    }
}
