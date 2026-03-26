using OnixRuntime.Api.Inputs;
using OnixRuntime.Api.Maths;
using OnixRuntime.Api.OnixClient;
using OnixRuntime.Api.OnixClient.Settings;
using OnixSM64.Library;

namespace OnixSM64 {
    public partial class OnixSM64Config : OnixModuleSettingRedirector {
	    [Button(nameof(SelectRomFileFunc), "Open")] [Name("Select Rom File", "Click this button to select your SM64 rom file.")]
	    public partial OnixSetting.SettingChangedDelegate SelectRomFile { get; set; }

	    [Value("")]
	    [Name("Rom Path", "The path to your SM64 rom file.")]
	    public partial OnixTextbox RomPath { get; set; }

	    public void SelectRomFileFunc() {
		    string? romPath = NativeFileDialog.ShowZ64FilePicker();

		    if (!string.IsNullOrEmpty(romPath) && File.Exists(romPath)) {
			    RomPath.Text = romPath;
		    }
	    }

	    [Category("Mario Control Settings")]
	    [Value(true)]
	    [Name("Use WASD", "If enabled, WASD is used to move Mario. Disabled means using Arrow Keys.")]
	    public partial bool MarioUsesWASD { get; set; }
	    
	    [Value(InputKey.Type.Z)]
	    [Name("Toggle Mario Key", "The key used to toggle between controlling Mario. Default is Z.")]
	    public partial InputKey MarioToggleKey { get; set; }

	    [Value(InputKey.Type.X)]
	    [Name("Teleport Mario Key", "The key used to teleport Mario back to the player. Default is X.")]
	    public partial InputKey MarioTeleportKey { get; set; }

	    [Value(InputKey.Type.LMB)]
	    [Name("Mario Punch Key", "The key used to make Mario punch. Default is Left Click.")]
	    public partial InputKey MarioPunchKey { get; set; }

	    [Value(InputKey.Type.Space)]
	    [Name("Mario Jump Key", "The key used to make Mario jump. Default is Space.")]
	    public partial InputKey MarioJumpKey { get; set; }

	    [Value(InputKey.Type.Shift)]
	    [Name("Mario Crouch Key", "The key used to make Mario crouch. Default is Shift.")]
	    public partial InputKey MarioCrouchKey { get; set; }

	    [Category("Mario Behavior Settings")]
	    [Value(false)]
	    [Name("Punch Breaks Blocks", "If enabled, Mario breaks blocks he is looking at when punching.")]
	    public partial bool MarioBreaksBlocks { get; set; }

	    [Value(false)]
	    [Name("Punch Hits Entities", "If enabled, Mario hits entities he is looking at when punching.")]
	    public partial bool MarioHitsEntities { get; set; }

	    [Value(false)]
	    [Name("Ground Pound Destroys Blocks", "If enabled, Mario destroys blocks when he ground-pounds.")]
	    public partial bool MarioGroundPoundGriefs { get; set; }

	    [Category("Mario Render Settings")]
	    [Value(false)]
	    [Name("Custom Mario Color", "If enabled, you can set the color of Mario's clothes.")]
	    public partial bool MarioHasCustomColor { get; set; }
	    
	    [Value(1, 0, 0, 1)]
	    [Name("Mario Shirt Color", "The color of Mario's shirt.")]
	    public partial ColorF MarioShirtColor { get; set; }

	    [Value(0, 0, 1, 1)]
	    [Name("Mario Pants Color", "The color of Mario's pants.")]
	    public partial ColorF MarioPantsColor { get; set; }

	    [Value(0.44705883f, 0.10980392f, 0.05490196f, 1)]
	    [Name("Mario Shoes Color", "The color of Mario's shoes.")]
	    public partial ColorF MarioShoesColor { get; set; }

	    [Value(1, 1, 1, 1)]
	    [Name("Mario Gloves Color", "The color of Mario's gloves.")]
	    public partial ColorF MarioGlovesColor { get; set; }

	    [Category("Mario World Settings")]
	    [Value(false)]
	    [Name("Mario Reacts To Water", "If enabled, mario swims in water.")]
	    public partial bool MarioWaterReaction { get; set; }

	    [Value(false)]
	    [Name("Mario Reacts To Lava", "If enabled, mario burns in lava.")]
	    public partial bool MarioLavaReaction { get; set; }

	    [Value(false)]
	    [Name("Stairs Become Slippery", "If enabled, mario slides down stair blocks.")]
	    public partial bool MarioStairBlocks { get; set; }
    }
}