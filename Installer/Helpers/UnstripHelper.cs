namespace Installer.Helpers;

internal class UnstripHelper(string rootDirectory, string type, string version)
{
	public class Builder(string directory, string version)
	{
		public UnstripHelper Get(string type)
		{
			return new(directory, type, version);
		}
	}

	public string RootDirectory => rootDirectory;
	public string OutDirectory => Path.Combine(rootDirectory, type, version);
	public string DownloadUrl => $"https://unity.bepinex.dev/{type}/{version}.zip";

	public override string ToString( )
	{
		throw new NotSupportedException($"Method {nameof(UnstripHelper)}.{nameof(ToString)} not supported!");
	}
}
