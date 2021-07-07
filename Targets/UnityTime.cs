using HarmonyLib;
using UnityEngine;

namespace hearthstone_ex.Targets
{
    //currently unsupported, game says "Attempted to access a missing method"

    ///note: if you wanna make perfect animation speed-up mod, you must keep "deltaTime" in every speeded-up places
    ///and replace "deltaTime" with "unscaledDeltaTime" in every real-animspeed places
    ///game have 200+ places. wanna fuck with it? i dont
    [HarmonyPatch(typeof(Time))]
    public class UnityTime
    {
        [HarmonyPrefix]
        [HarmonyPatch("deltaTime", MethodType.Getter)]
        public static bool DeltaTime(ref float __result)
        {
            __result = Time.fixedDeltaTime;
            return false;
        }
    }
}
