using Blizzard.Commerce;
using HarmonyLib;
using logger = Hearthstone.Commerce.BlizzardCommerceLogger;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(logger))]
    public class BlizzardCommerceLogger
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(logger.OnLogEvent))]
        public static bool OnLogEvent(CommerceLogLevel level, string message)
        {
            //i dont want shit in logs

            return level == CommerceLogLevel.ERROR || level == CommerceLogLevel.FATAL;

#if false
            switch (level)
            {
                case CommerceLogLevel.WARNING:
                case CommerceLogLevel.ERROR:
                case CommerceLogLevel.FATAL:
                    return true;
                default:
                    return false;
            }
#endif
        }
    }
}
