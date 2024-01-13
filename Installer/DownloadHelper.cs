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
#if true
		var response = await _client.GetAsync(url);
		return await response.Content.ReadAsStreamAsync( );
#else
		return await _client.GetStreamAsync(url);
#endif
	}

	public async Task<bool> FileExist(Uri url)
	{
		using var request = new HttpRequestMessage(HttpMethod.Head, url);
		using var response = await _client.SendAsync(request);
		return response.StatusCode == HttpStatusCode.OK;
	}

	public async Task<long> GetFileSize(Uri url)
	{
		var message = new HttpRequestMessage(HttpMethod.Head, url);
		var response = await _client.SendAsync(message);
		response.EnsureSuccessStatusCode( );
		return response.Content.Headers.ContentLength.Value;
	}
}
