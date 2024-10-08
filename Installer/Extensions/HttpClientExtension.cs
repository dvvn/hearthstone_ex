﻿using System.Net;

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

	
}
