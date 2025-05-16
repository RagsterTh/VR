using System.Collections;
using UnityEngine;

public class Location : MonoBehaviour, IShootable
{
    [SerializeField] GameObject _menuOBJ;
    [SerializeField] GameObject _menuConfirm;
    [SerializeField] bool isOpened;
    [SerializeField] bool isConfirmOpened;
    [SerializeField] Vector3 _originalScale;
    [SerializeField] Vector3 _upperScale;
    [SerializeField] float _interpolateTime;

    private void FixedUpdate()
    {
        _menuOBJ.SetActive(isOpened);

        isConfirmOpened = _menuConfirm.activeInHierarchy;
    }

    private void OnMouseDown()
    {
        Hit();
    }

    public void OnMouseEnter()
    {
        isOpened = true;
        StartCoroutine(InterpolateScale(transform.localScale, _upperScale, _interpolateTime));
    }

    public void OnMouseExit()
    {
        isOpened = false;
        StartCoroutine(InterpolateScale(transform.localScale, _originalScale, _interpolateTime));
    }

    IEnumerator InterpolateScale(Vector3 start, Vector3 end, float time)
    {
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            transform.localScale = Vector3.Lerp(start, end, elapsedTime / time);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = end;
    }

    public void Hit()
    {
        if (!_menuConfirm.activeInHierarchy)
        {
            _menuConfirm.SetActive(true);
        }
        else
        {
            _menuConfirm.SetActive(false);
        }
    }
}
