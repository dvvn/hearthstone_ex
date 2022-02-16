using HarmonyLib;
using hearthstone_ex.Utils;
using Button = EndTurnButton;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(Button))]
	public class EndTurnButton : LoggerGui.Static<EndTurnButton>
	{
#if false
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SetStateToYourTurn))]
        public static void SetStateToYourTurn([NotNull] Button __instance)
        {
            IEnumerator DoFlash()
            {
                var handle = Process.GetCurrentProcess().MainWindowHandle;
                var app = Hearthstone.HearthstoneApplication.Get();
                Logger.Message($"Trying to flash game window {handle}");

                if (app.HasFocus())
                    Logger.Message("Unable to flash window: game is focused");
                else
                {
                    Logger.Message("Window flashed");
                    FlashWindow.Flash();
                    while (!app.HasFocus() && !__instance.HasNoMorePlays()) yield return null;
                    Logger.Message("Window flash done");
                    FlashWindow.Stop();
                }
            }

            __instance.StartCoroutine(DoFlash());
        }
#endif
		[HarmonyPostfix]
		[HarmonyPatch(nameof(SetStateToNoMorePlays))]
		public static void SetStateToNoMorePlays( )
		{
			Logger.Message("End of the turn detected");
			InputManager.Get( ).DoEndTurnButton( );
		}
	}
}
