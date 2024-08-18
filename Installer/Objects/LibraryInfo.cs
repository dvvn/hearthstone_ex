using Installer.Helpers;

namespace Installer.Objects;

internal class LibraryInfo
{
	public SimpleFileInfo File;
	public ArchitectureType Architecture;
	public Version DotNetVersion;

	public LibraryInfo(SimpleFileInfo fileInfo, string dotNetFilePath)
	{
		File = fileInfo;
		Architecture = Utils.GetFileArchitecture(fileInfo.FullName);
		DotNetVersion = Utils.GetDotNetFrameworkVersion(
			dotNetFilePath == null ? fileInfo.FullName :
			Path.IsPathRooted(dotNetFilePath) ? dotNetFilePath : PathEx.Combine(fileInfo.Directory.FullName, dotNetFilePath));
	}

	public LibraryInfo(string filePath, string dotNetFilePath)
		: this(new SimpleFileInfo(filePath), dotNetFilePath)
	{
	}

	public void Verify(LibraryInfo other)
	{
		var exceptions = new List<PlatformNotSupportedException>(2);

		if ((Architecture & other.Architecture) == 0)
			exceptions.Add(new($"Architecture mismatch: {Architecture}, {other.Architecture}."));

		if (DotNetVersion.Major != other.DotNetVersion.Major || DotNetVersion.Minor > other.DotNetVersion.Minor)
			exceptions.Add(new($"DotNetVersion mismatch: {DotNetVersion}, {other.DotNetVersion}."));

		switch (exceptions.Count)
		{
			case 0:
				break;
			case 1:
				throw exceptions[0];
			default:
				throw new AggregateException("LibraryInfo verification failed.", exceptions);
		}
	}
}
