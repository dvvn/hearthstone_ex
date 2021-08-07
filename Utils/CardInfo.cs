using JetBrains.Annotations;
using PegasusShared;
using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
            return have_premium_texture == false            ? TAG_PREMIUM.NORMAL :
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
}
