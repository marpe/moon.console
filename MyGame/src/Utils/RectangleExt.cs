using MoonWorks.Math.Float;

namespace MyGame.Utils;

public static class RectangleExt
{
    public static Point Min(this Rectangle rect)
    {
        return new Point(rect.X, rect.Y);
    }
    public static Point BottomLeft(this Rectangle rect)
    {
        return new Point(rect.X, rect.Y + rect.Height);
    }
    public static Point TopRight(this Rectangle rect)
    {
        return new Point(rect.X + rect.Width, rect.Y);
    }
    public static Point Max(this Rectangle rect)
    {
        return new Point(rect.X + rect.Width, rect.Y + rect.Height);
    }
}
