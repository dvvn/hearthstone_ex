using Installer.Helpers;

namespace Installer;

internal class LibraryInfo : FileArchitectureInfo
{
	public readonly SimpleFileInfo File;

	private LibraryInfo(FileInfo file)
		: base(file.FullName)
	{
		File = new(file.FullName);
	}

	public LibraryInfo( )
		: this(Utils.FindInParentDirectory(Utils.GetWorkingDirectory( ), "hearthstone_ex.dll"))
	{
	}
}
