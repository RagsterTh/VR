using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private string _animationName;
    private Animation _animation;


    void Start()
    {
        _animation = GetComponent<Animation>();
        if (_animation == null)
        {
            Debug.LogError("Door: No Animation component found on this GameObject.");
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Collider col = GetComponent<Collider>();
            if (_animation != null && !string.IsNullOrEmpty(_animationName))
            {
                _animation.Play(_animationName);
                _animation.wrapMode = WrapMode.Once;
                Debug.Log("Door: Playing animation " + _animationName);
            }
            col.enabled = false;
        }
    }
   
}
