﻿using System.Reflection;
using System.Reflection.PortableExecutable;
using Microsoft.Win32;

namespace Installer.Helpers;

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

		throw new DllNotFoundException($"Unable to find ${fileName}!");
	}

	public static SimpleFileInfo FindInParentDirectory(string dir, string fileName)
	{
		var lib = Directory.EnumerateFiles(dir, fileName).FirstOrDefault(file => Path.GetFileName(file.AsSpan( )).SequenceEqual(fileName));
		if (lib != default)
			return new(lib);

		var lib2 = FindInParentDirectory(Directory.GetParent(dir), fileName);
		return new(lib2.FullName, lib2.Name /*, lib2.Extension.Length*/);
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

	public static string GetHearthstoneDirectory( )
	{
		return FromWOW64( ) ?? FromUninstall( );

		//----

		string FromWOW64( )
		{
			using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Hearthstone");
			if (key != null)
			{
				if (key.GetValue("InstallLocation") is string install)
				{
					return install;
				}
			}

			//throw new DllNotFoundException("Failed to retrieve install location from WOW6432Node registry.");
			return null;
		}

		string FromUninstall( )
		{
			using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
			if (key != null)
			{
				foreach (var subkeyName in key.GetSubKeyNames( ))
				{
					using var subkey = key.OpenSubKey(subkeyName);
					if (subkey == null)
						continue;
					if (subkey.GetValue("DisplayName") is not string name || !name.Contains("Hearthstone"))
						continue;
					if (subkey.GetValue("InstallSource") is not string install)
						continue;
					return install;
				}
			}

			//throw new DllNotFoundException("Failed to retrieve install location from Uninstall registry.");
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
