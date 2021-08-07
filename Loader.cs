using System;
using System.Collections.Generic;
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

        private static void RenewLogWriter([CanBeNull] string file_writer_path = null)
        {
            var file_writer_path_old = HarmonyFileLog.FileWriterPath;
            var writer_old = HarmonyFileLog.Writer;

            if (!string.IsNullOrEmpty(file_writer_path))
                HarmonyFileLog.FileWriterPath = file_writer_path;
            HarmonyFileLog.Writer = new StreamWriter(new MemoryStream()) { AutoFlush = true };

            if (writer_old == null) return;
            if (!(writer_old is StreamWriter writer)) return;
            if (!(writer.BaseStream is MemoryStream stream)) return;

            var bytes_used = (int)stream.Position;
            if (bytes_used == 0) return;
            using (var file = File.Create(file_writer_path_old, bytes_used))
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
            const string fileName = "PatchResult";
            var logs_directory = Logger.LogsPath;

            var dir_info = new DirectoryInfo(logs_directory);
            foreach (var file in dir_info.EnumerateFiles().Where(file => file.Name.StartsWith(fileName, StringComparison.Ordinal)))
                file.Delete();
            //string FilePath = Path.Combine(logs_path, $"{FileName}_default.log");

            void _SetLogPath(string postfix, string extension)
            {
                var name = $"{fileName}_{postfix}";
                if (!string.IsNullOrEmpty(extension))
                    name += $".{extension}";

                var file_writer_path = Path.Combine(logs_directory, name);
                RenewLogWriter(file_writer_path);
            }

            _SetLogPath("default", "log");
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
                make_log.Invoke(target_instance, new object[] { logs_directory });

                var log_path_prop = AccessTools.Property(target_class, "LogPath");
                var log_str = (string)log_path_prop.GetValue(target_instance, null);

                const string findStr = "hearthstone_";
                var offset = log_str.LastIndexOf(findStr, StringComparison.Ordinal) + findStr.Length;
                var log_info = log_str.Substring(offset);
                _SetLogPath(log_info, null);

                return true;
            }
            catch (Exception e)
            {
                HarmonyFileLog.Writer.WriteLine(e.ToString());
                ShowLogFile();

                return false;
            }
        }

        private static Harmony m_patcher;

        private struct EnumInfo
        {
            public string[] Names;
            public int[] Values;
        }

        private static bool ValidateSharedData()
        {
#if DEBUG
            return true;
#else

            var resolved_types = new Dictionary<string, EnumInfo>();

            void _ValidateEnum<T>(string name, T value)
                where T : Enum
            {
                var type = (typeof(T));
                if (!resolved_types.TryGetValue(type.Name, out var info))
                {
                    info = new EnumInfo { Names = type.GetEnumNames(), Values = type.GetEnumValues().Cast<int>().ToArray() };
                    resolved_types.Add(name, info);
                }

                for (var i = 0; i < info.Names.Length; i++)
                {
                    if (info.Names[i] == name)
                    {
                        if (info.Values[i] == (int)(object)value) return;
                        throw new Exception($"Enum {name} changed from {value} to {info.Values[i]}");
                    }
                }

                throw new Exception($"Enum {name} not found");
            }

            try
            {
                _ValidateEnum(nameof(GAME_TAG.HAS_DIAMOND_QUALITY), GAME_TAG.HAS_DIAMOND_QUALITY);
                _ValidateEnum(nameof(GAME_TAG.PREMIUM), GAME_TAG.PREMIUM);
                return true;
            }
            catch (Exception e)
            {
                if (!HarmonyFileLog.Enabled)
                {
                    if (Environment.GetEnvironmentVariable("HARMONY_DEBUG") == null)
                        HarmonyFileLog.Enabled = true;
                    else
                        return false;
                }

                HarmonyFileLog.Writer.WriteLine(e.ToString());
                ShowLogFile();
                return false;
            }

#endif
        }

        private static bool ApplyPatches()
        {
            try
            {
                m_patcher = new Harmony("patcher");
                m_patcher.PatchAll();
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
                    m_patcher.UnpatchSelf();
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

            if (!SetupLogging() || !ValidateSharedData() || !ApplyPatches()) ExitApp();
        }
    }
}
