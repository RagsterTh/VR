using UnityEngine;
using UnityEngine.UI;

public class TempGUI : MonoBehaviour
{
    [SerializeField] Image _healthBar;
    
    public void AtualizeBar(float max, float current)
    {
        _healthBar.fillAmount = max/current;
    }
}
