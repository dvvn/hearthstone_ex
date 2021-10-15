using HarmonyLib;
using LM = LettuceMercenary;

namespace hearthstone_ex.Targets
{
    #if false

    //have no idea where game set mercenary textures

    [HarmonyPatch(typeof(LM))]
    public class LettuceMercenary
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LM.IsArtVariationUnlocked))]
        public static bool IsArtVariationUnlocked(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
#endif
}