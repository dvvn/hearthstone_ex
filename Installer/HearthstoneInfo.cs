using Installer.Helpers;
using Installer.Objects;

namespace Installer;

internal sealed class HearthstoneInfo : LibraryInfo
{
	public UnityExecutableInfo UnityInfo;

	private HearthstoneInfo(string filePath, string dotNetFilePath)
		: base(filePath, dotNetFilePath)
	{
		UnityInfo = new(filePath);
	}

	public HearthstoneInfo( )
		: this(PathEx.Combine(Utils.GetInstallDirectory("Hearthstone"), "Hearthstone.exe"), @"Hearthstone_Data\Managed\System.dll")
	{
	}

	public async IAsyncEnumerable<string> EnumerateUnstrippedDLLs(HttpClient httpClient)
	{
		var rootDirectory = Path.GetDirectoryName(Utils.FindParentDirectory(Utils.GetWorkingDirectory( ), "bin")).ToString( );

		//todo: download / build / copy
		yield return Path.Combine(rootDirectory, "UnstrippedLibs");

		if (Utils.IsSoftwareInstalled(UnityInfo.ApplicationName))
		{
			foreach (var p in EnumerateUnityDlls( ))
				yield return p;
		}
		else
		{
			var localUnityDir = Path.Combine(rootDirectory, "bin", "unity");
			var builder = new BepInExUnstripHelper.Builder(localUnityDir, UnityInfo.FileVersion.ToString( ));

			yield return await builder.Get("corlibs").Download(httpClient);
			yield return await builder.Get("libraries").Download(httpClient);
		}

		IEnumerable<string> EnumerateUnityDlls( )
		{
			var unityInstallDir = Utils.GetInstallDirectory($"Unity {UnityInfo.ProductVersion}");

			var s1 = PathEx.Combine(unityInstallDir, @"Data\PlaybackEngines\windowsstandalonesupport\Variations\win32_player_development_mono\Data\Managed");
			var s2 = PathEx.Combine(unityInstallDir, @"Data\MonoBleedingEdge\lib\mono\unityjit-win32");

			yield return s1;
			yield return s2;
		}
	}
}
