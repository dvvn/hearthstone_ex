using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using HarmonyLib.Tools;
using JetBrains.Annotations;
using HarmonyLog = HarmonyLib.Tools.Logger;
using HarmonyLogChannel = HarmonyLib.Tools.Logger.LogChannel;

#pragma warning disable 618

namespace hearthstone_ex
{
    public class Loader
    {
        public static event Action OnCleanup, OnShutdown;

        private static void RenewLogWriter([CanBeNull] string FileWriterPath = null)
        {
            var FileWriterPath_old = HarmonyFileLog.FileWriterPath;
            var Writer_old = HarmonyFileLog.Writer;

            if (!string.IsNullOrEmpty(FileWriterPath))
                HarmonyFileLog.FileWriterPath = FileWriterPath;
            HarmonyFileLog.Writer = new StreamWriter(new MemoryStream()) {AutoFlush = true};

            if (Writer_old == null) return;
            if (!(Writer_old is StreamWriter writer)) return;
            if (!(writer.BaseStream is MemoryStream stream)) return;

            var bytes_used = (int) stream.Position;
            if (bytes_used == 0) return;
            using (var file = File.Create(FileWriterPath_old, bytes_used))
            {
                file.Write(stream.GetBuffer(), 0, bytes_used);
            }
        }

        private static void ShowLogFile(string app_name = "notepad.exe")
        {
            RenewLogWriter();

            var path = HarmonyFileLog.FileWriterPath;
            if (!File.Exists(path))
                throw new FileNotFoundException($"\"{path}\" not found");
            var proc = Process.Start(app_name, path);
            if (proc == null)
                throw new Win32Exception($"Unable to start {app_name} with \"{path}\"");

            OnShutdown += () =>
            {
                try
                {
                    proc.Kill();
                }
                catch { }
            };
            OnShutdown += () =>
            {
                try
                {
                    File.Delete(path);
                }
                catch { }
            };
        }

        private static bool SetupLogging()
        {
            const string _File_name = "PatchResult";
            var _Logs_directory = Logger.LogsPath;

            var dir_info = new DirectoryInfo(_Logs_directory);
            foreach (var file in dir_info.EnumerateFiles().Where(file => file.Name.StartsWith(_File_name, StringComparison.Ordinal)))
                file.Delete();
            //string FilePath = Path.Combine(logs_path, $"{FileName}_default.log");

            void SetLogPath(string postfix, string extension)
            {
                var name = $"{_File_name}_{postfix}";
                if (!string.IsNullOrEmpty(extension))
                    name += $".{extension}";

                var FileWriterPath = Path.Combine(_Logs_directory, name);
                RenewLogWriter(FileWriterPath);
            }

            SetLogPath("default", "log");
#if DEBUG
            HarmonyLog.ChannelFilter = HarmonyLogChannel.All;
            HarmonyFileLog.Enabled = true;
#else
            HarmonyLog.ChannelFilter = HarmonyLogChannel.Error | HarmonyLogChannel.Warn;
            var _ = new Harmony("__dummy__"); //to read "HARMONY_DEBUG"
            HarmonyFileLog.Enabled = Harmony.DEBUG;
#endif
            try
            {
                var target_class = AccessTools.TypeByName("LogArchive");
                var target_instance = AccessTools.CreateInstance(target_class);
                var make_log = AccessTools.Method(target_class, "MakeLogPath");
                make_log.Invoke(target_instance, new object[] {_Logs_directory});

                var log_path_prop = AccessTools.Property(target_class, "LogPath");
                var log_str = (string) log_path_prop.GetValue(target_instance, null);

                const string _Find_str = "hearthstone_";
                var offset = log_str.LastIndexOf(_Find_str, StringComparison.Ordinal) + _Find_str.Length;
                var log_info = log_str.Substring(offset);
                SetLogPath(log_info, null);

                return true;
            }
            catch (Exception e)
            {
                HarmonyFileLog.Writer.WriteLine(e.ToString());
                ShowLogFile();

                return false;
            }
        }

        private static Harmony Patcher;

        private static bool ApplyPatches()
        {
            try
            {
                Patcher = new Harmony("patcher");
                Patcher.PatchAll();
#if DEBUG
                ShowLogFile();
#endif
                return true;
            }
            catch
            {
                if (HarmonyFileLog.Enabled)
                    ShowLogFile();
                else if (Environment.GetEnvironmentVariable("HARMONY_DEBUG") == null)
                {
                    Patcher.UnpatchSelf();
                    HarmonyFileLog.Enabled = true;

                    return ApplyPatches();
                }

                return false;
            }
        }

        private static void ExitApp()
        {
            OnCleanup();
            Environment.Exit(1);
            //Application.Quit(1);
        }

        public static void OnGameStartup([NotNull] Hearthstone.HearthstoneApplication app)
        {
            app.OnShutdown += OnCleanup;
            app.OnShutdown += OnShutdown;
        }

        public static void Main(string[] args)
        {
            OnCleanup += () =>
            {
                HarmonyLog.ChannelFilter = HarmonyLogChannel.None;
                HarmonyFileLog.Enabled = Harmony.DEBUG = false;
            };

            if (!SetupLogging() || !ApplyPatches()) ExitApp();
        }
    }
}
