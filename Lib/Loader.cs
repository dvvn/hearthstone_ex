using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using HarmonyLib.Tools;
using HarmonyLog = HarmonyLib.Tools.Logger;
using HarmonyLogChannel = HarmonyLib.Tools.Logger.LogChannel;

#pragma warning disable 618

namespace hearthstone_ex
{
	public class Loader
	{
		public static event Action OnCleanup, OnShutdown;

		private static void RenewLogWriter(string fileWriterPath = null)
		{
			var fileWriterPathOld = HarmonyFileLog.FileWriterPath;
			var writerOld = HarmonyFileLog.Writer;

			if (!string.IsNullOrEmpty(fileWriterPath))
				HarmonyFileLog.FileWriterPath = fileWriterPath;
			HarmonyFileLog.Writer = new StreamWriter(new MemoryStream()) { AutoFlush = true };

			if (!(writerOld is StreamWriter writer))
				return;
			if (!(writer.BaseStream is MemoryStream stream))
				return;

			var bytesUsed = (int)stream.Position;
			if (bytesUsed == 0)
				return;
			using (var file = File.Create(fileWriterPathOld, bytesUsed))
			{
				file.Write(stream.GetBuffer(), 0, bytesUsed);
			}
		}

		private static void ShowLogFile(string appName = "notepad.exe")
		{
			RenewLogWriter();

			var path = HarmonyFileLog.FileWriterPath;
			if (!File.Exists(path))
				throw new FileNotFoundException($"\"{path}\" not found");
			var proc = Process.Start(new ProcessStartInfo { FileName = appName, Arguments = path });
			if (proc == null)
				throw new Win32Exception($"Unable to start {appName} with \"{path}\"");

			OnShutdown += () =>
			{
				try
				{
					proc.Kill();
				}
				catch
				{
					// ignored
				}
			};
			OnShutdown += () =>
			{
				try
				{
					File.Delete(path);
				}
				catch
				{
					// ignored
				}
			};
		}

		private static bool SetupLogging()
		{
			const string FILE_NAME = "PatchResult";
			var logsDirectory = Log.LogsPath;

			var dirInfo = new DirectoryInfo(logsDirectory);
			foreach (var file in dirInfo.GetFiles()
				         .Where(file => file.Name.StartsWith(FILE_NAME, StringComparison.Ordinal)))
				file.Delete();

			//string FilePath = Path.Combine(logs_path, $"{FileName}_default.log");

			void SetLogPath(string postfix, string extension)
			{
				var name = $"{FILE_NAME}_{postfix}";
				if (!string.IsNullOrEmpty(extension))
					name += $".{extension}";

				var fileWriterPath = Path.Combine(logsDirectory, name);
				RenewLogWriter(fileWriterPath);
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
				var targetClass = AccessTools.TypeByName("LogArchive");
				var targetInstance = AccessTools.CreateInstance(targetClass);
				var makeLog = AccessTools.Method(targetClass, "MakeLogPath");
				makeLog.Invoke(targetInstance, new object[] { logsDirectory });

				var logPathProp = AccessTools.Property(targetClass, "LogPath");
				var logStr = (string)logPathProp.GetValue(targetInstance, null);

				const string findStr = "hearthstone_";
				var offset = logStr.LastIndexOf(findStr, StringComparison.Ordinal) + findStr.Length;
				var logInfo = logStr.Substring(offset);
				SetLogPath(logInfo, null);

				return true;
			}
			catch (Exception e)
			{
				HarmonyFileLog.Writer.WriteLine(e.ToString());
				ShowLogFile();

				return false;
			}
		}

		private static Harmony _patcher;

#if !DEBUG
		private struct EnumInfo
		{
			public string[] Names;
			public int[] Values;
		}
#endif

		private static bool ValidateSharedData()
		{
#if DEBUG
			return true;
#else
			var resolvedTypes = new Dictionary<string, EnumInfo>();

			void ValidateEnum<T>(string name, T value)
				where T : Enum
			{
				var type = typeof(T);
				if (!resolvedTypes.TryGetValue(type.Name, out var info))
				{
					info = new EnumInfo { Names = type.GetEnumNames(), Values = type.GetEnumValues().Cast<int>().ToArray() };
					resolvedTypes.Add(name, info);
				}

				for (var i = 0; i < info.Names.Length; i++)
				{
					if (info.Names[i] == name)
					{
						if (info.Values[i] == (int)(object)value)
							return;
						throw new Exception($"Enum {name} changed from {value} to {info.Values[i]}");
					}
				}

				throw new Exception($"Enum {name} not found");
			}

			try
			{
				ValidateEnum(nameof(GAME_TAG.HAS_DIAMOND_QUALITY), GAME_TAG.HAS_DIAMOND_QUALITY);
				ValidateEnum(nameof(GAME_TAG.PREMIUM), GAME_TAG.PREMIUM);
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
				_patcher = new Harmony("patcher");
				_patcher.PatchAll();
#if DEBUG
				ShowLogFile();
#endif
				return true;
			}
			catch (Exception)
			{
				if (HarmonyFileLog.Enabled)
					ShowLogFile();
				else if (Environment.GetEnvironmentVariable("HARMONY_DEBUG") == null)
				{
					_patcher.UnpatchSelf();
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

		public static void OnGameStartup(Hearthstone.HearthstoneApplication app)
		{
			app.OnShutdown += OnCleanup;
			app.OnShutdown += OnShutdown;
		}

		public static void Start()
		{
			OnCleanup += () =>
			{
				HarmonyLog.ChannelFilter = HarmonyLogChannel.None;
				HarmonyFileLog.Enabled = Harmony.DEBUG = false;
			};

			if (!SetupLogging() || !ValidateSharedData() || !ApplyPatches())
				ExitApp();
		}
	}
}