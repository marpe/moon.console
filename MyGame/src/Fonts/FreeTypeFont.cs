using System.Runtime.InteropServices;
using FreeTypeSharp;
using FreeTypeSharp.Native;

namespace MyGame.Fonts;

public class FreeTypeFont : IDisposable
{
    private GCHandle _ttfDataHandle;
    private IntPtr _faceHandle;
    private readonly FT_FaceRec _faceRec;

    public FreeTypeFont(byte[] ttfData)
    {
        _ttfDataHandle = GCHandle.Alloc(ttfData, GCHandleType.Pinned);
        var err = FT.FT_New_Memory_Face(Shared.FreeTypeLibrary.Native, _ttfDataHandle.AddrOfPinnedObject(), ttfData.Length, 0, out var face);
        if (err != FT_Error.FT_Err_Ok)
            throw new FreeTypeException(err);

        _faceHandle = face;
        _faceRec = PInvokeHelper.PtrToStructure<FT_FaceRec>(_faceHandle);
    }

    ~FreeTypeFont() => Dispose(false);

    protected virtual void Dispose(bool isDisposing)
    {
        if (_faceHandle != IntPtr.Zero)
        {
            var err = FT.FT_Done_Face(_faceHandle);
            if (err != FT_Error.FT_Err_Ok)
                throw new FreeTypeException(err);
            _faceHandle = IntPtr.Zero;
        }

        if (_ttfDataHandle.IsAllocated)
            _ttfDataHandle.Free();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public uint GetCharIndex(uint charcode)
    {
        return FT.FT_Get_Char_Index(_faceHandle, charcode);
    }

    public int GetGlyphKernAdvance(int previousGlyphIndex, int glyphIndex)
    {
        var err = FT.FT_Get_Kerning(_faceHandle, (uint)previousGlyphIndex, (uint)glyphIndex, 0U, out var ftVector);
        if (err != FT_Error.FT_Err_Ok)
            throw new FreeTypeException(err);

        return (int)ftVector.x >> 6;
    }

    private void SetPixelSizes(uint width, uint height)
    {
        var err = FT.FT_Set_Pixel_Sizes(_faceHandle, width, height);
        if (err != FT_Error.FT_Err_Ok)
            throw new FreeTypeException(err);
    }

    private void LoadGlyph(uint glyphIndex)
    {
        var err = FT.FT_Load_Glyph(_faceHandle, glyphIndex, 0);
        if (err != FT_Error.FT_Err_Ok)
            throw new FreeTypeException(err);
    }

    private unsafe void GetCurrentGlyph(out FT_GlyphSlotRec glyph)
    {
        glyph = PInvokeHelper.PtrToStructure<FT_GlyphSlotRec>((IntPtr)_faceRec.glyph);
    }

    public void GetGlyphMetrics(uint glyphIndex, uint fontSize, out FT_Glyph_Metrics metrics)
    {
        SetPixelSizes(0u, fontSize);
        LoadGlyph(glyphIndex);
        GetCurrentGlyph(out var glyph);
        metrics = glyph.metrics;
    }

    public unsafe void GetMetricsForSize(
        uint fontSize,
        out int ascent,
        out int descent,
        out int lineHeight)
    {
        SetPixelSizes(0u, fontSize);
        var structure = PInvokeHelper.PtrToStructure<FT_SizeRec>((IntPtr)_faceRec.size);
        ascent = (int)structure.metrics.ascender >> 6;
        descent = (int)structure.metrics.descender >> 6;
        lineHeight = (int)structure.metrics.height >> 6;
    }

    public struct GlyphInfo
    {
        public int Width; // Glyph's width in pixels.
        public int Height; // Glyph's height in pixels.
        public int OffsetX; // The distance from the origin ("pen position") to the left of the glyph.
        public int OffsetY; // The distance from the origin to the top of the glyph. This is usually a value < 0.
        public float AdvanceX; // The distance from the origin to the origin of the next glyph. This is usually a value > 0.
        public bool IsColored; // The glyph is colored
    };

    public unsafe FT_Bitmap RenderGlyphAndGetInfo(uint glyphId, uint fontSize, out GlyphInfo glyphInfo)
    {
        SetPixelSizes(0u, fontSize);

        LoadGlyph(glyphId);

        var err = FT.FT_Render_Glyph((IntPtr)_faceRec.glyph, FT_Render_Mode.FT_RENDER_MODE_NORMAL);
        if (err != FT_Error.FT_Err_Ok)
            throw new FreeTypeException(err);

        GetCurrentGlyph(out var glyph);

        var bitmap = glyph.bitmap;

        const int FT_PIXEL_MODE_BGRA = 8;

        glyphInfo = new GlyphInfo();
        glyphInfo.Width = (int)bitmap.width;
        glyphInfo.Height = (int)bitmap.rows;
        glyphInfo.OffsetX = glyph.bitmap_left;
        glyphInfo.OffsetY = -glyph.bitmap_top;
        glyphInfo.AdvanceX = (int)glyph.advance.x >> 6;
        glyphInfo.IsColored = bitmap.pixel_mode == FT_PIXEL_MODE_BGRA;

        return bitmap;
    }
}
