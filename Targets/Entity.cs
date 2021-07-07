using HarmonyLib;
using JetBrains.Annotations;
using hearthstone_ex.Utils;
using Ent = Entity;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Ent))]
    public class Entity : LoggerGui.Static<Entity>
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ent.GetPremiumType))]
        public static void GetPremiumType(ref TAG_PREMIUM __result, Ent __instance)
        {
            //golden heroes & cards in game

            if (__result != TAG_PREMIUM.NORMAL)
                return;
            if (SpectatorManager.Get().IsSpectatingOrWatching)
                return;
            if (GameMgr.Get().IsBattlegrounds())
                return;
            //if (__instance.HasQueuedChangeEntity())
            //    return;
            if (__instance.GetControllerId() != GameState.Get().GetFriendlySidePlayer().GetPlayerId())
                return;
            if (__instance.GetCard().GetGoldenMaterial() == null) //all golden
                return;
            //if (!HavePremiumTag(__instance.GetEntityDef(), CUSTOM_PREMIUM_TAG))//all golden except coin
            //    return;

            __result = __instance.GetEntityDef().GetIdealPremiumTag();
            __instance.SetTag(GAME_TAG.PREMIUM, __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ent.GetCardTextBuilder))]
        public static bool GetCardTextBuilder(ref CardTextBuilder __result, [NotNull] Ent __instance)
        {
            //remove shit from logs

            CardTextBuilder GetResult()
            {
                var end_def = __instance.GetEntityDef();
                if (end_def != null)
                {
                    var builder = end_def.GetCardTextBuilder();
                    if (builder != null)
                        return builder;
                }

                return CardTextBuilder.GetFallbackCardTextBuilder();
            }

            __result = GetResult();
            return false;
        }
    }
}
