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
    public partial class Entity : LoggerGui.Static<Entity>
    {
#if false
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
            if (__instance.GetControllerId() != Gs.Get().GetFriendlySidePlayer().GetPlayerId())
                return;
            if (__instance.GetCard().GetGoldenMaterial() == null) //all golden
                return;
            //if (!HavePremiumTag(__instance.GetEntityDef(), CUSTOM_PREMIUM_TAG))//all golden except coin
            //    return;

            __result = __instance.GetEntityDef().GetIdealPremiumTag();
            __instance.SetTag(GAME_TAG.PREMIUM, __result);
        }

    }

#endif
    }

    public partial class Entity
    {
        private static bool UseRealGoldenTag()
        {
            return SpectatorManager.Get().IsSpectatingOrWatching || GameMgr.Get().IsBattlegrounds();
        }

        private static TAG_PREMIUM? SetGoldenTag([NotNull] Ent __instance, [NotNull] CallerInfo info)
        {
            if (__instance.GetControllerId() != Gs.Get().GetFriendlySidePlayer().GetPlayerId())
            {
                //Logger.Message($"{__instance} not owned", info);
                return null;
            }

            var original_premium = __instance.GetPremiumType();
            if ((original_premium) != TAG_PREMIUM.NORMAL)
                return original_premium;

            /*if (__instance.GetCard().GetGoldenMaterial() == null) //all golden
            {
                Logger.Message($"{__instance} have no golden material", info);
                return null;
            }*/
            //if (!HavePremiumTag(__instance.GetEntityDef(), CUSTOM_PREMIUM_TAG))//all golden except coin
            //    return;

            var tag_premium = __instance.GetBestPossiblePremiumType();
            if (tag_premium == TAG_PREMIUM.NORMAL)
            {
                Logger.Message($"{__instance} have no golden material", info);
                return null;
            }

            __instance.SetTag(GAME_TAG.PREMIUM, tag_premium);
            Logger.Message($"{__instance} set to {tag_premium}", info);

            return tag_premium;
        }

        private static TAG_PREMIUM? SetHistoryGoldenTag([NotNull] Ent __instance, Net.Entity entity, TAG_PREMIUM? premium_before, [NotNull] CallerInfo info)
        {
            if (UseRealGoldenTag())
                return null;

            const int GAME_TAG_PREMIUM = (int)GAME_TAG.PREMIUM;

            if (!premium_before.HasValue)
            {
                /*
                 how it works:
    
                fake golden hex (or similar) played
                game send it to server
                the server says that the card needs to be turned into a non-golden frog 
                we found the card we play before and force new card be golden again
                 */

                Logger.Message($"Played: {HistoryManager.m_lastPlayedEntity}", info);

                if (HistoryManager.m_lastTargetedEntity == null)
                {
                    if (!HistoryManager.m_lastPlayedEntity.IsSpell())
                    {
                        Logger.Message($"Wrong target");
                        return null;
                    }
                    //it probably evolved
                }
                else if (HistoryManager.m_lastTargetedEntity != __instance)
                {
                    Logger.Message($"Wrong target {HistoryManager.m_lastTargetedEntity}");
                    return null;
                }

                premium_before = (TAG_PREMIUM)HistoryManager.m_lastPlayedEntity.GetTag(GAME_TAG_PREMIUM);
            }

            var change_tags = entity.Tags;
            foreach (var tag in change_tags.Where(tag => tag.Name == GAME_TAG_PREMIUM))
            {
                //fix premium tag changed before
                Logger.Message($"Tag found. {(TAG_PREMIUM)tag.Value} -> {premium_before.Value}", info);
                tag.Value = (int)premium_before.Value;
                goto _END;
            }

            Logger.Message($"Tag added. {premium_before.Value}", info);
            change_tags.Add(new Net.Entity.Tag { Name = GAME_TAG_PREMIUM, Value = (int)premium_before.Value });

        _END:
            return premium_before;
        }
    }

    public partial class Entity
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ent.OnFullEntity))]
        public static void OnFullEntity([NotNull] Ent __instance)
        {
            if (UseRealGoldenTag())
                return;

            //golden cards while game starts
            SetGoldenTag(__instance, new CallerInfoMin());
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ent.OnShowEntity))]
        public static void OnShowEntity([NotNull] Ent __instance)
        {
            if (UseRealGoldenTag())
                return;

            //golden card when it taken from the deck, played by enemy, etc...
            SetGoldenTag(__instance, new CallerInfoMin());
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ent.OnChangeEntity))]
        public static void OnChangeEntity(ref TAG_PREMIUM? __state, [NotNull] Ent __instance,
                                          [NotNull] List<Net.HistChangeEntity> ___m_transformPowersProcessed, Net.HistChangeEntity changeEntity)
        {
            if (___m_transformPowersProcessed.Contains(changeEntity))
            {
                __state = null;
            }
            else
            {
                TAG_PREMIUM? tag = null;
                if (__instance.GetZone() == TAG_ZONE.HAND)
                {
                    tag = __instance.GetBestPossiblePremiumType();
                }

                __state = SetHistoryGoldenTag(__instance, changeEntity.Entity, tag, new CallerInfoMin());
            }
        }

        /*[HarmonyPostfix]
        [HarmonyPatch(nameof(Ent.OnChangeEntity))]
        public static void OnChangeEntity(ref TAG_PREMIUM? __state, [NotNull] Ent __instance)
        {
            if (!__state.HasValue)
                return;

            //testing
            SetGoldenTag(__instance, new CallerInfoMin());
        }*/
    }

    public partial class Entity
    {
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
