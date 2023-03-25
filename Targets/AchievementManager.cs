using System;
using System.Diagnostics;
using HarmonyLib;
using hearthstone_ex.Utils;
using PegasusUtil;
using Manager = Hearthstone.Progression.AchievementManager;
using Status = Hearthstone.Progression.AchievementManager.AchievementStatus;

namespace hearthstone_ex.Targets
{
	public partial class AchievementManager : LoggerGui.Static<AchievementManager>
	{
		[Conditional("DEBUG")]
		private static void LogAchievementName(int id)
		{
			string Msg( )
			{
				var record = GameDbf.Achievement.GetRecord(id);
				if (record == null)
					return "not found!";
				var recordName = record.Name.GetString( );
				return string.IsNullOrEmpty(recordName) ? "have incorrect name!" : $"and name \"{recordName}\" detected.";
			}

			Logger.Message($"Achievement with record id \"{id}\" " + Msg( ));
		}

		public static void Claim(Manager mgr, int id)
		{
			try
			{
				mgr.AckAchievement(id);
				if (!mgr.ClaimAchievementReward(id))
					return; //probably claimed already

				LogAchievementName(id);
				Logger.Message("Achievement successfully claimed!");
			}
			catch (Exception e)
			{
				LogAchievementName(id);
				Logger.Message($"Unable to claim achievement: ---- {e} ----");
			}
		}

		public static void Claim(int id) => Claim(Manager.Get( ), id);
	}

	[HarmonyPatch(typeof(Manager))]
	public partial class AchievementManager
	{
		public static AchievementComplete CompleteAchievements;

		[HarmonyPostfix]
		[HarmonyPatch(nameof(OnAchievementComplete))]
		public static void OnAchievementComplete( Manager __instance)
		{
			var ids = CompleteAchievements?.AchievementIds;
			if (ids == null || ids.Count == 0)
				return;
			CompleteAchievements.AchievementIds.ForEach(id => Claim(__instance, id));
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

			Claim(__instance, id);
		}
	}
}
