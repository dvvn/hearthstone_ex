using System;
using System.Collections.Generic;
using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using Mgr = HistoryManager;
using Ent = Entity;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Mgr))]
    public class HistoryManager : LoggerGui.Static<Mgr>
    {
        public static Ent m_lastPlayedEntity, m_lastTargetedEntity;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Mgr.CreatePlayedTile))]
        public static void CreatePlayedTile([NotNull] Mgr __instance,
                                            Ent playedEntity, Ent targetedEntity)
        {
            m_lastPlayedEntity = playedEntity;
            m_lastTargetedEntity = targetedEntity;
        }
    }
}
