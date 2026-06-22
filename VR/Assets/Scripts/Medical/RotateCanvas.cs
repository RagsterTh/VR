using UnityEngine;

public class RotateCanvas : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform hudObject;
    [SerializeField] float distanceAhead = 2f;
    [SerializeField] float heightOffset = 2f;
    [SerializeField] float positionSmooth = 5f;
    [SerializeField] float rotationSmooth = 5f;
    [SerializeField] float deadZoneAngle = 10f;

    void Update()
    {
        if (cameraTransform == null || hudObject == null) return;

        Vector3 cameraForwardFlat = cameraTransform.forward;
        cameraForwardFlat.y = 0f;
        cameraForwardFlat.Normalize();

        Vector3 toHudFlat = hudObject.position - cameraTransform.position;
        toHudFlat.y = 0f;
        toHudFlat.Normalize();

        float angle = Vector3.Angle(cameraForwardFlat, toHudFlat);

        if (angle > deadZoneAngle)
        {
            Vector3 targetPosition = cameraTransform.position + cameraForwardFlat * distanceAhead;
            targetPosition.y = heightOffset;
            hudObject.position = Vector3.Lerp(hudObject.position, targetPosition, Time.deltaTime * positionSmooth);
        }

        Vector3 direction = hudObject.position - cameraTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Vector3 euler = targetRotation.eulerAngles;
            Quaternion onlyYRotation = Quaternion.Euler(0f, euler.y, 0f);
            hudObject.rotation = Quaternion.Slerp(hudObject.rotation, onlyYRotation, Time.deltaTime * rotationSmooth);
        }
    }
}