using FreeTypeSharp;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;
using MyGame.Screens;
using MyGame.TWConsole;

namespace MyGame;

public class MyGameInstance : Game
{
    public ConsoleScreen ConsoleScreen;
    private readonly Renderer _renderer;

    private Texture _consoleRt;
    public Time Time;

    private readonly FPSDisplay _fpsDisplay;

    [CVar("con.use_freetype", "")]
    public static bool UseFreeType = true;
    
    public MyGameInstance(WindowCreateInfo windowCreateInfo, FrameLimiterSettings frameLimiterSettings, int targetTimestep = 60, bool debugMode = false) : base(
        windowCreateInfo, frameLimiterSettings, targetTimestep, debugMode)
    {
        Time = new Time();

        Shared.Game = this;
        Shared.FreeTypeLibrary = new FreeTypeLibrary();
        Shared.Console = new TWConsole.TWConsole();
        Shared.Console.Initialize();

        _renderer = new Renderer(this);
        ConsoleScreen = new ConsoleScreen(this);
        _consoleRt = Texture.CreateTexture2D(GraphicsDevice, 1920, 1080, TextureFormat.B8G8R8A8, TextureUsageFlags.Sampler | TextureUsageFlags.ColorTarget);

        _fpsDisplay = new FPSDisplay();
    }

    protected override void Update(TimeSpan delta)
    {
        Time.Update(delta);
        _fpsDisplay.BeginUpdate();

        if (Inputs.Keyboard.IsPressed(KeyCode.Grave))
        {
            ConsoleScreen.ToggleConsole();
        }

        var deltaSeconds = (float)delta.TotalSeconds;
        ConsoleScreen.Update(deltaSeconds);

        _fpsDisplay.EndUpdate();
    }

    protected override void Draw(double alpha)
    {
        Time.UpdateDrawCount();
        _fpsDisplay.BeginRender();

        var commandBuffer = GraphicsDevice.AcquireCommandBuffer();
        var swapTexture = commandBuffer.AcquireSwapchainTexture(MainWindow);

        if (swapTexture == null)
            return;

        // draw console to separate render target
        ConsoleScreen.Draw(_renderer, ref commandBuffer, _consoleRt, alpha);


        if (ConsoleScreen.HasBeenDrawn) // if the texture hasn't been populated vulcan gets sad :(
            _renderer.DrawSprite(_consoleRt, Matrix4x4.Identity, Color.White);
        _renderer.RunRenderPass(ref commandBuffer, swapTexture, Color.CornflowerBlue, null);

        _fpsDisplay.DrawFPS(_renderer, swapTexture);
        _renderer.RunRenderPass(ref commandBuffer, swapTexture, null, null);

        _fpsDisplay.EndRender();
        GraphicsDevice.Submit(commandBuffer);
    }
}
