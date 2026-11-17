using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class OfflineScore
{
    public const int MaxPoints = 10000;

    public const int CombatAccuracyPoints = 3000;
    public const int CombatReactionPoints = 2500;
    public const int CombatLifePoints = 2500;
    public const int CombatComboPoints = 2000;
    public const int FullAccuracyPoints = 2500;
    public const int FullReactionPoints = 1500;
    public const int FullLifePoints = 1500;
    public const int FullComboPoints = 1500;
    public const int FullMedicalPoints = 3000;

    public const int FullComboAt = 8;
    public const float FastAnswerSeconds = 6f;
    public const float SlowAnswerSeconds = 20f;

    public const float FullAccuracyAt = 0.75f;
    public const float FastKillSeconds = 4f;
    public const float SlowKillSeconds = 15f;

    public const int GradeS = 8500;
    public const int GradeA = 7000;
    public const int GradeB = 5000;

    public static bool Counting = true;

    public static OfflineExperienceMode Mode { get; private set; }
    public static OfflineDifficultyLevel Difficulty { get; private set; }
    public static int Kills { get; private set; }
    public static int Shots { get; private set; }
    public static int Hits { get; private set; }
    public static int MedicalCorrect { get; private set; }
    public static int MedicalWrong { get; private set; }
    public static float LifeRatio { get; private set; } = 1f;
    public static float Seconds { get; private set; }
    public static bool HasResult { get; private set; }

    private static float _startTime;
    private static float _aliveSum;
    private static int _aliveCount;
    private static float _answerSpeedSum;
    private static int _answerCount;

    public static void Begin(OfflineExperienceMode mode)
    {
        Mode = mode;
        Difficulty = OfflineDifficulty.Current;
        OfflineCombo.Reset();
        OfflineWeakPoint.ResetSession();
        _answerSpeedSum = 0f;
        _answerCount = 0;
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

    public static void AddKill(float secondsAlive = -1f)
    {
        if (!Counting)
            return;
        Kills++;
        if (secondsAlive >= 0f)
        {
            _aliveSum += Mathf.Min(secondsAlive, SlowKillSeconds * 2f);
            _aliveCount++;
        }
    }

    public static void AddTreatment(bool correct, float seconds = -1f)
    {
        if (correct) MedicalCorrect++; else MedicalWrong++;
        if (seconds < 0f)
            return;
        _answerSpeedSum += correct ? Mathf.InverseLerp(SlowAnswerSeconds, FastAnswerSeconds, seconds) : 0f;
        _answerCount++;
    }

    public static float AnswerSpeed => _answerCount > 0 ? _answerSpeedSum / _answerCount : -1f;
    public static void SetLifeRatio(float ratio) => LifeRatio = Mathf.Clamp01(ratio);

    public static bool Finish()
    {
        if (Mode == OfflineExperienceMode.None)
            Mode = OfflineSession.Mode;
        if (Mode == OfflineExperienceMode.None)
            return false;
        Seconds = Time.realtimeSinceStartup - _startTime;
        HasResult = true;
        return true;
    }

    public static void Clear() => HasResult = false;


    public static float Accuracy => Shots > 0 ? Mathf.Clamp01((float)Hits / Shots) : 0f;

    public static float AverageSecondsAlive => _aliveCount > 0 ? _aliveSum / _aliveCount : -1f;

    public static float MedicalRatio
    {
        get
        {
            int answers = MedicalCorrect + MedicalWrong;
            return answers > 0 ? (float)MedicalCorrect / answers : -1f;
        }
    }


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
    private static int MaxCombo => Full ? FullComboPoints : CombatComboPoints;

    public static int ComboPoints => Mathf.RoundToInt(Mathf.Clamp01((OfflineCombo.BestStreak - 1f) / (FullComboAt - 1f)) * MaxCombo);

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
    public static int MedicalPoints
    {
        get
        {
            float ratio = Mathf.Max(0f, MedicalRatio);
            float speed = AnswerSpeed >= 0f ? AnswerSpeed : ratio;
            return Mathf.RoundToInt((0.7f * ratio + 0.3f * speed) * MaxMedical);
        }
    }

    public static int Total => Mathf.Clamp(AccuracyPoints + ReactionPoints + LifePoints + ComboPoints + MedicalPoints, 0, MaxPoints);

    public static List<Part> Breakdown()
    {
        float alive = AverageSecondsAlive;
        var parts = new List<Part>
        {
            new("PRECISÃO", Shots > 0 ? Mathf.RoundToInt(Accuracy * 100f) + "%" : "--", AccuracyPoints, MaxAccuracy),
            new("TEMPO DE REAÇÃO", alive >= 0f ? alive.ToString("0.0") + "s" : "--", ReactionPoints, MaxReaction),
            new("VIDA RESTANTE", Mathf.RoundToInt(LifeRatio * 100f) + "%", LifePoints, MaxLife),
            new("MAIOR COMBO", OfflineCombo.BestStreak > 0 ? OfflineCombo.BestStreak + " SEGUIDOS" : "--", ComboPoints, MaxCombo),
        };
        if (Full)
        {
            string value = MedicalRatio >= 0f ? MedicalCorrect + "/" + (MedicalCorrect + MedicalWrong) : "--";
            parts.Add(new Part("TRATAMENTOS", value, MedicalPoints, MaxMedical));
        }
        return parts;
    }

    public static string Grade => GradeFor(Total);

    public static string GradeFor(int points)
    {
        if (points >= GradeS) return "S";
        if (points >= GradeA) return "A";
        if (points >= GradeB) return "B";
        return "C";
    }
}

public static class OfflineLeaderboard
{
    public const int MaxEntries = 10;
    private const int ScoringVersion = 3;

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
        public int version;
        public List<Board> boards = new();
    }

    [Serializable]
    private class Board
    {
        public string key;
        public List<Entry> entries = new();
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
            _data.boards ??= new List<Board>();
            return _data;
        }
    }

    public static IReadOnlyList<Entry> Top(OfflineExperienceMode mode) => List(mode, OfflineDifficulty.Current);

    public static IReadOnlyList<Entry> Top(OfflineExperienceMode mode, OfflineDifficultyLevel difficulty) => List(mode, difficulty);

    private static string Today => DateTime.Now.ToString("yyyy-MM-dd");

    private static List<Entry> List(OfflineExperienceMode mode, OfflineDifficultyLevel difficulty)
    {
        StartNewDayIfNeeded();
        string key = (mode == OfflineExperienceMode.FullExperience ? "full_" : "combat_") + difficulty.ToString().ToLowerInvariant();
        Board board = Loaded.boards.Find(b => b.key == key);
        if (board == null)
        {
            board = new Board { key = key };
            Loaded.boards.Add(board);
        }
        board.entries ??= new List<Entry>();
        return board.entries;
    }

    private static void StartNewDayIfNeeded()
    {
        Data data = Loaded;
        string today = Today;
        string oldDay = null;
        foreach (Board board in data.boards)
            foreach (Entry entry in board.entries ?? new List<Entry>())
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

        foreach (Board board in data.boards)
            board.entries?.RemoveAll(entry => entry.date == null || !entry.date.StartsWith(today));
        Save();
    }

    public static int RankFor(OfflineExperienceMode mode, int score)
    {
        List<Entry> list = List(mode, OfflineScore.Difficulty);
        int rank = 0;
        while (rank < list.Count && list[rank].score >= score)
            rank++;
        return rank < MaxEntries ? rank : -1;
    }

    public static int Add(OfflineExperienceMode mode, string name, int score, string grade)
    {
        int rank = RankFor(mode, score);
        if (rank < 0)
            return -1;

        List<Entry> list = List(mode, OfflineScore.Difficulty);
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
