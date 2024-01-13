using System.Net;

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
		return await _client.GetStreamAsync(url);
	}

	public async Task<bool> FileExist(Uri url)
	{
		using var request = new HttpRequestMessage(HttpMethod.Head, url);
		using var response = await _client.SendAsync(request);
		return response.StatusCode == HttpStatusCode.OK;
	}
}
