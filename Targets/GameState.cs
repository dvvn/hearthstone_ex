using System;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using hearthstone_ex.Utils;
using State = GameState;
using Net = Network;
using Ent = Entity;

namespace hearthstone_ex.Targets
{
    //[HarmonyPatch(typeof(State))]
    //public class GameState : LoggerGui.Static<GameState>
    //{
    //    //private static readonly int TAG_PREMIUM_BACKUP = Enum.GetValues(typeof(GAME_TAG)).Cast<int>().Max() + 1;

    //    [HarmonyPostfix]
    //    [HarmonyPatch(nameof(State.OnRealTimeFullEntity))]
    //    public static void OnRealTimeFullEntity( State __instance,Net.HistFullEntity fullEntity)
    //    {
    //        fullEntity.Entity.
    //    }
    //}
}
