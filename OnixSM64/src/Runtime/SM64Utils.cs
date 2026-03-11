using System.Numerics;
using OnixRuntime.Api;
using OnixRuntime.Api.Maths;
using OnixSM64.Misc;

namespace OnixSM64.Runtime;

public static class SM64Utils {
	public static Vec3 ConvertToSM64(Vec3 v) => v * Constants.SCALE_FACTOR;
	public static Vec3 ConvertFromSM64(Vec3 v) => v / Constants.SCALE_FACTOR;
	
	public static WorldSnapshot CaptureWorldSnapshot(Vec3 marioWorldPos, Vector3 worldOffset) {
		BoundingBox[] collisions = GetCollisionsAroundPoint(marioWorldPos, 3);
		string standingBlockName = Onix.Region!.GetBlock(new BlockPos(marioWorldPos)).Name;
		int waterLevel = ComputeWaterLevel(marioWorldPos, standingBlockName, worldOffset);

		return new WorldSnapshot {
			NearbyCollisions = collisions,
			StandingBlockName = standingBlockName,
			WaterLevel = waterLevel,
			IsValid = true
		};
	}

	private static BoundingBox[] GetCollisionsAroundPoint(Vec3 pos, int range) {
		Vec3 r = Vec3.One * range;
		return Onix.Region!.GetCollisions(new BoundingBox(pos - r, pos + r));
	}

	private static int ComputeWaterLevel(Vec3 marioWorldPos, string standingBlockName, Vector3 worldOffset) {
		if (!standingBlockName.Contains("water"))
			return int.MinValue + 1000;

		int surfaceY = (int)Math.Floor(marioWorldPos.Y);

		while (Onix.Region!.GetBlock(new BlockPos(new Vec3(marioWorldPos.X, surfaceY + 1, marioWorldPos.Z))).Name.Contains("water")) {
			surfaceY++;
		}

		float localSurfaceY = (surfaceY + 1f) - worldOffset.Y;
		return (int)(localSurfaceY * Constants.SCALE_FACTOR);
	}
}
