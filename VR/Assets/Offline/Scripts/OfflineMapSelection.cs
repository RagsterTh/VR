using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Map choice on the GloboV2 globe (offline). The buttons over the map picture used to call
/// WaitingPlayers.Finish (arena inside GloboV2); offline they load the chosen map scene instead,
/// where the combat kit runs. The choice is the only thing that waits for the player.
/// </summary>
public sealed class OfflineMapSelection : MonoBehaviour
{
    [Serializable]
    public struct MapButton
    {
        public Button Button;
        [Tooltip("Offline scene loaded by this button (Offline_Cambirela, Offline_Guarda or Offline_PedraBranca).")]
        public string Scene;
    }

    [Tooltip("Globe that appears when Eve's dialogue ends. The map panel opens together with it.")]
    [SerializeField] private GameObject _globe;
    [Tooltip("Panel/canvas with the map picture and the buttons. Opened automatically (no need to hit the pin).")]
    [SerializeField] private GameObject _mapPanel;
    [SerializeField] private MapButton[] _maps;

    private bool _chosen;

    private void Awake()
    {
        foreach (MapButton map in _maps)
        {
            if (map.Button == null)
                continue;

            // Offline the original action (open the GloboV2 arena) must not run.
            for (int i = 0; i < map.Button.onClick.GetPersistentEventCount(); i++)
                map.Button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

            string scene = map.Scene;
            map.Button.onClick.AddListener(() => Choose(scene));
        }
    }

    private IEnumerator Start()
    {
        if (_mapPanel == null)
            yield break;

        while (_globe != null && !_globe.activeInHierarchy)
            yield return null;

        _mapPanel.SetActive(true);
    }

    public void Choose(string scene)
    {
        if (_chosen || string.IsNullOrEmpty(scene))
            return;

        _chosen = true;
        Debug.Log($"[Offline] Mapa escolhido: {scene}");
        OfflineSession.LoadScene(scene);
    }
}
