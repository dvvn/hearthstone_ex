using System;
using System.Text;
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
            if (original_premium != TAG_PREMIUM.NORMAL)
                return;

            var tag = ent.GetBestPossiblePremiumType();
            if (tag == TAG_PREMIUM.NORMAL)
            {
                Logger.Message($"{ent} have no golden material", info);
                return;
            }

            ent.SetTag(GAME_TAG.PREMIUM, tag);
            Logger.Message($"{ent} set to {tag}", info);
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

        [NotNull]
        private static IEnumerable<EntityDef> GetAllEntityDefs()
        {
            return DefLoader.Get().GetAllEntityDefs().Select(p => p.Value);
        }

        private static void SetHistoryGoldenTag([NotNull] Ent from, [NotNull] Net.Entity to, [NotNull] CallerInfo info)
        {
            if (UseRealGoldenTag()) return;

            var from_entdef = from.GetEntityDef();
            var to_entdef = GetAllEntityDefs().First(e => e.GetCardId() == to.CardID);

            const int GAME_TAG_PREMIUM = (int)GAME_TAG.PREMIUM;

            string _PrintFromTo(EntityDef root_from_entdef = null, bool detailed = false)
            {
                var builder = new StringBuilder();
                var from_entdef_overriden = root_from_entdef ?? from_entdef;

                if (detailed)
                {
                    builder.AppendLine(from.ToString())
                           .AppendLine(JoinTags(from))
                           .AppendLine(from_entdef_overriden.ToString())
                           .AppendLine(JoinTags(from_entdef_overriden))
                           .AppendLine("---")
                           .AppendLine($"[{to}]")
                           .AppendLine(JoinTags(to))
                           .AppendLine(to_entdef.ToString())
                           .Append(JoinTags(to_entdef));
                }
                else
                {
                    builder.AppendLine($"{from} {from_entdef_overriden}")
                           .Append($"[{to}] {to_entdef}");
                }

                return builder.ToString();
            }

            TAG_PREMIUM? ideal_tag = null;
            if (from.GetZone() == TAG_ZONE.HAND)
            {
                // ReSharper disable InconsistentNaming
                bool from_HasTag(GAME_TAG tag) => from.HasTag(tag) || from_entdef.HasTag(tag);
                bool to_HasTag(GAME_TAG tag) => to.Tags.Any(t => t.Name == (int)tag && t.Value > 0) || to_entdef.HasTag(tag);
                // ReSharper restore InconsistentNaming

                if (to_HasTag(GAME_TAG.COLLECTIBLE))
                {
                    ideal_tag = to_HasTag(GAME_TAG.HAS_DIAMOND_QUALITY) ? TAG_PREMIUM.DIAMOND : TAG_PREMIUM.GOLDEN;
                    Logger.Message($"Tag set to {ideal_tag}. Target card is {GAME_TAG.COLLECTIBLE}\n{_PrintFromTo()}", info);
                }
                else if (to.CardID.StartsWith(from.GetCardId(), StringComparison.Ordinal))
                {
                    if (from_HasTag(GAME_TAG.COLLECTIBLE))
                    {
                        ideal_tag = from.GetBestPossiblePremiumType();
                        Logger.Message($"Tag set to {ideal_tag}. Target card is child\n{_PrintFromTo()}", info);
                    }
                    else
                    {
                        var root_entdef = GetAllEntityDefs()
                                         .Where(e => e.HasTag(GAME_TAG.COLLECTIBLE))
                                         .FirstOrDefault(e => to.CardID.StartsWith(e.GetCardId(), StringComparison.Ordinal));

                        if (root_entdef != default)
                        {
                            //WARNING!
                            from_entdef = root_entdef;
                            ideal_tag = root_entdef.GetBestPossiblePremiumType(true);
                            Logger.Message($"Tag set to {ideal_tag}. Target card is multi-level child\n{_PrintFromTo()}", info);
                        }

                        /*var from_card_id = from.GetCardId();
                        var remove = from_card_id.Reverse().TakeWhile(c => !char.IsDigit(c)).Count();

                        if (remove > 0)
                        {
                            var root_card_id = from_card_id.Substring(0, from_card_id.Length - remove);
                            var root_from_entdef = GetEntityDef(root_card_id);
                            if (root_from_entdef?.HasTag(GAME_TAG.COLLECTIBLE) == true)
                            {
                                //WARNING!
                                from_entdef = root_from_entdef;
                                ideal_tag = root_from_entdef.GetBestPossiblePremiumType();
                                Logger.Message($"Tag set to {ideal_tag}. Target card is multilevel child\n{Print_from_to()}", info);
                            }
                        }*/
                    }
                }

                if (!ideal_tag.HasValue)
                {
                    Logger.Message($"Unknown premium type\n{_PrintFromTo(detailed: true)}", info);
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
                        Logger.Message($"Target not found\n{_PrintFromTo(detailed: true)}", info);
                        return;
                    }
                    //it probably evolved
                }
                else if (HistoryManager.m_lastTargetedEntity != from)
                {
                    Logger.Message($"Wrong target\n{HistoryManager.m_lastTargetedEntity} !-> {HistoryManager.m_lastTargetedEntity}\n{_PrintFromTo(detailed: true)}", info);
                    return;
                }

                ideal_tag = (TAG_PREMIUM)HistoryManager.m_lastPlayedEntity.GetTag(GAME_TAG_PREMIUM);
                Logger.Message($"Tag {ideal_tag} taken from previously played card", info);
            }

            var change_tags = to.Tags;
            foreach (var tag in change_tags.Where(tag => tag.Name == GAME_TAG_PREMIUM))
            {
                //force premium tag changed before
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
            if (UseRealGoldenTag()) return;

            //golden cards while game starts
            SetGoldenTag(__instance, new CallerInfoMin());
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ent.OnShowEntity))]
        public static void OnShowEntity([NotNull] Ent __instance)
        {
            if (UseRealGoldenTag()) return;

            //golden card when it taken from the deck, played by enemy, etc...
            SetGoldenTag(__instance, new CallerInfoMin());
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ent.OnChangeEntity))]
        public static void OnChangeEntity([NotNull] Ent __instance, [NotNull] List<Net.HistChangeEntity> ___m_transformPowersProcessed, Net.HistChangeEntity changeEntity)
        {
            if (___m_transformPowersProcessed.Contains(changeEntity)) return;

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

            CardTextBuilder _GetResult()
            {
                var end_def = __instance.GetEntityDef();
                if (end_def != null)
                {
                    var builder = end_def.GetCardTextBuilder();
                    if (builder != null) return builder;
                }

                return CardTextBuilder.GetFallbackCardTextBuilder();
            }

            __result = _GetResult();
            return false;
        }
    }
}
