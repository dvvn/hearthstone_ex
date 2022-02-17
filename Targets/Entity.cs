using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using hearthstone_ex.Utils;
using Ent = Entity;
using Net = Network;
using HistoryMgr = HistoryManager;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(Ent))]
	public partial class Entity : LoggerGui.Static<Entity>
	{
		private static bool UseRealGoldenTag( )
		{
			return SpectatorManager.Get( ).IsSpectatingOrWatching || GameMgr.Get( ).IsBattlegrounds( ) || !HistoryMgr.Get( ).IsHistoryEnabled( );
		}

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

		private static void RegisterFakePremiumCard([NotNull] EntityBase ent, TAG_PREMIUM tag, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var id = ent.GetEntityId( );
			if (_fakePremiumCards.TryGetValue(ent.GetEntityId( ), out _))
			{
				Logger.Message($"{ent} already added", memberName, sourceLineNumber);
			}
			else
			{
				switch (ent.GetZone( ))
				{
					case TAG_ZONE.PLAY:
					case TAG_ZONE.DECK:
					case TAG_ZONE.HAND:
					case TAG_ZONE.SECRET:
					case TAG_ZONE.SETASIDE:
					{
						_fakePremiumCards.Add(id, tag);
						Logger.Message($"{ent} added ({tag})", memberName, sourceLineNumber);
						break;
					}
					default:
					{
						Logger.Message($"{ent} NOT added. Tags:{Environment.NewLine}" + ent.GetTags( ).MakeString( ), memberName, sourceLineNumber);
						break;
					}
				}
			}
		}

		public static void ResetFakePremiumData([CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			_fakePremiumCards.Clear( );
			Logger.Message("Cleared", memberName, sourceLineNumber);
		}

		private static void SetGoldenTag([NotNull] Ent ent, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			if (!ent.ControlledByFriendlyPlayer /*CreatedByFriendlyPlayer*/( ))
			{
				//Logger.Message($"{__instance} not owned");
				return;
			}

			if (ent.GetPremiumType( ) != TAG_PREMIUM.NORMAL)
				return;

			TAG_PREMIUM tag = default;

			if (!string.IsNullOrEmpty(ent.GetCardId( )))
			{
				tag = ent.GetBestPossiblePremiumType( );
			}
			else
			{
				var name = ent. /*GetEntityDef().*/GetName( );
				var entDef = GetAllEntityDefs( ).FirstOrDefault(e => e.GetName( ) == name);
				if (entDef != default)
				{
					tag = entDef.SelectBestPremiumType(entDef.HavePremiumTexture( ));
					ent.LoadCard(entDef.GetCardId( ));
				}
			}

			if (tag == TAG_PREMIUM.NORMAL)
			{
				Logger.Message($"{ent} have no golden material", memberName, sourceLineNumber);
				return;
			}

			RegisterFakePremiumCard(ent, tag, memberName, sourceLineNumber);
			ent.SetTag(GAME_TAG.PREMIUM, tag);

			//Logger.Message($"{ent} set to {tag}");
		}

		[NotNull]
		private static string ParseFlags([NotNull] TagMap map)
		{
			//var strings = pairs.Select(t => new {name = ((GAME_TAG) t.Key).ToString( ), value = t.Value}).OrderByDescending(t => t.name).Select(t => $"{t.name}: {t.value}");
			//return string.Join(", ", strings);
			return map.MakeString( );
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
		//stolen from deck cards ignored. to fixe it store ZONE_POSITION tag
		private static bool RestoreFakePremium([NotNull] Ent from, [NotNull] Net.Entity to)
		{
			if (UseRealGoldenTag( ))
				return false;

			var toEntdef = GetAllEntityDefs( ).First(e => e.GetCardId( ) == to.CardID);

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

			Logger.Message($"Updating{Environment.NewLine}{PrintFromTo(FROM_TO_FLAGS | FROM_TO_FLAGS)}");

			if (!toEntdef.HavePremiumTexture( ))
			{
				Logger.Message("Target ent doesn't have premium texture!");
				return false;
			}

			try
			{
				//sometimes it hits null pointer 
				Logger.Message($"Known fake premium cards:{Environment.NewLine}" +
							   string.Join(Environment.NewLine, _fakePremiumCards.Select(p => GameState.Get( ).GetEntity(p.Key))
																				 .OrderByDescending(s => s.GetEntityId( )))
							   , string.Empty, string.Empty, 0);
			}
			catch (Exception)
			{
				// ignored
			}

			Logger.Message("Trying to find native id", string.Empty, string.Empty, 0);
			if (!_fakePremiumCards.TryGetValue(to.ID, out var premium))
			{
				var creator = to.Tags.FirstOrDefault(t => t.Name == (int) GAME_TAG.CREATOR);
				if (creator != default)
				{
					Logger.Message($"Trying to find creator {GameState.Get( ).GetEntity(creator.Value)}", string.Empty, string.Empty, 0);
					if (!_fakePremiumCards.TryGetValue(creator.Value, out premium))
					{
						premium = default;
					}
				}
			}

			if (premium == default)
			{
				Logger.Message("Not found!");
				return false;
			}
			if (premium >= TAG_PREMIUM.DIAMOND) //>= for future use
			{
				premium = toEntdef.SelectBestPremiumType(true);
			}

			Logger.Message($"Updating successful. {premium} tag selected", string.Empty, string.Empty, 0);

			if (from.GetEntityId( ) != to.ID)
				_fakePremiumCards.Add(to.ID, premium);

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
			if (UseRealGoldenTag( ))
				return;

			//golden cards while game starts
			SetGoldenTag(__instance);
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(Ent.OnShowEntity))]
		public static void OnShowEntity([NotNull] Ent __instance)
		{
			if (UseRealGoldenTag( ))
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
