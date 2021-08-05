using JetBrains.Annotations;
using PegasusShared;
using UnityEngine;

namespace hearthstone_ex.Utils
{
    using System;

    public static class CardInfo
    {
        public static bool HavePremiumTexture(string card_id, [CanBeNull] Action<string> logger = null)
        {
            var assetRefFromCardId = HearthstoneServices.Get<IAliasedAssetResolver>().GetCardDefAssetRefFromCardId(card_id);
            var cardPrefabInstance = AssetLoader.Get().GetOrInstantiateSharedPrefab(assetRefFromCardId);

            if (cardPrefabInstance == default)
            {
                logger?.Invoke("card prefab is null");
                return false;
            }

            var cardDef = cardPrefabInstance.Asset.GetComponent<CardDef>();

            if (CardTextureLoader.PremiumAnimationAvailable(cardDef))
            {
                /*if (logger != default)
                {
                    var material = cardDef.GetPremiumPortraitMaterial();
                    logger.Invoke(material == default ? "texture is loading" : material.ToString());
                }*/

                return true;
            }

            if (logger != default && cardDef.GetPortraitQuality().TextureQuality == CardPortraitQuality.NOT_LOADED)
                logger.Invoke("texture isn't loaded");

            return false;
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
    }
}
