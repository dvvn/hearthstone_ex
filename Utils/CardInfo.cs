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
		private static readonly IDictionary<string, bool> m_premiumTexturesInfo = new ConcurrentDictionary<string, bool>();

		public static bool HavePremiumTexture([CanBeNull] string card_id, [CanBeNull] Action<string> logger = null)
		{
			if (string.IsNullOrEmpty(card_id))
				return false;

			if (m_premiumTexturesInfo.TryGetValue(card_id, out var result))
				return result;

			var asset = HearthstoneServices.Get<IAliasedAssetResolver>().GetCardDefAssetRefFromCardId(card_id);
			var prefab = AssetLoader.Get().GetOrInstantiateSharedPrefab(asset);

			if (prefab == default)
			{
				logger?.Invoke("card prefab is null");
				return false;
			}

			using (prefab)
			{
				var card_def = prefab.Asset.GetComponent<CardDef>();
				if (card_def == default)
				{
					logger?.Invoke("card def is null");
					return false;
				}

				result = CardTextureLoader.PremiumAnimationAvailable(card_def);
				m_premiumTexturesInfo.Add(card_id, result);

				if (result == false && logger != default && card_def.GetPortraitQuality().TextureQuality == CardPortraitQuality.NOT_LOADED)
					logger.Invoke("texture isn't loaded");

				return result;
			}
		}

		public static bool HavePremiumTexture([NotNull] this EntityBase ent, [CanBeNull] Action<string> logger = null)
		{
			return HavePremiumTexture(ent.GetCardId(), logger == default ? (Action<string>)null : msg => logger($"{ent} -> {msg}"));
		}

		public static bool HavePremiumTexture([NotNull] this Card card)
		{
			return card.GetGoldenMaterial() != null;
		}

		public static bool HavePremiumTexture([NotNull] this Entity ent)
		{
			return ent.GetCard().HavePremiumTexture();
		}

		private static TAG_PREMIUM SelectBestPremiumType([NotNull] this EntityBase ent, bool have_premium_texture)
		{
			return have_premium_texture == false ? TAG_PREMIUM.NORMAL :
				   ent.HasTag(GAME_TAG.HAS_DIAMOND_QUALITY) ? TAG_PREMIUM.DIAMOND : TAG_PREMIUM.GOLDEN;
		}

		public static TAG_PREMIUM GetBestPossiblePremiumType([NotNull] this EntityBase ent, [CanBeNull] Action<string> logger = null)
		{
			return ent.SelectBestPremiumType(ent.HavePremiumTexture(logger));
		}

		public static TAG_PREMIUM GetBestPossiblePremiumType([NotNull] this Entity ent)
		{
			return ent.GetEntityDef().SelectBestPremiumType(ent.HavePremiumTexture());
		}

		public static bool CreatedByFriendlyPlayer([NotNull] this EntityBase ent)
		{
			return ent.GetCreatorId() == GameState.Get().GetFriendlySidePlayer().GetPlayerId();
		}

		public static bool ControlledByFriendlyPlayer([NotNull] this EntityBase ent)
		{
			return ent.GetControllerId() == GameState.Get().GetFriendlySidePlayer().GetPlayerId();
		}
	}

	internal static class Wrapper
	{
		private struct TagInfo
		{
			//GAME_TAG.XXXX
			public string key;
			//TAG_XXXXX
			public Type tag;

			public override string ToString()
			{
				return $"{key}: {tag}";
			}
		}

		private static IReadOnlyDictionary<int, TagInfo> FillAllTags()
		{
			var known_tags = AccessTools.AllTypes().Where(t => t.IsEnum && t.Name.StartsWith("TAG_")).ToArray();

			var dict = new Dictionary<int, TagInfo>();
			foreach (var value in typeof(GAME_TAG).GetEnumValues().Cast<GAME_TAG>())
			{
				var name = value.ToString();

				if (dict.ContainsKey((int)value))
				{
					var entry = dict[(int)value];
					entry.key = null;
				}
				else
				{
					dict.Add((int)value, new TagInfo
					{
						key = name,
						tag = known_tags.FirstOrDefault(t => t.Name.Length == name.Length + 4 && t.Name.EndsWith(name))
					});
				}
			}

			return dict;
		}
		 
		private static readonly IReadOnlyDictionary<int, TagInfo> all_tags = FillAllTags();

		public static string ToString(this TagMap tags)
		{
			var builder = new StringBuilder();

			foreach (var item in tags.GetMap().OrderBy(p => p.Key))
			{
				if (builder.Length > 0)
					builder.Append(", ");
				
				if (!all_tags.TryGetValue(item.Key, out TagInfo info))
					info = new TagInfo();

				var key_str = info.key == null ? item.Key.ToString() : info.key.ToString();
				var tag_str = info.tag == default ? item.Value.ToString() : Enum.GetName(info.tag, item.Value);

				builder.Append($"{key_str}: {tag_str}");
			}

			return builder.ToString();
		}
	}
}
