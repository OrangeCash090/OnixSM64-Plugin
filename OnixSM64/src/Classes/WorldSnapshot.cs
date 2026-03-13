using OnixRuntime.Api.Maths;

namespace OnixSM64.Classes;

public struct WorldSnapshot {
	public BoundingBox[] NearbyCollisions;
	public StairBlock[] StairBlocks;
	
	public string StandingBlockName;
	public int WaterLevel;
}
