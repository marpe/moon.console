using MoonWorks.Graphics;

namespace MyGame.Utils;

public static class ColorExt
{
    public static Color MultiplyAlpha(this Color color, float alpha)
    {
        return new Color(
            color.R,
            color.G,
            color.B,
            (int)(color.A * alpha)
        );
    }
}
