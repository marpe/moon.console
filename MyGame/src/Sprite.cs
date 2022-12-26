using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MyGame;

public struct Sprite
{
    public Texture Texture;
    public Bounds SrcRect;
    public UV UV;

    public Sprite(Texture texture) : this(texture, new Rectangle(0, 0, (int)texture.Width, (int)texture.Height))
    {
    }

    public Sprite(TextureSlice textureSlice) : this(textureSlice.Texture,
        new Rectangle(textureSlice.Rectangle.X, textureSlice.Rectangle.Y, textureSlice.Rectangle.W, textureSlice.Rectangle.Y))
    {
    }

    public Sprite(Texture texture, Rectangle srcRect) : this(texture, (Bounds)srcRect)
    {
    }

    public Sprite(Texture texture, Bounds srcRect)
    {
        Texture = texture;
        SrcRect = srcRect;
        UV = new UV();
        GenerateUVs(ref UV, texture, srcRect);
    }

    public static void GenerateUVs(ref UV uv, in Texture texture, in Bounds srcRect)
    {
        var posX = srcRect.X / texture.Width;
        var posY = srcRect.Y / texture.Height;

        var dimX = srcRect.Width / texture.Width;
        var dimY = srcRect.Height / texture.Height;

        uv.Position.X = posX;
        uv.Position.Y = posY;

        uv.Dimensions.X = dimX;
        uv.Dimensions.Y = dimY;

        uv.TopLeft.X = uv.TopRight.X = uv.BottomLeft.X = uv.BottomRight.X = uv.Position.X;
        uv.TopLeft.Y = uv.TopRight.Y = uv.BottomLeft.Y = uv.BottomRight.Y = uv.Position.Y;

        uv.TopRight.X += uv.Dimensions.X;
        uv.BottomLeft.Y += uv.Dimensions.Y;
        uv.BottomRight.X += uv.Dimensions.X;
        uv.BottomRight.Y += uv.Dimensions.Y;
    }

    public static implicit operator Sprite(Texture texture) => new(texture);
}
