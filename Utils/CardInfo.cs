#if DEBUG && false
#define LOG_CARD_INFO
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets;
using JetBrains.Annotations;
using UnityEngine;
using HarmonyLib;

namespace hearthstone_ex.Utils
{
    public static class CardInfo
    {
        //public const TAG_PREMIUM CUSTOM_PREMIUM_TAG = TAG_PREMIUM.GOLDEN;

        private static Dictionary<string, List<TAG_PREMIUM>> m_premiumCache;
        private static object m_entsDefsStorage;

        private static readonly LoggerBase LoggerFile = new LoggerFile(typeof(CardInfo));

        private static readonly LoggerBase Logger =
#if DEBUG
                new LoggerGui(typeof(CardInfo))
#else
                LoggerFile
#endif
            ;

        [NotNull]
        private static string JoinHelper<T>([NotNull] IEnumerable<T> val)
        {
            return string.Join(", ", val);
        }

        [NotNull]
        public static IEnumerable<TAG_PREMIUM> GetKnownPremiumTags([CanBeNull] string target_card_id)
        {
            if (string.IsNullOrEmpty(target_card_id))
                yield break;

            if (DefLoader.Get().GetAllEntityDefs().Count == 0)
                DefLoader.Get().LoadAllEntityDefs();

            if (EnumUtils.Length<CardHero.HeroType>() != 5)
            {
                Logger.Error("Hero types updated!");
                yield break;
            }

            var cache = DefLoader.Get().GetAllEntityDefs();
            if (m_entsDefsStorage != cache)
            {
                m_entsDefsStorage = cache;
                m_premiumCache = new Dictionary<string, List<TAG_PREMIUM>>();

#if LOG_CARD_INFO
                var logs_holder = new SortedList<string, string>();
                var all_tags = AccessTools.AllTypes().Where(t => t.IsEnum && t.FullName?.StartsWith("TAG_", StringComparison.Ordinal) == true).ToArray();

                var logs_holder2 = new SortedSet<string>();
#endif

                if (!cache.TryGetValue("BAR_COIN1", out var the_coin))
                {
                    Logger.Error("Unable to find coint tag!");
                    yield break;
                }

                var coin_unknown_tag = the_coin.GetTags().GetMap()
                                               .Where(t => t.Value == 1)
                                               .Select(t => t.Key).FirstOrDefault(t => ((GAME_TAG)t).ToString() == t.ToString());

                if (coin_unknown_tag == default(int))
                {
                    Logger.Error("Unable to find coint tag!");
                    yield break;
                }

                foreach (var card in cache.Select(def => def.Value))
                {
                    var id = card.GetCardId();
                    if (!m_premiumCache.TryGetValue(id, out var storage))
                    {
                        storage = new List<TAG_PREMIUM>();
                        m_premiumCache.Add(id, storage);
                    }

#if LOG_CARD_INFO
                    string TranslateGameTag(string game_tag, int value)
                    {
                        string enum_name = null;

                        var enum_tag = all_tags.FirstOrDefault(t => t.FullName == "TAG_" + game_tag);
                        if (enum_tag != null)
                            enum_name = Enum.GetName(enum_tag, value);

                        return game_tag
                             + (": ")
                             + (enum_name ?? value.ToString());
                    }

                    var tags_sorted = card.GetTags().GetMap()
                                          .Select(t => new { tag_id = t.Key, name = ((GAME_TAG)t.Key).ToString(), value = t.Value })
                                          .OrderBy(t => t.name)
                                          .Select(t => t.tag_id == t.value ? t.name : TranslateGameTag(t.name, t.value));
                    var tags_sorted_str = JoinHelper(tags_sorted);
                    logs_holder.Add(card.ToString(), tags_sorted_str);

#endif

                    storage.Add(TAG_PREMIUM.NORMAL);

                    if (card.HasTag(GAME_TAG.HAS_DIAMOND_QUALITY))
                        storage.Add(TAG_PREMIUM.DIAMOND);

                    if (card.HasTag(GAME_TAG.COLLECTIBLE))
                    {
#if LOG_CARD_INFO
                        logs_holder2.Add(card.ToString());
#endif
                        storage.Add(TAG_PREMIUM.GOLDEN);
                        continue;
                    }

                    if (card.IsSpell())
                    {
                        if (card.HasTag((GAME_TAG)coin_unknown_tag) || card.IsCustomCoin())
                            storage.Add(TAG_PREMIUM.GOLDEN);
                        continue;
                    }

                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    switch (GameUtils.GetHeroType(id))
                    {
                        case CardHero.HeroType.VANILLA:
                        case CardHero.HeroType.HONORED:
                            storage.Add(TAG_PREMIUM.GOLDEN);
                            continue;
                        case CardHero.HeroType.BATTLEGROUNDS_HERO:
                        case CardHero.HeroType.BATTLEGROUNDS_GUIDE:
                            continue;
                    }
                }

#if LOG_CARD_INFO
                var logs_holder3 = new SortedList<string, string>();
#endif

                var core_prefixes = new[] { "CORE_", "VAN_" };
                foreach (var pair in m_premiumCache.Where(t => !t.Value.Contains(TAG_PREMIUM.GOLDEN)))
                {
                    foreach (var prefix in core_prefixes)
                    {
                        if (m_premiumCache.TryGetValue(prefix + pair.Key, out var core_card))
                        {
#if LOG_CARD_INFO
                            logs_holder3.Add($"{pair.Key} ({prefix + pair.Key})", $"[{JoinHelper(pair.Value)}] [{JoinHelper(core_card)}]");
#endif
                            if (core_card.Contains(TAG_PREMIUM.GOLDEN))
                            {
                                pair.Value.Add(TAG_PREMIUM.GOLDEN);
                            }

                            if (core_card.Contains(TAG_PREMIUM.DIAMOND) && pair.Value.Contains(TAG_PREMIUM.DIAMOND))
                                pair.Value.Add(TAG_PREMIUM.DIAMOND);

                            break;
                        }
                    }
                }

                //3 2 1 0
                foreach (var list in m_premiumCache.Select(l => l.Value))
                {
                    list.Sort((a, b) => (int)(a < b ? b : a));
                }

#if LOG_CARD_INFO
                var log_builder = new StringBuilder();
                var longest_string = logs_holder.Max(p => p.Key.Length);
                foreach (var log in logs_holder)
                {
                    log_builder.Append(log.Key.PadRight(longest_string));
                    log_builder.Append(" -> ");
                    log_builder.AppendLine(log.Value);
                }

                log_builder.AppendLine("---");

                var longest_string2 = logs_holder2.Max(p => p.Length);
                foreach (var log in logs_holder2)
                {
                    log_builder.AppendLine(log.PadRight(longest_string2));
                }

                log_builder.AppendLine("---");

                var longest_string3 = logs_holder3.Max(p => p.Key.Length);
                foreach (var log in logs_holder3)
                {
                    log_builder.Append(log.Key.PadRight(longest_string3));
                    log_builder.Append(" -> ");
                    log_builder.AppendLine(log.Value);
                }

                LoggerFile.Message(log_builder.ToString());
#endif
            }

            if (!m_premiumCache.TryGetValue(target_card_id, out var tags))
            {
                Logger.Error($"{target_card_id} not found!");
                yield break;
            }

            foreach (var tag in tags)
                yield return tag;
        }

        public static bool HavePremiumType([NotNull] string card_id, TAG_PREMIUM tag)
        {
            return GetKnownPremiumTags(card_id).Any(type => type == tag);
        }

        public static TAG_PREMIUM GetBestPossiblePremiumType([NotNull] string card_id)
        {
            return GetKnownPremiumTags(card_id).FirstOrDefault();
        }


        public static TAG_PREMIUM GetBestPossiblePremiumType([NotNull] this EntityDef ent)
        {
            return GetBestPossiblePremiumType(ent.GetCardId());
        }

        public static TAG_PREMIUM GetBestPossiblePremiumType([NotNull] this Entity ent)
        {
            var tag = GetBestPossiblePremiumType(ent.GetCardId());
            if (tag != TAG_PREMIUM.NORMAL)
                return tag;

            //todo: find a way to use only card_id here
            //hack
            if (ent.GetCard().GetGoldenMaterial() != null)
                return TAG_PREMIUM.GOLDEN;

            return TAG_PREMIUM.NORMAL;
        }
    }
}
