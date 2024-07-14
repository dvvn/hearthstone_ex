using System.Reflection;

namespace Installer.Helpers;

internal static class Utils
{
	public static FileInfo FindInParentDirectory(DirectoryInfo dir, string fileName)
	{
#if DEBUG
		// ReSharper disable once UnusedVariable
		var topDir = dir;
#endif

		for (; dir != null; dir = dir.Parent)
		{
			var lib = dir.EnumerateFiles( ).FirstOrDefault(file => file.Name == fileName);
			if (lib == default)
				continue;
			return lib;
		}

		throw new DllNotFoundException($"Unable to find ${fileName}!");
	}

	public static FileInfo FindInParentDirectory(string dir, string fileName)
	{
		return FindInParentDirectory(new DirectoryInfo(dir), fileName);
	}

	public static DirectoryInfo FindParentDirectory(DirectoryInfo dir, string directoryName)
	{
#if DEBUG
		// ReSharper disable once UnusedVariable
		var topDir = dir;
#endif

		for (; dir != null; dir = dir.Parent)
		{
			if (dir.Name == directoryName)
				return dir;
		}

		throw new FileNotFoundException($"Unable to directory ${directoryName}!");
	}

	public static DirectoryInfo FindParentDirectory(string dir, string directoryName)
	{
		return FindParentDirectory(new DirectoryInfo(dir), directoryName);
	}

	public static DirectoryInfo FindParentDirectory(ReadOnlySpan<char> dir, string directoryName)
	{
		return FindParentDirectory(dir.ToString( ), directoryName);
	}

	public static string GetWorkingDirectory( )
	{
		var selfPath = Assembly.GetExecutingAssembly( ).Location;
		return Path.GetDirectoryName(selfPath);
	}
}
