using Octokit;

namespace Installer;

internal class GithubHelper
{
	private readonly GitHubClient _client;

	public GithubHelper( )
	{
		_client = new(new ProductHeaderValue("_"));
	}

	public async Task<Release> GetRelease(string owner, string name)
	{
		return await _client.Repository.Release.GetLatest(owner, name);
	}

	public async Task<string> GetDoorstopUrl( )
	{
		var rel = await GetRelease("NeighTools", "UnityDoorstop");
		var asset = rel.Assets.Where(a => a.Name.Contains("win")).First(
			a => a.Name.Contains(
#if DEBUG
				"verbose"
#else
				"release"
#endif
			));
		return asset.BrowserDownloadUrl;
	}
}
