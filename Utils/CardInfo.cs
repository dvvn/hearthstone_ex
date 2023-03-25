using JetBrains.Annotations;
using PegasusShared;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Blizzard.T5.Services;

namespace hearthstone_ex.Utils
{
	internal static class CardInfo
	{
		private static readonly IDictionary<string, bool> _premiumTexturesInfo = new Dictionary<string, bool>();

		public static bool HavePremiumTexture(string cardId, Action<string> logger = null)
		{
			if (string.IsNullOrEmpty(cardId))
				return false;

			if (_premiumTexturesInfo.TryGetValue(cardId, out var result))
				return result;

			var asset = ServiceManager.Get<IAliasedAssetResolver>().GetCardDefAssetRefFromCardId(cardId);
			var prefab = AssetLoader.Get().GetOrInstantiateSharedPrefab(asset);

			if (prefab == default)
			{
				logger?.Invoke("card prefab is null");
				return false;
			}

			using (prefab)
			{
				var cardDef = prefab.Asset.GetComponent<CardDef>();
				if (cardDef == default)
				{
					logger?.Invoke("card def is null");
					return false;
				}

				result = CardTextureLoader.PremiumAnimationAvailable(cardDef);
				_premiumTexturesInfo.Add(cardId, result);

				if (result == false && logger != default && cardDef.GetPortraitQuality().TextureQuality == CardPortraitQuality.NOT_LOADED)
					logger.Invoke("texture isn't loaded");

				return result;
			}
		}

		public static bool HavePremiumTexture(this EntityBase ent, Action<string> logger = null)
		{
			return HavePremiumTexture(ent.GetCardId(), logger == default ? (Action<string>)null : msg => logger($"{ent} -> {msg}"));
		}

		//not work anymore
		/*public static bool HavePremiumTexture(this Card card)
		{
			return card.GetGoldenMaterial() != null;
		}

		public static bool HavePremiumTexture(this Entity ent)
		{
			return ent.GetCard().HavePremiumTexture();
		}*/

		public static TAG_PREMIUM SelectBestPremiumType(this EntityBase ent, bool havePremiumTexture)
		{
			if (!havePremiumTexture)
				return TAG_PREMIUM.NORMAL;
			if (ent.HasTag(GAME_TAG.HAS_SIGNATURE_QUALITY))
				return TAG_PREMIUM.SIGNATURE;
			if (ent.HasTag(GAME_TAG.HAS_DIAMOND_QUALITY))
				return TAG_PREMIUM.DIAMOND;
			return TAG_PREMIUM.GOLDEN;
		}

		public static TAG_PREMIUM GetBestPossiblePremiumType(this EntityBase ent, Action<string> logger = null)
		{
			return ent.SelectBestPremiumType(ent.HavePremiumTexture(logger));
		}

		public static TAG_PREMIUM GetBestPossiblePremiumType(this Entity ent)
		{
			return ent.GetEntityDef().SelectBestPremiumType(ent.HavePremiumTexture());
		}

		[Obsolete("Doesn't work properly")]
		public static bool CreatedByFriendlyPlayer(this EntityBase ent)
		{
			return ent.GetCreatorId() == GameState.Get().GetFriendlySidePlayer().GetPlayerId();
		}

		[Obsolete("Doesn't work properly")]
		public static bool ControlledByFriendlyPlayer(this Network.Entity ent)
		{
			//FAKE_CONTROLLER?

			var controller = ent.Tags.FirstOrDefault(p => p.Name == (int)GAME_TAG.CONTROLLER);
			return controller != default && controller.Value == GameState.Get().GetFriendlySidePlayer().GetPlayerId();
		}

		public static bool ControlledByFriendlyPlayer(this EntityBase ent)
		{
			return ent.GetControllerId() == GameState.Get().GetFriendlySidePlayer().GetPlayerId();
		}

		public static bool CanFakeGoldenTag()
		{
			//if any custom config here, add it also to
			//DeckTrayDeckTileVisual::SetUpActor
			//

			if (SpectatorManager.Get().IsSpectatingOrWatching)
				return false;
			var mgr = GameMgr.Get();
			if (mgr.IsBattlegroundsTutorial() || mgr.IsLettuceTutorial() || mgr.IsTraditionalTutorial())
				return false;
			if (mgr.IsArena())
				return !Options.Get().GetBool(Option.HAS_DISABLED_PREMIUMS_THIS_DRAFT);
			if (mgr.IsBattlegrounds())
				return false;
			//todo: if brawl/dungeon with generated deck return false
			return true;
		}

		public static IEnumerable<EntityDef> GetAllEntityDefs()
		{
			return DefLoader.Get().GetAllEntityDefs().Select(p => p.Value);
		}
	}
}