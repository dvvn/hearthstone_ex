using System.Diagnostics;
using Microsoft.Win32;

namespace Installer;

internal static class HearthstoneDirectory
{
	public static string Get( )
	{
		using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Hearthstone"))
		{
			// ReSharper disable once UseNullPropagation
			if (key != null)
				if (key.GetValue("InstallLocation") is string install)
				{
					return install;
				}
		}

		using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
		{
			if (key != null)
				foreach (var subkeyName in key.GetSubKeyNames( ))
				{
					using var subkey = key.OpenSubKey(subkeyName);
					if (subkey?.GetValue("DisplayName") is string name &&
						name.Contains("Hearthstone") &&
						subkey.GetValue("InstallSource") is string install)
					{
						return install;
					}
				}
		}

		return string.Empty;
	}
}

internal class UnityVersion
{
	public readonly FileVersionInfo Raw;
	public readonly string Full;
	public readonly string Basic;

	public UnityVersion(FileVersionInfo info)
	{
		Raw = info;

		var rawVersion = info.ProductVersion.AsSpan( );

		var space = rawVersion.IndexOf(' ');
		if (space != -1)
			rawVersion = rawVersion.Slice(0, space);
		Full = rawVersion.ToString( );

		if (rawVersion.EndsWith("f1"))
			rawVersion = rawVersion.Slice(0, rawVersion.Length - 2);
		Basic = rawVersion.ToString( );
	}
}

internal readonly struct HearthstoneInfo
{
	public readonly DirectoryInfo Directory;
	public readonly FileInfo Executable;
	public readonly UnityVersion Version;

	public HearthstoneInfo( )
	{
		var directory = HearthstoneDirectory.Get( );
		var executable = Path.Combine(directory, "Hearthstone.exe");

		Directory = new(directory);
		Executable = new(executable);
		Version = new(FileVersionInfo.GetVersionInfo(executable));
	}
}
