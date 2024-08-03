using System.Diagnostics;
using System.IO.Compression;
using Installer.Extensions;
using Installer.Helpers;
using Installer.Objects;
using Octokit;

namespace Installer;

internal static class Installer
{
	public static async Task Main(string[ ] args)
	{
		try
		{
			await Run( );
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			Console.ReadKey( );
		}
	}

	private static async Task Run( )
	{
		var hsFile = new SimpleFileInfo(PathEx.Combine(Utils.GetInstallDirectory("Hearthstone"), "Hearthstone.exe"));
		var hsUnityInfo = new UnityExecutableInfo(hsFile.FullName);
		var hsArch = Utils.GetFileArchitecture(hsFile.FullName);
		var hsDotNetVerison = Utils.GetDotNetFrameworkVersion(PathEx.Combine(hsFile.Directory.FullName, "Hearthstone_Data", "Managed", "System.dll"));

		var libFile = Utils.FindInParentDirectory(Utils.GetWorkingDirectory( ), "hearthstone_ex.dll");
		var libArch = Utils.GetFileArchitecture(libFile.FullName);
		var libDotNetVersion = Utils.GetDotNetFrameworkVersion(libFile.FullName);

		if (hsArch != libArch)
			throw new PlatformNotSupportedException($"Set library architecture to {hsArch}!");
		if (hsDotNetVerison.Major != libDotNetVersion.Major || hsDotNetVerison.Minor != libDotNetVersion.Minor)
			throw new PlatformNotSupportedException($"Set library .NET Framevork version to {hsDotNetVerison}!");

		using var httpClient = new HttpClient( );
		await using var doorstopHolder = new DoorstopHolder(hsFile.Directory);

		await doorstopHolder.Update(await DownloadDoorstopArchive( ), libArch.ToString( ));
		var unstrippedDLLs = FindUnstrippedDLLs( ) ?? await DownloadUnstrippedDLLs( );
		doorstopHolder.Write(libFile.FullName, unstrippedDLLs);

		async Task<ZipArchive> DownloadDoorstopArchive( )
		{
			var gitClient = new GitHubClient(new ProductHeaderValue(DateTime.Now.Ticks.ToString( )));

			var doorstopRelease = await gitClient.Repository.Release.GetLatest("NeighTools", "UnityDoorstop");
			var doorstopReleaseAsset = doorstopRelease.Assets.First(r => r.Name.Contains("win") && r.Name.Contains(DoorstopHolder.ReleaseType));
			var stream = await httpClient.GetStreamAsync(doorstopReleaseAsset.BrowserDownloadUrl);
			return new(stream);
		}

		string FindUnstrippedDLLs( )
		{
			var unityInstallDir = Utils.TryGetInstallDirectory($"Unity {hsUnityInfo.ProductVersion}");
			if (unityInstallDir == null)
				return null;

			var corLibsDir = PathEx.Combine(unityInstallDir, "Data", "MonoBleedingEdge", "lib", "mono", $"{hsDotNetVerison.Major}.{hsDotNetVerison.Minor}-api");
			var unityLibsDir = PathEx.Combine(unityInstallDir, "Data", "Managed");
			var unityLibsDir2 = Path.Combine(unityLibsDir, "UnityEngine");
			return string.Join(';', corLibsDir, unityLibsDir, unityLibsDir2);
		}

		async Task<string> DownloadUnstrippedDLLs( )
		{
			var builder = new UnstripHelper.Builder(PathEx.Combine(Utils.FindParentDirectory(libFile.Directory.FullName, "bin"), "unity"), hsUnityInfo.FileVersion.ToString( ));

			var corLibs = builder.Get("corlibs");
			var unityLibs = builder.Get("libraries");

			await Task.WhenAll(httpClient.Download(corLibs), httpClient.Download(unityLibs));

			return string.Join(';', corLibs.OutDirectory, unityLibs.OutDirectory);
		}
	}
}
