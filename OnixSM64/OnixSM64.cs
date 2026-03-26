using OnixRuntime.Api;
using OnixRuntime.Plugin;
using OnixSM64.Runtime;

namespace OnixSM64 {
	public class OnixSM64 : OnixPluginBase {
		public static OnixSM64 Instance { get; private set; } = null!;
		public static OnixSM64Config Config { get; private set; } = null!;
		
		public static SM64World? World;

		public OnixSM64(OnixPluginInitInfo initInfo) : base(initInfo) {
			Instance = this;
			base.DisablingShouldUnloadPlugin = false;
			
			#if DEBUG
			//base.WaitForDebuggerToBeAttached();
			#endif
		}

		protected override void OnLoaded() {
			Config = new OnixSM64Config(PluginDisplayModule, true);
			World = new SM64World(Config);
		}

		protected override void OnEnabled() {
			if (World != null) {
				World.EnabledFromPlugin = true;
				World.Enable();
			}
			
			if (World is { Loaded: false }) {
				if (Config.RomPath.Text != "" && File.Exists(Config.RomPath.Text)) {
					World.Initialize(Config.RomPath.Text, PluginAssetsPath + "\\");
				} else {
					throw new Exception("No ROM file found! Please select a ROM file in the plugin settings and re-enable.");
				}
			}
		}

		protected override void OnDisabled() {
			if (World != null) {
				World.EnabledFromPlugin = false;
				World.Disable();
			}
		}

		protected override void OnUnloaded() {
			World?.Dispose();
		}
	}
}