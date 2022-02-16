using HarmonyLib;
using JetBrains.Annotations;
using Handler = EnemyEmoteHandler;

namespace hearthstone_ex.Targets
{
	//todo emoteHandler.HideEmotes when game starts

	[HarmonyPatch(typeof(Handler))]
	public class EnemyEmoteHandler
	{
		[HarmonyPrefix]
		[HarmonyPatch(nameof(Handler.Get))]
		public static bool Get([CanBeNull] ref Handler __result)
		{
			//enemy emotes alawys disabled

			__result = null;
			return false;
		}
	}
}
