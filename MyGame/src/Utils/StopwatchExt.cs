using System.Diagnostics;
using MoonWorks;
using MoonWorks.Math;

namespace MyGame.Utils;

public static class StopwatchExt
{
    public static float GetElapsedMilliseconds(this Stopwatch stopwatch)
    {
        return (float)((double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000.0);
    }

    public static void StopAndLog(this Stopwatch stopwatch, string message)
    {
        stopwatch.Stop();
        Logger.LogInfo($"{message,-40} {stopwatch.GetElapsedMilliseconds(),8:0} ms");
    }
    
    public static float SmoothValue(float prev, float current)
    {
        return prev > current ? MathHelper.Lerp(prev, current, 0.05f) : current;
    }
}
