namespace Installer.Helpers;

internal abstract class SimpleFileSystemInfo
{
	public abstract ReadOnlySpan<char> FullName { get; }
	public abstract ReadOnlySpan<char> Name { get; }
}
