using System.Numerics;
using libsm64sharp;
using OnixRuntime.Api.Maths;

namespace OnixSM64.Classes;

public struct MarioRenderSnapshot {
	public ISm64MarioMesh? Mesh;
	public Vector3 WorldOffset;
	public Vec3 MarioWorldPos;
}