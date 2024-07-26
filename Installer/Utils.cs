using System.Reflection;
using System.Reflection.PortableExecutable;
using Installer.Helpers;
using Microsoft.Win32;

namespace Installer;

internal static class Utils
{
	public static FileInfo FindInParentDirectory(DirectoryInfo dir, string fileName)
	{
		for (; dir != null; dir = dir.Parent)
		{
			var lib = dir.EnumerateFiles( ).FirstOrDefault(file => file.Name == fileName);
			if (lib == default)
				continue;
			return lib;
		}

		throw new FileNotFoundException($"Unable to find ${fileName}!");
	}

	public static SimpleFileInfo FindInParentDirectory(string dir, string fileName)
	{
		for (var tmpDir = Path.TrimEndingDirectorySeparator(dir); tmpDir != null; tmpDir = Path.GetDirectoryName(tmpDir))
		{
			var lib = Directory.EnumerateFiles(tmpDir).FirstOrDefault(file => Path.GetFileName(file.AsSpan( )).SequenceEqual(fileName));
			if (lib == default)
				continue;
			return new(lib, fileName);
		}

		throw new FileNotFoundException($"Unable to find ${fileName}!");
	}

	public static DirectoryInfo FindParentDirectory(DirectoryInfo dir, string directoryName)
	{
		for (; dir != null; dir = dir.Parent)
		{
			if (dir.Name == directoryName)
				return dir;
		}

		throw new FileNotFoundException($"Unable to directory ${directoryName}!");
	}

	public static ReadOnlySpan<char> FindParentDirectory(ReadOnlySpan<char> dir, string directoryName)
	{
		for (var tmpDir = Path.TrimEndingDirectorySeparator(dir); tmpDir != null; tmpDir = Path.GetDirectoryName(tmpDir))
		{
			if (Path.GetFileName(tmpDir).SequenceEqual(directoryName))
				return tmpDir;
		}

		throw new FileNotFoundException($"Unable to directory ${directoryName}!");
	}

	[Obsolete]
	public static string FindParentDirectory(string dir, string directoryName)
	{
		return FindParentDirectory(dir.AsSpan( ), directoryName).ToString( );
	}

	public static string GetWorkingDirectory( )
	{
		var selfPath = Assembly.GetExecutingAssembly( ).Location;
		return Path.GetDirectoryName(selfPath);
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
}
