using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using hearthstone_ex.Utils;
using Ent = Entity;
using Net = Network;
using Gs = GameState;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Ent))]
    public class Entity : LoggerGui.Static<Entity>
    {
        private static void SetGoldenTag([NotNull] Ent __instance, [NotNull] CallerInfo info)
        {
            var __result = __instance.GetPremiumType();
            if (__result != TAG_PREMIUM.NORMAL)
                return;

            if (SpectatorManager.Get().IsSpectatingOrWatching)
                return;
            if (GameMgr.Get().IsBattlegrounds())
                return;
            if (__instance.GetControllerId() != Gs.Get().GetFriendlySidePlayer().GetPlayerId())
            {
                Logger.Message($"({__instance}) not owned", info);
                return;
            }

            if (__instance.GetCard().GetGoldenMaterial() == null) //all golden
            {
                Logger.Message($"({__instance}) have no golden material", info);
                return;
            }
            //if (!HavePremiumTag(__instance.GetEntityDef(), CUSTOM_PREMIUM_TAG))//all golden except coin
            //    return;

            __result = __instance.GetEntityDef().GetIdealPremiumTag();
            __instance.SetTag(GAME_TAG.PREMIUM, __result);

            Logger.Message($"({__instance}) set to {__result}", info);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ent.OnFullEntity))]
        public static void OnFullEntity([NotNull] Ent __instance)
        {
            //golden cards while game starts
            SetGoldenTag(__instance, new CallerInfoMin());
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ent.OnShowEntity))]
        public static void OnShowEntity([NotNull] Ent __instance)
        {
            //golden card when it taken from the deck, played by enemy, etc...
            SetGoldenTag(__instance, new CallerInfoMin());
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ent.OnChangeEntity))]
        public static void OnChangeEntity([NotNull] Ent __instance, [NotNull] List<Net.HistChangeEntity> ___m_transformPowersProcessed, Net.HistChangeEntity changeEntity)
        {
            if (___m_transformPowersProcessed.Contains(changeEntity))
                return;

            /*
             how it works:

            fake golden hex (or similar) played
            game send it to server
            the server says that the card needs to be turned into a non-golden frog 
            we found the card we play before and force new card be golden again
             */

            Logger.Message($"Played: {HistoryManager.m_lastPlayedEntity}");

            if (HistoryManager.m_lastTargetedEntity == null)
            {
                if (!HistoryManager.m_lastPlayedEntity.IsSpell())
                {
                    Logger.Message($"Wrong target");
                    return;
                }
                //it probably evolved
            }
            else if (HistoryManager.m_lastTargetedEntity != __instance)
            {
                Logger.Message($"Wrong target {HistoryManager.m_lastTargetedEntity}");
                return;
            }

            const int GAME_TAG_PREMIUM = (int)GAME_TAG.PREMIUM;
            var premium_before = HistoryManager.m_lastPlayedEntity.GetTag(GAME_TAG_PREMIUM);

            var change_tags = changeEntity.Entity.Tags;
            foreach (var tag in change_tags.Where(tag => tag.Name == GAME_TAG_PREMIUM))
            {
                //fix premium tag changed before
                Logger.Message($"Tag found. {(TAG_PREMIUM)tag.Value} -> {(TAG_PREMIUM)premium_before}");
                tag.Value = premium_before;
                return;
            }

            Logger.Message($"Tag added. {(TAG_PREMIUM)premium_before}");
            change_tags.Add(new Net.Entity.Tag { Name = GAME_TAG_PREMIUM, Value = premium_before });
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
