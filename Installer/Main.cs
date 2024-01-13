using System.Diagnostics;
using System.Linq;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;

namespace Installer;

internal class Installer
{
	private static Uri MakeBepinexUrl(ReadOnlySpan<char> subDir, string unityVersion)
	{
		return new($"https://unity.bepinex.dev/{subDir}/{unityVersion}.zip");
	}

	private static async Task ExtractArchive<T>(DirectoryInfo dir, params Task<T>[ ] archive) where T : IArchive
	{
		await Task.WhenAll(
			archive.SelectMany(
				t => t.Result.Entries.Select(
					async e =>
					{
						Debug.Assert(e.Key != null);
						var filePath = Path.Combine(dir.FullName, e.Key);
						await using var file = e.Size == 0 ? File.Create(filePath) : File.Create(filePath, (int)e.Size);
						e.WriteTo(file);
					}))).ConfigureAwait(false);
	}

	//todo: add code to download from unity servers if bepinex dll doesn't work
	public static async Task Main(string[ ] args)
	{
		var hsInfo = new HearthstoneInfo( );
		var libInfo = new LibraryInfo( );
		var gitHelper = new GithubHelper( );
		var downloadHelper = new DownloadHelper( );
		await using var doorstopHolder = new DoorstopHolder(hsInfo.Directory);

		var doorstopRelease = await gitHelper.GetAsset(
								  "NeighTools", "UnityDoorstop",
								  r => r.Name.Contains("win") && r.Name.Contains(DoorstopHolder.ReleaseType));
		var doorstopUrl = new Uri(doorstopRelease.BrowserDownloadUrl);
		await doorstopHolder.Update(ZipArchive.Open(await downloadHelper.Get(doorstopUrl)));

		var unstrippedDir = new DirectoryInfo(Path.Combine(libInfo.Library.Directory.Parent.FullName, "unity", hsInfo.Version.Basic));
		if (unstrippedDir.Exists)
		{
			if (!unstrippedDir.EnumerateFileSystemInfos( ).Any( ))
				goto _UNSTRIPPED_DIR_CREATED;
			unstrippedDir.Delete(true);
		}

		unstrippedDir.Create( );

	_UNSTRIPPED_DIR_CREATED:

		await ExtractArchive(
			unstrippedDir,
			downloadHelper.Get(MakeBepinexUrl("corlibs", hsInfo.Version.Basic)).ContinueWith(t => ZipArchive.Open(t.Result)),
			downloadHelper.Get(MakeBepinexUrl("libraries", hsInfo.Version.Basic)).ContinueWith(t => ZipArchive.Open(t.Result)));

		doorstopHolder.Write(libInfo.Library, unstrippedDir);
	}
}
