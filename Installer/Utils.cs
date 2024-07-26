using System.Reflection;
using System.Reflection.PortableExecutable;
using Installer.Helpers;
using Microsoft.Win32;

namespace Installer;

internal static class Utils
{
	public static FileInfo FindInParentDirectory(DirectoryInfo dir, ReadOnlySpan<char> fileName)
	{
		for (; dir != null; dir = dir.Parent)
		{
			foreach (var file in dir.EnumerateFiles( ))
			{
				if (fileName.SequenceEqual(file.Name))
				{
					return file;
				}
			}
		}

		throw new FileNotFoundException($"Unable to find {fileName}!");
	}

	public static SimpleFileInfo FindInParentDirectory(SimpleDirectoryInfo dir, ReadOnlySpan<char> fileName)
	{
		for (; dir != null; dir = dir.Parent)
		{
			foreach (var file in dir.EnumerateFiles( ))
			{
				if (fileName.SequenceEqual(file.Name))
				{
					return file;
				}
			}
		}

		throw new FileNotFoundException($"Unable to find {fileName}!");
	}

	public static SimpleFileInfo FindInParentDirectory(ReadOnlySpan<char> dir, ReadOnlySpan<char> fileName)
	{
		for (var tmpDir = Path.TrimEndingDirectorySeparator(dir); tmpDir != null; tmpDir = Path.GetDirectoryName(tmpDir))
		{
			foreach (var file in Directory.EnumerateFiles(tmpDir.ToString( )))
			{
				if (Path.GetFileName(file.AsSpan( )).SequenceEqual(fileName))
				{
					return new(file, fileName);
				}
			}
		}

		throw new FileNotFoundException($"Unable to find {fileName}!");
	}

	public static DirectoryInfo FindParentDirectory(DirectoryInfo dir, ReadOnlySpan<char> directoryName)
	{
		for (; dir != null; dir = dir.Parent)
		{
			if (directoryName.SequenceEqual(dir.Name))
				return dir;
		}

		throw new FileNotFoundException($"Unable to directory ${directoryName}!");
	}

	public static SimpleDirectoryInfo FindParentDirectory(SimpleDirectoryInfo dir, ReadOnlySpan<char> directoryName)
	{
		for (; dir != null; dir = dir.Parent)
		{
			if (directoryName.SequenceEqual(dir.Name))
				return dir;
		}

		throw new FileNotFoundException($"Unable to directory ${directoryName}!");
	}

	[Obsolete]
	public static ReadOnlySpan<char> FindParentDirectory(ReadOnlySpan<char> dir, ReadOnlySpan<char> directoryName)
	{
		for (var tmpDir = Path.TrimEndingDirectorySeparator(dir); tmpDir != null; tmpDir = Path.GetDirectoryName(tmpDir))
		{
			if (Path.GetFileName(tmpDir).SequenceEqual(directoryName))
				return tmpDir;
		}

		throw new FileNotFoundException($"Unable to directory ${directoryName}!");
	}

	public static string GetWorkingDirectory( )
	{
		var selfPath = Assembly.GetExecutingAssembly( ).Location;
		return Path.GetDirectoryName(selfPath);
	}

	public static ReadOnlySpan<char> GetWorkingDirectoryAsSpan( )
	{
		var selfPath = Assembly.GetExecutingAssembly( ).Location;
		return Path.GetDirectoryName(selfPath.AsSpan( ));
	}

	public static string GetInstallDirectory(string applicationName)
	{
		return TryGetKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall") ?? //
			   TryGetKeyEx(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall") ??
			   throw new FileNotFoundException($"Unable to find install directory for {applicationName}");

		string TryGetKey(string subKeyPath)
		{
			var keyName = Path.Combine(subKeyPath, applicationName);
			using var root = Registry.LocalMachine.OpenSubKey(keyName);
			return root != null ? GetInstallSource(root) : null;
		}

		string TryGetKeyEx(string subKeyPath)
		{
			using var root = Registry.LocalMachine.OpenSubKey(subKeyPath);
			if (root != null)
				foreach (var subkeyName in root.GetSubKeyNames( ))
				{
					using var key = root.OpenSubKey(subkeyName);
					if (key == null)
						continue;
					if (key.GetValue("DisplayName") is not string name)
						continue;
					if (!name.Contains(applicationName))
						continue;
					var installSource = GetInstallSource(key);
					if (installSource == null)
						continue;
					return installSource;
				}

			return null;
		}

		string GetInstallSource(RegistryKey key)
		{
			if (key.GetValue("InstallSource") is string installSource)
				return installSource;
			if (key.GetValue("InstallLocation") is string installLocation)
				return installLocation;
			return null;
		}
	}

	public static string GetFileArchitecture(string filePath)
	{
		using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
		using var reader = new PEReader(stream);

		if (reader.PEHeaders.PEHeader != null)
		{
			switch (reader.PEHeaders.PEHeader.Magic)
			{
				case PEMagic.PE32:
					return "x86";
				case PEMagic.PE32Plus:
					return "x64";
			}
		}

		if (reader.PEHeaders.CorHeader != null)
		{
			var flags = reader.PEHeaders.CorHeader.Flags;
			if ((flags & CorFlags.ILOnly) != 0 && (flags & CorFlags.Requires32Bit) == 0)
				return "AnyCPU";
			if ((flags & CorFlags.Requires32Bit) != 0)
				return "x86";
		}

		throw new InvalidOperationException("Unable to determine the architecture of the File.");
	}

	public static string GetFileArchitecture(ReadOnlySpan<char> filePath)
	{
		return GetFileArchitecture(filePath.ToString( ));
	}
}

internal static class PathEx
{
	public static string Combine(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
	{
		var path1EndsInSeparator = Path.EndsInDirectorySeparator(path1);
		var path2StartsInSeparator = path2.Length > 0 && path2[0] == Path.DirectorySeparatorChar;

		var combinedLength = path1.Length + path2.Length + (path1EndsInSeparator || path2StartsInSeparator ? 0 : 1);
		var result = new char[combinedLength];

		path1.CopyTo(result.AsSpan(0, path1.Length));
		var index = path1.Length;

		if (!path1EndsInSeparator && !path2StartsInSeparator)
		{
			result[index] = Path.DirectorySeparatorChar;
			index++;
		}

		path2.CopyTo(result.AsSpan(index, path2.Length));

		return new(result);
	}

	public static string Combine(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3)
	{
		var path1EndsInSeparator = Path.EndsInDirectorySeparator(path1);
		var path2EndsInSeparator = Path.EndsInDirectorySeparator(path2);
		var path2StartsInSeparator = path2.Length > 0 && path2[0] == Path.DirectorySeparatorChar;
		var path3StartsInSeparator = path3.Length > 0 && path3[0] == Path.DirectorySeparatorChar;

		var combinedLength = path1.Length + path2.Length + path3.Length;
		if (!path1EndsInSeparator && !path2StartsInSeparator) combinedLength++;
		if (!path2EndsInSeparator && !path3StartsInSeparator) combinedLength++;

		var result = new char[combinedLength];
		var index = 0;

		path1.CopyTo(result.AsSpan(index, path1.Length));
		index += path1.Length;

		if (!path1EndsInSeparator && !path2StartsInSeparator)
		{
			result[index] = Path.DirectorySeparatorChar;
			index++;
		}

		path2.CopyTo(result.AsSpan(index, path2.Length));
		index += path2.Length;

		if (!path2EndsInSeparator && !path3StartsInSeparator)
		{
			result[index] = Path.DirectorySeparatorChar;
			index++;
		}

		path3.CopyTo(result.AsSpan(index, path3.Length));

		return new(result);
	}
}
