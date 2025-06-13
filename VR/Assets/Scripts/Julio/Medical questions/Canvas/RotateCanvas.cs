using UnityEngine;

public class RotateCanvas : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform hudObject;
    [SerializeField] float distanceAhead = 2f;
    [SerializeField] float heightOffset = 0f;
    [SerializeField] float positionSmooth = 5f;
    [SerializeField] float rotationSmooth = 5f;

    void Update()
    {
        if (cameraTransform == null || hudObject == null) return;

        Vector3 forward = cameraTransform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 targetPos = cameraTransform.position + forward * distanceAhead + Vector3.up * heightOffset;
        hudObject.position = Vector3.Lerp(hudObject.position, targetPos, Time.deltaTime * positionSmooth);

        Quaternion targetRot = Quaternion.LookRotation(hudObject.position - cameraTransform.position);
        hudObject.rotation = Quaternion.Slerp(hudObject.rotation, targetRot, Time.deltaTime * rotationSmooth);
    }
}
