using System.Numerics;
using libsm64sharp;
using OnixRuntime.Api.Maths;

namespace OnixSM64.Runtime;

public sealed class SM64CollisionWorld {
	private readonly ISm64Context _context;
	private ISm64StaticCollisionMeshBuilder _currentBuilder;

	public SM64CollisionWorld(ISm64Context context) {
		_context = context;
		_currentBuilder = _context.CreateStaticCollisionMesh();
	}

	public void BeginFrame() {
		_currentBuilder = _context.CreateStaticCollisionMesh();
	}

	public void AddSafetyFloor(Vector3 center, float width = 40f, float depth = 40f) {
		SM64CollisionUtils.AddFloorSafe(_currentBuilder, center, width, depth);
	}

	public void AddCube(
		Vector3 center,
		Vector3 size,
		Sm64SurfaceType surfaceType = Sm64SurfaceType.SURFACE_DEFAULT,
		Sm64TerrainType terrainType = Sm64TerrainType.TERRAIN_GRASS
	) {
		SM64CollisionUtils.AddCubeSafe(_currentBuilder, center, size, surfaceType, terrainType);
	}

	public void AddWedge(
		Vector3 center,
		Vector3 size,
		int rotation = 0,
		Sm64SurfaceType surfaceType = Sm64SurfaceType.SURFACE_DEFAULT,
		Sm64TerrainType terrainType = Sm64TerrainType.TERRAIN_GRASS
	) {
		SM64CollisionUtils.AddWedgeSafe(_currentBuilder, center, size, rotation, surfaceType, terrainType);
	}

	public void AddQuad(
		Vector3 v0,
		Vector3 v1,
		Vector3 v2,
		Vector3 v3,
		Sm64SurfaceType surfaceType = Sm64SurfaceType.SURFACE_DEFAULT,
		Sm64TerrainType terrainType = Sm64TerrainType.TERRAIN_GRASS
	) {
		SM64CollisionUtils.AddQuadSafe(_currentBuilder, v0, v1, v2, v3, surfaceType, terrainType);
	}

	public void AddTriangle(
		Vector3 v0,
		Vector3 v1,
		Vector3 v2,
		Sm64SurfaceType surfaceType = Sm64SurfaceType.SURFACE_DEFAULT,
		Sm64TerrainType terrainType = Sm64TerrainType.TERRAIN_GRASS
	) {
		SM64CollisionUtils.AddTriangleSafe(_currentBuilder, v0, v1, v2, surfaceType, terrainType);
	}

	public void Commit() {
		try {
			_currentBuilder.Build();
		} catch (Exception ex) {
			Console.WriteLine($"Failed to commit collision: {ex.Message}");
		}
	}
}