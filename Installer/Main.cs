using Installer.Extensions;
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

		var httpClient = new HttpClient( );

		var doorstopHolder = new DoorstopHolder(hsInfo.File.Directory);
		var doorstopUpdate = await doorstopHolder.Update(new(await httpClient.GetStreamAsync(await DoorstopHolder.GetDownloadUrl( ))), hsInfo.Architecture);
		var dllSearchPath = string.Join(
			';',
			Utils.IsSoftwareInstalled(hsInfo.UnityInfo.ApplicationName) ?
				hsInfo.FindUnstrippedDLLs( ) :
				await Task.WhenAll(
					hsInfo.UnstrippedDLLsDownloadPrepare(PathEx.Combine(Utils.FindParentDirectory(libInfo.File.Directory.FullName, "bin"), "unity")).Select(httpClient.Download)));
		await doorstopHolder.Write(doorstopUpdate, new( ) { TargetAssembly = libInfo.File.FullName, DllSearchPathOverride = dllSearchPath });
	}
}
