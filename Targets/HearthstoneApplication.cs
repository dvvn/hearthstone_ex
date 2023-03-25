using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using hearthstone_ex.Utils;
using App = Hearthstone.HearthstoneApplication;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(App))]
	public class HearthstoneApplication : LoggerFile.Static<HearthstoneApplication>
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(RunStartup))]
		public static void RunStartup(App __instance)
		{
			Loader.OnGameStartup(__instance);
			Logger.Message("Started!", sourceLineNumber: 0);
			Logger.Message(RuntimeInformation.FrameworkDescription, sourceLineNumber: 0);
			Logger.Message($"CLR: {Environment.Version}", sourceLineNumber: 0);
		}

		private static ApplicationMode? _applicationModeOwerriden;

		public class AppModeOwerriden : IDisposable
		{
			private readonly ApplicationMode? _prevMode;

			public AppModeOwerriden(ApplicationMode applicationMode)
			{
				_prevMode = _applicationModeOwerriden;
				_applicationModeOwerriden = applicationMode;
				Logger.Message($"App mode forced to {applicationMode}");
			}

			public void Dispose()
			{
				_applicationModeOwerriden = _prevMode;
				Logger.Message($"App mode restored to {_prevMode?.ToString() ?? "default"}");
			}
		}

		public static AppModeOwerriden OwerrideAppMode(ApplicationMode mode) => new AppModeOwerriden(mode);

		[HarmonyPrefix]
		[HarmonyPatch(nameof(App.GetMode))]
		public static bool GetMode(ref ApplicationMode __result)
		{
			if (_applicationModeOwerriden.HasValue)
			{
				__result = _applicationModeOwerriden.Value;
				return HookInfo.SKIP_ORIGINAL;
			}

			return HookInfo.CALL_ORIGINAL;
		}
	}
}