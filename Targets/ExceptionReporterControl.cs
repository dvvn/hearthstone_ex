using Blizzard.BlizzardErrorMobile;
using HarmonyLib;
using hearthstone_ex.Utils;
using Control = Hearthstone.ExceptionReporterControl;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(Control))]
    public class ExceptionReporterControl : LoggerFile.Static<Control>
    {
        //prevent sending logs to the server 

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

            if (reporter.ReportOnTheFly)
            {
                reporter.ReportOnTheFly = false;
                Logger.Message($"{reporter}.{nameof(reporter.ReportOnTheFly)} forced to {reporter.ReportOnTheFly}");
            }

            if (reporter.SendExceptions == false)
            {
                reporter.SendExceptions = true;
                Logger.Message($"{reporter}.{nameof(reporter.SendExceptions)} forced to {reporter.SendExceptions}");
            }

            if (reporter.SendAsserts == false)
            {
                reporter.SendAsserts = true;
                Logger.Message($"{reporter}.{nameof(reporter.SendAsserts)} forced to {reporter.SendAsserts}");
            }

            if (reporter.SendErrors == false)
            {
                reporter.SendErrors = true;
                Logger.Message($"{reporter}.{nameof(reporter.SendErrors)} forced to {reporter.SendErrors}");
            }
        }
    }
}
