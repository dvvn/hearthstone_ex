using System.IO.Compression;
using System.Net;
using System.Diagnostics;
using Installer.Helpers;

namespace Installer;

internal class DownloadHelper : IDisposable
{
	private readonly HttpClient _client;

	public DownloadHelper( )
	{
		_client = new( );
	}

	public void Dispose( )
	{
		_client.Dispose( );
	}

	public async Task<Stream> Get(Uri url)
	{
#if true
		var response = await _client.GetAsync(url);
		return await response.Content.ReadAsStreamAsync( );
#else
		return await _client.GetStreamAsync(url);
#endif
	}

	private async Task<HttpResponseMessage> Send(Uri url)
	{
		using var request = new HttpRequestMessage(HttpMethod.Head, url);
		return await _client.SendAsync(request);
	}

	public async Task<bool> FileExist(Uri url)
	{
		using var response = await Send(url);
		return response.StatusCode == HttpStatusCode.OK;
	}

	public async Task<long> GetFileSize(Uri url)
	{
		using var response = await Send(url);
		response.EnsureSuccessStatusCode( );
		return response.Content.Headers.ContentLength.Value;
	}

	//----------

	public async Task Get(UnstrippedDirectory unstrippedDirectory)
	{
		var directory = unstrippedDirectory.ToString( );
		var directoryInfo = new DirectoryInfo(directory);
		if (directoryInfo.Exists && directoryInfo.EnumerateFiles( ).Any( ))
			return;

		var archive = new ZipArchive(await Get(unstrippedDirectory.GetUrl( )));
#if DEBUG
		foreach (var e in archive.Entries)
			Debug.Assert(e.Name == e.FullName);
#endif
		directoryInfo.Create( );
		await Task.WhenAll(archive.Entries.Select(e => e.WriteTo(Path.Combine(directory, e.Name))));
	}

	public async Task<Stream> Get(Octokit.ReleaseAsset asset)
	{
		return await Get(new Uri(asset.BrowserDownloadUrl));
	}
}
