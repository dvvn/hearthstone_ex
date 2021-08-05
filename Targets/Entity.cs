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

            const int GAME_TAG_PREMIUM = (int)GAME_TAG.PREMIUM;

            void _Logger(string msg) => Logger.Message(msg, info);

            var from_entdef = from.GetEntityDef();
            var to_entdef = GetAllEntityDefs().First(e => e.GetCardId() == to.CardID);

            const int FROM_TO_DEFS = 1 << 0;
            const int FROM_TO_FLAGS = 1 << 1;

            string _PrintFromTo(int bflags = 0)
            {
                var builder = new StringBuilder();

                var defs = (bflags & FROM_TO_DEFS) > 0;
                var flags = (bflags & FROM_TO_FLAGS) > 0;

                if (defs)
                {
                    builder.Append("From def: ");
                    builder.AppendLine(from_entdef.ToString());
                    if (flags)
                        builder.AppendLine(JoinTags(from_entdef));
                }

                builder.Append("From: ");
                builder.AppendLine(from.ToString());
                if (flags)
                    builder.AppendLine(JoinTags(from));
                if (defs)
                {
                    builder.Append("To def: ");
                    builder.AppendLine(to_entdef.ToString());
                    if (flags)
                        builder.AppendLine(JoinTags(to_entdef));
                }

                builder.AppendLine($"To: [{to}]");
                if (flags)
                    builder.Append(JoinTags(to));

                return builder.ToString();
            }

            var tag_before = from.GetBestPossiblePremiumType(logger: _Logger);
            var tag_after = to_entdef.GetBestPossiblePremiumType(logger: _Logger);
            TAG_PREMIUM ideal_tag;

            if (tag_before < tag_after)
                ideal_tag = tag_before;
            else
            {
                ideal_tag = (TAG_PREMIUM)Math.Min((int)tag_before, (int)tag_after);
                if (ideal_tag == default)
                {
                    Logger.Message($"Unknown premium type\n{_PrintFromTo(FROM_TO_DEFS | FROM_TO_FLAGS)}", info);
                    return;
                }
            }

            var change_tags = to.Tags;
            foreach (var tag in change_tags.Where(tag => tag.Name == GAME_TAG_PREMIUM))
            {
                //force premium tag changed before
                Logger.Message($"Tag found. {(TAG_PREMIUM)tag.Value} -> {ideal_tag}\n{_PrintFromTo()}", info);
                tag.Value = (int)ideal_tag;
                return;
            }

            Logger.Message($"Tag added. {ideal_tag}\n{_PrintFromTo()}", info);
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
