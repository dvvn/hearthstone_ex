using System.Diagnostics;
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
		: this(PathEx.Combine(Utils.GetInstallDirectory("Hearthstone"), "Hearthstone.exe"), Path.Combine("Hearthstone_Data", "Managed", "System.dll"))
	{
	}

	public IEnumerable<string> FindUnstrippedDLLs( )
	{
		return Impl( ).Select(
			s =>
			{
				Debug.Assert(Directory.Exists(s));
				return s;
			});

		IEnumerable<string> Impl( )
		{
			var unityInstallDir = Utils.GetInstallDirectory($"Unity {UnityInfo.ProductVersion}");
			var dataDir = PathEx.Combine(unityInstallDir, "Data");

			yield return Path.Combine(dataDir, "MonoBleedingEdge", "lib", "mono", $"net_{DotNetVersion.Major}_x-win32");
			yield return Path.Combine(dataDir, "Managed", "UnityEngine");
		}
	}

	public IEnumerable<UnstripHelper> UnstrippedDLLsDownloadPrepare(string outDir)
	{
		var builder = new UnstripHelper.Builder(outDir, UnityInfo.FileVersion.ToString( ));

		yield return builder.Get("corlibs");
		yield return builder.Get("libraries");
	}
}
