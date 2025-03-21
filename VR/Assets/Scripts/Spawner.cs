using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;
enum SpawnerType
{
    Terrestial, Aerial
}

public class Spawner : MonoBehaviourPunCallbacks
{
    [SerializeField] ObjectPool _enemyPool;
    [SerializeField] float _timeToSpawn;

    /*
    private void Start()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            gameObject.SetActive(false);
            return;
        }
        StartCoroutine(Spawn());
    }
    IEnumerator Spawn()
    {
        yield return new WaitForSeconds(_timeToSpawn);
        _enemyPool.CallObject(transform.position);
        StartCoroutine(Spawn());

    }
    */
    public const string CountdownStartTime = "StartTime";
    public delegate void spawnRateTriggered();

    public int Countdown = 2;

    private bool isTimerRunning;

    private int startTime;


    /// <summary>
    ///     Called when the timer has expired.
    /// </summary>
    public static event spawnRateTriggered OnSpawnRateTriggered;



    public override void OnEnable()
    {
        Debug.Log("OnEnable CountdownTimer");
        base.OnEnable();

        // the starttime may already be in the props. look it up.
        Initialize();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        Debug.Log("OnDisable CountdownTimer");
    }


    public void Update()
    {
        if (!this.isTimerRunning) return;

        float countdown = TimeRemaining();

        if (countdown > 0.0f) return;

        OnTimerEnds();
    }


    private void OnTimerRuns()
    {
        this.isTimerRunning = true;
        this.enabled = true;
    }

    private void OnTimerEnds()
    {
        this.isTimerRunning = false;
        this.enabled = false;

        if (OnSpawnRateTriggered != null) OnSpawnRateTriggered();
    }


    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        Initialize();
    }


    private void Initialize()
    {
        int propStartTime;
        if (TryGetStartTime(out propStartTime))
        {
            this.startTime = propStartTime;

            this.isTimerRunning = TimeRemaining() > 0;

            if (this.isTimerRunning)
                OnTimerRuns();
            else
                OnTimerEnds();
        }
    }


    private float TimeRemaining()
    {
        int timer = PhotonNetwork.ServerTimestamp - this.startTime;
        return this.Countdown - timer / 1000f;
    }


    public static bool TryGetStartTime(out int startTimestamp)
    {
        startTimestamp = PhotonNetwork.ServerTimestamp;

        object startTimeFromProps;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CountdownStartTime, out startTimeFromProps))
        {
            startTimestamp = (int)startTimeFromProps;
            return true;
        }

        return false;
    }


    public static void SetStartTime()
    {
        int startTime = 0;
        bool wasSet = TryGetStartTime(out startTime);

        Hashtable props = new Hashtable
            {
                {CountdownTimer.CountdownStartTime, (int)PhotonNetwork.ServerTimestamp}
            };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);


        Debug.Log("Set Custom Props for Time: " + props.ToStringFull() + " wasSet: " + wasSet);
    }
}
