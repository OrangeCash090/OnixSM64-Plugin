using System.Diagnostics;
using System.Numerics;
using libsm64sharp;
using OnixRuntime.Api;
using OnixRuntime.Api.Entities;
using OnixRuntime.Api.Inputs;
using OnixRuntime.Api.Maths;
using OnixRuntime.Api.Rendering;
using OnixRuntime.Api.UI;
using OnixSM64.Classes;

namespace OnixSM64.Runtime;

public sealed class SM64World(OnixSM64Config pluginConfig) : IDisposable {
	public readonly OnixSM64Config Config = pluginConfig;

	public bool Loaded;
	public bool Enabled;
	public bool EnabledFromPlugin;
	public bool Disposed;

	public ISm64Context? Context;
	public ISm64Mario? Mario;
	public SM64Renderer? Renderer;
	public SM64Input? Input;
	public SM64Commands? Commands;

	private readonly object _simLock = new();
	private Thread? _fixedUpdateThread;

	private MarioRenderSnapshot _renderSnapshot;
	private InputEvents _pendingInputEvents;

	private bool _inHud;
	private SM64CollisionWorld? _collisionWorld;
	private Vector3 _worldOffset = Vector3.Zero;

	private bool _pendingReset;
	private Vector3 _pendingResetWorldOffset;

	private WorldSnapshot _latestWorldSnapshot;

	public void Initialize(string romPath, string assetsPath) {
		SM64Lib.LoadSm64Native(assetsPath);

		Onix.Events.Input.Input += OnInput;
		Onix.Events.Common.ChatMessage += OnChatMessage;
		Onix.Events.Common.WorldRender += OnWorldRender;
		Onix.Events.Session.SessionJoined += OnSessionJoined;
		Onix.Events.Session.SessionLeft += Disable;

		Context = Sm64Context.InitFromRom(File.ReadAllBytes(romPath));
		Renderer = new SM64Renderer(Context, Config);
		Input = new SM64Input(Config);
		Commands = new SM64Commands(Config);

		_collisionWorld = new SM64CollisionWorld(Context);
		_collisionWorld.BeginFrame();
		_collisionWorld.AddSafetyFloor(new Vector3(0, 0, 0), 50f, 50f);
		_collisionWorld.Commit();

		Mario = Context.CreateMario(0, 200, 0);
		Mario.Tick();

		_fixedUpdateThread = new Thread(SimulationLoop) {
			Name = "SM64 Simulation",
			IsBackground = true
		};

		_fixedUpdateThread.Start();

		Loaded = true;
		Enabled = true;
	}

	public void Enable() {
		if (!Loaded || !EnabledFromPlugin) return;
		Enabled = true;
	}

	public void Disable() {
		if (!Loaded) return;
		Enabled = false;
	}

	public void OnSessionJoined() {
		if (!Loaded || !EnabledFromPlugin) return;
		Enabled = true;

		lock (_simLock) {
			Vec3 playerPos = Onix.LocalPlayer!.Position;

			_pendingResetWorldOffset = new Vector3(playerPos.X, playerPos.Y, playerPos.Z);
			_pendingReset = true;

			if (Input!.IsControlling) {
				Commands!.DisablePlayerInput();
			}
		}
	}

	private void SimulationLoop() {
		double accumulator = 0.0;
		long prevTicks = Stopwatch.GetTimestamp();

		while (!Disposed) {
			if (!Enabled) continue;

			long nowTicks = Stopwatch.GetTimestamp();
			double elapsed = (double)(nowTicks - prevTicks) / Stopwatch.Frequency;

			prevTicks = nowTicks;

			if (elapsed > 0.1) elapsed = 0.1;

			accumulator += elapsed;

			// Why does it work? Because... fuck you that's why.
			while (accumulator >= Constants.FIXED_TIME_STEP) {
				FixedUpdate();
				accumulator -= Constants.FIXED_TIME_STEP;
			}

			double timeUntilNext = Constants.FIXED_TIME_STEP - accumulator;

			if (timeUntilNext > 0.001) {
				Thread.Sleep((int)(timeUntilNext * 1000.0));
			}
		}
	}

	private void FixedUpdate() {
		if (!Loaded) return;

		lock (_simLock) {
			if (_pendingReset) {
				ApplyReset();
				_pendingReset = false;
			}

			UpdateCollision(_latestWorldSnapshot);
			UpdateWorldElements(_latestWorldSnapshot);

			InputEvents events = default;

			if (Input!.IsControlling) {
				events = Input.UpdateInput(Mario!);
				events.CameraCommand = Input.BuildCameraCommand(Mario!, _worldOffset);

				if (events.PunchFired) {
					(Vec3 lookFrom, Vec3 lookTo, Vec3 ep1, Vec3 ep2) = Input.BuildPunchVectors(Mario!, _worldOffset);

					events.PunchLookFrom = lookFrom;
					events.PunchLookTo = lookTo;
					events.PunchEntityPos1 = ep1;
					events.PunchEntityPos2 = ep2;
				}

				if (events.GroundPoundFired) {
					Vec3 marioPos = SM64Utils.ConvertFromSM64(Mario!.Position);
					events.GroundPoundWorldPos = marioPos + SM64Utils.ToVec3(_worldOffset);
				}
			}

			// this is so stupid
			if (events.PunchFired) _pendingInputEvents.PunchFired = true;
			if (events.GroundPoundFired) _pendingInputEvents.GroundPoundFired = true;
			if (events.CameraCommand != null) _pendingInputEvents.CameraCommand = events.CameraCommand;

			if (events.PunchFired) {
				_pendingInputEvents.PunchLookFrom = events.PunchLookFrom;
				_pendingInputEvents.PunchLookTo = events.PunchLookTo;
				_pendingInputEvents.PunchEntityPos1 = events.PunchEntityPos1;
				_pendingInputEvents.PunchEntityPos2 = events.PunchEntityPos2;
			}

			if (events.GroundPoundFired) {
				_pendingInputEvents.GroundPoundWorldPos = events.GroundPoundWorldPos;
			}

			Mario?.Tick();

			Vec3 marioLocalPos = SM64Utils.ConvertFromSM64(Mario!.Position);

			_renderSnapshot = new MarioRenderSnapshot {
				Mesh = Mario.Mesh,
				WorldOffset = _worldOffset,
				MarioWorldPos = marioLocalPos + SM64Utils.ToVec3(_worldOffset),
			};
		}
	}

	private void ApplyReset() {
		Mario!.Action = MarioAction.ACT_IDLE;
		Mario!.Health = 2176;
		_worldOffset = _pendingResetWorldOffset;
		Mario.Position = new Vec3(0, 0, 0);
	}

	private void UpdateCollision(WorldSnapshot snapshot) {
		_collisionWorld!.BeginFrame();

		Vec3 marioPos = SM64Utils.ConvertFromSM64(Mario!.Position);
		_collisionWorld.AddSafetyFloor(marioPos with { Y = marioPos.Y - 5f });

		List<Vec3> stairPositions = [];

		foreach (StairBlock block in snapshot.StairBlocks) {
			Vector3 worldCenter = SM64Utils.ToVector3(block.Position.Center);
			Vector3 localCenter = worldCenter - _worldOffset;

			stairPositions.Add(block.Position.Center.Floor());
			_collisionWorld.AddWedge(localCenter, Vector3.One, block.Rotation);
		}

		foreach (BoundingBox box in snapshot.NearbyCollisions) {
			if (stairPositions.Contains(box.Center.Floor())) continue;

			Vector3 worldCenter = SM64Utils.ToVector3(box.Center);
			Vector3 localCenter = worldCenter - _worldOffset;
			Vector3 size = SM64Utils.ToVector3(box.Size);

			_collisionWorld.AddCube(localCenter, size);
		}

		_collisionWorld.Commit();
	}

	private void UpdateWorldElements(WorldSnapshot snapshot) {
		if (snapshot.StandingBlockName.Contains("lava")) {
			Mario!.Action = MarioAction.ACT_LAVA_BOOST;
		}

		if (snapshot.StandingBlockName.Contains("fire") && Mario!.Action != MarioAction.ACT_BURNING_GROUND) {
			Mario!.Action = MarioAction.ACT_BURNING_GROUND;
		}

		SM64Lib.sm64_set_mario_water_level(0, snapshot.WaterLevel);
	}

	private bool OnInput(InputKey key, bool isDown) {
		if (!Loaded || !Enabled) return false;

		lock (_simLock) {
			if (!_inHud) {
				Input!.State = new MarioInputState();
			}

			if (Input!.IsControlling && _inHud) {
				if (key == InputKey.Type.W) {
					Input.State.Forward = isDown;
					return isDown;
				}

				if (key == InputKey.Type.A) {
					Input.State.Left = isDown;
					return isDown;
				}

				if (key == InputKey.Type.S) {
					Input.State.Backward = isDown;
					return isDown;
				}

				if (key == InputKey.Type.D) {
					Input.State.Right = isDown;
					return isDown;
				}

				if (key == Config.MarioJumpKey) {
					Input.State.AButton = isDown;
					return isDown;
				}

				if (key == Config.MarioPunchKey) {
					Input.State.BButton = isDown;
					return isDown;
				}

				if (key == Config.MarioCrouchKey) {
					Input.State.ZButton = isDown;
					return isDown;
				}
			}

			if (key == Config.MarioToggleKey && isDown) {
				Input!.State = new MarioInputState();
				Input.UpdateInput(Mario!);
				Input.IsControlling = !Input.IsControlling;

				return true;
			}

			if (key == Config.MarioTeleportKey && isDown) {
				Vec3 playerPos = Onix.LocalPlayer!.Position;

				_pendingResetWorldOffset = new Vector3(playerPos.X, playerPos.Y, playerPos.Z);
				_pendingReset = true;

				return true;
			}
		}

		return false;
	}

	private bool OnChatMessage(string message, string username, string xuid, ChatMessageType type) {
		return Input!.IsControlling && type == ChatMessageType.SystemMessage && message.Contains("§c");
	}

	private void OnWorldRender(RendererWorld gfx, float delta) {
		if (!Loaded || !Enabled) return;
		if (Onix.LocalPlayer == null) return;

		if (Onix.LocalPlayer.PermissionLevel != PlayerPermissionLevel.Operator) {
			EnabledFromPlugin = false;
			Disable();
			throw new Exception("You must be Operator to use this plugin!");
		}

		PlayerSnapshot playerSnap = new() {
			Yaw = Onix.LocalPlayer.RawHeadRot,
			Pitch = Onix.LocalPlayer.Rotation.Pitch
		};

		MarioRenderSnapshot renderSnap = _renderSnapshot;

		InputEvents events;
		bool isControlling;

		lock (_simLock) {
			Input!.LatestPlayerSnapshot = playerSnap;

			events = _pendingInputEvents;
			_pendingInputEvents = default;

			isControlling = Input.IsControlling;
		}

		if (isControlling) {
			Commands!.DisablePlayerInput();

			if (events.CameraCommand != null) {
				Commands.EnqueueCameraCommand(events.CameraCommand);
			}

			if (events.PunchFired) {
				Commands.HandlePunch(
					events.PunchLookFrom,
					events.PunchLookTo,
					events.PunchEntityPos1,
					events.PunchEntityPos2
				);
			}

			if (events.GroundPoundFired) {
				Commands.HandleGroundPound(events.GroundPoundWorldPos);
			}
		} else {
			Commands!.ResetCamera();
		}

		WorldSnapshot worldSnap = SM64Utils.CaptureWorldSnapshot(
			renderSnap.MarioWorldPos,
			renderSnap.WorldOffset
		);

		lock (_simLock) {
			_latestWorldSnapshot = worldSnap;
		}

		_inHud = Onix.Gui.MouseGrabbed;

		if (renderSnap.Mesh?.TriangleData != null) {
			gfx.SetMaterialParameters(new GameMaterialParameters { Light = true, Blending = true });
			Renderer!.RenderMarioMesh(gfx, renderSnap.Mesh, renderSnap.WorldOffset);
		}

		Commands!.Flush();
	}

	public void Dispose() {
		Disposed = true;

		Onix.Events.Input.Input -= OnInput;
		Onix.Events.Common.ChatMessage -= OnChatMessage;
		Onix.Events.Common.WorldRender -= OnWorldRender;
		Onix.Events.Session.SessionJoined -= OnSessionJoined;
		Onix.Events.Session.SessionLeft -= Disable;

		_fixedUpdateThread?.Join(2000);

		Mario?.Dispose();
		Context?.Dispose();
		Commands?.Dispose();
	}
}