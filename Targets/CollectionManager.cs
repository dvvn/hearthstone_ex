using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
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
            if (!ent.IsHero() && !ent.IsHeroPower())
                return;

            premium = ent.GetBestPossiblePremiumType(msg => Logger.Message(msg, nameof(CollectionManager)));
        }
    }

#if false
//done by DialogManager

    [HarmonyPatch(typeof(DisenchantButton))]
    public class DisenchantButton_EX : LoggerGui.Static<DisenchantButton_EX>
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnReadyToStartDisenchant")]
        public static bool OnReadyToStartDisenchant( /*ref DisenchantButton __instance*/)
        {
            //sell cards without confirms

            var mgr = CraftingManager.Get();
            if (mgr.IsCardShowing())
                mgr.DisenchantButtonPressed();

            return false;
        }
    }
#endif
}
