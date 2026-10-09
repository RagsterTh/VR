using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// What the visitor did in the current offline session and the score/grade shown on the result screen.
///
/// Every mode is worth up to <see cref="MaxPoints"/>. The points only measure how well the visitor played
/// (aim, how fast enemies go down, life kept, first-try treatments) - never how many enemies happened to spawn -
/// and the grade comes straight from the points, so more points never means a lower grade.
/// </summary>
public static class OfflineScore
{
    public const int MaxPoints = 10000;

    // Weight of each part (both modes add up to MaxPoints).
    public const int CombatAccuracyPoints = 4000;
    public const int CombatReactionPoints = 3000;
    public const int CombatLifePoints = 3000;
    public const int FullAccuracyPoints = 3000;
    public const int FullReactionPoints = 2000;
    public const int FullLifePoints = 2000;
    public const int FullMedicalPoints = 3000;

    /// <summary>Accuracy that already gives all the accuracy points (nobody hits 100% in VR).</summary>
    public const float FullAccuracyAt = 0.75f;
    /// <summary>Average seconds an enemy stays alive: at or below this, all the reaction points.</summary>
    public const float FastKillSeconds = 4f;
    /// <summary>At or above this, no reaction points.</summary>
    public const float SlowKillSeconds = 15f;

    // Grades as a share of MaxPoints.
    public const int GradeS = 8500;
    public const int GradeA = 7000;
    public const int GradeB = 5000;

    /// <summary>False while shots must not count (tutorial).</summary>
    public static bool Counting = true;

    public static OfflineExperienceMode Mode { get; private set; }
    public static int Kills { get; private set; }
    public static int Shots { get; private set; }
    public static int Hits { get; private set; }
    public static int MedicalCorrect { get; private set; }
    public static int MedicalWrong { get; private set; }
    /// <summary>Life left, 0..1 (kept up to date during the battle, final when it ends).</summary>
    public static float LifeRatio { get; private set; } = 1f;
    public static float Seconds { get; private set; }
    /// <summary>A finished session is waiting to be shown on the result screen.</summary>
    public static bool HasResult { get; private set; }

    private static float _startTime;
    private static float _aliveSum;
    private static int _aliveCount;

    public static void Begin(OfflineExperienceMode mode)
    {
        Mode = mode;
        Kills = Shots = Hits = MedicalCorrect = MedicalWrong = 0;
        LifeRatio = 1f;
        Seconds = 0f;
        HasResult = false;
        Counting = true;
        _aliveSum = 0f;
        _aliveCount = 0;
        _startTime = Time.realtimeSinceStartup;
    }

    public static void AddShot() { if (Counting) Shots++; }
    public static void AddHit() { if (Counting) Hits++; }

    /// <summary>An enemy went down; <paramref name="secondsAlive"/> since it appeared (negative = unknown).</summary>
    public static void AddKill(float secondsAlive = -1f)
    {
        if (!Counting)
            return;
        Kills++;
        if (secondsAlive >= 0f)
        {
            // One enemy forgotten in a corner must not wipe out the whole average.
            _aliveSum += Mathf.Min(secondsAlive, SlowKillSeconds * 2f);
            _aliveCount++;
        }
    }

    public static void AddTreatment(bool correct) { if (correct) MedicalCorrect++; else MedicalWrong++; }
    public static void SetLifeRatio(float ratio) => LifeRatio = Mathf.Clamp01(ratio);

    /// <summary>
    /// The session reached its end (credits): keep the numbers for the result screen.
    /// Returns false when there is nothing to show (no mode was being played).
    /// </summary>
    public static bool Finish()
    {
        // A scene played directly in the Editor never went through Begin: use the mode inferred from it.
        if (Mode == OfflineExperienceMode.None)
            Mode = OfflineSession.Mode;
        if (Mode == OfflineExperienceMode.None)
            return false;
        Seconds = Time.realtimeSinceStartup - _startTime;
        HasResult = true;
        return true;
    }

    /// <summary>Result shown or session abandoned: nothing pending any more.</summary>
    public static void Clear() => HasResult = false;

    // ---------- Measures ----------

    public static float Accuracy => Shots > 0 ? Mathf.Clamp01((float)Hits / Shots) : 0f;

    /// <summary>Average seconds between an enemy appearing and going down (-1 = no data).</summary>
    public static float AverageSecondsAlive => _aliveCount > 0 ? _aliveSum / _aliveCount : -1f;

    /// <summary>Treatments right on the first try, 0..1 (-1 = none answered).</summary>
    public static float MedicalRatio
    {
        get
        {
            int answers = MedicalCorrect + MedicalWrong;
            return answers > 0 ? (float)MedicalCorrect / answers : -1f;
        }
    }

    // ---------- Points ----------

    public readonly struct Part
    {
        public readonly string Label;
        public readonly string Value;
        public readonly int Points;
        public readonly int Max;

        public Part(string label, string value, int points, int max)
        {
            Label = label;
            Value = value;
            Points = points;
            Max = max;
        }
    }

    private static bool Full => Mode == OfflineExperienceMode.FullExperience;

    private static int MaxAccuracy => Full ? FullAccuracyPoints : CombatAccuracyPoints;
    private static int MaxReaction => Full ? FullReactionPoints : CombatReactionPoints;
    private static int MaxLife => Full ? FullLifePoints : CombatLifePoints;
    private static int MaxMedical => Full ? FullMedicalPoints : 0;

    public static int AccuracyPoints => Mathf.RoundToInt(Mathf.Clamp01(Accuracy / FullAccuracyAt) * MaxAccuracy);

    public static int ReactionPoints
    {
        get
        {
            float alive = AverageSecondsAlive;
            return alive < 0f ? 0 : Mathf.RoundToInt(Mathf.InverseLerp(SlowKillSeconds, FastKillSeconds, alive) * MaxReaction);
        }
    }

    public static int LifePoints => Mathf.RoundToInt(LifeRatio * MaxLife);
    public static int MedicalPoints => Mathf.RoundToInt(Mathf.Max(0f, MedicalRatio) * MaxMedical);

    /// <summary>Total points (cheap: the HUD reads it every frame).</summary>
    public static int Total => Mathf.Clamp(AccuracyPoints + ReactionPoints + LifePoints + MedicalPoints, 0, MaxPoints);

    /// <summary>The parts of the score, in the order the result screen shows them.</summary>
    public static List<Part> Breakdown()
    {
        float alive = AverageSecondsAlive;
        var parts = new List<Part>
        {
            new("PRECISÃO", Shots > 0 ? Mathf.RoundToInt(Accuracy * 100f) + "%" : "--", AccuracyPoints, MaxAccuracy),
            new("TEMPO DE REAÇÃO", alive >= 0f ? alive.ToString("0.0") + "s" : "--", ReactionPoints, MaxReaction),
            new("VIDA RESTANTE", Mathf.RoundToInt(LifeRatio * 100f) + "%", LifePoints, MaxLife),
        };
        if (Full)
        {
            string value = MedicalRatio >= 0f ? MedicalCorrect + "/" + (MedicalCorrect + MedicalWrong) : "--";
            parts.Add(new Part("TRATAMENTOS DE 1ª", value, MedicalPoints, MaxMedical));
        }
        return parts;
    }

    public static string Grade => GradeFor(Total);

    /// <summary>The grade only depends on the points: S 8500+, A 7000+, B 5000+, C below.</summary>
    public static string GradeFor(int points)
    {
        if (points >= GradeS) return "S";
        if (points >= GradeA) return "A";
        if (points >= GradeB) return "B";
        return "C";
    }
}

/// <summary>Top 10 of the day per mode, saved on the headset (Application.persistentDataPath/offline_ranking.json).</summary>
public static class OfflineLeaderboard
{
    public const int MaxEntries = 10;
    /// <summary>Bump when the score formula changes: older rankings are archived and a new one starts.</summary>
    private const int ScoringVersion = 2;

    [Serializable]
    public class Entry
    {
        public string name;
        public int score;
        public string grade;
        public string date;
    }

    [Serializable]
    private class Data
    {
        /// <summary>Scoring version: entries from another version are not comparable (different scale).</summary>
        public int version;
        public List<Entry> combat = new();
        public List<Entry> full = new();
    }

    private static Data _data;

    private static string FilePath => Path.Combine(Application.persistentDataPath, "offline_ranking.json");

    private static Data Loaded
    {
        get
        {
            if (_data != null)
                return _data;
            try
            {
                _data = File.Exists(FilePath) ? JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)) : new Data();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Offline] Ranking ilegível, começando vazio: {e.Message}");
                _data = new Data();
            }
            _data ??= new Data();
            if (_data.version != ScoringVersion)
            {
                Archive("v" + _data.version);
                _data = new Data();
            }
            _data.version = ScoringVersion;
            _data.combat ??= new List<Entry>();
            _data.full ??= new List<Entry>();
            return _data;
        }
    }

    public static IReadOnlyList<Entry> Top(OfflineExperienceMode mode) => List(mode);

    private static string Today => DateTime.Now.ToString("yyyy-MM-dd");

    private static List<Entry> List(OfflineExperienceMode mode)
    {
        StartNewDayIfNeeded();
        return mode == OfflineExperienceMode.FullExperience ? Loaded.full : Loaded.combat;
    }

    /// <summary>
    /// The ranking is per day (each day of the event has its own winners). Scores from another day are moved
    /// to offline_ranking_YYYY-MM-DD.json, next to the current file, so nothing is lost.
    /// </summary>
    private static void StartNewDayIfNeeded()
    {
        Data data = Loaded;
        string today = Today;
        string oldDay = null;
        foreach (List<Entry> list in new[] { data.combat, data.full })
            foreach (Entry entry in list)
                if (entry.date == null || !entry.date.StartsWith(today))
                    oldDay = entry.date != null && entry.date.Length >= 10 ? entry.date.Substring(0, 10) : "antigo";
        if (oldDay == null)
            return;

        try
        {
            File.WriteAllText(Path.Combine(Application.persistentDataPath, $"offline_ranking_{oldDay}.json"), JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Offline] Não foi possível arquivar o ranking de {oldDay}: {e.Message}");
        }

        data.combat.RemoveAll(entry => entry.date == null || !entry.date.StartsWith(today));
        data.full.RemoveAll(entry => entry.date == null || !entry.date.StartsWith(today));
        Save();
    }

    /// <summary>Position (0-based) this score would take, or -1 if it does not make the top.</summary>
    public static int RankFor(OfflineExperienceMode mode, int score)
    {
        List<Entry> list = List(mode);
        int rank = 0;
        while (rank < list.Count && list[rank].score >= score)
            rank++;
        return rank < MaxEntries ? rank : -1;
    }

    /// <summary>Adds the score and returns its position (0-based), or -1 if it did not make the top.</summary>
    public static int Add(OfflineExperienceMode mode, string name, int score, string grade)
    {
        int rank = RankFor(mode, score);
        if (rank < 0)
            return -1;

        List<Entry> list = List(mode);
        list.Insert(rank, new Entry { name = name, score = score, grade = grade, date = DateTime.Now.ToString("yyyy-MM-dd HH:mm") });
        if (list.Count > MaxEntries)
            list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        Save();
        return rank;
    }

    public static void ClearAll()
    {
        _data = new Data { version = ScoringVersion };
        Save();
    }

    /// <summary>Keeps a copy of the ranking file as offline_ranking_SUFFIX.json before it is replaced.</summary>
    private static void Archive(string suffix)
    {
        try
        {
            if (File.Exists(FilePath))
                File.Copy(FilePath, Path.Combine(Application.persistentDataPath, $"offline_ranking_{suffix}.json"), true);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Offline] Não foi possível arquivar o ranking antigo: {e.Message}");
        }
    }

    private static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Loaded, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Offline] Não foi possível salvar o ranking: {e.Message}");
        }
    }
}
