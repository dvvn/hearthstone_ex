#if false
using System;
using HarmonyLib;
using hearthstone_ex.Utils;
using Mgr = HistoryManager;
using Info = HistoryInfo;
using Ent = Entity;

namespace hearthstone_ex.Targets
{
	//uselles

	//[HarmonyPatch(typeof(Mgr))]
	public class HistoryManager : LoggerGui.Static<Mgr>
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(CreateTransformTile))]
		public static void CreateTransformTile(Mgr __instance, Info sourceInfo)
		{
			string LogMsg( )
			{
				var ent = sourceInfo.GetDuplicatedEntity( ) ?? sourceInfo.GetOriginalEntity( );

				var msg = $"type: {sourceInfo.m_infoType}, entity: {ent} died :{sourceInfo.HasDied( )}";

				if (GameUtils.TranslateDbIdToCardId(ent.GetTag(GAME_TAG.TRANSFORMED_FROM_CARD)) != null)
					msg += ", prev: " + GameState.Get( ).GetEntity(ent.GetTag(GAME_TAG.TRANSFORMED_FROM_CARD));
				if (GameUtils.TranslateDbIdToCardId(ent.GetTag(GAME_TAG.CARD_TARGET)) != null)
					msg += ", target: " + GameState.Get( ).GetEntity(ent.GetTag(GAME_TAG.CARD_TARGET));

				return msg; // + Environment.NewLine + ent.GetTags( ).MakeString( );
			}

			Logger.Message(LogMsg( ));
		}

		[HarmonyPrefix]
		[HarmonyPatch(nameof(NotifyEntityDied), typeof(Ent))]
		public static void NotifyEntityDied(Ent entity)
		{
			Logger.Message($"Called {entity}");
		}
	}
}
#endif