using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using MyGame.TWConsole;
using MyGame.Utils;
using Buffer = MoonWorks.Graphics.Buffer;

namespace MyGame;

[Flags]
public enum SpriteFlip
{
    None = 0,
    FlipVertically = 1,
    FlipHorizontally = 2,
}

public class SpriteBatch
{
    [CVar("batch_round", "Toggle rounding of destinations when rendering with SpriteBatch")]
    public static bool ShouldRoundPositions;

    private static Color[] _tempColors = new Color[4];

    // Used to calculate texture coordinates
    public static readonly float[] CornerOffsetX = { 0.0f, 0.0f, 1.0f, 1.0f };
    public static readonly float[] CornerOffsetY = { 0.0f, 1.0f, 0.0f, 1.0f };
    private readonly GraphicsDevice _device;
    private Buffer _indexBuffer;

    private uint[] _indices;
    private uint _numSprites = 0;

    public uint LastNumAddedSprites { get; private set; }

    private TextureSamplerBinding[] _spriteInfo;
    private Buffer _vertexBuffer;

    private Position3DTextureColorVertex[] _vertices;

    public uint DrawCalls { get; private set; }

    public uint MaxDrawCalls { get; private set; }
    public uint MaxNumAddedSprites { get; private set; }

    private TextureSamplerBinding[] _fragmentSamplerBindings;

    public SpriteBatch(GraphicsDevice device)
    {
        _device = device;
        var maxSprites = 8192u;
        _spriteInfo = new TextureSamplerBinding[maxSprites];
        _vertices = new Position3DTextureColorVertex[_spriteInfo.Length * 4];
        _vertexBuffer = Buffer.Create<Position3DTextureColorVertex>(device, BufferUsageFlags.Vertex, (uint)_vertices.Length);
        _indices = GenerateIndexArray((uint)(_spriteInfo.Length * 6));
        _indexBuffer = Buffer.Create<uint>(device, BufferUsageFlags.Index, (uint)_indices.Length);
        _fragmentSamplerBindings = new TextureSamplerBinding[1];
    }

    public void Unload()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }

    public void Draw(Sprite sprite, Color color, float depth, Matrix4x4 transform, Sampler sampler, SpriteFlip flip = SpriteFlip.None)
    {
        _tempColors.AsSpan().Fill(color);
        Draw(sprite, _tempColors, depth, transform, sampler, flip);
    }

    public void Draw(Sprite sprite, Color[] colors, float depth, Matrix4x4 transform, Sampler sampler, SpriteFlip flip = SpriteFlip.None)
    {
        if (sprite.Texture.IsDisposed)
            throw new ObjectDisposedException(nameof(sprite.Texture));

        if (_numSprites == _spriteInfo.Length)
        {
            var maxNumSprites = (int)(_numSprites + 2048);
            Logger.LogInfo($"Max number of sprites reached, resizing buffers ({_numSprites} -> {maxNumSprites})");
            Array.Resize(ref _spriteInfo, maxNumSprites);
            Array.Resize(ref _vertices, _vertices.Length + _spriteInfo.Length * 4);

            _indices = GenerateIndexArray((uint)(_spriteInfo.Length * 6));

            _vertexBuffer.Dispose();
            _vertexBuffer = Buffer.Create<Position3DTextureColorVertex>(_device, BufferUsageFlags.Vertex, (uint)_vertices.Length);

            _indexBuffer.Dispose();
            _indexBuffer = Buffer.Create<uint>(_device, BufferUsageFlags.Index, (uint)_indices.Length);
        }

        _spriteInfo[_numSprites].Sampler = sampler;
        _spriteInfo[_numSprites].Texture = sprite.Texture;

        PushSpriteVertices(_vertices, _numSprites * 4, sprite, transform, depth, colors, flip);

        _numSprites += 1;
    }

    /// Subtract b from a, modifying a
    private static void SubtractVector(ref Vector2 a, in Vector2 b)
    {
        a.X -= b.X;
        a.Y -= b.Y;
    }

    private static void SetVector(ref Vector3 dest, in Vector2 src, float z)
    {
        dest.X = src.X;
        dest.Y = src.Y;
        dest.Z = z;
    }

    public void UpdateBuffers(ref CommandBuffer commandBuffer)
    {
        if (_numSprites == 0)
        {
            // Logger.LogWarn("Buffers are empty");
            return;
        }

        commandBuffer.SetBufferData(_indexBuffer, _indices, 0, 0, _numSprites * 6);
        commandBuffer.SetBufferData(_vertexBuffer, _vertices, 0, 0, _numSprites * 4);
    }

    /// Iterates the submitted sprites, binds uniforms, samplers and calls DrawIndexedPrimitives 
    public void DrawIndexed<TVertUniform, TFragmentUniform>(ref CommandBuffer commandBuffer, TVertUniform vertUniforms, TFragmentUniform fragmentUniforms,
        TextureSamplerBinding[] fragmentSamplerBindings) where TVertUniform : unmanaged where TFragmentUniform : unmanaged
    {
        var vertexParamOffset = commandBuffer.PushVertexShaderUniforms(vertUniforms);
        var fragmentParamOffset = commandBuffer.PushFragmentShaderUniforms(fragmentUniforms);
        DrawIndexed(ref commandBuffer, vertexParamOffset, fragmentParamOffset, fragmentSamplerBindings);
    }

    public void DrawIndexed(ref CommandBuffer commandBuffer, Matrix4x4 viewProjection)
    {
        var vertexParamOffset = commandBuffer.PushVertexShaderUniforms(viewProjection);
        DrawIndexed(ref commandBuffer, vertexParamOffset, 0u, _fragmentSamplerBindings);
    }

    public void Discard()
    {
        _numSprites = 0;
    }

    private void DrawIndexed(ref CommandBuffer commandBuffer, uint vertexUniformOffset, uint fragmentUniformOffset, TextureSamplerBinding[] fragmentSamplerBindings)
    {
        MaxDrawCalls = Math.Max(MaxDrawCalls, DrawCalls);
        DrawCalls = 0;

        if (_numSprites == 0)
        {
            // Logger.LogWarn("Flushing empty SpriteBatch");
            return;
        }

        commandBuffer.BindVertexBuffers(_vertexBuffer);
        commandBuffer.BindIndexBuffer(_indexBuffer, IndexElementSize.ThirtyTwo);

        fragmentSamplerBindings[0] = _spriteInfo[0];
        var offset = 0u;

        for (var i = 1u; i < _numSprites; i += 1)
        {
            var spriteInfo = _spriteInfo[i];

            if (!BindingsAreEqual(fragmentSamplerBindings[0], spriteInfo))
            {
                commandBuffer.BindFragmentSamplers(fragmentSamplerBindings);
                DrawIndexedQuads(ref commandBuffer, offset, i - offset, vertexUniformOffset, fragmentUniformOffset);
                DrawCalls++;
                fragmentSamplerBindings[0] = spriteInfo;
                offset = i;
            }
        }

        commandBuffer.BindFragmentSamplers(fragmentSamplerBindings);
        DrawIndexedQuads(ref commandBuffer, offset, _numSprites - offset, vertexUniformOffset, fragmentUniformOffset);
        DrawCalls++;

        MaxNumAddedSprites = Math.Max(MaxNumAddedSprites, _numSprites);
        LastNumAddedSprites = _numSprites;
        _numSprites = 0;
    }

    public static uint[] GenerateIndexArray(uint maxIndices)
    {
        var result = new uint[maxIndices];
        for (uint i = 0, j = 0; i < maxIndices; i += 6, j += 4)
        {
            result[i] = j;
            result[i + 1] = j + 1;
            result[i + 2] = j + 2;
            result[i + 3] = j + 2;
            result[i + 4] = j + 1;
            result[i + 5] = j + 3;
        }

        return result;
    }

    public static void PushSpriteVertices(Position3DTextureColorVertex[] vertices, uint vertexOffset, in Sprite sprite, Matrix4x4 transform, float depth,
        Color[] colors, SpriteFlip flip)
    {
        /*
        var topLeft = sprite.DstRect.MinVec();
        var bottomLeft = sprite.DstRect.BottomLeftVec();
        var topRight = sprite.DstRect.TopRightVec();
        var bottomRight = sprite.DstRect.MaxVec();
        */

        var topLeft = Vector2.Zero;
        var bottomLeft = new Vector2(0, sprite.SrcRect.Height);
        var topRight = new Vector2(sprite.SrcRect.Width, 0);
        var bottomRight = new Vector2(sprite.SrcRect.Width, sprite.SrcRect.Height);

        Vector2.Transform(ref topLeft, ref transform, out topLeft);
        Vector2.Transform(ref bottomLeft, ref transform, out bottomLeft);
        Vector2.Transform(ref topRight, ref transform, out topRight);
        Vector2.Transform(ref bottomRight, ref transform, out bottomRight);

        if (ShouldRoundPositions)
        {
            topLeft = topLeft.Floor();
            bottomLeft = bottomLeft.Floor();
            topRight = topRight.Floor();
            bottomRight = bottomRight.Floor();
        }

        SetVector(ref vertices[vertexOffset].Position, topLeft, depth);
        SetVector(ref vertices[vertexOffset + 1].Position, bottomLeft, depth);
        SetVector(ref vertices[vertexOffset + 2].Position, topRight, depth);
        SetVector(ref vertices[vertexOffset + 3].Position, bottomRight, depth);

        vertices[vertexOffset].TexCoord = sprite.UV.TopLeft;
        vertices[vertexOffset + 1].TexCoord = sprite.UV.BottomLeft;
        vertices[vertexOffset + 2].TexCoord = sprite.UV.TopRight;
        vertices[vertexOffset + 3].TexCoord = sprite.UV.BottomRight;

        var effects = (byte)(flip & (SpriteFlip.FlipVertically | SpriteFlip.FlipHorizontally));
        vertices[vertexOffset].TexCoord.X = CornerOffsetX[0 ^ effects] * sprite.UV.Dimensions.X + sprite.UV.Position.X;
        vertices[vertexOffset].TexCoord.Y = CornerOffsetY[0 ^ effects] * sprite.UV.Dimensions.Y + sprite.UV.Position.Y;
        vertices[vertexOffset + 1].TexCoord.X = CornerOffsetX[1 ^ effects] * sprite.UV.Dimensions.X + sprite.UV.Position.X;
        vertices[vertexOffset + 1].TexCoord.Y = CornerOffsetY[1 ^ effects] * sprite.UV.Dimensions.Y + sprite.UV.Position.Y;
        vertices[vertexOffset + 2].TexCoord.X = CornerOffsetX[2 ^ effects] * sprite.UV.Dimensions.X + sprite.UV.Position.X;
        vertices[vertexOffset + 2].TexCoord.Y = CornerOffsetY[2 ^ effects] * sprite.UV.Dimensions.Y + sprite.UV.Position.Y;
        vertices[vertexOffset + 3].TexCoord.X = CornerOffsetX[3 ^ effects] * sprite.UV.Dimensions.X + sprite.UV.Position.X;
        vertices[vertexOffset + 3].TexCoord.Y = CornerOffsetY[3 ^ effects] * sprite.UV.Dimensions.Y + sprite.UV.Position.Y;

        vertices[vertexOffset].Color = colors[0];
        vertices[vertexOffset + 1].Color = colors[1];
        vertices[vertexOffset + 2].Color = colors[2];
        vertices[vertexOffset + 3].Color = colors[3];
    }

    public static void DrawIndexedQuads(ref CommandBuffer commandBuffer, uint offset, uint numSprites, uint vertexParamOffset, uint fragmentParamOffset)
    {
        commandBuffer.DrawIndexedPrimitives(offset * 4u, 0u, numSprites * 2u, vertexParamOffset, fragmentParamOffset);
    }

    public static bool BindingsAreEqual(in TextureSamplerBinding a, in TextureSamplerBinding b)
    {
        return a.Sampler.Handle == b.Sampler.Handle &&
               a.Texture.Handle == b.Texture.Handle;
    }
}
