using HarmonyLib;
using hearthstone_ex.Utils;
using Kicker = InactivePlayerKicker;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(Kicker))]
	public class InactivePlayerKicker : LoggerGui.Static<InactivePlayerKicker>
	{
		//dont kick me for inactivity!!!

		[HarmonyPrefix]
		[HarmonyPatch(nameof(CanCheckForInactivity))]
		public static bool CanCheckForInactivity(ref bool __result)
		{
			__result = false;
			return false;
		}
	}
}
