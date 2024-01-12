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
		private static string FindHearthstoneDir( )
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
				foreach (var subkeyName in key.GetSubKeyNames( ))
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

		private static string GetWorkingDirectory( )
		{
			var selfPath = System.Reflection.Assembly.GetExecutingAssembly( ).Location;
			return Path.GetDirectoryName(selfPath);
		}

		private static FileInfo FindLibDir( )
		{
			var workPath = GetWorkingDirectory( );
			if (workPath != null)
				for (DirectoryInfo dir = new(workPath); dir != null; dir = dir.Parent)
				{
					foreach (var file in dir.EnumerateFiles( ))
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
			await using var st = entry.Open( );
			await using var file = File.Create(Path.Combine(dir, entry.Name));
			await st.CopyToAsync(file);
		}

		private static async Task<ZipArchive> DownloadZipFromBepinex(string subDir, string unityVersion, HttpClient client)
		{
			var url = $"https://unity.bepinex.dev/{subDir}/{unityVersion}.zip";
			await using var zipFile = await client.GetStreamAsync(url);
			return new(zipFile);
		}

		private static async Task<ZipArchive> DownloadZipFromBepinex(string subDir, string unityVersion)
		{
			using var client = new HttpClient( );
			return await DownloadZipFromBepinex(subDir, unityVersion, client);
		}

		//ExtractToDirectory
		private static async Task ExtractArchive(ZipArchive archive, FileSystemInfo dir)
		{
			foreach (var entry in archive.Entries)
				await WriteZippedFile(dir.FullName, entry);
		}

		//todo: add code to download from unity servers if bepinex dll doesn't work
		private static async Task<DirectoryInfo> DownloadUnstrippedDlls(string rootDir, string unityVersionRaw)
		{
			var space = unityVersionRaw.IndexOf(' ');
			var unityVersion = space == -1 ? unityVersionRaw : unityVersionRaw.Remove(space);

			using HttpClient httpClient = new( );

			for (;;)
			{
				var fullDir = new DirectoryInfo(Path.Combine(rootDir, unityVersion));
				if (!fullDir.Exists)
				{
					try
					{
						using var system = await DownloadZipFromBepinex("corlibs", unityVersion, httpClient);
						using var unity = await DownloadZipFromBepinex("libraries", unityVersion, httpClient);
						fullDir.Create( );
						await ExtractArchive(system, fullDir);
						await ExtractArchive(unity, fullDir);
					}
					catch (HttpRequestException e)
					{
						_ = e;
						if (!unityVersion.Contains('.'))
							return null;
						unityVersion = unityVersion.Remove(unityVersion.Length - 1);
						if (unityVersion.EndsWith('.'))
							unityVersion = unityVersion.Remove(unityVersion.Length - 1);
						continue;
					}
				}

				return fullDir;
			}
		}

		private static async Task DownloadDoorstop(string hsDir)
		{
			var github = new GitHubClient(new ProductHeaderValue("_"));
			var rel = await github.Repository.Release.GetLatest("NeighTools", "UnityDoorstop");
			var asset = rel.Assets.Where(a => a.Name.Contains("win")).First(
				a => a.Name.Contains(
#if DEBUG
					"verbose"
#else
					"release"
#endif
				));
			using var client = new HttpClient( );
			await using var zipFile = await client.GetStreamAsync(asset.BrowserDownloadUrl);
			using var archive = new ZipArchive(zipFile);
			var entry = archive.Entries.First(e => e.Name.EndsWith(".dll") && e.FullName.Contains("x86")); //todo: auto detect x64/x86
			await WriteZippedFile(hsDir, entry);
		}

		private static void WriteDoorstopConfig(string hsDir, FileInfo Assembly, DirectoryInfo DllSearch)
		{
#if DEBUG
			const string debugTrue = "true";
#else
			const string debugTrue = "false";
#endif

			var file = new INIFile(Path.Combine(hsDir, "doorstop_config.ini"));

			var general = file["General"];
			general["enabled"] = "true";
			general["redirect_output_log"] = debugTrue;
			general["ignore_disable_switch"] = "false";
			general["target_assembly"] = Assembly.FullName;

			var mono = file["UnityMono"];
			mono["dll_search_path_override"] = DllSearch.FullName;
			mono["debug_enabled"] = debugTrue;
			mono["debug_address"] = "127.0.0.1:10000";
			mono["debug_suspend"] = "false";

			file.WrapValueInQuotes = false;
			file.Persist( );
		}

		private static async Task MainAsync( )
		{
			var hsDir = FindHearthstoneDir( );
			await DownloadDoorstop(hsDir);
			var hsVersion = GetHearthstoneVersion(hsDir);

			var libFile = FindLibDir( );
			string dllsFolder;

			if (true || hsDir[0] == libFile.FullName[0]) //hs installed into same drive
			{
				dllsFolder = Path.Combine(libFile.Directory.Parent.FullName, "unity");
			}
			else
			{
				var hsExDir = Path.Combine(hsDir, "hs_ex");
				var libDir = Path.Combine(hsExDir, libFile.Directory.Name);
				Directory.CreateDirectory(libDir);
				foreach (var file in libFile.Directory.EnumerateFiles( ))
				{
					var targetFile = Path.Combine(libDir, file.Name);
					file.CopyTo(targetFile, true);
				}

				libFile = new(Path.Combine(libDir, libFile.Name));
				dllsFolder = Path.Combine(hsExDir, "unity");
			}

			var dlls = await DownloadUnstrippedDlls(dllsFolder, hsVersion);
			WriteDoorstopConfig(hsDir, libFile, dlls);
		}

		public static void Main(string[ ] args)
		{
			MainAsync( ).Wait( );
		}
	}
}
