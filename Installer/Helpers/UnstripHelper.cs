using System.Diagnostics;
using System.IO.Compression;
using Installer.Extensions;

namespace Installer.Helpers;

internal abstract class UnstripHelper
{
	public class Builder<T>(string directory, string version)
	{
		public T Get(string s)
		{
			return (T)Activator.CreateInstance(typeof(T), directory, s, version);
		}
	}

	protected abstract string OutDirectory( );
	protected abstract string DownloadUrl( );

	protected virtual bool WriteFiles(string directory)
	{
		return !Directory.EnumerateFiles(directory).Any( );
	}

	protected abstract Task ExtractTo(string directory, ZipArchive archive);

	public async virtual Task<string> Download(HttpClient client)
	{
		var directory = OutDirectory( );

		Directory.CreateDirectory(directory);

		if (WriteFiles(directory))
		{
			using var archive = new ZipArchive(await client.GetStreamAsync(DownloadUrl( )));
			await ExtractTo(directory, archive);
		}

		return directory;
	}
}

internal class BepInExUnstripHelper(string rootDirectory, string type, string version) : UnstripHelper
{
	public class Builder(string directory, string version) : Builder<BepInExUnstripHelper>(directory, version);

	protected override string OutDirectory( ) => Path.Combine(rootDirectory, type, version);
	protected override string DownloadUrl( ) => $"https://unity.bepinex.dev/{type}/{version}.zip";

	protected override Task ExtractTo(string directory, ZipArchive archive)
	{
		Debug.Assert(archive.Entries.All(e => e.Name == e.FullName));
		return Task.WhenAll(archive.Entries.Select(e => e.WriteTo(Path.Combine(directory, e.Name))));
	}
}

internal class NuGetUnstripHelper(string rootDirectory, string name, string version) : UnstripHelper
{
	public class Builder(string directory, string version) : Builder<NuGetUnstripHelper>(directory, version);

	private IEnumerable<string> UrlData( )
	{
		yield return "https://www.nuget.org/api/v2/package";
		yield return name;
		//if (version != null)
		yield return version;
	}

	protected override string OutDirectory( ) => Path.Combine(rootDirectory, name, version /*?? "latest"*/);
	protected override string DownloadUrl( ) => string.Join('/', UrlData( ));

	/*protected virtual bool WriteFiles(string directory)
	{
		return version == null || !Directory.EnumerateFiles(directory).Any( );
	}*/

	protected override Task ExtractTo(string directory, ZipArchive archive)
	{
		return Task.WhenAll(archive.Entries.Where(e => e.FullName.Contains("net461")).Select(e => e.WriteTo(Path.Combine(directory, e.Name))));
	}
}
