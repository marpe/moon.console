using System.Diagnostics;
using System.Runtime.InteropServices;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using MyGame.Utils;

namespace MyGame.Fonts;

public struct FontAtlasNode
{
    public int X;
    public int Y;
    public int Width;
}

public class FontGlyph
{
    public int Index;
    public int Codepoint;
    public int FontSize;
    public Rectangle Bounds;
    public int XAdvance;
    public int XOffset;
    public int YOffset;
}

public class FreeTypeFontAtlas
{
    public Dictionary<int, FontGlyph> Glyphs = new();
    public Texture? Texture;
    private GraphicsDevice _graphicsDevice;
    public FontAtlasNode[] Nodes;
    public int NodesNumber;
    public int Height;
    public int Width;
    private FreeTypeFont? _font;
    private int _lineHeight;
    private int _ascent;
    private Dictionary<(int, int), int> _kerning = new();

    public FreeTypeFontAtlas(GraphicsDevice graphicsDevice, int width, int height)
    {
        _graphicsDevice = graphicsDevice;

        Width = width;
        Height = height;

        Nodes = new FontAtlasNode[256];
        Nodes[0].X = 0;
        Nodes[0].Y = 0;
        Nodes[0].Width = Width;
        NodesNumber++;
    }

    public void InsertNode(int idx, int x, int y, int w)
    {
        if (NodesNumber + 1 > Nodes.Length)
        {
            var oldNodes = Nodes;
            var newLength = Nodes.Length == 0 ? 8 : Nodes.Length * 2;
            Nodes = new FontAtlasNode[newLength];
            for (var i = 0; i < oldNodes.Length; ++i)
            {
                Nodes[i] = oldNodes[i];
            }
        }

        for (var i = NodesNumber; i > idx; i--)
        {
            Nodes[i] = Nodes[i - 1];
        }

        Nodes[idx].X = x;
        Nodes[idx].Y = y;
        Nodes[idx].Width = w;
        NodesNumber++;
    }

    public void RemoveNode(int idx)
    {
        if (NodesNumber == 0)
        {
            return;
        }

        for (var i = idx; i < NodesNumber - 1; i++)
        {
            Nodes[i] = Nodes[i + 1];
        }

        NodesNumber--;
    }

    public void Reset(int w, int h)
    {
        Width = w;
        Height = h;
        NodesNumber = 0;
        Nodes[0].X = 0;
        Nodes[0].Y = 0;
        Nodes[0].Width = w;
        NodesNumber++;
    }

    public bool AddSkylineLevel(int idx, int x, int y, int w, int h)
    {
        InsertNode(idx, x, y + h, w);
        for (var i = idx + 1; i < NodesNumber; i++)
        {
            if (Nodes[i].X < Nodes[i - 1].X + Nodes[i - 1].Width)
            {
                var shrink = Nodes[i - 1].X + Nodes[i - 1].Width - Nodes[i].X;
                Nodes[i].X += shrink;
                Nodes[i].Width -= shrink;
                if (Nodes[i].Width <= 0)
                {
                    RemoveNode(i);
                    i--;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        for (var i = 0; i < NodesNumber - 1; i++)
        {
            if (Nodes[i].Y == Nodes[i + 1].Y)
            {
                Nodes[i].Width += Nodes[i + 1].Width;
                RemoveNode(i + 1);
                i--;
            }
        }

        return true;
    }

    public int RectFits(int i, int w, int h)
    {
        var x = Nodes[i].X;
        var y = Nodes[i].Y;
        if (x + w > Width)
        {
            return -1;
        }

        var spaceLeft = w;
        while (spaceLeft > 0)
        {
            if (i == NodesNumber)
            {
                return -1;
            }

            y = Math.Max(y, Nodes[i].Y);
            if (y + h > Height)
            {
                return -1;
            }

            spaceLeft -= Nodes[i].Width;
            ++i;
        }

        return y;
    }

    public bool AddRect(int rw, int rh, ref int rx, ref int ry)
    {
        var besth = Height;
        var bestw = Width;
        var besti = -1;
        var bestx = -1;
        var besty = -1;
        for (var i = 0; i < NodesNumber; i++)
        {
            var y = RectFits(i, rw, rh);
            if (y != -1)
            {
                if (y + rh < besth || (y + rh == besth && Nodes[i].Width < bestw))
                {
                    besti = i;
                    bestw = Nodes[i].Width;
                    besth = y + rh;
                    bestx = Nodes[i].X;
                    besty = y;
                }
            }
        }

        if (besti == -1)
        {
            return false;
        }

        if (!AddSkylineLevel(besti, bestx, besty, rw, rh))
        {
            return false;
        }

        rx = bestx;
        ry = besty;
        return true;
    }

    public void AddFont(string fileName, uint fontSize = 24u, bool premultiplyAlpha = true)
    {
        var sw = Stopwatch.StartNew();
        var fontBytes = File.ReadAllBytes(fileName);
        _font = new FreeTypeFont(fontBytes);

        // (charIndex, codepoint/charcode)
        var glyphList = GetGlyphList(_font);

        var atlasBuffer = new byte[Width * Height * 4];
        
        _font.GetMetricsForSize(fontSize, out var ascent, out var descent, out var lineHeight);

        _ascent = ascent; 
        _lineHeight = lineHeight;
        
        foreach (var (charIndex, charcode) in glyphList)
        {
            if (!ProcessGlyph(fontSize, premultiplyAlpha, _font, charIndex, out var info, out var colorBuffer))
                continue;

            var dstX = 0;
            var dstY = 0;
            if (!AddRect(info.Width, info.Height, ref dstX, ref dstY))
            {
                Logger.LogWarn("Font atlas full");
                break;
            }

            // setup destination rect
            var dstRect = new Rect(dstX, dstY, info.Width, info.Height);

            // render glyph to texture
            SetData(atlasBuffer, Width, dstRect, colorBuffer);

            var glyph = new FontGlyph
            {
                Index = (int)charIndex,
                Bounds = new Rectangle(dstX, dstY, info.Width, info.Height),
                Codepoint = (int)charcode,
                XAdvance = (int)info.AdvanceX,
                XOffset = info.OffsetX,
                YOffset = info.OffsetY,
                FontSize = (int)fontSize,
            };
            Glyphs.Add((int)charcode, glyph);
        }

        Texture = Texture.CreateTexture2D(_graphicsDevice, (uint)Width, (uint)Height, TextureFormat.R8G8B8A8, TextureUsageFlags.Sampler);
        SetTextureData(_graphicsDevice, new TextureSlice(Texture), atlasBuffer);

        sw.StopAndLog("FontAtlas.AddFont");
    }

    public void DrawText(Renderer renderer, ReadOnlySpan<char> str, Vector2 position, Color color)
    {
        var offset = position;
        offset.Y += _ascent;
        FontGlyph? prevGlyph = null;
        foreach (var rune in str.EnumerateRunes())
        {
            var codepoint = rune.Value;
            
            if (!Glyphs.TryGetValue(codepoint, out var glyph))
            {
                offset.X += Glyphs.First().Value.XAdvance;
                continue;
            }

            if (prevGlyph != null)
            {
                if (!_kerning.TryGetValue((prevGlyph.Index, glyph.Index), out var kerning))
                {
                    kerning = _font!.GetGlyphKernAdvance(prevGlyph.Index, glyph.Index);
                    _kerning.Add((prevGlyph.Index, glyph.Index), kerning);
                }
                offset.X += kerning;
            }

            var transform = Matrix3x2.CreateTranslation(glyph.XOffset, glyph.YOffset) *
                            Matrix3x2.CreateScale(1, 1) *
                            Matrix3x2.CreateTranslation(offset);
            
            renderer.DrawSprite(new Sprite(Texture!, glyph.Bounds), transform.ToMatrix4x4(), color);
            offset.X += glyph.XAdvance;
            prevGlyph = glyph;
        }
    }

    private static void SetData(byte[] dstBuffer, int dstWidth, Rect dstRect, byte[] src)
    {
        var offset = dstRect.Y * dstWidth + dstRect.X;
        for (var y = 0; y < dstRect.H; y++)
        {
            for (var x = 0; x < dstRect.W; x++)
            {
                var si = (y * dstRect.W + x) * 4;
                var di = (offset + y * dstWidth + x) * 4;
                dstBuffer[di] = src[si];
                dstBuffer[di + 1] = src[si + 1];
                dstBuffer[di + 2] = src[si + 2];
                dstBuffer[di + 3] = src[si + 3];
            }
        }
    }

    private static HashSet<(uint, uint)> GetGlyphList(FreeTypeFont font)
    {
        var codePointMin = 0x0u;
        var codePointMax = 0x7fu;

        var glyphList = new HashSet<(uint, uint)>();
        for (var i = codePointMin; i < codePointMax; i++)
        {
            var charIndex = font.GetCharIndex(i);
            if (charIndex == 0)
            {
                continue;
            }

            glyphList.Add((charIndex, i));
        }

        return glyphList;
    }

    private static bool ProcessGlyph(uint fontSize, bool premultiplyAlpha, FreeTypeFont font, uint glyphIndex, out FreeTypeFont.GlyphInfo info,
        out byte[] colorBuffer)
    {
        // font.GetGlyphMetrics(glyphIndex, fontSize, out var metrics);

        var bitmap = font.RenderGlyphAndGetInfo(glyphIndex, fontSize, out info);

        if (bitmap.buffer == nint.Zero)
        {
            colorBuffer = Array.Empty<byte>();
            return false;
        }

        // copy native arr to managed
        var bufferSize = info.Width * info.Height;
        var buffer = new byte[bufferSize];
        Marshal.Copy(bitmap.buffer, buffer, 0, buffer.Length);

        // create a 4 channel buffer
        colorBuffer = new byte[bufferSize * 4];
        for (var i = 0; i < buffer.Length; i++)
        {
            var ci = i * 4;
            var c = buffer[i];
            if (premultiplyAlpha)
            {
                colorBuffer[ci] = colorBuffer[ci + 1] = colorBuffer[ci + 2] = colorBuffer[ci + 3] = c;
            }
            else
            {
                colorBuffer[ci] = colorBuffer[ci + 1] = colorBuffer[ci + 2] = 255;
                colorBuffer[ci + 3] = c;
            }
        }

        return true;
    }

    private static void SetTextureData(GraphicsDevice device, TextureSlice slice, byte[] data)
    {
        var commandBuffer = device.AcquireCommandBuffer();
        commandBuffer.SetTextureData(slice, data);
        device.Submit(commandBuffer);
        device.Wait();
    }
}
