using System.Numerics;
using libsm64sharp;
using OnixRuntime.Api.Maths;
using OnixRuntime.Api.Rendering;
using OnixRuntime.Api.Utils;
using OnixSM64.Misc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OnixSM64.Runtime;

public class SM64Renderer(ISm64Context context, OnixSM64Config pluginConfig) {
	public OnixSM64Config Config = pluginConfig;
    public ISm64Context Context = context;

    private int _vertexCount;
    private bool _marioTexUploaded;
    private uint[] _cachedVertexColors;

    private static byte[] ImageToRgba8(Image<Rgba32> image) {
        int width = image.Width;
        int height = image.Height;
        
        byte[] data = new byte[width * height * 4];
        ImageFrame<Rgba32>? frame = image.Frames[0];
        
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Rgba32 pixel = frame[x, y];
                int i = 4 * (y * width + x);
                
                if (pixel.A == 0) {
                    pixel.R = 255;
                    pixel.G = 255;
                    pixel.B = 255;
                    pixel.A = 255;
                }

                data[i] = pixel.R;
                data[i + 1] = pixel.G;
                data[i + 2] = pixel.B;
                data[i + 3] = pixel.A;
            }
        }

        return data;
    }

    private List<MeshBuilderVertexColorUvNormal> SM64MarioTrisToVerts(ISm64MarioMeshTrianglesData triangles) {
        List<MeshBuilderVertexColorUvNormal> verts = [];

        bool buildingCache = false;

        if (_vertexCount != triangles.TriangleCount * 3) {
	        _cachedVertexColors = new uint[triangles.TriangleCount * 3];
	        _vertexCount = triangles.TriangleCount * 3;
	        buildingCache = true;
        }
        
        for (int i = 0; i < _vertexCount; i++) {
            float u = triangles.Uvs[i].X;
            float v = triangles.Uvs[i].Y;
            
            if (u < 0) u = 0f;
            if (v < 0) v = 0f;

            uint vertexColor;
            
            if (buildingCache)
	            _cachedVertexColors[i] = new ColorF(triangles.Colors[i].X, triangles.Colors[i].Y, triangles.Colors[i].Z).ToRGBA();

            if (Config.MarioHasCustomColor) {
	            vertexColor = _cachedVertexColors[i] switch {
		            4278190335 => Config.MarioShirtColor.ToRGBA(),
		            4294901760 => Config.MarioPantsColor.ToRGBA(),
		            4279114866 => Config.MarioShoesColor.ToRGBA(),
		            4294967295 => Config.MarioGlovesColor.ToRGBA(),
		            _ => _cachedVertexColors[i]
	            };
            } else {
	            vertexColor = _cachedVertexColors[i];
            }
            
            verts.Add(new MeshBuilderVertexColorUvNormal {
                Position = triangles.Positions[i] / Constants.SCALE_FACTOR, 
                Normal = new Vec4(triangles.Normals[i], 0f), 
                Uv = new Vec2(u, v), 
                Color = vertexColor
            });
        }
        
        return verts;
    }

    private void UploadMarioTexture(RendererWorld gfx, Image<Rgba32> textureImage) {
        byte[] rgba = ImageToRgba8(textureImage);
        
        gfx.UploadTexture(new TexturePath("mario.tex", TexturePathBase.Game), new RawImageData(rgba, textureImage.Width, textureImage.Height));
        _marioTexUploaded = true;
    }
    
    public void RenderMarioMesh(RendererWorld gfx, ISm64MarioMesh marioMesh, Vector3 worldOffset) {
        using GameMeshBuilder.GameMeshBuilderSession session = gfx.NewMeshBuilderSession(
            MeshBuilderPrimitiveType.Triangle, 
            ColorF.White,
            TexturePath.Assets("mario.png")
        );

        List<MeshBuilderVertexColorUvNormal> verts = SM64MarioTrisToVerts(marioMesh.TriangleData!);
        
        // hi onix
        for (int i = 0; i < verts.Count; i++) {
            MeshBuilderVertexColorUvNormal vert = verts[i];
            vert.Position += SM64CollisionWorld.ToVec3(worldOffset);
            verts[i] = vert;
        }

        session.Builder.VertexBatch(verts);
    }
}