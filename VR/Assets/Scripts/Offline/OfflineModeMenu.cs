using UnityEngine;

/// <summary>
/// Button entry points used only by the Offline scene.
/// </summary>
public sealed class OfflineModeMenu : MonoBehaviour
{
    private void Awake()
    {
        OfflineSession.PrepareMenu();
    }

    public void StartCombat()
    {
        OfflineSession.StartCombatOnly();
    }

    public void StartFullExperience()
    {
        OfflineSession.StartFullExperience();
    }

    public void Quit()
    {
        Application.Quit();
    }
}
