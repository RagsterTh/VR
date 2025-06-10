using UnityEngine;

public class RotateCanvas : MonoBehaviour
{
    [SerializeField] GameObject rotationBase;
    [SerializeField] GameObject objToRotate;

    private void Update()
    {
        Vector3 currentRotation = objToRotate.transform.eulerAngles;
        float targetY = rotationBase.transform.eulerAngles.y;
        objToRotate.transform.eulerAngles = new Vector3(currentRotation.x, targetY, currentRotation.z);
    }
}
