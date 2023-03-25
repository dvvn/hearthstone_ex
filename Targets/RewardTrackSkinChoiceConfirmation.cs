using HarmonyLib;
using Hearthstone.UI;
using hearthstone_ex.Utils;
using Confirmation = RewardTrackSkinChoiceConfirmation;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(Confirmation))]
	public class RewardTrackSkinChoiceConfirmation : LoggerGui.Static<Confirmation>
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(Awake))]
		public static void Awake(WidgetTemplate ___m_widget)
		{
			___m_widget.RegisterEventListener(eventName => Logger.Message(eventName));
		}
	}
}