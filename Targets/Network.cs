using HarmonyLib;
using JetBrains.Annotations;
using PegasusUtil;
using NetworkHs = Network;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(NetworkHs))]
    public class Network
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(NetworkHs.GetAchievementComplete))]
        public static void GetAchievementComplete([NotNull] AchievementComplete __result)
        {
            AchievementManager.CompleteAchievements = __result;
        }
    }
}
