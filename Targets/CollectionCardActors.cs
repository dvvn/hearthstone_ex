using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using Actors = CollectionCardActors;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(Actors))]
	public class CollectionCardActors : LoggerGui.Static<Actors>
	{
		//golden hero skins in collection, on arena and other places

		[HarmonyPrefix]
		[HarmonyPatch(nameof(Actors.AddCardActor))]
		public static void AddCardActor([NotNull] Actor actor)
		{
			if (actor.GetPremium( ) != TAG_PREMIUM.NORMAL)
				return;
			var ent = actor.GetEntity( );
			if (ent == null)
				return;

			if (!ent.IsHero( ) || !ent.IsHeroPower( ))
				return;

			var premium = ent.GetBestPossiblePremiumType(msg => Logger.Message(msg, nameof(CollectionCardActors)));
			actor.SetPremium(premium);
		}
	}
}
