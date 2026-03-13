using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OnixSM64.Runtime;

public static class SM64Lib {
	[DllImport("kernel32", SetLastError = true)]
	private static extern IntPtr LoadLibrary(string lpFileName);

	[DllImport("kernel32", SetLastError = true)]
	private static extern bool FreeLibrary(IntPtr hModule);

	[DllImport("kernel32", SetLastError = true)]
	private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

	private static bool _nativeLoaded;
	private static IntPtr _moduleHandle;

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void Sm64SetWaterLevelDelegate(int marioId, int yLevel);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void Sm64SetStateDelegate(int marioId, uint flags);

	public static Sm64SetWaterLevelDelegate sm64_set_mario_water_level { get; private set; } = null!;
	public static Sm64SetStateDelegate sm64_set_mario_state { get; private set; } = null!;

	public static void LoadSm64Native(string assetsPath) {
		if (_nativeLoaded) return;
		_nativeLoaded = true;

		string runtimePath = assetsPath.Replace("assets\\", "") + "runtimes\\win-x64\\native\\";
		string dllPath = Path.Combine(runtimePath, "sm64.dll");

		if (!File.Exists(dllPath))
			throw new FileNotFoundException("sm64.dll not found", dllPath);

		_moduleHandle = LoadLibrary(dllPath);

		if (_moduleHandle == IntPtr.Zero)
			throw new Win32Exception(Marshal.GetLastWin32Error());

		sm64_set_mario_water_level = GetFunction<Sm64SetWaterLevelDelegate>("sm64_set_mario_water_level");
		sm64_set_mario_state = GetFunction<Sm64SetStateDelegate>("sm64_set_mario_state");
	}

	public static void UnloadSm64Native() {
		if (!_nativeLoaded) return;

		sm64_set_mario_water_level = null!;
		sm64_set_mario_state = null!;

		try {
			FreeLibrary(_moduleHandle);
		} catch {
			// ignored
		}

		_moduleHandle = IntPtr.Zero;
		_nativeLoaded = false;
	}

	private static T GetFunction<T>(string name) where T : Delegate {
		IntPtr ptr = GetProcAddress(_moduleHandle, name);

		if (ptr == IntPtr.Zero)
			throw new EntryPointNotFoundException($"Could not find '{name}' in sm64.dll");

		return Marshal.GetDelegateForFunctionPointer<T>(ptr);
	}
}