using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using System;
using Manager = CollectionManager;

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
			if (!ent.IsHero( ) || !ent.IsHeroSkin( ))
				return;

			var newPremium = ent.GetBestPossiblePremiumType(msg => Logger.Message(msg));
			Logger.Message($"{ent}: premium tag changed from {premium} to {newPremium}\n" + ent.GetTags( ).JoinTags( ));
			premium = newPremium;
		}
	}
}
