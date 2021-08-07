using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private static readonly IDictionary<int, TAG_PREMIUM> m_fakePremiumCards = new Dictionary<int, TAG_PREMIUM>();

        private static void RegisterFakePremiumCard([NotNull] EntityBase ent, TAG_PREMIUM tag)
        {
            var id = ent.GetEntityId();
            if (!m_fakePremiumCards.ContainsKey(id))
                m_fakePremiumCards.Add(id, tag);
        }

        private static bool TryGetFakePremium(int ent_id, out TAG_PREMIUM tag)
        {
            return m_fakePremiumCards.TryGetValue(ent_id, out tag);
        }

        private static bool TryGetFakePremium([NotNull] EntityBase ent, out TAG_PREMIUM tag)
        {
            return TryGetFakePremium(ent.GetEntityId(), out tag);
        }

        public static void ResetFakePremiumData()
        {
            m_fakePremiumCards.Clear();
        }

        private static void SetGoldenTag([NotNull] Ent ent, [NotNull] CallerInfo info)
        {
            if (!ent.ControlledByFriendlyPlayer())
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

            RegisterFakePremiumCard(ent, tag);
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

        private enum UPDATE_ACTOR
        {
            ORIGINAL, NO, YES
        }

        private static UPDATE_ACTOR SetHistoryGoldenTag([NotNull] Ent from, [NotNull] Net.Entity to, [NotNull] CallerInfo info)
        {
            if (UseRealGoldenTag())
                return UPDATE_ACTOR.ORIGINAL;

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

            //var tag_before = from.GetBestPossiblePremiumType(_Logger);
            //var tag_after = to_entdef.GetBestPossiblePremiumType(_Logger);

            if (!TryGetFakePremium(from, out var tag_before))
            {
                if (from.GetCardId() != HistoryManager.LastTargetedEntity.GetCardId() || !TryGetFakePremium(HistoryManager.LastPlayedEntity, out tag_before))
                {
                    Logger.Message($"Last played: {HistoryManager.LastPlayedEntity}\n"
                                 + $"Last target: {HistoryManager.LastTargetedEntity}\n"
                                 + "Fake premium type not set before\n"
                                 + $"{_PrintFromTo(FROM_TO_DEFS)}", info);
                    return UPDATE_ACTOR.ORIGINAL;
                }
            }

            if (from.GetEntityId() == to.ID || !TryGetFakePremium(from, out var tag_after))
                tag_after = to_entdef.GetBestPossiblePremiumType(_Logger);

            TAG_PREMIUM ideal_tag;
            if (tag_before < tag_after)
                ideal_tag = tag_before;
            else
            {
                ideal_tag = (TAG_PREMIUM)Math.Min((int)tag_before, (int)tag_after);
                if (ideal_tag == default)
                {
                    Logger.Message($"Unknown premium type\n{_PrintFromTo(FROM_TO_DEFS | FROM_TO_FLAGS)}", info);
                    return UPDATE_ACTOR.ORIGINAL;
                }
            }

            var result = tag_before == tag_after ? UPDATE_ACTOR.NO : UPDATE_ACTOR.YES;

            foreach (var tag in to.Tags.Where(tag => tag.Name == GAME_TAG_PREMIUM))
            {
                //force premium tag changed before
                Logger.Message($"Tag found. {(TAG_PREMIUM)tag.Value} -> {ideal_tag}\n{_PrintFromTo(FROM_TO_FLAGS)}", info);
                tag.Value = (int)ideal_tag;
                return result;
            }

            Logger.Message($"Tag added. {ideal_tag}\n{_PrintFromTo(FROM_TO_FLAGS)}", info);
            to.Tags.Add(new Net.Entity.Tag { Name = GAME_TAG_PREMIUM, Value = (int)ideal_tag });
            return /*UPDATE_ACTORS.YES*/result;
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

        // ReSharper disable InconsistentNaming
        private static readonly MethodInfo ShouldUpdateActorOnChangeEntity = AccessTools.Method(typeof(Ent), nameof(ShouldUpdateActorOnChangeEntity));
        private static readonly MethodInfo ShouldRestartStateSpellsOnChangeEntity = AccessTools.Method(typeof(Ent), nameof(ShouldRestartStateSpellsOnChangeEntity));

        private static readonly MethodInfo HandleEntityChange = AccessTools.Method(typeof(Ent), nameof(HandleEntityChange));
        // ReSharper restore InconsistentNaming

        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(ShouldUpdateActorOnChangeEntity))]
        //public static bool ShouldUpdateActorOnChangeEntity(ref bool __result)
        //{
        //    __result = false;
        //    return false;
        //}

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Ent.OnChangeEntity))]
        public static bool OnChangeEntity([NotNull] Ent __instance,
                                          [NotNull] Net.HistChangeEntity changeEntity,
                                          [NotNull] List<Net.HistChangeEntity> ___m_transformPowersProcessed,
                                          [NotNull] List<int> ___m_subCardIDs,
                                          ref int ___m_queuedChangeEntityCount)
        {
            if (___m_transformPowersProcessed.Contains(changeEntity))
            {
                ___m_transformPowersProcessed.Remove(changeEntity);
            }
            else
            {
                var update_actor = SetHistoryGoldenTag(__instance, changeEntity.Entity, new CallerInfoMin());

                bool _UpdateActor()
                {
                    switch (update_actor)
                    {
                        case UPDATE_ACTOR.ORIGINAL:
                            return (bool)ShouldUpdateActorOnChangeEntity.Invoke(__instance, new object[] { changeEntity });
                        case UPDATE_ACTOR.NO:
                            return false;
                        case UPDATE_ACTOR.YES:
                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                ___m_subCardIDs.Clear();
                --___m_queuedChangeEntityCount;
                var data = new Ent.LoadCardData
                {
                    updateActor = _UpdateActor()
                  , restartStateSpells = (bool)ShouldRestartStateSpellsOnChangeEntity.Invoke(__instance, new object[] { changeEntity })
                  , fromChangeEntity = true
                };

                HandleEntityChange.Invoke(__instance, new object[] { changeEntity.Entity, data, false });
            }

            return false;
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
