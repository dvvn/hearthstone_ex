using JetBrains.Annotations;

namespace hearthstone_ex.Utils
{
    public static class CardInfo
    {
        public static TAG_PREMIUM GetBestPossiblePremiumType([NotNull] this EntityDef ent, bool collectibe = false)
        {
            return ent.HasTag(GAME_TAG.HAS_DIAMOND_QUALITY) ? TAG_PREMIUM.DIAMOND :
                   (collectibe || ent.HasTag(GAME_TAG.COLLECTIBLE)) ? TAG_PREMIUM.GOLDEN : TAG_PREMIUM.NORMAL;
        }

        public static TAG_PREMIUM GetBestPossiblePremiumType([NotNull] this Entity ent)
        {
            return ent.GetCard().GetGoldenMaterial() == null ? TAG_PREMIUM.NORMAL :
                   ent.HasTag(GAME_TAG.HAS_DIAMOND_QUALITY) ? TAG_PREMIUM.DIAMOND : TAG_PREMIUM.GOLDEN;
        }
    }
}
