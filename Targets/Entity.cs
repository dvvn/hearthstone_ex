using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using hearthstone_ex.Utils;
using Ent = Entity;
using Net = Network;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(Ent))]
	public partial class Entity : LoggerGui.Static<Entity>
	{
		private static bool UseRealGoldenTag( )
		{
			return SpectatorManager.Get( ).IsSpectatingOrWatching || GameMgr.Get( ).IsBattlegrounds( );
		}

		private static readonly IList<KeyValuePair<EntityBase, TAG_PREMIUM>> _fakePremiumCards = new List<KeyValuePair<EntityBase, TAG_PREMIUM>>( );

		private static void RegisterFakePremiumCard([NotNull] EntityBase ent, TAG_PREMIUM tag)
		{
			if (_fakePremiumCards.Select(p => p.Key.GetEntityId( )).Contains(ent.GetEntityId( )))
			{
				Logger.Message($"{ent} already added");
			}
			else
			{
				_fakePremiumCards.Add(new KeyValuePair<EntityBase, TAG_PREMIUM>(ent, tag));
				Logger.Message($"{ent} added ({tag})");
			}
		}

		public static void ResetFakePremiumData( )
		{
			_fakePremiumCards.Clear( );
			Logger.Message("Cleared");
		}

		private static void SetGoldenTag([NotNull] Ent ent, [NotNull] CallerInfo info)
		{
			if (!ent.ControlledByFriendlyPlayer /*CreatedByFriendlyPlayer*/( ))
			{
				//Logger.Message($"{__instance} not owned", info);
				return;
			}

			if (ent.GetPremiumType( ) != TAG_PREMIUM.NORMAL)
				return;

			var tag = ent.GetBestPossiblePremiumType( );
			if (tag == TAG_PREMIUM.NORMAL)
			{
				Logger.Message($"{ent} have no golden material", info);
				return;
			}

			RegisterFakePremiumCard(ent, tag);
			ent.SetTag(GAME_TAG.PREMIUM, tag);
			//Logger.Message($"{ent} set to {tag}", info);
		}

		[NotNull]
		private static string ParseFlags([NotNull] TagMap map)
		{
			//var strings = pairs.Select(t => new {name = ((GAME_TAG) t.Key).ToString( ), value = t.Value}).OrderByDescending(t => t.name).Select(t => $"{t.name}: {t.value}");
			//return string.Join(", ", strings);
			return TagConvertor.ToString(map);
		}

		[NotNull]
		private static string ParseFlags([NotNull] EntityBase ent)
		{
			return ParseFlags(ent.GetTags( ));
		}

		[NotNull]
		private static string ParseFlags([NotNull] Net.Entity ent)
		{
			var tmap = new TagMap( );
			var map = tmap.GetMap( );
			foreach (var tag in ent.Tags)
				map.Add(tag.Name, tag.Value);
			return ParseFlags(tmap);
		}

		[NotNull]
		private static IEnumerable<EntityDef> GetAllEntityDefs( )
		{
			return DefLoader.Get( ).GetAllEntityDefs( ).Select(p => p.Value);
		}

		//NEVER UPDATE THE ACTOR!!!
		//stolen cards ignored
		//secrets with transformation ignored
		private static bool SetHistoryGoldenTag([NotNull] Ent from, [NotNull] Net.Entity to, [NotNull] CallerInfo info)
		{
			if (UseRealGoldenTag( ))
				return false;

			var fromEntdef = from.GetEntityDef( );
			var toEntdef = GetAllEntityDefs( ).First(e => e.GetCardId( ) == to.CardID);

			//const int FROM_TO_LAST = 1 << 0;
			const int FROM_TO_DEFS = 1 << 1;
			const int FROM_TO_FLAGS = 1 << 2;

			string PrintFromTo( /*int def = FROM_TO_LAST, int ex = 0*/ int bflags = 0)
			{
				var builder = new StringBuilder( );

				//var bflags = def | ex;
				//var played = (bflags & FROM_TO_LAST) > 0;
				var defs = (bflags & FROM_TO_DEFS) > 0;
				var flags = (bflags & FROM_TO_FLAGS) > 0;

				//if (played)
				//{
				//	builder.Append($"---Played: {HistoryManager.LastPlayedEntity}\n");
				//	builder.Append($"---Target: {HistoryManager.LastTargetedEntity}\n");
				//}

				if (defs)
				{
					builder.Append("---From def: ");
					builder.AppendLine(fromEntdef.ToString( ));
					if (flags)
						builder.AppendLine(ParseFlags(fromEntdef));
				}

				builder.Append("---From: ");
				builder.AppendLine(from.ToString( ));
				if (flags)
					builder.AppendLine(ParseFlags(from));
				if (defs)
				{
					builder.Append("---To def: ");
					builder.AppendLine(toEntdef.ToString( ));
					if (flags)
						builder.AppendLine(ParseFlags(toEntdef));
				}

				builder.AppendLine($"---To: [{to}]");
				if (flags)
					builder.Append(ParseFlags(to));

				return builder.ToString( );
			}

			var premiumTag = to.Tags.FirstOrDefault(tag => tag.Name == (int) GAME_TAG.PREMIUM);
			//target card can't be premium
			if (premiumTag == default)
			{
				Logger.Message($"Premium tag not found\n{PrintFromTo( )}", info);
				return false;
			}

			Logger.Message($"Updating\n{PrintFromTo(FROM_TO_FLAGS | FROM_TO_FLAGS)}", info);

			string MakeSubstr(string str)
			{
				var offset = str.LastIndexOf('_');
				if (offset == -1)
					return "-";

				return str.Substring(0, offset + 1);
			}

			var fromId = from.GetEntityId( );
			var toId = to.ID;
			var fromName = MakeSubstr(from.GetCardId( ));
			var toName = MakeSubstr(to.CardID);

			Logger.Message($"Testing\n{nameof(fromId)}: {fromId}\n{nameof(fromName)}: {from.GetCardId( )}|{fromName}\n{nameof(toId)}: {toId}\n{nameof(toName)}: {to.CardID}|{toName}"
						 , info);

			var index = -1;
			bool exact = default;

			for (var i = _fakePremiumCards.Count - 1; i >= 0; --i)
			{
				var ent = _fakePremiumCards[i].Key;
				var id = ent.GetEntityId( );
				var name = ent.GetCardId( );
				Logger.Message($"Id: {id} Name: {name}", info);

				if (id == fromId || id == toId)
				{
					exact = true;
				}
				else if (name.StartsWith(fromName) || name.StartsWith(toName))
				{
					exact = false;
				}
				else
				{
					continue;
				}

				index = i;
				break;
			}

			if (index == -1)
			{
				Logger.Message("Not found!");
				return false;
			}

			if (!exact)
			{
				if (!from.ControlledByFriendlyPlayer( ))
				{
					//todo: do something to detect who change the card
					//now if opponent have same card it also becomes golden
				}
			}

			var value = _fakePremiumCards[index].Value;
			Logger.Message($"Updating successful.{(exact ? " exact" : string.Empty)} {value} tag selected", info);
			premiumTag.Value = (int) value;

			return true;
		}
	}

	public partial class Entity
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(Ent.OnFullEntity))]
		public static void OnFullEntity([NotNull] Ent __instance)
		{
			if (UseRealGoldenTag( ))
				return;

			//golden cards while game starts
			SetGoldenTag(__instance, new CallerInfoMin( ));
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Ent.OnShowEntity))]
		public static void OnShowEntity([NotNull] Ent __instance)
		{
			if (UseRealGoldenTag( ))
				return;

			//golden card when it taken from the deck, played by enemy, etc...
			SetGoldenTag(__instance, new CallerInfoMin( ));
		}

		// ReSharper disable InconsistentNaming
		private static readonly MethodInfo ShouldUpdateActorOnChangeEntity = AccessTools.Method(typeof(Ent), nameof(ShouldUpdateActorOnChangeEntity));
		private static readonly MethodInfo ShouldRestartStateSpellsOnChangeEntity = AccessTools.Method(typeof(Ent), nameof(ShouldRestartStateSpellsOnChangeEntity));

		private static readonly MethodInfo HandleEntityChange = AccessTools.Method(typeof(Ent), nameof(HandleEntityChange));
		// ReSharper restore InconsistentNaming

		[HarmonyPrefix]
		[HarmonyPatch(nameof(Ent.OnChangeEntity))]
		public static bool OnChangeEntity([NotNull] Ent __instance,
										  [NotNull] Net.HistChangeEntity changeEntity,
										  [NotNull] List<Net.HistChangeEntity> ___m_transformPowersProcessed,
										  [NotNull] List<int> ___m_subCardIDs,
										  ref int ___m_queuedChangeEntityCount)
		{
			if (___m_transformPowersProcessed.Contains(changeEntity))
			{
				___m_transformPowersProcessed.Remove(changeEntity);
			}
			else
			{
				var dontUpdateActor = SetHistoryGoldenTag(__instance, changeEntity.Entity, new CallerInfoMin( ));

				bool UpdateActorFn( )
				{
					if (dontUpdateActor)
						return false;
					return (bool) ShouldUpdateActorOnChangeEntity.Invoke(__instance, new object[ ] {changeEntity});
				}

				___m_subCardIDs.Clear( );
				--___m_queuedChangeEntityCount;
				var data = new Ent.LoadCardData
				{
					updateActor = UpdateActorFn( )
				  , restartStateSpells = (bool) ShouldRestartStateSpellsOnChangeEntity.Invoke(__instance, new object[ ] {changeEntity})
				  , fromChangeEntity = true
				};

				HandleEntityChange.Invoke(__instance, new object[ ] {changeEntity.Entity, data, false});
			}

			return false;
		}
	}

	public partial class Entity
	{
		[HarmonyPrefix]
		[HarmonyPatch(nameof(Ent.GetCardTextBuilder))]
		public static bool GetCardTextBuilder(ref CardTextBuilder __result, [NotNull] Ent __instance)
		{
			//remove shit from logs

			var entDef = __instance.GetEntityDef( );
			var builder = entDef?.GetCardTextBuilder( );
			__result = builder ?? CardTextBuilder.GetFallbackCardTextBuilder( );

			return false;
		}
	}
}
