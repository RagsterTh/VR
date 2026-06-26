using System.Collections;
using UnityEngine;
using System;
public class MoveTween : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] bool useRectTransform;
    [SerializeField] Vector3 targetPos;
    [SerializeField] float duration;

    [Header("Extra")]
    [SerializeField] AnimationCurve easeType;

    RectTransform rectTransform;
    Vector3 originalPos;
    void Start()
    {
        if (useRectTransform)
        {
            rectTransform = transform.GetComponent<RectTransform>();
            originalPos = rectTransform.anchoredPosition;
        }
        else
            originalPos = transform.position;
    }
    [ContextMenu("Move")]
    public void Move()
    {
        if (useRectTransform)
            StartCoroutine(MoveCoroutine(targetPos, (newPos) => rectTransform.anchoredPosition = newPos));
        else
            StartCoroutine(MoveCoroutine(originalPos, (newPos) => transform.position = newPos));
    }
    IEnumerator MoveCoroutine(Vector2 targetPos, Action<Vector2> moveVariable)
    {
        float t = 0;
        Vector3 startPos = useRectTransform ? rectTransform.anchoredPosition : transform.position;
        while(t < duration)
        {
            t += Time.deltaTime;
            print("moving");
            float normalizedTime = Mathf.Clamp01(t / duration);
            float easedTime = easeType.Evaluate(normalizedTime);

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, easedTime);

            moveVariable?.Invoke(newPos);

            yield return null;
        }
    }
    [ContextMenu("Reset Tween")]
    public void ResetTween()
    {
        if (useRectTransform)
            rectTransform.anchoredPosition = originalPos;
        else
            transform.position = originalPos;
    }
}
