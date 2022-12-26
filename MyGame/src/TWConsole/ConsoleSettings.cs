using MoonWorks.Graphics;

namespace MyGame.TWConsole;

public static class ConsoleSettings
{
    public static Color InputTextColor = Color.White;
    public static Color InputBackgroundColor = Color.Black;
    public static Color BackgroundColor = Color.Black;
    public static Color CaretColor = Color.LimeGreen;
    public static Color InputLineCharColor = Color.Yellow;
    public static Color ActiveCommandHistoryColor = Color.Magenta;
    public static Color AutocompleteSuggestionColor = Color.LightGreen;
    public static Color ScrollIndicatorColor = Color.Yellow;

    public static Color Color0 = Color.White;
    public static Color Color1 = Color.Yellow;
    public static Color Color2 = Color.Cyan;
    public static Color Color3 = Color.DarkCyan;
    public static Color Color4 = Color.OrangeRed;
    public static Color Color5 = Color.Brown;
    public static Color Color6 = Color.Magenta;
    public static Color Color7 = Color.LightGreen;
    public static Color Color8 = new(90, 90, 90);
    public static Color Color9 = Color.Orange;

    public static Color[] Colors = {
        Color0,
        Color1,
        Color2,
        Color3,
        Color4,
        Color5,
        Color6,
        Color7,
        Color8,
        Color9,
    };
    
    public static float CaretBlinkSpeed = 5f;
    public static int ScrollSpeed = 3;
    public static int HorizontalPadding = 30;

    public static string CaretChar = "_";
    public static string InputLineChar = "> ";
    public static string ScrollIndicatorChar = "^";

    [CVar("con.alpha", "Sets console alpha [0-1]")]
    public static float BackgroundAlpha = .9f;

    [CVar("con.height", "Sets console height [0-1]")]
    public static float RelativeConsoleHeight = 1f;

    [CVar("con.speed", "Config transition duration in seconds")]
    public static float TransitionDuration = 0.1f;

    [CVar("con.debug", "Show console debug info")]
    public static bool ShowDebug;

    [CVar("con.bmfont", "Use BMFont to render console")]
    public static bool UseBMFont;

    [CVar("con.render_rate", "Controls the console render rate")]
    public static int RenderRate = 2;
}
