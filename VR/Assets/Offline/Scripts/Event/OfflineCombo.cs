using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kill streak of the offline combat. Kills close together raise it; a missed shot, taking damage or a long
/// pause breaks it. The multiplier (x1..x4) is shown on the helmet and the best streak counts for the score.
/// </summary>
public static class OfflineCombo
{
    /// <summary>Seconds after a kill to make the next one and keep the streak.</summary>
    public const float Window = 6f;
    /// <summary>A shot with no enemy hit after this long missed.</summary>
    private const float MissAfter = 1.2f;
    public const int MaxMultiplier = 4;

    public static int Streak { get; private set; }
    public static int BestStreak { get; private set; }
    public static float LastKillTime { get; private set; } = -100f;

    /// <summary>x1 for 1-2 kills, x2 from 3, x3 from 6, x4 from 9.</summary>
    public static int Multiplier => Mathf.Clamp(1 + Streak / 3, 1, MaxMultiplier);

    /// <summary>0..1 time left to keep the streak.</summary>
    public static float WindowLeft => Streak > 0 ? Mathf.Clamp01(1f - (Time.unscaledTime - LastKillTime) / Window) : 0f;

    /// <summary>Raised when the streak is lost (reason shown on the helmet).</summary>
    public static event System.Action<string> Broken;
    public static event System.Action<int> MultiplierUp;

    private static readonly Queue<float> PendingShots = new();

    public static void Reset()
    {
        Streak = 0;
        BestStreak = 0;
        LastKillTime = -100f;
        PendingShots.Clear();
    }

    public static void Shot()
    {
        if (OfflineScore.Counting)
            PendingShots.Enqueue(Time.unscaledTime);
    }

    /// <summary>A bullet hit an enemy: the oldest shot in flight did not miss.</summary>
    public static void Hit()
    {
        if (PendingShots.Count > 0)
            PendingShots.Dequeue();
    }

    public static void Kill()
    {
        if (!OfflineScore.Counting)
            return;
        int before = Multiplier;
        Streak = Time.unscaledTime - LastKillTime <= Window ? Streak + 1 : 1;
        LastKillTime = Time.unscaledTime;
        BestStreak = Mathf.Max(BestStreak, Streak);
        if (Multiplier > before)
            MultiplierUp?.Invoke(Multiplier);
    }

    public static void Damaged() => Break("DANO RECEBIDO");

    /// <summary>Called every frame by the combat feedback: misses and timeouts.</summary>
    public static void Tick()
    {
        float now = Time.unscaledTime;
        bool missed = false;
        while (PendingShots.Count > 0 && now - PendingShots.Peek() > MissAfter)
        {
            PendingShots.Dequeue();
            missed = true;
        }
        if (missed)
            Break("TIRO PERDIDO");
        else if (Streak > 0 && now - LastKillTime > Window)
            Break(null);
    }

    private static void Break(string reason)
    {
        if (Streak == 0)
            return;
        bool hadMultiplier = Multiplier > 1;
        Streak = 0;
        if (hadMultiplier)
            Broken?.Invoke(reason);
    }
}
