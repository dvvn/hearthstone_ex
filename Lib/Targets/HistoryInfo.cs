#if false
using HarmonyLib;
using hearthstone_ex.Utils;
using Info = HistoryInfo;

namespace hearthstone_ex.Targets
{
	//this never called

	//[HarmonyPatch(typeof(Info))]
	public class HistoryInfo : LoggerGui.Static<HistoryInfo>
	{
		[HarmonyPrefix]
		[HarmonyPatch(nameof(Info.SetDied))]
		public static void SetDied(Info __instance, bool set)
		{
			Logger.Message($"Called {set}");

			if (!set)
				return;

			var ent = __instance.GetDuplicatedEntity( ) ?? __instance.GetOriginalEntity( );
			var removed = Entity.RemoveFakePremiumCard(ent.GetEntityId( ));

			string RemovedMsg( )
			{
				return removed ? string.Empty : " NOT";
			}

			Logger.Message($"{ent}{RemovedMsg( )} removed!");
		}
	}
}
#endif