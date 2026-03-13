using System.Numerics;
using libsm64sharp;
using OnixRuntime.Api.Maths;
using OnixSM64.Classes;

namespace OnixSM64.Runtime;

public class SM64Input(OnixSM64Config pluginConfig) {
	public readonly OnixSM64Config Config = pluginConfig;

	public MarioInputState State;
	public PlayerSnapshot LatestPlayerSnapshot;

	public bool IsControlling { get; set; } = true;

	private bool _mouseDown;
	private bool _groundPounded;

	private static int BoolToInt(bool value) => value ? 1 : 0;

	public InputEvents UpdateInput(ISm64Mario mario) {
		PlayerSnapshot snap = LatestPlayerSnapshot;
		InputEvents events = default;

		Vec2 analogStick = new Vec2(
			BoolToInt(State.Right) - BoolToInt(State.Left),
			BoolToInt(State.Backward) - BoolToInt(State.Forward)
		).Normalized;

		mario.Gamepad.IsAButtonDown = State.AButton;
		mario.Gamepad.IsBButtonDown = State.BButton;
		mario.Gamepad.IsZButtonDown = State.ZButton;

		mario.Gamepad.AnalogStick.X = analogStick.X;
		mario.Gamepad.AnalogStick.Y = analogStick.Y;

		float playerYawRadians = -snap.Yaw * (MathF.PI / 180f);
		mario.Gamepad.CameraNormal.X = MathF.Sin(playerYawRadians);
		mario.Gamepad.CameraNormal.Y = MathF.Cos(playerYawRadians);

		if (State.BButton) {
			if (!_mouseDown) {
				events.PunchFired = true;
				_mouseDown = true;
			}
		} else {
			_mouseDown = false;
		}

		if (mario.Action == MarioAction.ACT_GROUND_POUND_LAND && Config.MarioGroundPoundGriefs && !_groundPounded) {
			events.GroundPoundFired = true;
			_groundPounded = true;
		}

		if (mario.Action != MarioAction.ACT_GROUND_POUND_LAND) {
			_groundPounded = false;
		}

		return events;
	}

	public string BuildCameraCommand(ISm64Mario mario, Vector3 worldOffset) {
		PlayerSnapshot snap = LatestPlayerSnapshot;

		Vec3 marioPos = SM64Utils.ConvertFromSM64(mario.Position);
		Vec3 marioWorldPos = marioPos + SM64Utils.ToVec3(worldOffset);
		Vec3 cameraTarget = marioWorldPos + new Vec3(0, 1f, 0);

		float yawRad = (-snap.Yaw) * (MathF.PI / 180f);
		float pitchRad = snap.Pitch * (MathF.PI / 180f);
		const float dist = 5f;

		Vec3 cam = new(
			cameraTarget.X - MathF.Sin(yawRad) * MathF.Cos(pitchRad) * dist,
			cameraTarget.Y + MathF.Sin(pitchRad) * dist,
			cameraTarget.Z - MathF.Cos(yawRad) * MathF.Cos(pitchRad) * dist
		);

		return $"/camera @s set minecraft:free ease 0.1 linear pos " +
		       $"{float.Round(cam.X, 3)} {float.Round(cam.Y, 3)} {float.Round(cam.Z, 3)} " +
		       $"facing {float.Round(cameraTarget.X, 3)} {float.Round(cameraTarget.Y, 3)} {float.Round(cameraTarget.Z, 3)}";
	}

	public (Vec3 lookFrom, Vec3 lookTo, Vec3 entityPos1, Vec3 entityPos2) BuildPunchVectors(
		ISm64Mario mario, Vector3 worldOffset) {
		PlayerSnapshot snap = LatestPlayerSnapshot;

		Vec3 marioPos = SM64Utils.ConvertFromSM64(mario.Position) + new Vec3(0, 1f, 0);
		Vec3 marioWorldPos = marioPos + SM64Utils.ToVec3(worldOffset);

		float yaw = mario.FaceAngle;
		float pitch = -snap.Pitch * (MathF.PI / 180f);

		Vec3 look = new(
			MathF.Sin(yaw) * MathF.Cos(pitch),
			MathF.Sin(pitch),
			MathF.Cos(yaw) * MathF.Cos(pitch)
		);

		return (marioWorldPos, marioWorldPos + look * 2, marioWorldPos + look, marioWorldPos + look * 2);
	}
}