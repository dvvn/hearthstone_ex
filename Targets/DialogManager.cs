using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using hearthstone_ex.Utils;
using static AlertPopup;
using Manager = DialogManager;
using HsString = GameStrings;

namespace hearthstone_ex.Targets
{
	[HarmonyPatch(typeof(Manager))]
	public class DialogManager : LoggerGui.Static<DialogManager>
	{
		private static readonly string[] _headers = { "GLUE_COLLECTION_DELETE_CONFIRM_HEADER", "GLUE_CRAFTING_DISENCHANT_CONFIRM_HEADER" };

		private static ICollection<string> _localizedHeaders /*=
			Enumerable.Repeat(string.Empty, _headers.Length).ToArray()*/;

		public static void Load()
		{
			_localizedHeaders = _headers.Select(HsString.Get).ToArray();
		}

		private static bool Ignore(string text)
		{
			return _localizedHeaders.Any(text.Equals);
		}

		[HarmonyPrefix]
		[HarmonyPatch(nameof(Manager.ShowPopup), typeof(PopupInfo))]
		public static bool ShowPopup(PopupInfo info)
		{
			if (info.m_responseDisplay != ResponseDisplay.CONFIRM_CANCEL)
				return HookInfo.CALL_ORIGINAL;

			//skip confirmations on some actions
			if (Ignore(info.m_headerText))
			{
				info.m_responseCallback(Response.CONFIRM, null);
				return HookInfo.SKIP_ORIGINAL;
			}

			return HookInfo.CALL_ORIGINAL;
		}
	}
}