using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using hearthstone_ex.Utils;
using Ent = Entity;
using Net = Network;
using Gs = GameState;
using Mgr = CollectionManager;

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

        private static void SetGoldenTag([NotNull] Ent ent, [NotNull] CallerInfo info)
        {
            if (ent.GetControllerId() != Gs.Get().GetFriendlySidePlayer().GetPlayerId())
            {
                //Logger.Message($"{__instance} not owned", info);
                return;
            }

            var original_premium = ent.GetPremiumType();
            if ((original_premium) != TAG_PREMIUM.NORMAL)
                return;

            /*if (__instance.GetCard().GetGoldenMaterial() == null) //all golden
            {
                Logger.Message($"{__instance} have no golden material", info);
                return null;
            }*/
            //if (!HavePremiumTag(__instance.GetEntityDef(), CUSTOM_PREMIUM_TAG))//all golden except coin
            //    return;

            var tag_premium = ent.GetBestPossiblePremiumType();
            if (tag_premium == TAG_PREMIUM.NORMAL)
            {
                Logger.Message($"{ent} have no golden material", info);
                return;
            }

            ent.SetTag(GAME_TAG.PREMIUM, tag_premium);
            Logger.Message($"{ent} set to {tag_premium}", info);
        }

        [NotNull]
        private static string JoinTags([NotNull] IEnumerable<KeyValuePair<int, int>> pairs)
        {
            var strings = pairs.Select(t => new { name = ((GAME_TAG)t.Key).ToString(), value = t.Value })
                               .OrderByDescending(t => t.name)
                               .Select(t => $"{t.name}: {t.value}");
            return string.Join(", ", strings);
        }

        [NotNull]
        private static string JoinTags([NotNull] EntityBase ent)
        {
            return JoinTags(ent.GetTags().GetMap());
        }

        [NotNull]
        private static string JoinTags([NotNull] Net.Entity ent)
        {
            return JoinTags(ent.Tags.Select(t => new KeyValuePair<int, int>(t.Name, t.Value)));
        }

        private static void SetHistoryGoldenTag([NotNull] Ent from, [NotNull] Net.Entity to, [NotNull] CallerInfo info)
        {
            if (UseRealGoldenTag())
                return;

            var from_entdef = from.GetEntityDef();
            var to_entdef = DefLoader.Get().GetAllEntityDefs().Select(p => p.Value).First(e => e.GetCardId() == to.CardID);

            const int GAME_TAG_PREMIUM = (int)GAME_TAG.PREMIUM;

            string Print_from_to(bool detailed = false)
            {
                var builder = new System.Text.StringBuilder();

                if (detailed)
                {
                    builder.AppendLine(from.ToString());
                    builder.AppendLine(JoinTags(from));
                    builder.AppendLine(from_entdef.ToString());
                    builder.AppendLine(JoinTags(from_entdef));
                    builder.AppendLine("---");
                    builder.AppendLine($"[{to}]");
                    builder.AppendLine(JoinTags(to));
                    builder.AppendLine(to_entdef.ToString());
                    builder.Append(JoinTags(to_entdef));
                }
                else
                {
                    builder.AppendLine($"{from} {from_entdef}");
                    builder.Append($"[{to}] {to_entdef}");
                }

                return builder.ToString();
            }

            TAG_PREMIUM ideal_tag;
            if (from.GetZone() == TAG_ZONE.HAND)
            {
                bool from_HasTag(GAME_TAG tag) => from.HasTag(tag) || from_entdef.HasTag(tag);
                bool to_HasTag(GAME_TAG tag) => to.Tags.Any(t => t.Name == (int)tag && t.Value > 0) || to_entdef.HasTag(tag);

                if (to_HasTag(GAME_TAG.COLLECTIBLE))
                {
                    ideal_tag = to_HasTag(GAME_TAG.HAS_DIAMOND_QUALITY) ? TAG_PREMIUM.DIAMOND : TAG_PREMIUM.GOLDEN;
                    Logger.Message($"Tag set to {ideal_tag}. Target card is {GAME_TAG.COLLECTIBLE}\n{Print_from_to()}", info);
                }
                else if (from_HasTag(GAME_TAG.COLLECTIBLE) && to.CardID.StartsWith(from.GetCardId(), StringComparison.Ordinal))
                {
                    ideal_tag = from.GetBestPossiblePremiumType();
                    Logger.Message($"Tag set to {ideal_tag}. Target card is child\n{Print_from_to()}", info);
                }
                else
                {
                    Logger.Message($"Unknown premium type\n{Print_from_to(true)}", info);
                    return;
                }
            }
            else
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
                        Logger.Message($"Target not found\n{Print_from_to(true)}", info);
                        return;
                    }
                    //it probably evolved
                }
                else if (HistoryManager.m_lastTargetedEntity != from)
                {
                    Logger.Message($"Wrong target\n{HistoryManager.m_lastTargetedEntity} !-> {HistoryManager.m_lastTargetedEntity}\n{Print_from_to(true)}", info);
                    return;
                }

                ideal_tag = (TAG_PREMIUM)HistoryManager.m_lastPlayedEntity.GetTag(GAME_TAG_PREMIUM);
                Logger.Message($"Tag {ideal_tag} taken from previously played card", info);
            }

            var change_tags = to.Tags;
            foreach (var tag in change_tags.Where(tag => tag.Name == GAME_TAG_PREMIUM))
            {
                //fix premium tag changed before
                Logger.Message($"Tag found. {(TAG_PREMIUM)tag.Value} -> {ideal_tag}", info);
                tag.Value = (int)ideal_tag;
                return;
            }

            Logger.Message($"Tag added. {ideal_tag}", info);
            change_tags.Add(new Net.Entity.Tag { Name = GAME_TAG_PREMIUM, Value = (int)ideal_tag });
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
        public static void OnChangeEntity([NotNull] Ent __instance, [NotNull] List<Net.HistChangeEntity> ___m_transformPowersProcessed, Net.HistChangeEntity changeEntity)
        {
            if (___m_transformPowersProcessed.Contains(changeEntity))
                return;

            SetHistoryGoldenTag(__instance, changeEntity.Entity, new CallerInfoMin());
        }
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
