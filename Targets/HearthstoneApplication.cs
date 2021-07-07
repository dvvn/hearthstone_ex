using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Blizzard.BlizzardErrorMobile;
using HarmonyLib;
using hearthstone_ex.Utils;
using JetBrains.Annotations;
using Microsoft.Win32;
using App = Hearthstone.HearthstoneApplication;

namespace hearthstone_ex.Targets
{
    [HarmonyPatch(typeof(App))]
    public class HearthstoneApplication : LoggerConsole.Static<HearthstoneApplication>
    {
        [NotNull]
        private static string GetFrameworkVersion()
        {
            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                                           .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    var releaseKey = (int) ndpKey.GetValue("Release");
                    if (releaseKey >= 528040)
                        return "4.8 or later";
                    if (releaseKey >= 461808)
                        return "4.7.2";
                    if (releaseKey >= 461308)
                        return "4.7.1";
                    if (releaseKey >= 460798)
                        return "4.7";
                    if (releaseKey >= 394802)
                        return "4.6.2";
                    if (releaseKey >= 394254)
                        return "4.6.1";
                    if (releaseKey >= 393295)
                        return "4.6";
                    if (releaseKey >= 379893)
                        return "4.5.2";
                    if (releaseKey >= 378675)
                        return "4.5.1";
                    if (releaseKey >= 378389)
                        return "4.5";
                    // This code should never execute. A non-null release key should mean
                    // that 4.5 or later is installed.
                    //return "No 4.5 or later version detected";
                }
                else
                {
                    var installed_versions = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP");
                    if (installed_versions != null)
                    {
                        var version_names = installed_versions.GetSubKeyNames();

                        //version names start with 'v', eg, 'v3.5' which needs to be trimmed off before conversion
                        var Framework = Convert.ToDouble(version_names[version_names.Length - 1].Remove(0, 1), CultureInfo.InvariantCulture);
                        var key = installed_versions.OpenSubKey(version_names[version_names.Length - 1]);
                        if (key != null)
                        {
                            var SP = Convert.ToInt32(key.GetValue("SP", 0));
                            return $"{Framework}.{SP}";
                        }
                    }

                    return "UNKNOWN";
                }
            }

            throw new MissingMethodException($"{nameof(GetFrameworkVersion)}: unable to get version");
        }

        [Conditional("DEBUG")]
        private static void LogFrameworkVersion([NotNull] CallerInfo info)
        {
            Logger.Message(RuntimeInformation.FrameworkDescription, info);
            Logger.Message($"Framework: {GetFrameworkVersion()}", info);
            Logger.Message($"CLR: {Environment.Version}", info);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(RunStartup))]
        public static void RunStartup([NotNull] App __instance)
        {
            Loader.OnGameStartup(__instance);
            var debug_info = new CallerInfoMin();
            Logger.Message("Started!", debug_info);
            LogFrameworkVersion(debug_info);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ExceptionReportInitialize))]
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

        private static ApplicationMode? m_applicationModeOwerriden;

        public class AppModeOwerriden : IDisposable
        {
            private readonly ApplicationMode? m_prevMode;

            public AppModeOwerriden(ApplicationMode application_mode)
            {
                this.m_prevMode = m_applicationModeOwerriden;
                m_applicationModeOwerriden = application_mode;
                Logger.Message($"App mode forced to {application_mode}");
            }

            public void Dispose()
            {
                m_applicationModeOwerriden = this.m_prevMode;
                Logger.Message($"App mode restored to {this.m_prevMode?.ToString() ?? "default"}");
            }
        }

        [NotNull]
        public static AppModeOwerriden OwerrideAppMode(ApplicationMode mode) => new AppModeOwerriden(mode);

        [HarmonyPrefix]
        [HarmonyPatch(nameof(App.GetMode))]
        public static bool GetMode(ref ApplicationMode __result)
        {
            if (m_applicationModeOwerriden.HasValue)
            {
                __result = m_applicationModeOwerriden.Value;
                return false;
            }

            return true;
        }
    }
}
