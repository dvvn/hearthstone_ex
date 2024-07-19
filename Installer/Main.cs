using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Installer.Extensions;
using Installer.Helpers;
using Octokit;

namespace Installer;

internal class Installer
{
	public static async Task Main(string[ ] args)
	{
		await Run( );
	}

	[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
	private static async Task Run( )
	{
		var hsFile = new SimpleFileInfo(Path.Combine(Utils.GetHearthstoneDirectory( ), "Hearthstone.exe"));
		var hsVersion = new UnityVersion(FileVersionInfo.GetVersionInfo(hsFile.FullName));

		var libFile = Utils.FindInParentDirectory(Utils.GetWorkingDirectory( ), "hearthstone_ex.dll");
		var libArch = Utils.GetFileArchitecture(libFile.FullName);

		Debug.Assert(Utils.GetFileArchitecture(hsFile.FullName) == libArch);

		var gitClient = new GitHubClient(new ProductHeaderValue(DateTime.Now.Ticks.ToString( )));
		using var httpClient = new HttpClient( );
		await using var doorstopHolder = new DoorstopHolder(hsFile.Directory);

		await doorstopHolder.Update(await GetDoorstopArchive( ), libArch);
		await WriteDoorstopConfig( );

		async Task<ZipArchive> GetDoorstopArchive( )
		{
			var doorstopRelease = await gitClient.Repository.Release.GetLatest("NeighTools", "UnityDoorstop");
			var doorstopReleaseAsset = doorstopRelease.Assets.First(r => r.Name.Contains("win") && r.Name.Contains(DoorstopHolder.ReleaseType));
			var stream = await httpClient.GetStreamAsync(doorstopReleaseAsset.BrowserDownloadUrl);
			return new(stream);
		}

		async Task WriteDoorstopConfig( )
		{
			var rootDir = Utils.FindParentDirectory(libFile.Directory, "bin");
			var unityDir = Path.Combine(rootDir.FullName, "unity");

			var corLibs = MakeUnstripHelper("corlibs");
			var unityLibs = MakeUnstripHelper("libraries");

			await Task.WhenAll(httpClient.Download(corLibs), httpClient.Download(unityLibs));

			doorstopHolder.Write(libFile.FullName, string.Join(';', corLibs, unityLibs));

			UnstripHelper MakeUnstripHelper(string type) => new(unityDir, type, hsVersion.Number);
		}
	}
}
