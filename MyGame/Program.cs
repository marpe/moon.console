using MoonWorks;

namespace MyGame;

internal class Program
{
    private static void Main(string[] args)
    {
        var windowCreateInfo = new WindowCreateInfo
        {
            WindowWidth = 1920,
            WindowHeight = 1080,
            WindowTitle = "ProjectName",
            ScreenMode = ScreenMode.Windowed,
            PresentMode = PresentMode.Immediate,
            SystemResizable = true,
        };
        
        var frameLimiterSettings = new FrameLimiterSettings
        {
            Mode = FrameLimiterMode.Uncapped,
            Cap = 120,
        };
        
        var gameMain = new MyGameInstance(
            windowCreateInfo,
            frameLimiterSettings,
            120,
            true
        );
        gameMain.Run();
    }
}
