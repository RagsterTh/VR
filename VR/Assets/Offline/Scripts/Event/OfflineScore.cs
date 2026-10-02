using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// What the visitor did in the current offline session (kills, accuracy, life left, medical answers)
/// and the score/grade shown on the result screen. Reset when a mode starts.
/// </summary>
public static class OfflineScore
{
    public const int PointsPerKill = 100;
    public const int AccuracyBonus = 500;
    public const int LifeBonus = 300;
    public const int PointsPerTreatment = 250;
    public const int PenaltyPerWrongTreatment = 75;

    /// <summary>False while shots must not count (tutorial).</summary>
    public static bool Counting = true;

    public static OfflineExperienceMode Mode { get; private set; }
    public static int Kills { get; private set; }
    public static int Shots { get; private set; }
    public static int Hits { get; private set; }
    public static int MedicalCorrect { get; private set; }
    public static int MedicalWrong { get; private set; }
    /// <summary>Life left when the battle ended, 0..1.</summary>
    public static float LifeRatio { get; private set; } = 1f;
    public static float Seconds { get; private set; }
    /// <summary>A finished session is waiting to be shown on the result screen.</summary>
    public static bool HasResult { get; private set; }

    private static float _startTime;

    public static void Begin(OfflineExperienceMode mode)
    {
        Mode = mode;
        Kills = Shots = Hits = MedicalCorrect = MedicalWrong = 0;
        LifeRatio = 1f;
        Seconds = 0f;
        HasResult = false;
        Counting = true;
        _startTime = Time.realtimeSinceStartup;
    }

    public static void AddShot() { if (Counting) Shots++; }
    public static void AddHit() { if (Counting) Hits++; }
    public static void AddKill() { if (Counting) Kills++; }
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

    public static float Accuracy => Shots > 0 ? Mathf.Clamp01((float)Hits / Shots) : 0f;

    public static int Total
    {
        get
        {
            int total = Kills * PointsPerKill
                        + Mathf.RoundToInt(Accuracy * AccuracyBonus)
                        + Mathf.RoundToInt(LifeRatio * LifeBonus)
                        + MedicalCorrect * PointsPerTreatment
                        - MedicalWrong * PenaltyPerWrongTreatment;
            return Mathf.Max(0, total);
        }
    }

    /// <summary>S, A, B or C from how well the visitor played (accuracy, life kept, treatments), not from raw points.</summary>
    public static string Grade
    {
        get
        {
            float performance;
            int answers = MedicalCorrect + MedicalWrong;
            if (Mode == OfflineExperienceMode.FullExperience && answers > 0)
                performance = 0.45f * Accuracy + 0.3f * LifeRatio + 0.25f * ((float)MedicalCorrect / answers);
            else
                performance = 0.6f * Accuracy + 0.4f * LifeRatio;

            if (performance >= 0.8f) return "S";
            if (performance >= 0.62f) return "A";
            if (performance >= 0.42f) return "B";
            return "C";
        }
    }
}

/// <summary>Top 10 of the day per mode, saved on the headset (Application.persistentDataPath/offline_ranking.json).</summary>
public static class OfflineLeaderboard
{
    public const int MaxEntries = 10;

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
        _data = new Data();
        Save();
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
