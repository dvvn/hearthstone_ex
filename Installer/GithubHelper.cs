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

	public async Task<ReleaseAsset> GetAsset(string owner, string name, Func<ReleaseAsset, bool> predicate)
	{
		var release = await GetRelease(owner, name);
		return release.Assets.First(predicate);
	}
}
