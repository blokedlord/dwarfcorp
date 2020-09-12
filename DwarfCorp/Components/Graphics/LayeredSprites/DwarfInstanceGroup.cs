using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace DwarfCorp.DwarfSprites
{
    public class DwarfInstanceGroup
    {
        private class Dwarf
        {
            public DwarfCharacterSprite Sprite;
            public Point AtlasFrame = new Point(-1, -1);
            public Point LastFrame = new Point(-1, -1);
        }

        private const int InstanceQueueSize = 256;

        public BatchBillboardPrimitive Primitive = null;
        private TiledInstancedVertex[] Instances = new TiledInstancedVertex[InstanceQueueSize];
        private int InstanceCount = 0;
        private TiledInstancedVertex[] SilhouetteInstances = new TiledInstancedVertex[InstanceQueueSize];
        private int SilhouetteInstanceCount = 0;
        private DynamicVertexBuffer InstanceBuffer = null;
        private List<Dwarf> Dwarves = new List<Dwarf>();
        private Rectangle AtlasBounds;
        private Texture2D AtlasTexture = null;
        private SpriteSheet Sheet = null;
        private int LastUpdatedDwarf = 0;

        public DwarfInstanceGroup()
        {
        }

        public void AddDwarfSprite(DwarfCharacterSprite Sprite)
        {
            Dwarves.Add(new Dwarf { Sprite = Sprite });
        }

        public Texture2D GetAtlasTexture()
        {
            return AtlasTexture;
        }

        public void Update(GraphicsDevice Device)
        {
            Dwarves.RemoveAll(d => d.Sprite.IsDead);

            // Calculate atlas size.
            var columns = Math.Max((int)Math.Ceiling(Math.Sqrt(Dwarves.Count) * 1.5f), 1);
            var rows = Math.Max((int)Math.Ceiling((float)Dwarves.Count / (float)columns), 1);
            AtlasBounds = new Rectangle(0, 0, columns * 48, rows * 40);

            if (AtlasTexture == null || AtlasTexture.IsDisposed || AtlasTexture.Width != AtlasBounds.Width || AtlasTexture.Height != AtlasBounds.Height)
            {
                if (AtlasTexture != null && !AtlasTexture.IsDisposed)
                    AtlasTexture.Dispose();
                AtlasTexture = new Texture2D(Device, AtlasBounds.Width, AtlasBounds.Height);
                Sheet = new SpriteSheet(AtlasTexture, 48, 40);
            }

            var x = 0;
            var y = 0;
            var updates = 0;
            var index = 0;

            foreach (var dwarf in Dwarves)
            {
                var needsUpdate = false;

                var atlasFrame = new Point(x, y);
                var atlasRect = new Rectangle(x * 48, y * 40, 48, 40);

                if (atlasFrame != dwarf.AtlasFrame)
                    needsUpdate = true;

                var frame = dwarf.Sprite.AnimPlayer.GetCurrentAnimation().Frames[dwarf.Sprite.AnimPlayer.CurrentFrame];
                if (frame != dwarf.LastFrame)
                    needsUpdate = true;

                x += 1;
                if (x >= columns)
                {
                    x = 0;
                    y += 1;
                }

                if (dwarf.Sprite.SpriteSheet != null && needsUpdate && updates < GameSettings.Current.MaxDwarfSpriteUpdates && index > LastUpdatedDwarf)
                {
                    var sourceRectangle = dwarf.Sprite.GetCurrentFrameRect();

                        var realTexture = dwarf.Sprite.SpriteSheet.GetTexture();
                        var textureData = new Color[sourceRectangle.Width * sourceRectangle.Height];
                        realTexture.GetData(0, sourceRectangle, textureData, 0, sourceRectangle.Width * sourceRectangle.Height);
                        AtlasTexture.SetData(0, atlasRect, textureData, 0, 48 * 40);

                    updates += 1;
                    LastUpdatedDwarf = index;
                }
                else
                    frame = new Point(-1, -1);

                dwarf.AtlasFrame = atlasFrame;
                dwarf.LastFrame = frame;
                index += 1;

                
            }

            if (LastUpdatedDwarf >= Dwarves.Count - 1)
                LastUpdatedDwarf = -1;
        }

        public void Render(GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode)
        {
            foreach (var dwarf in Dwarves)
            {
                if (dwarf.Sprite.SpriteSheet != null)
                {
                    var v = new TiledInstancedVertex
                    {
                        Transform = dwarf.Sprite.GetWorldMatrix(Camera),
                        LightRamp = dwarf.Sprite.LightRamp,
                        SelectionBufferColor = dwarf.Sprite.GetGlobalIDColor(),
                        VertexColorTint = dwarf.Sprite.VertexColorTint,
                        TileBounds = Sheet.GetTileUVBounds(dwarf.AtlasFrame)
                    };

                    if (dwarf.Sprite.DrawSilhouette)
                    {
                        SilhouetteInstances[SilhouetteInstanceCount] = v;
                        SilhouetteInstanceCount += 1;
                        if (SilhouetteInstanceCount >= SilhouetteInstances.Length)
                            FlushSilhouette(Device, Effect, Camera, Mode);
                    }

                    Instances[InstanceCount] = v;

                    InstanceCount += 1;
                    if (InstanceCount >= InstanceQueueSize)
                        Flush(Device, Effect, Camera, Mode);
                }
            }

            Flush(Device, Effect, Camera, Mode);
            FlushSilhouette(Device, Effect, Camera, Mode);
        }

        private void Flush(GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode)
        {
            if (InstanceCount == 0) return;

            BlendState blendState;
            bool ghostEnabled;
            PrepEffect(Device, Effect, Mode, out blendState, out ghostEnabled);

            InstanceBuffer.SetData(Instances, 0, InstanceCount, SetDataOptions.Discard);
            Device.SetVertexBuffers(new VertexBufferBinding(Primitive.VertexBuffer), new VertexBufferBinding(InstanceBuffer, 0, 1));

            foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
                    Primitive.VertexCount, 0,
                    Primitive.Indexes.Length / 3,
                    InstanceCount);
            }

            Effect.GhostClippingEnabled = ghostEnabled;
            Effect.SetTexturedTechnique();
            Effect.World = Matrix.Identity;
            Device.BlendState = blendState;
            Effect.EnableWind = false;

            InstanceCount = 0;
        }

        private void FlushSilhouette(GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode)
        {
            if (SilhouetteInstanceCount == 0) return;

            BlendState blendState;
            bool ghostEnabled;
            PrepEffect(Device, Effect, Mode, out blendState, out ghostEnabled);

            InstanceBuffer.SetData(SilhouetteInstances, 0, SilhouetteInstanceCount, SetDataOptions.Discard);
            Device.SetVertexBuffers(new VertexBufferBinding(Primitive.VertexBuffer), new VertexBufferBinding(InstanceBuffer, 0, 1));

            Color oldTint = Effect.VertexColorTint;
            Effect.VertexColorTint = new Color(0.0f, 1.0f, 1.0f, 0.5f);
            Device.DepthStencilState = new DepthStencilState
            {
                DepthBufferWriteEnable = false,
                DepthBufferFunction = CompareFunction.Greater
            };

            var oldTechnique = Effect.CurrentTechnique;

            Effect.CurrentTechnique = Effect.Techniques["TiledInstancedSilhouette"];

            foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
                    Primitive.VertexCount, 0,
                    Primitive.Indexes.Length / 3,
                    SilhouetteInstanceCount);
            }

            Device.DepthStencilState = DepthStencilState.Default;
            Effect.VertexColorTint = oldTint;
            Effect.CurrentTechnique = oldTechnique;


            Effect.GhostClippingEnabled = ghostEnabled;
            Effect.SetTexturedTechnique();
            Effect.World = Matrix.Identity;
            Device.BlendState = blendState;
            Effect.EnableWind = false;

            SilhouetteInstanceCount = 0;
        }

        private void PrepEffect(GraphicsDevice Device, Shader Effect, InstanceRenderMode Mode, out BlendState blendState, out bool ghostEnabled)
        {
            if (Primitive == null)
                Primitive = new BatchBillboardPrimitive(new NamedImageFrame("newgui\\error"), 32, 32,
                            new Point(0, 0), 1.0f, 1.0f, false,
                            new List<Matrix> { Matrix.Identity },
                            new List<Color> { Color.White },
                            new List<Color> { Color.White });

            if (Primitive.VertexBuffer == null || Primitive.IndexBuffer == null ||
                (Primitive.VertexBuffer != null && Primitive.VertexBuffer.IsContentLost) ||
                (Primitive.IndexBuffer != null && Primitive.IndexBuffer.IsContentLost))
                Primitive.ResetBuffer(Device);


            if (InstanceBuffer == null || InstanceBuffer.IsDisposed || InstanceBuffer.IsContentLost)
                InstanceBuffer = new DynamicVertexBuffer(Device, TiledInstancedVertex.VertexDeclaration, InstanceQueueSize, BufferUsage.None);

            Device.RasterizerState = new RasterizerState { CullMode = CullMode.None };
            if (Mode == InstanceRenderMode.Normal)
                Effect.SetTiledInstancedTechnique();
            else
                Effect.CurrentTechnique = Effect.Techniques[Shader.Technique.SelectionBufferTiledInstanced];

            Effect.EnableWind = false;
            Effect.EnableLighting = true;
            Effect.VertexColorTint = Color.White;

            Device.Indices = Primitive.IndexBuffer;

            blendState = Device.BlendState;
            Device.BlendState = Mode == InstanceRenderMode.Normal ? BlendState.NonPremultiplied : BlendState.Opaque;

            Effect.MainTexture = AtlasTexture;
            Effect.LightRamp = Color.White;



            ghostEnabled = Effect.GhostClippingEnabled;
            Effect.GhostClippingEnabled = false;//RenderData.EnableGhostClipping && ghostEnabled;
        }
    }
}