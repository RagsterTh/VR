
using UnityEngine;
using UnityEngine.Events;

public class Boss : MonoBehaviour
{
    public UnityEvent OnDeath;

    private void OnDestroy()
    {
        OnDeath.Invoke();
    }
}
