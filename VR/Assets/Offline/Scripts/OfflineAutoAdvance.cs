using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

/// <summary>
/// Offline replacement for a click the host (PC operator) used to make: waits for a moment of the
/// experience (an object becoming active and/or a narration finishing) and then runs the same action.
/// </summary>
public sealed class OfflineAutoAdvance : MonoBehaviour
{
    [Tooltip("Wait until this object is active (e.g. the combat area that opens later). Empty = start right away.")]
    [SerializeField] private GameObject _waitUntilActive;

    [Tooltip("If this object is already active when it is time to advance, do nothing (the player got there first).")]
    [SerializeField] private GameObject _skipIfActive;

    [Tooltip("Timeline to wait for: waits for it to start and then to reach its end (for signals placed on the very last frame, which never fire).")]
    [SerializeField] private PlayableDirector _waitForDirector;

    [Tooltip("Narration to wait for: waits for it to start and then to finish. Empty = only the delay.")]
    [SerializeField] private AudioSource _waitForAudio;
    [Tooltip("If the narration does not start within this time, continue anyway.")]
    [Min(0f)]
    [SerializeField] private float _maxWaitForAudioStart = 10f;

    [Tooltip("Extra seconds after the narration (or after becoming active) before advancing.")]
    [Min(0f)]
    [SerializeField] private float _delayAfter = 1.5f;

    [Tooltip("What the host used to trigger (e.g. GameController.ActiveBattle, WaitingPlayers.Finish).")]
    [SerializeField] private UnityEvent _onAdvance = new();

    public UnityEvent OnAdvance => _onAdvance;

    private IEnumerator Start()
    {
        while (_waitUntilActive != null && !_waitUntilActive.activeInHierarchy)
            yield return null;

        if (_waitForDirector != null)
        {
            float waited = 0f;
            while (_waitForDirector.state != PlayState.Playing && waited < _maxWaitForAudioStart)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            // WrapMode None stops the director at the end; Hold keeps it "playing" on the last frame.
            while (_waitForDirector.state == PlayState.Playing && _waitForDirector.time < _waitForDirector.duration - 0.05)
                yield return null;
        }

        if (_waitForAudio != null)
        {
            float waited = 0f;
            while (!_waitForAudio.isPlaying && waited < _maxWaitForAudioStart)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            while (_waitForAudio != null && _waitForAudio.isPlaying)
                yield return null;
        }

        yield return new WaitForSeconds(_delayAfter);

        if (_skipIfActive != null && _skipIfActive.activeInHierarchy)
            yield break;

        Debug.Log($"[Offline] Avanço automático: {name}");
        _onAdvance.Invoke();
    }
}
