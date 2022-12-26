using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Math.Float;
using MyGame.Fonts;
using MyGame.Utils;

namespace MyGame;

public class Renderer
{
    public Color DefaultClearColor = Color.CornflowerBlue;
    public static Sampler PointClamp = null!;
    public SpriteBatch SpriteBatch;
    public TextButcher TextButcher;

    private Sprite _blankSprite;
    private GraphicsPipeline _pipeline;
    private ColorAttachmentInfo _colorAttachmentInfo;
    public FreeTypeFontAtlas FreeTypeFontAtlas;

    public Renderer(Game game)
    {
        var blankTexture = TextureUtils.CreateColoredTexture(game.GraphicsDevice, 1, 1, Color.White);
        _blankSprite = new Sprite(blankTexture);
        PointClamp = new Sampler(game.GraphicsDevice, SamplerCreateInfo.PointClamp);
        SpriteBatch = new SpriteBatch(game.GraphicsDevice);
        TextButcher = new TextButcher(game.GraphicsDevice);

        FreeTypeFontAtlas = new FreeTypeFontAtlas(game.GraphicsDevice, 512, 512);
        FreeTypeFontAtlas.AddFont("Content/fonts/consola.ttf", 18u, true);

        _pipeline = CreateGraphicsPipeline(game.GraphicsDevice, ColorAttachmentBlendState.AlphaBlend);

        _colorAttachmentInfo = new ColorAttachmentInfo()
        {
            ClearColor = Color.CornflowerBlue,
            LoadOp = LoadOp.Clear,
        };
    }

    public void RunRenderPass(ref CommandBuffer commandBuffer, Texture renderTarget, Color? clearColor, Matrix4x4? viewProjection)
    {
        if (!MyGameInstance.UseFreeType)
            TextButcher.FlushToSpriteBatch(SpriteBatch);
        SpriteBatch.UpdateBuffers(ref commandBuffer);

        _colorAttachmentInfo.Texture = renderTarget;
        _colorAttachmentInfo.LoadOp = clearColor == null ? LoadOp.Load : LoadOp.Clear;
        _colorAttachmentInfo.ClearColor = clearColor ?? DefaultClearColor;

        commandBuffer.BeginRenderPass(_colorAttachmentInfo);
        commandBuffer.BindGraphicsPipeline(_pipeline);
        SpriteBatch.DrawIndexed(ref commandBuffer,
            viewProjection ?? GetViewProjection(_colorAttachmentInfo.Texture.Width, _colorAttachmentInfo.Texture.Height));
        commandBuffer.EndRenderPass();
    }

    public void DrawSprite(Sprite sprite, Matrix4x4 transform, Color color, float depth = 0, SpriteFlip flip = SpriteFlip.None)
    {
        SpriteBatch.Draw(sprite, color, depth, transform, PointClamp, flip);
    }

    public void DrawText( /*FontType fontType,*/ ReadOnlySpan<char> text, Vector2 position, float depth, Color color,
        HorizontalAlignment alignH = HorizontalAlignment.Left, VerticalAlignment alignV = VerticalAlignment.Top)
    {
        TextButcher.Add( /*fontType,*/text, position.X, position.Y, depth, color, alignH, alignV);
    }

    public void DrawFTText(ReadOnlySpan<char> text, Vector2 position, Color color)
    {
        FreeTypeFontAtlas.DrawText(this, text, position, color);
    }

    public void DrawRect(Rectangle rect, Color color, float depth = 0)
    {
        var scale = Matrix3x2.CreateScale(rect.Width, rect.Height) * Matrix3x2.CreateTranslation(rect.X, rect.Y);
        SpriteBatch.Draw(_blankSprite, color, depth, scale.ToMatrix4x4(), PointClamp);
    }

    public void DrawLine(Point from, Point to, Color color, float thickness = 1.0f)
    {
        DrawLine(new Vector2(from.X, from.Y), new Vector2(to.X, to.Y), color, thickness);
    }

    public void DrawLine(Vector2 from, Vector2 to, Color color, float thickness = 1.0f)
    {
        var length = (from - to).Length();
        var origin = Matrix3x2.CreateTranslation(0, 0);
        var scale = Matrix3x2.CreateScale(length, thickness);
        var rotation = Matrix3x2.CreateRotation(Vector2Ext.AngleBetweenVectors(from, to));
        var translation = Matrix3x2.CreateTranslation(from);
        var tAll = origin * scale * rotation * translation;
        SpriteBatch.Draw(_blankSprite, color, 0, tAll.ToMatrix4x4(), PointClamp);
    }

    public static Matrix4x4 GetViewProjection(uint width, uint height)
    {
        var view = Matrix4x4.CreateTranslation(0, 0, -1000);
        var projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0.0001f, 10000f);
        return view * projection;
    }

    #region Pipeline Setup

    public static VertexInputState GetVertexInputState()
    {
        var myVertexBindings = new[]
        {
            VertexBinding.Create<Position3DTextureColorVertex>(),
        };

        var myVertexAttributes = new[]
        {
            VertexAttribute.Create<Position3DTextureColorVertex>(nameof(Position3DTextureColorVertex.Position), 0),
            VertexAttribute.Create<Position3DTextureColorVertex>(nameof(Position3DTextureColorVertex.TexCoord), 1),
            VertexAttribute.Create<Position3DTextureColorVertex>(nameof(Position3DTextureColorVertex.Color), 2),
        };

        return new VertexInputState
        {
            VertexBindings = myVertexBindings,
            VertexAttributes = myVertexAttributes,
        };
    }

    public static GraphicsPipeline CreateGraphicsPipeline(GraphicsDevice device, ColorAttachmentBlendState blendState)
    {
        var spriteVertexShader = new ShaderModule(device, "Content/Shaders/sprite.vert.spv");
        var spriteFragmentShader = new ShaderModule(device, "Content/Shaders/sprite.frag.spv");

        var vertexShaderInfo = GraphicsShaderInfo.Create<Matrix4x4>(spriteVertexShader, "main", 0);
        var fragmentShaderInfo = GraphicsShaderInfo.Create(spriteFragmentShader, "main", 1);

        var myGraphicsPipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            AttachmentInfo = new GraphicsPipelineAttachmentInfo(
                new ColorAttachmentDescription(TextureFormat.B8G8R8A8, blendState)
            ),
            DepthStencilState = DepthStencilState.Disable,
            VertexShaderInfo = vertexShaderInfo,
            FragmentShaderInfo = fragmentShaderInfo,
            MultisampleState = MultisampleState.None,
            RasterizerState = RasterizerState.CCW_CullNone,
            PrimitiveType = PrimitiveType.TriangleList,
            VertexInputState = GetVertexInputState(),
        };

        return new GraphicsPipeline(
            device,
            myGraphicsPipelineCreateInfo
        );
    }

    #endregion
}
