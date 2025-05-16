using UnityEngine;
using UnityEngine.XR.Management;
public class VRSystemController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        var manager = XRGeneralSettings.Instance.Manager;

        if (manager != null && manager.isInitializationComplete)
        {
            var loader = manager.activeLoader;
            if (loader != null && loader.name.Contains("OpenXR"))
            {
                ConnectionManager.isVR = true;
            }
        } else
        {
            ConnectionManager.isVR = false;
        }
    }
}
