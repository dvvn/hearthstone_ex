using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
		//struct EntInfo
		//{
		//	public int Id;
		//	public string Card;
		//	public TAG_PREMIUM Premium;

		//	public override string ToString( )
		//	{
		//		return $"EntityId: {Id}, CardId: {Card}";
		//	}
		//}

		private static readonly IDictionary<int, TAG_PREMIUM> _fakePremiumCards = new Dictionary<int, TAG_PREMIUM>( );

		private static void RegisterFakePremiumCard([NotNull] EntityBase ent, TAG_PREMIUM tag, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = -1)
		{
			var id = ent.GetEntityId( );
			if (_fakePremiumCards.TryGetValue(ent.GetEntityId( ), out _))
			{
				Logger.Message($"{ent} already added", memberName, sourceLineNumber);
			}
			else
			{
				var zone = ent.GetZone( );
				switch (zone)
				{
					case TAG_ZONE.PLAY:
					case TAG_ZONE.DECK:
					case TAG_ZONE.HAND:
					case TAG_ZONE.SECRET: //not sure
					case TAG_ZONE.SETASIDE: //all temp cars also stored
					{
						_fakePremiumCards.Add(id, tag);
						Logger.Message($"{ent} added ({tag})", memberName, sourceLineNumber);
						break;
					}
					default:
					{
						Logger.Message($"{ent} NOT added. Zone: {zone}. Tags:{Environment.NewLine}" + ent.GetTags( ).JoinTags( ), memberName, sourceLineNumber);
						break;
					}
				}
			}
		}

		public static void ResetFakePremiumData([CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = -1)
		{
			_fakePremiumCards.Clear( );
			Logger.Message("Cleared", memberName, sourceLineNumber);
		}

		private static void SetGoldenTag([NotNull] Ent ent, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = -1)
		{
			if (!ent.ControlledByFriendlyPlayer /*CreatedByFriendlyPlayer*/( ))
			{
				//Logger.Message($"{__instance} not owned");
				return;
			}

			//todo: find why cards shuffled in deck not processed

			if (ent.GetPremiumType( ) != TAG_PREMIUM.NORMAL)
				return;
			var tag = ent.GetBestPossiblePremiumType( );
			if (tag == TAG_PREMIUM.NORMAL)
			{
				Logger.Message($"{ent} have no golden material", memberName, sourceLineNumber);
				return;
			}

			RegisterFakePremiumCard(ent, tag, memberName, sourceLineNumber);
			ent.SetTag(GAME_TAG.PREMIUM, tag);

			//Logger.Message($"{ent} set to {tag}");
		}

		[Conditional("DEBUG")]
		private static void SimpleLog(string msg)
		{
			Logger.Message(msg, string.Empty, string.Empty, 0);
		}

		//NEVER UPDATE THE ACTOR!!!
		private static bool RestoreFakePremium([NotNull] Ent from, [NotNull] Net.Entity to)
		{
			var toEntdef = CardInfo.GetAllEntityDefs( ).First(e => e.GetCardId( ) == to.CardID);

			//const int FROM_TO_LAST = 1 << 0;
			const int FROM_TO_DEFS = 1 << 1;
			const int FROM_TO_FLAGS = 1 << 2;

			string PrintFromTo( /*int def = FROM_TO_LAST, int ex = 0*/ int bflags = 0)
			{
				var fromEntdef = from.GetEntityDef( );

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
						builder.AppendLine(fromEntdef.JoinTags( ));
				}

				builder.Append("---From: ");
				builder.AppendLine(from.ToString( ));
				if (flags)
					builder.AppendLine(from.JoinTags( ));
				if (defs)
				{
					builder.Append("---To def: ");
					builder.AppendLine(toEntdef.ToString( ));
					if (flags)
						builder.AppendLine(toEntdef.JoinTags( ));
				}

				builder.AppendLine($"---To: [{to}]");
				if (flags)
					builder.Append(to.JoinTags( ));

				return builder.ToString( );
			}

			var premiumTag = to.Tags.FirstOrDefault(tag => tag.Name == (int) GAME_TAG.PREMIUM);
			if (premiumTag == default)
			{
				Logger.Message($"Premium tag not found{Environment.NewLine}{PrintFromTo( )}");
				return false;
			}
			if (premiumTag.Value != (int) TAG_PREMIUM.NORMAL)
			{
				Logger.Message("Premium tag already set");
				return false;
			}
			if (_fakePremiumCards.Count == 0)
			{
				Logger.Message("No fake premium cards stored!");
				return false;
			}

			SimpleLog($"Updating{Environment.NewLine}{PrintFromTo(FROM_TO_FLAGS | FROM_TO_FLAGS)}");

			if (!toEntdef.HavePremiumTexture( ))
			{
				Logger.Message("Target ent doesn't have premium texture!");
				return false;
			}

			SimpleLog("Known fake premium cards:" + Environment.NewLine +
					  string.Join(Environment.NewLine, _fakePremiumCards.OrderBy(p => p.Key).Select(p =>
					  {
						  var ent = GameState.Get( ).GetEntity(p.Key);
						  return ent == null ? $"ENTITY {p.Key} NOT EXIST" : ent.ToString( );
					  })));

			string FoundMsg(bool found) => string.Format("{0}found", found ? string.Empty : "NOT ");

			TAG_PREMIUM premium;
			var creatorTag = to.Tags.FirstOrDefault(t => t.Name == (int) GAME_TAG.CREATOR);
			//goto is simplier solution here
			var creatorTagValid = creatorTag != default;
			CREATOR_CHECK:
			//SimpleLog($"{nameof(creatorTagValid)}: {creatorTagValid}");
			if (!creatorTagValid)
			{
				//card probably changed by self
				var found = _fakePremiumCards.TryGetValue(to.ID, out premium);
				SimpleLog(string.Format("Card {0} in cache. {1}",
										FoundMsg(found),
										from.GetEntityId( ) == to.ID ? "texture updated" : $"changed from {from.GetEntityId( )} to {to.ID}"));
			}
			else
			{
				//prefer creator's tag
				var found = _fakePremiumCards.TryGetValue(creatorTag.Value, out premium);
				var creator = GameState.Get( ).GetEntity(creatorTag.Value);

				if (creator == null)
				{
					SimpleLog($"Creator {FoundMsg(found)}");
				}
				else
				{
					var removed = creator.GetZone( ) == TAG_ZONE.REMOVEDFROMGAME;

					SimpleLog(string.Format("Creator {0} {1} in cache{2}",
											creator,
											FoundMsg(found),
											removed ? ", REMOVED from game!" : string.Empty));

					if (!found && removed)
					{
						creatorTagValid = false;
						goto CREATOR_CHECK;
					}
				}
			}

			if (premium == default)
			{
				Logger.Message($"Used default ({premium}) tag.");
				return false;
			}
			if (premium >= TAG_PREMIUM.DIAMOND) // '>=' for future use
			{
				//target probably isn't diamond
				var newPremium = toEntdef.SelectBestPremiumType(true);
				if (newPremium != premium)
				{
					SimpleLog($"{premium} tag owerriden with {newPremium}");
					premium = newPremium;
				}
			}

			string FinalMsg( ) => $"Updating successful. {premium} tag selected";

			if (from.GetEntityId( ) == to.ID)
			{
				SimpleLog(FinalMsg( ));
			}
			else
			{
				_fakePremiumCards.Add(to.ID, premium);
				SimpleLog($"{FinalMsg( )}. New entity stored in cache");
			}

			premiumTag.Value = (int) premium;
			return true;
		}
	}

	public partial class Entity
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(Ent.OnFullEntity))]
		public static void OnFullEntity([NotNull] Ent __instance)
		{
			if (!CardInfo.CanFakeGoldenTag( ))
				return;

			//golden cards while game starts
			SetGoldenTag(__instance);
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Ent.OnShowEntity))]
		public static void OnShowEntity([NotNull] Ent __instance)
		{
			if (!CardInfo.CanFakeGoldenTag( ))
				return;

			//golden card when it taken from the deck, played by enemy, etc...
			SetGoldenTag(__instance);
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
			if (!CardInfo.CanFakeGoldenTag( ))
				return true;

			//full original function rebuild

			if (___m_transformPowersProcessed.Contains(changeEntity))
			{
				___m_transformPowersProcessed.Remove(changeEntity);
			}
			else
			{
				var dontUpdateActor = RestoreFakePremium(__instance, changeEntity.Entity);

				//if (dontUpdateActor)
				//{
				//	Logger.Message("___m_transformPowersProcessed:" + string.Join(", ", ___m_transformPowersProcessed));
				//	Logger.Message("___m_subCardIDs:" + string.Join(", ", ___m_subCardIDs));
				//	Logger.Message("___m_queuedChangeEntityCount:" + ___m_queuedChangeEntityCount);
				//}

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
					updateActor = UpdateActorFn( ),
					restartStateSpells = (bool) ShouldRestartStateSpellsOnChangeEntity.Invoke(__instance, new object[ ] {changeEntity}),
					fromChangeEntity = true
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
