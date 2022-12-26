using MoonWorks.Math.Float;

namespace MyGame;

public struct Bounds
{
    public Vector2 Min;
    public Vector2 Size;
    public Vector2 Max => Min + Size;

    public Vector2 Center => Min + Size * 0.5f;

    public float Top => Min.Y;
    public float Right => Max.X;
    public float Bottom => Max.Y;
    public float Left => Min.X;

    public float Height => Size.Y;
    public float Width => Size.X;

    public float X => Min.X;
    public float Y => Min.Y;

    public Bounds(float x, float y, float w, float h)
    {
        Min = new Vector2(x, y);
        Size = new Vector2(w, h);
    }

    public Bounds(Vector2 min, Vector2 max) : this(min.X, min.Y, max.X - min.X, max.Y - min.Y)
    {
    }

    public static implicit operator Bounds(Rectangle r)
    {
        return new Bounds(r.X, r.Y, r.Width, r.Height);
    }

    public static explicit operator Rectangle(Bounds b)
    {
        return new Rectangle((int)b.Min.X, (int)b.Min.Y, (int)b.Size.X, (int)b.Size.Y);
    }
    
    public static Bounds Lerp(Bounds a, Bounds b, double alpha)
    {
        var min = Vector2.Lerp(a.Min, b.Min, (float)alpha);
        var max = Vector2.Lerp(a.Max, b.Max, (float)alpha);
        return new Bounds(min, max);
    }
}
