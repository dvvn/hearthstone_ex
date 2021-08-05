﻿using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using static AlertPopup;
using Manager = DialogManager;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Manager))]
    public class DialogManager : LoggerGui.Static<DialogManager>
    {
        private static readonly string[] m_headers = { "GLUE_COLLECTION_DELETE_CONFIRM_HEADER", "GLUE_CRAFTING_DISENCHANT_CONFIRM_HEADER" };
        private static string[] m_localizedHeaders;
        private static Locale m_gameLocale = Locale.UNKNOWN;

        //private static readonly Dictionary<Locale, string[]> m_LocalizedHeaders = new Dictionary<Locale, string[]>(1);
        private static bool Ignore([NotNull] string text)
        {
            var locale = Localization.GetLocale();
            if (m_gameLocale != locale)
            {
                m_localizedHeaders = m_headers.Select(GameStrings.Get).ToArray();
                m_gameLocale = locale;
            }

            return m_localizedHeaders.Any(text.Equals);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Manager.ShowPopup), typeof(PopupInfo))]
        public static bool ShowPopup([NotNull] PopupInfo info)
        {
            if (info.m_responseDisplay != ResponseDisplay.CONFIRM_CANCEL)
                return true;

            //skip confirmations on some actions
            if (Ignore(info.m_headerText))
            {
                info.m_responseCallback(Response.CONFIRM, null);
                return false;
            }

            return true;
        }
    }
}
