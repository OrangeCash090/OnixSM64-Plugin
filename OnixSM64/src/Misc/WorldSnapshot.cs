using OnixRuntime.Api.Maths;

namespace OnixSM64.Misc;

public struct WorldSnapshot {
	public BoundingBox[] NearbyCollisions;
	public string StandingBlockName;
	public int WaterLevel;
	public bool IsValid;
}
