using Installer.Objects;

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
		var hsInfo = new HearthstoneInfo( );
		var libInfo = new InjectedLibraryInfo( );

		hsInfo.Verify(libInfo);

		var doorstopHolder = new DoorstopHolder(hsInfo.File.Directory);
		var httpClient = new HttpClient( );
		var dllSearchSources = hsInfo.EnumerateUnstrippedDLLs(httpClient).ToBlockingEnumerable( );
		var doorstopUpdate = await doorstopHolder.Update(new(await httpClient.GetStreamAsync(await DoorstopHolder.GetDownloadUrl( ))), hsInfo.Architecture);
		await doorstopHolder.Write(doorstopUpdate, new( ) { TargetAssembly = libInfo.File.FullName, DllSearchPathOverride = new(dllSearchSources) });
	}
}
