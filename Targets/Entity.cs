using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using hearthstone_ex.Utils;
using Ent = Entity;
using Net = Network;
using State = GameState;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Ent))]
    public class Entity : LoggerGui.Static<Entity>
    {
        private static void SetGoldenTag([NotNull] Ent __instance, [NotNull] CallerInfo info)
        {
            //golden heroes & cards in game

            var __result = __instance.GetPremiumType();

            if (__result != TAG_PREMIUM.NORMAL)
            {
                Logger.Message($"not set: type is {__result}", info);
                return;
            }

            if (SpectatorManager.Get().IsSpectatingOrWatching)
                return;
            if (GameMgr.Get().IsBattlegrounds())
                return;
            if (__instance.GetControllerId() != State.Get().GetFriendlySidePlayer().GetPlayerId())
            {
                Logger.Message($"not set: card not owned", info);
                return;
            }

            if (__instance.GetCard().GetGoldenMaterial() == null) //all golden
            {
                Logger.Message($"not set: no golden material", info);
                return;
            }
            //if (!HavePremiumTag(__instance.GetEntityDef(), CUSTOM_PREMIUM_TAG))//all golden except coin
            //    return;

            __result = __instance.GetEntityDef().GetIdealPremiumTag();
            __instance.SetTag(GAME_TAG.PREMIUM, __result);

            Logger.Message($"set to {__result}", info);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ent.OnFullEntity))]
        public static void OnFullEntity([NotNull] Ent __instance)
        {
            SetGoldenTag(__instance, new CallerInfoMin());
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ent.OnShowEntity))]
        public static void OnShowEntity([NotNull] Ent __instance)
        {
            SetGoldenTag(__instance, new CallerInfoMin());
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(Ent.OnChangeEntity))]
        public static void OnChangeEntity([NotNull] Ent __instance, [NotNull] List<Net.HistChangeEntity> ___m_transformPowersProcessed, Net.HistChangeEntity changeEntity)
        {
            if (___m_transformPowersProcessed.Contains(changeEntity))
                return;

            /*
             todo: find a way to check can we play hex or similar or it evolved etc
             */
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
