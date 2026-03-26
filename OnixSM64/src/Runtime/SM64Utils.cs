using System.Numerics;
using OnixRuntime.Api;
using OnixRuntime.Api.Maths;
using OnixRuntime.Api.NBT;
using OnixRuntime.Api.World;
using OnixSM64.Classes;

namespace OnixSM64.Runtime;

public static class SM64Utils {
	private static readonly int[] StairDirectionLookup = [3, 1, 2, 0];

	public static Vec3 ConvertToSM64(Vec3 v) => v * Constants.SCALE_FACTOR;
	public static Vec3 ConvertFromSM64(Vec3 v) => v / Constants.SCALE_FACTOR;

	public static Vector3 ToVector3(Vec3 v) {
		return new Vector3(v.X, v.Y, v.Z);
	}

	public static Vec3 ToVec3(Vector3 v) {
		return new Vec3(v.X, v.Y, v.Z);
	}

	public static WorldSnapshot CaptureWorldSnapshot(Vec3 marioWorldPos, Vector3 worldOffset, bool doWater, bool doStairs) {
		BoundingBox[] collisions = GetCollisionsAroundPoint(marioWorldPos, 3);
		string standingBlockName = Onix.Region!.GetBlock(new BlockPos(marioWorldPos)).Name;
		int waterLevel = doWater ? ComputeWaterLevel(marioWorldPos, standingBlockName, worldOffset) : -1000;

		return new WorldSnapshot {
			NearbyCollisions = collisions,
			StairBlocks = doStairs ? GetStairsAroundPoint(marioWorldPos, 3) : [],
			StandingBlockName = standingBlockName,
			WaterLevel = waterLevel
		};
	}

	private static BoundingBox[] GetCollisionsAroundPoint(Vec3 pos, int range) {
		Vec3 r = Vec3.One * range;
		return Onix.Region!.GetCollisions(new BoundingBox(pos - r, pos + r));
	}

	private static StairBlock[] GetStairsAroundPoint(Vec3 pos, int range) {
		List<StairBlock> stairs = [];

		for (int x = (int)pos.X - range; x <= (int)pos.X + range; x++) {
			for (int y = (int)pos.Y - range; y <= (int)pos.Y + range; y++) {
				for (int z = (int)pos.Z - range; z <= (int)pos.Z + range; z++) {
					Block block = Onix.Region!.GetBlock(x, y, z);

					if (!block.Name.Contains("stair")) continue;

					Dictionary<string, NbtTag> states = block.State.Value;
					states.TryGetValue("states", out NbtTag? tag);
					((ObjectTag)tag!).Value.TryGetValue("weirdo_direction", out NbtTag? value);
					IntTag direction = (IntTag)value!;

					stairs.Add(
						new StairBlock {
							Position = new BlockPos(x, y, z),
							Rotation = StairDirectionLookup[direction.Value]
						}
					);
				}
			}
		}

		return stairs.ToArray();
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