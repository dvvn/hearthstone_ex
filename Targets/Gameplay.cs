using HarmonyLib;
using hearthstone_ex.Utils;
using UnityEngine;
using Gp = Gameplay;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Gp))]
    public class Gameplay : LoggerGui.Static<Gameplay>
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Gp.SaveOriginalTimeScale))]
        public static bool SaveOriginalTimeScale()
        {
            //dont touch timescale!!!

            return false;
        }
    }
}
