using System.Collections;
using UnityEngine;
using System;
public class MoveTween : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] bool useRectTransform;

    [SerializeField] float duration;

    [Header("Extra")]
    [SerializeField] AnimationCurve easeType;

    RectTransform rectTransform;
    Vector3 targetScale;
    void Start()
    {
        if (useRectTransform)
        {
            rectTransform = transform.GetComponent<RectTransform>();
            targetScale = rectTransform.localScale;
        }
        else
            targetScale = transform.localScale;
    }
    [ContextMenu("Move")]
    public void Move()
    {
        
        if (useRectTransform)
            StartCoroutine(MoveCoroutine(targetScale, (newScale) => rectTransform.localScale = newScale));
        else
            StartCoroutine(MoveCoroutine(targetScale, (newScale) => transform.localScale = newScale));
    }
    IEnumerator MoveCoroutine(Vector2 targetScale, Action<Vector2> moveVariable)
    {
        float t = 0;
        Vector3 startScale = useRectTransform ? rectTransform.localScale * 0.1f : transform.localScale;
        print(easeType.Evaluate(0.75f));
        while(t < duration)
        {
            t += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(t / duration);
            float easedTime = easeType.Evaluate(normalizedTime);

            Vector3 newScale = Vector3.LerpUnclamped(startScale, targetScale, easedTime);

            moveVariable?.Invoke(newScale);

            yield return null;
        }
    }
    [ContextMenu("Reset Tween")]
    public void ResetTween()
    {
        if (useRectTransform)
            rectTransform.localScale = rectTransform.localScale * 0.1f;
        else
            transform.localScale = transform.localScale * 0.1f;
    }
    [ContextMenu("TestMoveTween")]
    public void TestTween()
    {
        Move();
    }
}
