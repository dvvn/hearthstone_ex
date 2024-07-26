using System.Diagnostics;
using System.IO.Compression;
using Installer.Extensions;
using Installer.Helpers;
using Installer.Objects;
using Octokit;

namespace Installer;

internal class Installer
{
	public static async Task Main(string[ ] args)
	{
		await Run( );
	}

	private static async Task Run( )
	{
		var hsFile = new SimpleFileInfo(Path.Combine(Utils.GetInstallDirectory("Hearthstone"), "Hearthstone.exe"));
		var hsUnityInfo = new UnityExecutableInfo(hsFile.FullName);
		var hsUnityVersion = hsUnityInfo.FileVersion.ToString( );

		var libFile = Utils.FindInParentDirectory(Utils.GetWorkingDirectoryAsSpan( ), "hearthstone_ex.dll");
		var libArch = Utils.GetFileArchitecture(libFile.FullName);

		Debug.Assert(Utils.GetFileArchitecture(hsFile.FullName).SequenceEqual(libArch));

		using var httpClient = new HttpClient( );
		await using var doorstopHolder = new DoorstopHolder(hsFile.Directory);

		await doorstopHolder.Update(await GetDoorstopArchive( ), libArch);
		var unstrippedDLLs = await FindUnstrippedDLLs( );
		doorstopHolder.Write(libFile.FullName, unstrippedDLLs);

		async Task<ZipArchive> GetDoorstopArchive( )
		{
			var gitClient = new GitHubClient(new ProductHeaderValue(DateTime.Now.Ticks.ToString( )));

			var doorstopRelease = await gitClient.Repository.Release.GetLatest("NeighTools", "UnityDoorstop");
			var doorstopReleaseAsset = doorstopRelease.Assets.First(r => r.Name.Contains("win") && r.Name.Contains(DoorstopHolder.ReleaseType));
			var stream = await httpClient.GetStreamAsync(doorstopReleaseAsset.BrowserDownloadUrl);
			return new(stream);
		}

		async Task<string> FindUnstrippedDLLs( )
		{
			//todo: check for unity installed. if true, use dlls from there

			var unityLocalDir = PathEx.Combine(Utils.FindParentDirectory(libFile.Directory.FullName, "bin"), "unity");

			var corLibs = MakeUnstripHelper("corlibs");
			var unityLibs = MakeUnstripHelper("libraries");

			await Task.WhenAll(httpClient.Download(corLibs), httpClient.Download(unityLibs));

			return string.Join(';', corLibs, unityLibs);

			UnstripHelper MakeUnstripHelper(string type) => new(unityLocalDir, type, hsUnityVersion);
		}
	}
}
