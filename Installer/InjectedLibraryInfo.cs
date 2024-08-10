using Installer.Objects;

namespace Installer;

internal sealed class InjectedLibraryInfo : LibraryInfo
{
	private const string libraryName = "hearthstone_ex.dll";

	public InjectedLibraryInfo( )
		: base(Utils.FindInParentDirectory(Utils.GetWorkingDirectory( ), libraryName), null)
	{
	}
}
