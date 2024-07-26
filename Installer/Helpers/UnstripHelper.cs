namespace Installer.Helpers;

internal class UnstripHelper(string rootDirectory, string type, string version)
{
	public string RootDirectory => rootDirectory;
	public string OutDirectory => PathEx.Combine(rootDirectory, type, version);
	public Uri BepinExUrl => new($"https://unity.bepinex.dev/{type}/{version}.zip");
}
