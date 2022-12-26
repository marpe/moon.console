using System.Runtime.InteropServices;
using System.Text;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Math.Float;
using MyGame.Utils;
using WellspringCS;

namespace MyGame;

public class FontData
{
    public int Size;
    private static byte[] _stringBytes = new byte[128];

    public TextBatch Batch;
    public Font Font;
    public bool HasStarted;

    public Packer Packer;
    public Texture? Texture;

    public FontData(TextBatch batch, Packer packer, Font font, int size)
    {
        Size = size;
        Batch = batch;
        Packer = packer;
        Font = font;
    }

    public unsafe Vector2 MeasureString(ReadOnlySpan<char> text)
    {
        var byteCount = Encoding.UTF8.GetByteCount(text);

        if (_stringBytes.Length < byteCount)
        {
            Array.Resize(ref _stringBytes, byteCount);
        }

        var byteSpan = _stringBytes.AsSpan();
        Encoding.UTF8.GetBytes(text, byteSpan);

        fixed (byte* bytes = byteSpan)
        {
            Wellspring.Wellspring_TextBounds(
                Packer.Handle,
                0, 0,
                Wellspring.HorizontalAlignment.Left, Wellspring.VerticalAlignment.Top,
                (IntPtr)bytes,
                (uint)byteCount,
                out var rect
            );
            return new Vector2(rect.W, rect.H);
        }
    }

    public Vector2 MeasureString(char previousChar, char currentChar)
    {
        return MeasureString(stackalloc char[] { currentChar });
    }
}

// TODO (marpe): This has been heavily edited for brevity for the example app :)

public class TextButcher
{
    public readonly FontData Font;

    private uint _addCountSinceDraw;

    public static readonly FontRange BasicLatin = new()
    {
        FirstCodepoint = 0x0,
        NumChars = 0x7f + 1,
        OversampleH = 0,
        OversampleV = 0,
    };

    public TextButcher(GraphicsDevice device)
    {
        Font = LoadFont(device, @"Content/fonts/consola.ttf", 18);
    }

    public void Unload()
    {
        Font.Packer.Dispose();
        Font.Font.Dispose();
        Font.Texture?.Dispose();
    }

    public static FontData LoadFont(GraphicsDevice device, string path, float fontSize)
    {
        var commandBuffer = device.AcquireCommandBuffer();

        var font = new Font(path);

        var fontPacker = new Packer(device, font, fontSize, 512, 512, 2u);
        fontPacker.PackFontRanges(BasicLatin);
        fontPacker.SetTextureData(commandBuffer);
        var butcheredFont = new FontData(new TextBatch(device), fontPacker, font, (int)fontSize);

        device.Submit(commandBuffer);

        var pixels = TextureUtils.ConvertSingleChannelTextureToRGBA(device, fontPacker.Texture);
        TextureUtils.PremultiplyAlpha(pixels);
        var (width, height) = (fontPacker.Texture.Width, fontPacker.Texture.Height);
        var fontTexture = TextureUtils.CreateTexture(device, width, height, pixels);
        butcheredFont.Texture = fontTexture;

        // SaveTexture("test.png", device, fontTexture);
        
        return butcheredFont;
    }

    public void Add(ReadOnlySpan<char> text, float x, float y, float depth, Color color, HorizontalAlignment alignH, VerticalAlignment alignV)
    {
        if (text.Length == 0)
        {
            return;
        }

        _addCountSinceDraw++;

        if (!Font.HasStarted)
        {
            Font.Batch.Start(Font.Packer);
            Font.HasStarted = true;
        }

        // TODO (marpe): Ugh, allocating
        var str = text.ToString();
        Font.Batch.Draw(str, x, y, depth, color, alignH, alignV);
    }

    public void FlushToSpriteBatch(SpriteBatch spriteBatch)
    {
        if (_addCountSinceDraw == 0)
        {
            return;
        }

        if (!Font.HasStarted)
        {
            return;
        }

        Wellspring.Wellspring_GetBufferData(
            Font.Batch.Handle,
            out var vertexCount,
            out var vertexDataPointer,
            out var vertexDataLengthInBytes,
            out var indexDataPointer,
            out var indexDataLengthInBytes
        );

        unsafe
        {
            var vertices = (Vertex*)vertexDataPointer.ToPointer();
            var sizeOfVert = Marshal.SizeOf<Vertex>();
            var numVerts = vertexDataLengthInBytes / sizeOfVert;

            var sprite = new Sprite();
            sprite.Texture = Font.Texture ?? throw new InvalidOperationException();
            var fontTextureSize = new Vector2(Font.Texture.Width, Font.Texture.Height);

            for (var i = 0; i < numVerts; i += 4)
            {
                var topLeftVert = vertices[i];
                var bottomRightVert = vertices[i + 3];
                var transform = Matrix3x2.CreateTranslation(new Vector2((int)topLeftVert.Position.X, (int)topLeftVert.Position.Y));
                var srcPos = topLeftVert.TexCoord * fontTextureSize;
                var srcDim = (bottomRightVert.TexCoord - topLeftVert.TexCoord) * fontTextureSize;
                var srcRect = new Rectangle((int)srcPos.X, (int)srcPos.Y, (int)srcDim.X, (int)srcDim.Y);

                sprite.SrcRect = srcRect;
                Sprite.GenerateUVs(ref sprite.UV, sprite.Texture, srcRect);
                var color = topLeftVert.Color;
                spriteBatch.Draw(sprite, color, topLeftVert.Position.Z, transform.ToMatrix4x4(), Renderer.PointClamp);
            }
        }

        Font.HasStarted = false;

        _addCountSinceDraw = 0;
    }
}
