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
            __result = Options.Get().GetFloat(Option.DEV_TIMESCALE, 1f);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyArgument(0, "value")]
        [HarmonyPatch("SetDevTimescaleMultiplier")]
        public static bool SetDevTimescaleMultiplier(ref float value)
        {
            var mgr = TimeScaleMgr.Get();
            var Multiplier = mgr.GetTimeScaleMultiplier();

            bool IsDifferent(float val) => val != Multiplier;

            if (!IsDifferent(value))
                return false;

            float new_value;
            if (value == 0.0)
                new_value = 0.0001f;
            else
                new_value = (float) Math.Round(value, 1);

            if (!IsDifferent(new_value))
                return false;

            Options.Get().SetFloat(Option.DEV_TIMESCALE, new_value);
            mgr.SetTimeScaleMultiplier(new_value);

            return false;
        }
    }

    public partial class SceneDebugger
    {
        private sealed class GameplayWindowCloser
        {
            private readonly DebuggerGuiWindow Window_;
            private bool WantFix_;
            private bool WantPrintLog_; //added to prevent ~500 internal calls at same time

            public void Update()
            {
                if (ScriptDebugDisplay.Get().m_isDisplayed || !Options.Get().GetBool(Option.HUD))
                    return;
                var state = GameState.Get();
                this.WantFix_ = state != null && state.GetSlushTimeTracker().GetAccruedLostTimeInSeconds() > GameplayDebug.LOST_SLUSH_TIME_ERROR_THRESHOLD_SECONDS;
                this.TryEndableLogging();
            }

            [Conditional("DEBUG")]
            private void TryEndableLogging()
            {
                if (!this.WantPrintLog_ && !this.WantFix_) this.WantPrintLog_ = true;
            }

            [Conditional("DEBUG")]
            private void LogMessage()
            {
                if (!this.WantPrintLog_) return;
                Logger.Message("GameplayWindow forced to close!", string.Empty);
                this.WantPrintLog_ = false;
            }

            private void Apply()
            {
                if (!this.WantFix_)
                    return;

                this.LogMessage();

                this.WantFix_ = false;
                this.Window_.IsShown = false;
            }

            public GameplayWindowCloser([NotNull] DebuggerGuiWindow window)
            {
                Logger.Message("GameplayWindow closer created!", string.Empty);
                this.Window_ = window;
                this.Update();
                window.OnChanged += this.Apply;
            }

            ~GameplayWindowCloser()
            {
                if (this.Window_ != null) this.Window_.OnChanged -= this.Apply;
            }
        }

        private static GameplayWindowCloser Closer_;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(OnGUI))]
        public static void OnGUI(DebuggerGuiWindow ___m_gameplayWindow)
        {
            if (Closer_ == null /*|| Closer_.Window_ != ___m_gameplayWindow*/)
                Closer_ = new GameplayWindowCloser(___m_gameplayWindow);
            else
                Closer_.Update();
        }
    }
}
