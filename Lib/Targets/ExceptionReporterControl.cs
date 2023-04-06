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

			bool LogValue(string name, bool value, bool targetValue)
			{
				Logger.Message(value == targetValue ? $"{reporter}.{name} is already {value}" : $"{reporter}.{name} forced to {targetValue}");
				return targetValue;
			}

			const bool SEND_REPORTS = false;

			//disable old records deserializing
			reporter.IsInDebugMode = LogValue(nameof(reporter.IsInDebugMode), reporter.IsInDebugMode, true);
			//dont make zips
			//reporter.ReportOnTheFly = LogValue(nameof(reporter.ReportOnTheFly), reporter.ReportOnTheFly, true);

			reporter.SendExceptions = LogValue(nameof(reporter.SendExceptions), reporter.SendExceptions, SEND_REPORTS);
			reporter.SendAsserts = LogValue(nameof(reporter.SendAsserts), reporter.SendAsserts, SEND_REPORTS);
			reporter.SendErrors = LogValue(nameof(reporter.SendErrors), reporter.SendErrors, SEND_REPORTS);
			//reporter.IsFakeReport = LogValue(nameof(reporter.IsFakeReport), reporter.IsFakeReport, SEND_REPORTS == false);
		}
	}
}