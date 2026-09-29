using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Brings the Karen/EVE robot to life (offline). The timeline's recorded clip animates an old armature that the
/// robot's meshes are not attached to, so the robot stood still. This animates the robot's root instead (the timeline
/// does not drive it): hover, slowly turning towards the player, talking nods/bounce while the voice plays and
/// neon parts glowing brighter when she speaks. Everything moves together, so the skinned mouth stays aligned.
/// </summary>
public sealed class OfflineEveAnimator : MonoBehaviour
{
    [SerializeField] private float _hoverHeight = 0.035f;
    [SerializeField] private float _hoverSpeed = 1.6f;
    [Tooltip("Max degrees she turns from her rest direction to face the player.")]
    [SerializeField] private float _maxTurn = 30f;
    [SerializeField] private float _turnSpeed = 1.5f;
    [SerializeField] private float _talkNod = 4f;
    [SerializeField] private float _voiceLength = 11.8f;

    public PlayableDirector Director;

    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private float _yaw;
    private float _talk;
    private Renderer[] _neon;
    private Color[] _neonColors;
    private MaterialPropertyBlock _block;

    private void Start()
    {
        _basePosition = transform.localPosition;
        _baseRotation = transform.localRotation;
        _block = new MaterialPropertyBlock();

        var neon = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            if (r.sharedMaterial != null && r.sharedMaterial.name.Contains("Neon"))
                neon.Add(r);
        _neon = neon.ToArray();
        _neonColors = new Color[_neon.Length];
        for (int i = 0; i < _neon.Length; i++)
        {
            Material m = _neon[i].sharedMaterial;
            _neonColors[i] = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.cyan;
        }
    }

    private void LateUpdate()
    {
        float t = Time.time;
        bool talking = Director != null && Director.state == PlayState.Playing && Director.time < _voiceLength;
        _talk = Mathf.MoveTowards(_talk, talking ? 1f : 0f, Time.deltaTime * 3f);

        // Turn towards the player's head, within limits, smoothly.
        Camera head = Camera.main;
        float targetYaw = 0f;
        if (head != null && transform.parent != null)
        {
            Vector3 toPlayer = transform.parent.InverseTransformDirection(head.transform.position - transform.position);
            toPlayer.y = 0f;
            Vector3 restForward = _baseRotation * Vector3.forward;
            restForward.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f && restForward.sqrMagnitude > 0.01f)
                targetYaw = Mathf.Clamp(Vector3.SignedAngle(restForward, toPlayer, Vector3.up), -_maxTurn, _maxTurn);
        }
        _yaw = Mathf.LerpAngle(_yaw, targetYaw, 1f - Mathf.Exp(-_turnSpeed * Time.deltaTime));

        // Speech rhythm: a mix of two frequencies reads as natural nodding.
        float speech = (Mathf.Sin(t * 7.3f) * 0.6f + Mathf.Sin(t * 11.1f) * 0.4f) * _talk;
        float hover = Mathf.Sin(t * _hoverSpeed) * _hoverHeight + Mathf.Abs(speech) * 0.012f;
        float sway = Mathf.Sin(t * _hoverSpeed * 0.5f) * 1.5f;

        transform.localPosition = _basePosition + Vector3.up * hover;
        transform.localRotation = _baseRotation * Quaternion.Euler(speech * _talkNod, _yaw, sway * (1f - 0.5f * _talk));

        // Neon glows brighter while she talks.
        float glow = 1f + 0.8f * _talk * (0.6f + 0.4f * Mathf.Abs(speech)) + 0.15f * Mathf.Sin(t * 2f);
        for (int i = 0; i < _neon.Length; i++)
        {
            if (_neon[i] == null) continue;
            _neon[i].GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", _neonColors[i] * glow);
            _block.SetColor("_EmissionColor", _neonColors[i] * glow);
            _neon[i].SetPropertyBlock(_block);
        }
    }
}
