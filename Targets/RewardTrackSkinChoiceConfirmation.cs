using HarmonyLib;
using Hearthstone.UI;
using hearthstone_ex.Utils;
using JetBrains.Annotations;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(global::RewardTrackSkinChoiceConfirmation))]
	public class RewardTrackSkinChoiceConfirmation : LoggerGui.Static<RewardTrackSkinChoiceConfirmation>
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(Awake))]
		public static void Awake([NotNull] WidgetTemplate ___m_widget)
		{
			___m_widget.RegisterEventListener(eventName => Logger.Message(eventName));
		}
	}
}
