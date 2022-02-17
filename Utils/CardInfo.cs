using JetBrains.Annotations;
using PegasusShared;
using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using HarmonyLib;

namespace hearthstone_ex.Utils
{
	internal static class CardInfo
	{
		private static readonly IDictionary<string, bool> _premiumTexturesInfo = new ConcurrentDictionary<string, bool>( );

		public static bool HavePremiumTexture([CanBeNull] string cardId, [CanBeNull] Action<string> logger = null)
		{
			if (string.IsNullOrEmpty(cardId))
				return false;

			if (_premiumTexturesInfo.TryGetValue(cardId, out var result))
				return result;

			var asset = HearthstoneServices.Get<IAliasedAssetResolver>( ).GetCardDefAssetRefFromCardId(cardId);
			var prefab = AssetLoader.Get( ).GetOrInstantiateSharedPrefab(asset);

			if (prefab == default)
			{
				logger?.Invoke("card prefab is null");
				return false;
			}

			using (prefab)
			{
				var cardDef = prefab.Asset.GetComponent<CardDef>( );
				if (cardDef == default)
				{
					logger?.Invoke("card def is null");
					return false;
				}

				result = CardTextureLoader.PremiumAnimationAvailable(cardDef);
				_premiumTexturesInfo.Add(cardId, result);

				if (result == false && logger != default && cardDef.GetPortraitQuality( ).TextureQuality == CardPortraitQuality.NOT_LOADED)
					logger.Invoke("texture isn't loaded");

				return result;
			}
		}

		public static bool HavePremiumTexture([NotNull] this EntityBase ent, [CanBeNull] Action<string> logger = null)
		{
			return HavePremiumTexture(ent.GetCardId( ), logger == default ? (Action<string>) null : msg => logger($"{ent} -> {msg}"));
		}

		public static bool HavePremiumTexture([NotNull] this Card card)
		{
			return card.GetGoldenMaterial( ) != null;
		}

		public static bool HavePremiumTexture([NotNull] this Entity ent)
		{
			return ent.GetCard( ).HavePremiumTexture( );
		}

		public static TAG_PREMIUM SelectBestPremiumType([NotNull] this EntityBase ent, bool havePremiumTexture)
		{
			return havePremiumTexture == false
					   ? TAG_PREMIUM.NORMAL
					   : ent.HasTag(GAME_TAG.HAS_DIAMOND_QUALITY) ? TAG_PREMIUM.DIAMOND : TAG_PREMIUM.GOLDEN;
		}

		public static TAG_PREMIUM GetBestPossiblePremiumType([NotNull] this EntityBase ent, [CanBeNull] Action<string> logger = null)
		{
			return ent.SelectBestPremiumType(ent.HavePremiumTexture(logger));
		}

		public static TAG_PREMIUM GetBestPossiblePremiumType([NotNull] this Entity ent)
		{
			return ent.GetEntityDef( ).SelectBestPremiumType(ent.HavePremiumTexture( ));
		}

		[Obsolete]
		public static bool CreatedByFriendlyPlayer([NotNull] this EntityBase ent)
		{
			return ent.GetCreatorId( ) == GameState.Get( ).GetFriendlySidePlayer( ).GetPlayerId( );
		}

		public static bool ControlledByFriendlyPlayer([NotNull] this Network.Entity ent)
		{
			//FAKE_CONTROLLER?

			var controller = ent.Tags.FirstOrDefault(p => p.Name == (int) GAME_TAG.CONTROLLER);
			return controller != default && controller.Value == GameState.Get( ).GetFriendlySidePlayer( ).GetPlayerId( );
		}

		public static bool ControlledByFriendlyPlayer([NotNull] this EntityBase ent)
		{
			return ent.GetControllerId( ) == GameState.Get( ).GetFriendlySidePlayer( ).GetPlayerId( );
		}
	}

	internal static class TagConvertor
	{
		private struct TagInfo
		{
			//GAME_TAG.XXXX
			public string Key;

			//TAG_XXXXX
			public Type Tag;

			//public override string ToString( )
			//{
			//	return $"{Key}: {Tag}";
			//}
		}

		private static IReadOnlyDictionary<int, TagInfo> FillAllTags( )
		{
			var knownTags = AccessTools.AllTypes( ).Where(t => t.IsEnum && t.Name.StartsWith("TAG_")).ToArray( );

			var dict = new Dictionary<int, TagInfo>( );
			foreach (var value in typeof(GAME_TAG).GetEnumValues( ).Cast<GAME_TAG>( ))
			{
				var name = value.ToString( );

				if (dict.ContainsKey((int) value))
				{
					var entry = dict[(int) value];
					entry.Key = null;
				}
				else
				{
					dict.Add((int) value, new TagInfo {Key = name, Tag = knownTags.FirstOrDefault(t => t.Name.Length == name.Length + 4 && t.Name.EndsWith(name))});
				}
			}

			return dict;
		}

		private static readonly IReadOnlyDictionary<int, TagInfo> _allTags = FillAllTags( );

		public static string MakeString(this TagMap tags)
		{
			var infoDef = new TagInfo( );
			return string.Join(Environment.NewLine, tags.GetMap( ).Select(item =>
			{
				if (!_allTags.TryGetValue(item.Key, out var info))
					info = infoDef;

				var keyStr = info.Key ?? item.Key.ToString( );
				var tagStr = info.Tag == null ? item.Value.ToString( ) : Enum.GetName(info.Tag, item.Value);

				return new {Key = keyStr, Tag = tagStr};
			}).OrderBy(p => p.Key).Select(p => $"{p.Key}: {p.Tag}"));
		}
	}
}
