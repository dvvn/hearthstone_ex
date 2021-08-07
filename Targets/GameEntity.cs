using HarmonyLib;
using hearthstone_ex.Utils;
using Ent = GameEntity;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Ent))]
    public class GameEntity : LoggerGui.Static<GameEntity>
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ShowEndGameScreenAfterEffects))]
        public static void ShowEndGameScreenAfterEffects()
        {
            Entity.ResetFakePremiumData();
        }
    }
}
