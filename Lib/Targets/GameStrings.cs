using HarmonyLib;
using HsString = GameStrings;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(HsString))]
	public class GameStrings
	{
		/*[HarmonyPostfix]
		[HarmonyPatch(nameof(LoadAll))]
		public static void LoadAll()
		{
			DialogManager.Load();
		}*/

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Job_LoadAll))]
		public static void Job_LoadAll()
		{
			DialogManager.Load();
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(ReloadAll))]
		public static void ReloadAll()
		{
			DialogManager.Load();
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(LoadNative))]
		public static void LoadNative()
		{
			DialogManager.Load();
		}
	}
}