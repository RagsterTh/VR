using UnityEngine;

public class LightPulse : MonoBehaviour
{

    private Light _light;
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;
    public float speed = 1f;

    void Start()
    {
        _light = GetComponent<Light>();
        if (_light == null)
        {
            Debug.LogError("LightPulse: No Light component found on this GameObject.");
        }
    }

    void Update()
    {
        if (_light != null)
        {
            float t = Mathf.PingPong(Time.time * speed, 1f);
            _light.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        }
    }
}
