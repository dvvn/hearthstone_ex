using System.Diagnostics;
using Installer.Helpers;

namespace Installer;

internal class Installer
{
	public static async Task Main(string[ ] args)
	{
		await Run( );
	}

	private static async Task Run( )
	{
		var hsInfo = new HearthstoneInfo( );
		var libInfo = new LibraryInfo( );
		Debug.Assert(hsInfo.Architecture == libInfo.Architecture);
		var gitHelper = new GithubHelper( );
		using var downloadHelper = new DownloadHelper( );
		await using var doorstopHolder = new DoorstopHolder(hsInfo.File.Directory);

		var doorstopRelease = await gitHelper.GetAsset("NeighTools", "UnityDoorstop", r => r.Name.Contains("win") && r.Name.Contains(DoorstopHolder.ReleaseType));
		await using var doorstopZip = await downloadHelper.Get(doorstopRelease);
		await doorstopHolder.Update(new(doorstopZip), libInfo.Architecture);

		var unstrippedDir = Path.Combine(Utils.FindParentDirectory(libInfo.File.Directory, "bin").FullName, "unity");
		var corLibs = new UnstrippedDirectory(unstrippedDir, "corlibs", hsInfo.Version.Number);
		var unityLibs = new UnstrippedDirectory(unstrippedDir, "libraries", hsInfo.Version.Number);

		await Task.WhenAll(downloadHelper.Get(corLibs), downloadHelper.Get(unityLibs));

		doorstopHolder.Write(libInfo.File.FullName, string.Join(';', corLibs, unityLibs));
	}
}
