﻿namespace Installer.Helpers;

internal class UnstrippedDirectory
{
	private readonly string _directory;
	private readonly string _downloadUrl;

	public UnstrippedDirectory(string rootDirectory, string type, ReadOnlySpan<char> version)
	{
		_directory = Path.Combine(rootDirectory, type, version.ToString( ));
		_downloadUrl = $"https://unity.bepinex.dev/{type}/{version}.zip";
	}

	public Uri GetUrl( )
	{
		return new(_downloadUrl);
	}

	public override string ToString( )
	{
		return _directory;
	}
}
