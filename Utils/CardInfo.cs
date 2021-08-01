using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace hearthstone_ex.Utils
{
    public static class CardInfo
    {
        //public const TAG_PREMIUM CUSTOM_PREMIUM_TAG = TAG_PREMIUM.GOLDEN;

        private static readonly Dictionary<string, bool[]> m_premiumTagsCache = new Dictionary<string, bool[]>();

        public static bool HavePremiumTag([NotNull] string card_id, TAG_PREMIUM tag)
        {
            if (!m_premiumTagsCache.TryGetValue(card_id, out var info))
            {
                var all_cards = CollectionManager.Get().GetAllCards();
                info = new bool[Enum.GetNames(typeof(TAG_PREMIUM)).Length];
                for (var i = 0; i < info.Length; ++i)
                    info[i] = false;
                m_premiumTagsCache.Add(card_id, info);
                foreach (var type in all_cards.Where(card => card.CardId == card_id).Select(card => (int)card.PremiumType))
                    info[type] = true;
            }

            return info[(int)tag];
        }

        public static bool HavePremiumTag([NotNull] this EntityBase ent, TAG_PREMIUM tag)
        {
            var id = ent.GetCardId();
            return HavePremiumTag(id, tag);
        }

        public static TAG_PREMIUM GetIdealPremiumTag([NotNull] string card_id)
        {
            //@note: add cfg here if wanted
            return HavePremiumTag(card_id, TAG_PREMIUM.DIAMOND) ? TAG_PREMIUM.DIAMOND : TAG_PREMIUM.GOLDEN;
        }

        public static TAG_PREMIUM GetIdealPremiumTag([NotNull] this EntityBase ent)
        {
            return GetIdealPremiumTag(ent.GetCardId());
        }
    }
}
