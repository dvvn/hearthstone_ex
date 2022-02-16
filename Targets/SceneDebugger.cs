using System;
using System.Diagnostics;
using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using Debugger = SceneDebugger;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(Debugger))]
	public partial class SceneDebugger : LoggerGui.Static<SceneDebugger>
	{
		[HarmonyPrefix]
		[HarmonyPatch("GetDevTimescaleMultiplier")]
		public static bool GetDevTimescaleMultiplier(ref float __result)
		{
			__result = Options.Get( ).GetFloat(Option.DEV_TIMESCALE, 1f);
			return false;
		}

		[HarmonyPrefix]
		[HarmonyArgument(0, "value")]
		[HarmonyPatch(nameof(SetDevTimescaleMultiplier))]
		public static bool SetDevTimescaleMultiplier(ref float value)
		{
			var mgr = TimeScaleMgr.Get( );
			var multiplier = mgr.GetTimeScaleMultiplier( );

			bool IsDifferent(float val) => val != multiplier;

			if (!IsDifferent(value))
				return false;

			var new_value = value == 0.0 ? 0.0001f : (float) Math.Round(value, 1);
			if (!IsDifferent(new_value))
				return false;

			Options.Get( ).SetFloat(Option.DEV_TIMESCALE, new_value);
			mgr.SetTimeScaleMultiplier(new_value);

			return false;
		}
	}

	public partial class SceneDebugger
	{
		private sealed class GameplayWindowCloser
		{
			private readonly DebuggerGuiWindow _window;
			private bool _wantFix;
			private bool _wantPrintLog; //added to prevent ~500 internal calls at same time

			public void Update( )
			{
				if (ScriptDebugDisplay.Get( ).m_isDisplayed || !Options.Get( ).GetBool(Option.HUD))
					return;
				var state = GameState.Get( );
				_wantFix = state != null && state.GetSlushTimeTracker( ).GetAccruedLostTimeInSeconds( ) > GameplayDebug.LOST_SLUSH_TIME_ERROR_THRESHOLD_SECONDS;
				TryEndableLogging( );
			}

			[Conditional("DEBUG")]
			private void TryEndableLogging( )
			{
				if (_wantPrintLog || _wantFix)
					return;
				_wantPrintLog = true;
			}

			[Conditional("DEBUG")]
			private void LogMessage( )
			{
				if (!_wantPrintLog)
					return;
				Logger.Message("GameplayWindow forced to close!", string.Empty);
				_wantPrintLog = false;
			}

			private void Apply( )
			{
				if (!_wantFix)
					return;

				LogMessage( );

				_wantFix = false;
				_window.IsShown = false;
			}

			public GameplayWindowCloser([NotNull] DebuggerGuiWindow window)
			{
				Logger.Message("GameplayWindow closer created!", string.Empty);
				_window = window;
				Update( );
				window.OnChanged += Apply;
			}

			~GameplayWindowCloser( )
			{
				if (_window == null)
					return;
				_window.OnChanged -= Apply;
			}
		}

		private static GameplayWindowCloser _windowCloser;

		[HarmonyPrefix]
		[HarmonyPatch(nameof(OnGUI))]
		public static void OnGUI(DebuggerGuiWindow ___m_gameplayWindow)
		{
			if (_windowCloser == null)
				_windowCloser = new GameplayWindowCloser(___m_gameplayWindow);
			else
				_windowCloser.Update( );
		}
	}
}
