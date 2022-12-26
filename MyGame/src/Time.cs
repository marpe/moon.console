using System.Diagnostics;

namespace MyGame;

public class Time
{
    private readonly Stopwatch _stopwatch;
    public double DrawFps { get; private set; }
    public double UpdateFps { get; private set; }
    public ulong UpdateCount { get; private set; }
    public ulong DrawCount { get; private set; }
    public float TotalElapsedTime { get; private set; }
    public float ElapsedTime { get; private set; }
    
    private double _nextFPSUpdate = 1.0;
    private ulong _prevDrawCount;
    private ulong _prevUpdateCount;
    
    public Time()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    public void Update(TimeSpan dt)
    {
        UpdateCount++;
        ElapsedTime = (float)dt.TotalSeconds;
        TotalElapsedTime += ElapsedTime;
        
        if (_stopwatch.Elapsed.TotalSeconds < _nextFPSUpdate)
            return;

        UpdateFps = UpdateCount - _prevUpdateCount;
        DrawFps = DrawCount - _prevDrawCount;
        _prevUpdateCount = UpdateCount;
        _prevDrawCount = DrawCount;
        _nextFPSUpdate = _stopwatch.Elapsed.TotalSeconds + 1.0;
    }

    public void UpdateDrawCount()
    {
        DrawCount++;
    }
}
