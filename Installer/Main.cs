using System.Diagnostics;
using System.IO.Compression;
using PeanutButter.INI;
using Microsoft.Win32;
using Octokit;

#pragma warning disable CA1416

namespace hearthstone_ex
{
	internal class Installer
	{
		private static string FindHearthstoneDir()
		{
			using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Hearthstone"))
			{
				if (key != null)
				{
					var val = key.GetValue("InstallLocation");
					Debug.Assert(val != null);
					return (string)val;
				}
			}

			using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
			{
				Debug.Assert(key != null);
				foreach (var subkeyName in key.GetSubKeyNames())
				{
					using var subkey = key.OpenSubKey(subkeyName);
					if (subkey == null)
						continue;
					var name = subkey.GetValue("DisplayName");
					if (name == null)
						continue;
					if (!((string)name).Contains("Hearthstone"))
						continue;
					var src = subkey.GetValue("InstallSource");
					Debug.Assert(src != null);
					return (string)src;
				}
			}

			return string.Empty;
		}

		private static FileInfo FindLibDir()
		{
			var selfPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			var workPath = Path.GetDirectoryName(selfPath);
			Debug.Assert(workPath != null);
			for (DirectoryInfo dir = new(workPath); dir != null; dir = dir.Parent)
			{
				foreach (var file in dir.EnumerateFiles())
				{
					if (file.Name == "hearthstone_ex.dll")
						return file;
				}
			}

			return null;
		}

		private static string GetHearthstoneVersion(string hsDir)
		{
			var info = FileVersionInfo.GetVersionInfo(Path.Combine(hsDir, "Hearthstone.exe"));
			Debug.Assert(info.ProductVersion != null);
			return info.ProductVersion;
		}

		private static async Task WriteZippedFile(string dir, ZipArchiveEntry entry)
		{
			await using var st = entry.Open();
			await using var file = File.Create(Path.Combine(dir, entry.Name));
			await st.CopyToAsync(file);
		}

		private static async Task<DirectoryInfo> DownloadFullDlls(string dir, string hsVersion)
		{
			for (var unityVersion = hsVersion; !string.IsNullOrEmpty(unityVersion); unityVersion = unityVersion.Remove(unityVersion.LastIndexOf('.')))
			{
				var url = $"https://unity.bepinex.dev/corlibs/{unityVersion}.zip";
				var dllsDir = new DirectoryInfo(Path.Combine(dir, unityVersion));
				if (!dllsDir.Exists)
				{
					using var client = new HttpClient();
					try
					{
						await using var zipFile = await client.GetStreamAsync(url);
						using var archive = new ZipArchive(zipFile);
						dllsDir.Create();
						foreach (var entry in archive.Entries)
						{
							await WriteZippedFile(dllsDir.FullName, entry);
						}
					}
					catch (Exception e)
					{
						continue;
					}
				}

				return dllsDir;
			}

			return null;
		}

		private static async Task DownloadDoorstop(string hsDir)
		{
			//var dll = new FileInfo(Path.Combine(hsDir, "winhttp.dll"));
			//if (dll.Exists)
			//	return;

			var github = new GitHubClient(new ProductHeaderValue("_"));
			var rel = await github.Repository.Release.GetLatest("NeighTools", "UnityDoorstop");
			var asset = rel.Assets.Where(a => a.Name.Contains("win")).First(a =>
				a.Name.Contains(
#if DEBUG
					"verbose"
#else
					"release"
#endif
				));
			//file.BrowserDownloadUrl

			using var client = new HttpClient();
			await using var zipFile = await client.GetStreamAsync(asset.BrowserDownloadUrl);
			using var archive = new ZipArchive(zipFile);
			foreach (var entry in archive.Entries)
			{
				if (!entry.Name.EndsWith(".dll"))
					continue;
				if (entry.FullName.Contains("x64")) //todo: auto detect it
					continue;
				
				await WriteZippedFile(hsDir, entry);
				break;
			}
		}

		private static void WriteDoorstopConfig(string lib, string hsDir, string dllsDir)
		{
			var file = new INIFile(Path.Combine(hsDir, "doorstop_config.ini"));

			IDictionary<string, string> CreateSection(string name, Action<IDictionary<string, string>> init)
			{
				if (file.HasSection(name))
					return file.GetSection(name);

				file.AddSection(name);
				var sec = file.GetSection(name);
				init(sec);
				return sec;
			}

			var general = CreateSection("General", (sec) =>
			{
				sec["enabled"] = "true";
				sec["redirect_output_log"] = "false";
				sec["ignore_disable_switch"] = "false";
			});
			general["target_assembly"] = lib;

			var mono = CreateSection("UnityMono", sec =>
			{
				sec["debug_address"] = "127.0.0.1:10000";
				sec["debug_suspend"] = "false";
			});
			mono["dll_search_path_override"] = dllsDir;
#if DEBUG
			mono["debug_enabled"] = "true";
#else
			mono["debug_enabled"] = "false";
#endif
			file.WrapValueInQuotes = false;
			file.Persist();
		}

		public static void Main(string[] args)
		{
			var hs = FindHearthstoneDir();
			var hsVersion = GetHearthstoneVersion(hs);

			var doorstop = DownloadDoorstop(hs);

			var lib = FindLibDir();
			var dllsFolder = Path.Combine(lib.Directory.Parent.FullName, "unity");
			var dlls = DownloadFullDlls(dllsFolder, hsVersion).Result;

			WriteDoorstopConfig(lib.FullName, hs, dlls.FullName);

			doorstop.Wait();
		}
	}
}