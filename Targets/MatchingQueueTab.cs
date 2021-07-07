﻿using HarmonyLib;
using UnityEngine;
using Tab = global::MatchingQueueTab;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Tab))]
    public class MatchingQueueTab //: Utils.LoggerGui.Static<MatchingQueueTab>
    {
#if false
//this breaks the engine
        private static readonly MethodInfo m_initTimeStringSet = AccessTools.Method(typeof(global::MatchingQueueTab), "InitTimeStringSet");

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool Update([NotNull] global::MatchingQueueTab __instance, ref float ___m_timeInQueue, ElapsedStringSet ___m_timeStringSet)
        {
            //full rebuild

            m_initTimeStringSet.Invoke(__instance, null);
            ___m_timeInQueue += Time.fixedDeltaTime;
            __instance.m_waitTime.Text = GetElapsedTimeString(Mathf.RoundToInt(___m_timeInQueue), ___m_timeStringSet);

            return false;
        }
#endif

        private static int last_frame_count;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(InitTimeStringSet))]
        public static void InitTimeStringSet(ref float ___m_timeInQueue)
        {
            var count = Time.frameCount;
            if (count != last_frame_count)
            {
                last_frame_count = count;

                ___m_timeInQueue -= Time.deltaTime;
                ___m_timeInQueue += Time.unscaledDeltaTime;
            }
        }
    }
}
