using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using System;
using Manager = CollectionManager;
using static hearthstone_ex.Utils.Wrapper;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(Manager))]
	public class CollectionManager : LoggerGui.Static<CollectionManager>
	{
		[HarmonyPrefix]
		[HarmonyArgument(0, "ent")]
		[HarmonyPatch(nameof(RegisterCard))]
		public static void RegisterCard(Manager __instance, [CanBeNull] EntityDef ent, ref TAG_PREMIUM premium)
		{
			//golden heroes on decks

			if (premium != TAG_PREMIUM.NORMAL)
				return;
			if (ent == null)
				return;
			if (!ent.IsHero() || !ent.IsHeroSkin())
				return;

			//Mercenaries
			//Lettuce

			var logger = new Action<string>(msg => Logger.Message(msg, nameof(CollectionManager)));
			var new_premium = ent.GetBestPossiblePremiumType(logger);
			logger($"{ent}: premium tag changed from {premium} to {new_premium} ({Wrapper.ToString(ent.GetTags())})");
			premium = new_premium;
		}
	}
}