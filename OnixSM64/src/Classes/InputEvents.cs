using OnixRuntime.Api.Maths;

namespace OnixSM64.Classes;

public struct InputEvents {
	public bool PunchFired;
	public bool GroundPoundFired;

	public string? CameraCommand;

	public Vec3 PunchLookFrom;
	public Vec3 PunchLookTo;
	public Vec3 PunchEntityPos1;
	public Vec3 PunchEntityPos2;

	public Vec3 GroundPoundWorldPos;
}