using System.Collections.Concurrent;
using OnixRuntime.Api;

namespace OnixSM64.Library;

public class CommandQueue {
	private readonly ConcurrentQueue<string> _commands = new();

	public int BatchAmount { get; set; } = 10;

	public void QueueCommand(string command) {
		_commands.Enqueue(command);
	}
	
	public void AdvanceQueue() {
		int remaining = BatchAmount;

		while (remaining-- > 0 && _commands.TryDequeue(out string? cmd)) {
			Onix.Game.ExecuteCommand(cmd);
		}
	}

	public void Clear() {
		_commands.Clear();
	}
}
