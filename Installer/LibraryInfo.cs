using System.Diagnostics;
using System.Reflection;

namespace Installer;

internal class LibraryInfo
{
	private static readonly string _selfPath = Assembly.GetExecutingAssembly( ).Location;

	public static string GetWorkingDirectory( )
	{
		return Path.GetDirectoryName(_selfPath);
	}

	public static FileVersionInfo GetVersion( )
	{
		return FileVersionInfo.GetVersionInfo(_selfPath);
	}

	private const string LibraryName = "hearthstone_ex.dll";

	private static FileInfo FindLibrary(DirectoryInfo dir)
	{
		return dir.EnumerateFiles( ).FirstOrDefault(file => file.Name == LibraryName);
	}

	private FileInfo FindLibrary( )
	{
		Debug.Assert(_workingDirectory != null);
		
		for (var dir = _workingDirectory;; dir = dir.Parent)
		{
			var lib = FindLibrary(dir);
			if (lib != null)
				return lib;
		}
	}

	private readonly DirectoryInfo _workingDirectory;
	public readonly FileInfo Library;

	public LibraryInfo( )
	{
		_workingDirectory = new(GetWorkingDirectory( ));
		Library = FindLibrary( );
	}
}
