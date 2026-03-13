using System.Numerics;
using libsm64sharp;

namespace OnixSM64.Runtime;

public static class SM64CollisionUtils {
	private const float MIN_HEIGHT = 0.05f;
	private const float EPSILON = 0.001f;
	private const float MAX_COORD = 32000f;

	public static (int x, int y, int z) ToSM64Coords(Vector3 v) {
		float x = Math.Clamp(v.X * Constants.SCALE_FACTOR, -MAX_COORD, MAX_COORD);
		float y = Math.Clamp(v.Y * Constants.SCALE_FACTOR, -MAX_COORD, MAX_COORD);
		float z = Math.Clamp(v.Z * Constants.SCALE_FACTOR, -MAX_COORD, MAX_COORD);

		return ((int)x, (int)y, (int)z);
	}

	public static bool IsDegenerate(Vector3 v0, Vector3 v1, Vector3 v2) {
		return Vector3.Distance(v0, v1) < EPSILON ||
		       Vector3.Distance(v1, v2) < EPSILON ||
		       Vector3.Distance(v2, v0) < EPSILON;
	}

	public static void AddTriangleSafe(
		ISm64StaticCollisionMeshBuilder builder,
		Vector3 v0,
		Vector3 v1,
		Vector3 v2,
		Sm64SurfaceType surfaceType = Sm64SurfaceType.SURFACE_DEFAULT,
		Sm64TerrainType terrainType = Sm64TerrainType.TERRAIN_GRASS
	) {
		if (IsDegenerate(v0, v1, v2)) return;

		builder.AddTriangle(surfaceType, terrainType, ToSM64Coords(v0), ToSM64Coords(v1), ToSM64Coords(v2));
	}

	public static void AddQuadSafe(
		ISm64StaticCollisionMeshBuilder builder,
		Vector3 v0,
		Vector3 v1,
		Vector3 v2,
		Vector3 v3,
		Sm64SurfaceType surfaceType = Sm64SurfaceType.SURFACE_DEFAULT,
		Sm64TerrainType terrainType = Sm64TerrainType.TERRAIN_GRASS
	) {
		AddTriangleSafe(builder, v0, v1, v2, surfaceType, terrainType);
		AddTriangleSafe(builder, v2, v3, v0, surfaceType, terrainType);
	}

	public static void AddCubeSafe(
		ISm64StaticCollisionMeshBuilder builder,
		Vector3 center,
		Vector3 size,
		Sm64SurfaceType surfaceType = Sm64SurfaceType.SURFACE_DEFAULT,
		Sm64TerrainType terrainType = Sm64TerrainType.TERRAIN_GRASS
	) {
		if (Math.Abs(size.Y) < MIN_HEIGHT) size.Y = MIN_HEIGHT;

		Vector3 half = size / 2f;

		Vector3 p000 = center + new Vector3(-half.X, -half.Y, -half.Z);
		Vector3 p001 = center + new Vector3(-half.X, -half.Y, +half.Z);
		Vector3 p010 = center + new Vector3(-half.X, +half.Y, -half.Z);
		Vector3 p011 = center + new Vector3(-half.X, +half.Y, +half.Z);
		Vector3 p100 = center + new Vector3(+half.X, -half.Y, -half.Z);
		Vector3 p101 = center + new Vector3(+half.X, -half.Y, +half.Z);
		Vector3 p110 = center + new Vector3(+half.X, +half.Y, -half.Z);
		Vector3 p111 = center + new Vector3(+half.X, +half.Y, +half.Z);

		// Top (+Y)
		AddQuadSafe(builder, p011, p111, p110, p010, surfaceType, terrainType);

		// Bottom (-Y)
		AddQuadSafe(builder, p000, p100, p101, p001, surfaceType, terrainType);

		// Front (-Z)
		AddQuadSafe(builder, p010, p110, p100, p000, surfaceType, terrainType);

		// Back (+Z)
		AddQuadSafe(builder, p001, p101, p111, p011, surfaceType, terrainType);

		// Left (-X)
		AddQuadSafe(builder, p001, p011, p010, p000, surfaceType, terrainType);

		// Right (+X)
		AddQuadSafe(builder, p100, p110, p111, p101, surfaceType, terrainType);
	}

	public static void AddWedgeSafe(
		ISm64StaticCollisionMeshBuilder builder,
		Vector3 center,
		Vector3 size,
		int rotation = 0,
		Sm64SurfaceType surfaceType = Sm64SurfaceType.SURFACE_DEFAULT,
		Sm64TerrainType terrainType = Sm64TerrainType.TERRAIN_GRASS
	) {
		if (Math.Abs(size.Y) < MIN_HEIGHT) size.Y = MIN_HEIGHT;

		Vector3 half = size / 2f;

		Vector3 bbl = new(-half.X, -half.Y, -half.Z); // bottom-back-left
		Vector3 bbr = new(+half.X, -half.Y, -half.Z); // bottom-back-right
		Vector3 bfl = new(-half.X, -half.Y, +half.Z); // bottom-front-left
		Vector3 bfr = new(+half.X, -half.Y, +half.Z); // bottom-front-right
		Vector3 btl = new(-half.X, +half.Y, -half.Z); // back-top-left
		Vector3 btr = new(+half.X, +half.Y, -half.Z); // back-top-right

		rotation = ((rotation % 4) + 4) % 4;

		if (rotation != 0) {
			float sin = rotation switch { 1 => 1f, 2 => 0f, 3 => -1f, _ => 0f };
			float cos = rotation switch { 1 => 0f, 2 => -1f, 3 => 0f, _ => 1f };

			bbl = RotateY(bbl, sin, cos);
			bbr = RotateY(bbr, sin, cos);
			bfl = RotateY(bfl, sin, cos);
			bfr = RotateY(bfr, sin, cos);
			btl = RotateY(btl, sin, cos);
			btr = RotateY(btr, sin, cos);
		}

		bbl += center;
		bbr += center;
		bfl += center;
		bfr += center;
		btl += center;
		btr += center;

		AddQuadSafe(builder, bfl, bbl, bbr, bfr, surfaceType, terrainType);

		AddQuadSafe(builder, bbl, bbr, btr, btl, surfaceType, terrainType);

		AddQuadSafe(builder, btl, bfl, bfr, btr, surfaceType, terrainType);

		AddTriangleSafe(builder, bfl, bbl, btl, surfaceType, terrainType);

		AddTriangleSafe(builder, bbr, bfr, btr, surfaceType, terrainType);
	}

	private static Vector3 RotateY(Vector3 v, float sin, float cos) {
		return new Vector3(
			v.X * cos + v.Z * sin,
			v.Y,
			-v.X * sin + v.Z * cos
		);
	}

	public static void AddFloorSafe(
		ISm64StaticCollisionMeshBuilder builder,
		Vector3 center,
		float width,
		float depth,
		Sm64SurfaceType surfaceType = Sm64SurfaceType.SURFACE_DEFAULT,
		Sm64TerrainType terrainType = Sm64TerrainType.TERRAIN_GRASS
	) {
		float halfX = width / 2f;
		float halfZ = depth / 2f;
		float y = center.Y;

		Vector3 p0 = new(center.X - halfX, y, center.Z - halfZ);
		Vector3 p1 = new(center.X - halfX, y, center.Z + halfZ);
		Vector3 p2 = new(center.X + halfX, y, center.Z + halfZ);
		Vector3 p3 = new(center.X + halfX, y, center.Z - halfZ);

		AddQuadSafe(builder, p0, p1, p2, p3, surfaceType, terrainType);
	}
}