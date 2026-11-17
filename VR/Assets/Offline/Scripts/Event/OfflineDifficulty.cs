using UnityEngine;

public enum OfflineDifficultyLevel
{
    Recruta,
    Soldado,
    Elite
}

/// <summary>
/// Difficulty chosen on the offline menu. Changes how fast enemies come, how much they hurt and how many body
/// shots they take (the glowing weak point always kills in one). Each difficulty has its own ranking.
/// Remembered on the headset, so the operator sets it once for the event.
/// </summary>
public static class OfflineDifficulty
{
    private const string PrefKey = "offline_difficulty";
    private static bool _loaded;
    private static OfflineDifficultyLevel _current = OfflineDifficultyLevel.Soldado;

    public static OfflineDifficultyLevel Current
    {
        get
        {
            if (!_loaded)
            {
                _loaded = true;
                _current = (OfflineDifficultyLevel)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, (int)OfflineDifficultyLevel.Soldado), 0, 2);
            }
            return _current;
        }
        set
        {
            _loaded = true;
            _current = value;
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static string Label(OfflineDifficultyLevel level) => level switch
    {
        OfflineDifficultyLevel.Recruta => "RECRUTA",
        OfflineDifficultyLevel.Elite => "ELITE",
        _ => "SOLDADO"
    };

    public static string Description(OfflineDifficultyLevel level) => level switch
    {
        OfflineDifficultyLevel.Recruta => "INIMIGOS MAIS LENTOS E FRÁGEIS",
        OfflineDifficultyLevel.Elite => "MAIS RÁPIDOS, MAIS FORTES, 3 TIROS NO CORPO",
        _ => "EQUILIBRADO  -  2 TIROS NO CORPO"
    };

    public static Color Color(OfflineDifficultyLevel level) => level switch
    {
        OfflineDifficultyLevel.Recruta => new Color(0.35f, 1f, 0.55f),
        OfflineDifficultyLevel.Elite => new Color(1f, 0.3f, 0.85f),
        _ => new Color(1f, 0.75f, 0.25f)
    };

    /// <summary>Multiplies how often enemies spawn.</summary>
    public static float SpawnScale => Current switch
    {
        OfflineDifficultyLevel.Recruta => 0.65f,
        OfflineDifficultyLevel.Elite => 1.2f,
        _ => 1f
    };

    /// <summary>Multiplies the damage the player takes.</summary>
    public static float DamageScale => Current switch
    {
        OfflineDifficultyLevel.Recruta => 0.6f,
        OfflineDifficultyLevel.Elite => 1.5f,
        _ => 1f
    };

    /// <summary>Hits on the body to take an enemy down (the weak point always takes one).</summary>
    public static int BodyHits => Current switch
    {
        OfflineDifficultyLevel.Recruta => 1,
        OfflineDifficultyLevel.Elite => 3,
        _ => 2
    };
}
