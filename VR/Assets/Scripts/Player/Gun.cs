using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class Gun : MonoBehaviour
{
    PhotonView _phView;
    [SerializeField] float _bulletSpeed = 4;
    ObjectPool _playersBullets;
    Transform _gunPoint;
    ISoundable _soundEmitter;
    HapticImpulsePlayer _haptic;
    ParticleSystem _muzzleParticles;
    Light _muzzleLight;
    AudioSource _hitAudio;
    float _muzzleRemaining;
    static AudioClip _hitClip;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _phView = GetComponentInParent<PhotonView>();
        if (!OfflineSession.IsOffline && !_phView.IsMine)
        {
            enabled = false;
            return;
        }

        _gunPoint = transform.GetChild(0);
        if (GameController.instance != null)
            _playersBullets = GameController.instance.PlayersBullets;
        _soundEmitter = GetComponent<ISoundable>();
        _haptic = GetComponentInParent<HapticImpulsePlayer>();
        if (OfflineSession.IsOffline)
            CreateOfflineFeedback();
    }

    // Update is called once per frame
    void Update()
    {
        if (_muzzleLight == null || _muzzleRemaining <= 0f)
            return;

        _muzzleRemaining = Mathf.Max(0f, _muzzleRemaining - Time.unscaledDeltaTime);
        _muzzleLight.intensity = 1.8f * (_muzzleRemaining / 0.07f);
    }
    public void Shoot(InputAction.CallbackContext value)
    {
        if (value.performed)
        {
            if (_playersBullets == null)
                return;

            GameObject bullet = _playersBullets.CallObject(_gunPoint.position, _gunPoint.rotation);
            if (bullet == null)
                return;

            _soundEmitter?.PlaySound();
            if (OfflineSession.IsOffline)
            {
                if (bullet.TryGetComponent(out PlayerBullet playerBullet))
                    playerBullet.SetShooter(this);
                _muzzleRemaining = 0.07f;
                if (_muzzleLight != null)
                    _muzzleLight.intensity = 1.8f;
                _muzzleParticles?.Play(true);
                if (_haptic != null && _haptic.isActiveAndEnabled)
                    _haptic.SendHapticImpulse(0.28f, 0.055f);
                OfflineCombatFeedback.Shot(this, bullet);
            }
            //temp.GetComponent<Rigidbody>().linearVelocity = -transform.up * _bulletSpeed;
        }

    }

    public void ConfirmHit()
    {
        if (!OfflineSession.IsOffline)
            return;
        if (_haptic != null && _haptic.isActiveAndEnabled)
            _haptic.SendHapticImpulse(0.48f, 0.07f);
        if (_hitAudio != null)
            _hitAudio.PlayOneShot(_hitClip);
    }

    private void CreateOfflineFeedback()
    {
        if (_gunPoint == null)
            return;

        var flash = new GameObject("Muzzle Flash");
        flash.transform.SetParent(_gunPoint, false);
        flash.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        _muzzleParticles = flash.AddComponent<ParticleSystem>();
        // Configure while stopped, and give it the URP additive material: without one the renderer falls back to the
        // built-in Default-ParticleSystem, which renders pink in URP.
        _muzzleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        flash.GetComponent<ParticleSystemRenderer>().sharedMaterial = OfflineFx.ParticleMaterial;
        var main = _muzzleParticles.main;
        main.duration = 0.08f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.05f);
        main.startColor = new Color(0.45f, 0.85f, 1f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = _muzzleParticles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 7) });
        var shape = _muzzleParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.01f;
        _muzzleLight = flash.AddComponent<Light>();
        _muzzleLight.type = LightType.Point;
        _muzzleLight.color = new Color(0.35f, 0.8f, 1f);
        _muzzleLight.range = 0.9f;
        _muzzleLight.intensity = 0f;

        _hitAudio = gameObject.AddComponent<AudioSource>();
        _hitAudio.playOnAwake = false;
        _hitAudio.spatialBlend = 0f;
        _hitAudio.volume = 0.22f;
        if (_hitClip == null)
            _hitClip = CreateHitClip();
    }

    private static AudioClip CreateHitClip()
    {
        const int rate = 24000;
        const int count = 2160;
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float envelope = 1f - i / (float)count;
            samples[i] = Mathf.Sin(2f * Mathf.PI * (920f * t + 1500f * t * t)) * envelope * envelope * 0.18f;
        }
        AudioClip clip = AudioClip.Create("Offline Hit Confirm", count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
