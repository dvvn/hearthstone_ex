﻿using Blizzard.BlizzardErrorMobile;
using HarmonyLib;
using hearthstone_ex.Utils;
using Control = Hearthstone.ExceptionReporterControl;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Control))]
    public class ExceptionReporterControl : LoggerConsole.Static<Control>
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Control.ExceptionReportInitialize))]
        public static void ExceptionReportInitialize()
        {
            var reporter = ExceptionReporter.Get();

            if (reporter.IsInDebugMode)
                Logger.Message($"{reporter} already in debug mode");
            else
            {
                Logger.Message($"{reporter} isn't in debug mode, forcing...");
                reporter.IsInDebugMode = true;
            }
        }
    }
}