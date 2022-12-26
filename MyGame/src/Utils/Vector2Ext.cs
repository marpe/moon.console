using System.Runtime.CompilerServices;
using MoonWorks.Math.Float;

namespace MyGame.Utils;

public static class Vector2Ext
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Floor(this Vector2 self)
    {
        return new Vector2(MathF.Floor(self.X), MathF.Floor(self.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float AngleBetweenVectors(Vector2 from, Vector2 to)
    {
        return (float)Math.Atan2(to.Y - from.Y, to.X - from.X);
    }
}
