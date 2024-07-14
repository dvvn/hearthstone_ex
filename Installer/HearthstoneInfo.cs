using System.Diagnostics;
using Installer.Helpers;

namespace Installer;

internal class HearthstoneInfo : FileArchitectureInfo
{
	private const string _exeName = "Hearthstone.exe";

	public readonly SimpleFileInfo File;
	public readonly UnityVersion Version;

	private HearthstoneInfo(SimpleFileInfo fileInfo)
		: base(fileInfo.FullName)
	{
		File = fileInfo;
		Version = new(FileVersionInfo.GetVersionInfo(fileInfo.FullName));
	}

	public HearthstoneInfo( )
		: this(new(Path.Combine(HearthstoneDirectory.Get( ), _exeName), _exeName.Length, 4))
	{
	}
}
