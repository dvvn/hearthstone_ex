using HarmonyLib;
using hearthstone_ex.Utils;
using Dialog = global::ReconnectHelperDialog;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Dialog))]
    public class ReconnectHelperDialog : LoggerFile.Static<ReconnectHelperDialog>
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ChangeState_FullResetRequired))]
        public static bool ChangeState_FullResetRequired()
        {
            //fuck you blizzard. i dont want to restart game after reconnect

            var mgr = ReconnectMgr.Get();
            if (mgr.UpdateRequired)
                return true;

            Logger.Message("the game wants to restart");
            mgr.FullResetRequired = false;
            //mgr.ReconnectToGameFromLogin();
            return false;
        }
    }
}
