using HarmonyLib;
using BaconShop = TB_BaconShop;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(BaconShop))]
	public class TB_BaconShop
	{
		[HarmonyPrefix]
		[HarmonyPatch(nameof(GetBobActor))]
		public static bool GetBobActor(ref Actor __result)
		{
			//STFU Bob retard!!!

			__result = null;
			return HookInfo.SKIP_ORIGINAL;
		}
	}
}