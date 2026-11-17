using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

            for (int i = 0; i < map.Button.onClick.GetPersistentEventCount(); i++)
                map.Button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

            string scene = map.Scene;
            Button button = map.Button;
            map.Button.onClick.AddListener(() => Choose(scene, button));
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

    public void Choose(string scene) => Choose(scene, null);

    public void Choose(string scene, Button button)
    {
        if (_chosen || string.IsNullOrEmpty(scene))
            return;

        _chosen = true;
        Debug.Log($"[Offline] Mapa escolhido: {scene}");
        StartCoroutine(ChooseRoutine(scene, button));
    }

    private IEnumerator ChooseRoutine(string scene, Button button)
    {
        yield return OfflineLobbyFeedback.MapChosen(button);
        OfflineSession.LoadScene(scene);
    }
}
