using System;
using System.Diagnostics;
using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using PegasusUtil;
using Manager = Hearthstone.Progression.AchievementManager;
using Status = Hearthstone.Progression.AchievementManager.AchievementStatus;

namespace hearthstone_ex.Targets
{
    public partial class AchievementManager : LoggerGui.Static<AchievementManager>
    {
        [Conditional("DEBUG")]
        private static void LogAchievementName(int id, [NotNull] CallerInfo info)
        {
            var record = GameDbf.Achievement.GetRecord(id);
            string text;

            if (record == null)
                text = "not found!";
            else
            {
                var record_name = record.Name.GetString();
                text = string.IsNullOrEmpty(record_name) ? "have incorrect name!" : $"and name \"{record_name}\" detected.";
            }

            Logger.Message($"Achievement with record id \"{id}\" {text}", info);
        }

        public static void Claim(Manager mgr, int id, CallerInfo info)
        {
            try
            {
                mgr.AckAchievement(id);
                if (!mgr.ClaimAchievementReward(id)) return; //probably claimed already

                LogAchievementName(id, info);
                Logger.Message("Achievement successfully claimed!", info);
            }
            catch (Exception e)
            {
                LogAchievementName(id, info);
                Logger.Message($"Unable to claim achievement: ---- {e} ----", info);
            }
        }

        public static void Claim(int id, CallerInfo info) => Claim(Manager.Get(), id, info);
    }

    [HarmonyPatch(typeof(Manager))]
    public partial class AchievementManager
    {
        public static AchievementComplete CompleteAchievements;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(OnAchievementComplete))]
        public static void OnAchievementComplete([NotNull] Manager __instance)
        {
            var ids = CompleteAchievements?.AchievementIds;
            if (ids == null || ids.Count == 0)
                return;
            var debug_info = new CallerInfoMin();
            CompleteAchievements.AchievementIds.ForEach(id => Claim(__instance, id, debug_info));
        }

        [HarmonyPostfix]
        [HarmonyArgument(0, "id")]
        [HarmonyPatch(nameof(UpdateStatus))]
        public static void UpdateStatus(Manager __instance, int id, Status oldStatus, Status newStatus)
        {
            //claim when game loads
            //game also have OnStatusChanged event, but this do absolute same thing

            //Logger.Message($"id {id}, old status {oldStatus}, new status {newStatus})");

            if (newStatus != Status.COMPLETED)
                return;

            Claim(__instance, id, new CallerInfoMin());
        }
    }
}
