using System.Diagnostics;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using MyGame.Utils;

namespace MyGame;

public enum FPSDisplayPosition
{
    TopLeft,
    TopRight,
    BottomRight,
    BottomLeft
}

public class FPSDisplay
{
    private Stopwatch _renderStopwatch = new();
    private Stopwatch _updateStopwatch = new();

    private float _renderDurationMs;
    private float _updateDurationMs;

    private float _peakUpdateDurationMs;
    private float _peakRenderDurationMs;

    public void BeginRender()
    {
        _renderStopwatch.Restart();
    }

    public void EndRender()
    {
        _renderStopwatch.Stop();
        _renderDurationMs = _renderStopwatch.GetElapsedMilliseconds();
    }

    public void BeginUpdate()
    {
        _updateStopwatch.Restart();
    }

    public void EndUpdate()
    {
        _updateStopwatch.Stop();
        _updateDurationMs = _updateStopwatch.GetElapsedMilliseconds();
    }

    public void DrawFPS(Renderer renderer, Texture renderDestination, FPSDisplayPosition corner = FPSDisplayPosition.BottomRight)
    {
        var origin = corner switch
        {
            FPSDisplayPosition.TopLeft => new Vector2(0, 0),
            FPSDisplayPosition.TopRight => new Vector2(1, 0),
            FPSDisplayPosition.BottomRight => new Vector2(1, 1),
            _ => new Vector2(0, 1),
        };

        var position = new Vector2(renderDestination.Width, renderDestination.Height) * origin;
        _peakUpdateDurationMs = StopwatchExt.SmoothValue(_peakUpdateDurationMs, _updateDurationMs);
        _peakRenderDurationMs = StopwatchExt.SmoothValue(_peakRenderDurationMs, _renderDurationMs);

        var updateFps = Shared.Game.Time.UpdateFps;
        var drawFps = Shared.Game.Time.DrawFps;

        var str = $"Update: {updateFps:0.##} ({_peakUpdateDurationMs:00.00}), Draw: {drawFps:0.##} ({_peakRenderDurationMs:00.00})";
        // var strSize = renderer.TextButcher.GetFont(FontType.ConsolasMonoMedium).MeasureString(str);
        var strSize = renderer.TextButcher.Font.MeasureString(str);
        var min = position - strSize * origin;
        var max = min + strSize;
        var bg = new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));

        renderer.DrawRect(bg, Color.Black * 0.66f);

        if (MyGameInstance.UseFreeType)
            renderer.DrawFTText(str, min, Color.Yellow);
        else
            renderer.DrawText(str, min, 0, Color.Yellow);
    }
}
