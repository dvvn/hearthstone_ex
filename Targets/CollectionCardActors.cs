using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using Actors = CollectionCardActors;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Actors))]
    public class CollectionCardActors
    {
        //golden hero skins in collection, on arena end other places

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Actors.AddCardActor))]
        public static void AddCardActor([NotNull] Actor actor)
        {
            if (actor.GetPremium() != TAG_PREMIUM.NORMAL)
                return;
            var ent = actor.GetEntityDef();
            // ReSharper disable once UseNullPropagationWhenPossible
            if (ent == null)
                return;
            if (!ent.IsHero() && !ent.IsHeroPower())
                return;
            if (!ent.HavePremiumTag(TAG_PREMIUM.GOLDEN))
                return;

            actor.SetPremium(ent.GetIdealPremiumTag());
        }
    }
}
