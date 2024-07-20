using System.IO.Compression;
using System.Net;
using System.Diagnostics;
using Installer.Helpers;

namespace Installer.Extensions;

internal static class HttpClientExtension
{
	public static async Task<HttpResponseMessage> SendHttpRequest(this HttpClient client, Uri url)
	{
		using var request = new HttpRequestMessage(HttpMethod.Head, url);
		return await client.SendAsync(request);
	}

	public static async Task<bool> FileExist(this HttpClient client, Uri url)
	{
		using var response = await client.SendHttpRequest(url);
		return response.StatusCode == HttpStatusCode.OK;
	}

	public static async Task<long> GetFileSize(this HttpClient client, Uri url)
	{
		using var response = await client.SendHttpRequest(url);
		response.EnsureSuccessStatusCode( );
		return response.Content.Headers.ContentLength.Value;
	}

	public static async Task Download(this HttpClient client, UnstripHelper unstripHelper)
	{
		var directory = unstripHelper.ToString( );

		if (!Directory.Exists(directory))
			Directory.CreateDirectory(directory);
		else if (Directory.EnumerateFiles(directory).Any( ))
			return;

		using var archive = new ZipArchive(await client.GetStreamAsync(unstripHelper.GetUrl( )));

		await Task.WhenAll(
			archive.Entries.Select(
				e =>
				{
					Debug.Assert(e.Name == e.FullName);
					return e.WriteTo(Path.Combine(directory, e.Name));
				}));
	}
}
