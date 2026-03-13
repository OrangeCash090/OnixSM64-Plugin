using OnixRuntime.Api;
using OnixRuntime.Api.Maths;
using OnixRuntime.Api.World;
using OnixSM64.Library;

namespace OnixSM64.Runtime;

public class SM64Commands(OnixSM64Config config) : IDisposable {
	private readonly CommandQueue _queue = new();

	public void Flush() {
		_queue.AdvanceQueue();
	}

	public void EnqueueCameraCommand(string command) {
		_queue.QueueCommand(command);
	}

	public void ResetCamera() {
		_queue.QueueCommand("/camera @s clear");
		_queue.QueueCommand("/inputpermission set @s movement enabled");
	}

	public void DisablePlayerInput() {
		_queue.QueueCommand("/gamerule sendcommandfeedback false");
		_queue.QueueCommand("/inputpermission set @s movement disabled");
	}

	public void HandlePunch(Vec3 lookFrom, Vec3 lookTo, Vec3 entityPos1, Vec3 entityPos2) {
		if (config.MarioBreaksBlocks) {
			RaycastResult raycast = Onix.Region!.Raycast(lookFrom, lookTo, BlockShapeType.Collision);

			if (raycast.BlockPosition != BlockPos.Zero) {
				_queue.QueueCommand(
					$"/setblock {raycast.BlockPosition.X} {raycast.BlockPosition.Y} {raycast.BlockPosition.Z} air [] destroy"
				);
			}
		}

		if (config.MarioHitsEntities) {
			// Don't ask...
			_queue.QueueCommand($"/execute positioned {entityPos1.X} {entityPos1.Y} {entityPos1.Z} run damage @e[r=2] 5 entity_attack entity @s");
			_queue.QueueCommand($"/execute positioned {entityPos2.X} {entityPos2.Y} {entityPos2.Z} run damage @e[r=2] 5 entity_attack entity @s");
		}
	}

	public void HandleGroundPound(Vec3 marioWorldPos) {
		_queue.QueueCommand(
			$"/summon ender_crystal {marioWorldPos.X} {marioWorldPos.Y} {marioWorldPos.Z} 0 0 minecraft:crystal_explode"
		);
	}

	public void Dispose() {
		_queue.Clear();
	}
}