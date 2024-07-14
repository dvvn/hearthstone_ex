using Microsoft.Win32;

namespace Installer;

internal static class HearthstoneDirectory
{
	private static string FromWOW64( )
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

	private static string FromUninstall( )
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

	public static string Get( )
	{
		return FromWOW64( ) ?? FromUninstall( );
	}
}