using System.IO.Compression;

namespace Installer;

internal class Installer
{
	private static void Gap<T>(T _)
	{
	}

	private static async Task WriteZippedFile(DirectoryInfo dir, ZipArchiveEntry entry)
	{
		Gap(dir);

		await using var st = entry.Open( );
		await using var file = File.Create(Path.Combine(dir.FullName, entry.Name));
		await st.CopyToAsync(file);
	}

	//ExtractToDirectory
	private static async Task ExtractArchive(ZipArchive archive, DirectoryInfo dir)
	{
#if false
		foreach (var e in archive.Entries)
			await WriteZippedFile(dir, e);
#else
		await Task.WhenAll(archive.Entries.Select(e => WriteZippedFile(dir, e)));
#endif
	}

	private static async Task ExtractArchive(Stream archiveStream, DirectoryInfo dir)
	{
		await ExtractArchive(new ZipArchive(archiveStream), dir);
	}

	private static Uri MakeBepinexUrl(ReadOnlySpan<char> subDir, string unityVersion)
	{
		return new($"https://unity.bepinex.dev/{subDir}/{unityVersion}.zip");
	}

	//todo: add code to download from unity servers if bepinex dll doesn't work
	private static async Task MainAsync( )
	{
		var hsInfo = new HearthstoneInfo( );
		var libInfo = new LibraryInfo( );
		var gitHelper = new GithubHelper( );
		var downloadHelper = new DownloadHelper( );

		var doorstopUrl = new Uri(await gitHelper.GetDoorstopUrl( ));
		using var doorstop = new DoorstopHolder(hsInfo.Directory);
		if (!doorstop.CheckVersion(doorstopUrl))
			doorstop.Write(await downloadHelper.Get(doorstopUrl));

		var unstrippedDir = new DirectoryInfo(Path.Combine(libInfo.Library.Directory.Parent.FullName, "unity", hsInfo.Version.Basic));
		unstrippedDir.Create( );
		await ExtractArchive(await downloadHelper.Get(MakeBepinexUrl("corlibs", hsInfo.Version.Basic)), unstrippedDir);
		await ExtractArchive(await downloadHelper.Get(MakeBepinexUrl("libraries", hsInfo.Version.Basic)), unstrippedDir);

		doorstop.Write(true, libInfo.Library, unstrippedDir);
	}

	public static void Main(string[ ] args)
	{
		Gap(args);
		MainAsync( ).Wait( );
	}
}
