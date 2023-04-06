using HarmonyLib;
using State = GameState;
using HistFullEntity = Network.HistFullEntity;
using HistShowEntity = Network.HistShowEntity;
using HistChangeEntity = Network.HistChangeEntity;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(State))]
	public class GameState
	{
		[HarmonyPrefix]
		[HarmonyPatch(nameof(State.OnFullEntity))]
		public static bool OnFullEntity(State __instance, ref bool __result, HistFullEntity fullEntity)
		{
			//whole function rebuild

			var ent = __instance.GetEntity(fullEntity.Entity.ID);
			if (ent == null)
			{
				Log.Power.PrintWarning($"{nameof(State.OnFullEntity)} - WARNING entity {fullEntity.Entity.ID} DOES NOT EXIST!");
				__result = false;
			}
			else
			{
				ent.OnFullEntity(fullEntity);
				Entity.OnFullEntity(ent);
				__result = true;
			}

			return HookInfo.SKIP_ORIGINAL;
		}

		[HarmonyPrefix]
		[HarmonyPatch(nameof(State.OnShowEntity))]
		public static bool OnShowEntity(State __instance, ref bool __result, HistShowEntity showEntity)
		{
			//whole function rebuild

			if (__instance.EntityRemovedFromGame(showEntity.Entity.ID))
			{
				__result = false;
				return HookInfo.SKIP_ORIGINAL;
			}

			var ent = __instance.GetEntity(showEntity.Entity.ID);
			if (ent == null)
			{
				Log.Power.PrintWarning($"{nameof(State.OnShowEntity)} - WARNING entity {showEntity.Entity.ID} DOES NOT EXIST!");
				__result = false;
			}
			else
			{
				ent.OnShowEntity(showEntity);
				Entity.OnShowEntity(ent);
				__result = true;
			}

			return HookInfo.SKIP_ORIGINAL;
		}

		[HarmonyPrefix]
		[HarmonyPatch(nameof(State.OnChangeEntity))]
		public static bool OnChangeEntity(State __instance, ref bool __result, HistChangeEntity changeEntity)
		{
			//whole function rebuild

			if (__instance.EntityRemovedFromGame(changeEntity.Entity.ID))
			{
				__result = false;
				return HookInfo.SKIP_ORIGINAL;
			}

			var ent = __instance.GetEntity(changeEntity.Entity.ID);
			if (ent == null)
			{
				Log.Power.PrintWarning($"{nameof(State.OnChangeEntity)} - WARNING entity {changeEntity.Entity.ID} DOES NOT EXIST!");
				__result = false;
			}
			else
			{
				if (Entity.OnChangeEntity(ent, changeEntity) == HookInfo.CALL_ORIGINAL)
					ent.OnChangeEntity(changeEntity);
				__result = true;
			}

			return HookInfo.SKIP_ORIGINAL;
		}
	}
}