using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net;
using Installer.Helpers;
using Octokit;
using FileMode = System.IO.FileMode;

namespace Installer.Objects;

internal ref struct DoorstopConfig
{
	public ReadOnlySpan<char> TargetAssembly;
	public string DllSearchPathOverride;
	public IPEndPoint DebugAddress = new(IPAddress.Loopback, 10000);

	public DoorstopConfig( )
	{
	}
}

[SuppressMessage("ReSharper", "UnusedMember.Local")]
internal class DoorstopConfigUpdater
{
	private readonly bool[ ] _usedIndex;
	private readonly IList<string> _configData;

	public DoorstopConfigUpdater(IList<string> configData)
	{
		_usedIndex = new bool[configData.Count];
		_configData = configData;
	}

	private void Set(int index, string key, string value)
	{
		_configData[index] = $"{key}={value}";
	}

	private void Set(int index, string key, ReadOnlySpan<char> value)
	{
		var data = new char[key.Length + 1 + value.Length];
		key.CopyTo(data);
		data[key.Length] = '=';
		value.CopyTo(data.AsSpan(key.Length + 1));
		_configData[index] = new(data);
	}

	private int Find(string key)
	{
		Debug.Assert(_configData.Count(s => s.StartsWith(key)) == 1, "Multiple keys found");
		for (var index = 0; index != _configData.Count; ++index)
		{
			if (_usedIndex[index] || !_configData[index].StartsWith(key))
				continue;
			return index;
		}

		return -1;
	}

	public void Set(string key, ReadOnlySpan<char> value)
	{
		var index = Find(key);
		Set(index, key, value);
		_usedIndex[index] = true;
	}

	public void Set(string key, bool value)
	{
		var index = Find(key);
		Set(index, key, value ? "true" : "false");
		_usedIndex[index] = true;
	}
}

internal class DoorstopUpdateResult
{
	public Stream dllData;
	public IList<string> configData;

	public DoorstopConfigUpdater MakeConfigUpdater( ) => new(configData);
}

internal class DoorstopHolder
{
	private const string releaseType =
#if DEBUG
			"verbose"
#else
			"release"
#endif
		;

	private readonly SimpleFileInfo _configFile, _dllFile;

	public DoorstopHolder(ReadOnlySpan<char> gameDirectory)
	{
		_configFile = new(PathEx.Combine(gameDirectory, "doorstop_config.ini"));
		_dllFile = new(PathEx.Combine(gameDirectory, "winhttp.dll"));
	}

	public DoorstopHolder(DirectoryInfo gameDirectory)
		: this(gameDirectory.FullName)
	{
	}

	public DoorstopHolder(SimpleDirectoryInfo gameDirectory)
		: this(gameDirectory.FullName)
	{
	}

	[SuppressMessage("ReSharper", "RedundantJumpStatement")]
	public async Task<DoorstopUpdateResult> Update(ZipArchive archive, ArchitectureType architectureType)
	{
		var result = new DoorstopUpdateResult( );

		foreach (var entry in archive.Entries.Where(e => e.Length != 0 && e.FullName.StartsWith(architectureType.ToString( ), StringComparison.OrdinalIgnoreCase)))
		{
			if (result.dllData == null && TryOpenEntry(entry, _dllFile, out result.dllData))
			{
				if (result.configData != null)
					break;
				continue;
			}

			if (result.configData == null && TryOpenEntry(entry, _configFile, out var stream))
			{
				using var reader = new StreamReader(stream);
				var lines = await reader.ReadToEndAsync( );
				result.configData = lines.Split(Environment.NewLine /*, StringSplitOptions.RemoveEmptyEntries*/);

				if (result.dllData != null)
					break;
				continue;
			}
		}

		return result;

		bool TryOpenEntry(ZipArchiveEntry entry, SimpleFileInfo info, out Stream stream)
		{
			if (entry.FullName.AsSpan( ).EndsWith(info.Extension))
			{
				Debug.Assert(info.Name.SequenceEqual(entry.Name));
				stream = entry.Open( );
				return true;
			}

			stream = null;
			return false;
		}
	}

	public Task Write(DoorstopUpdateResult updateResult, DoorstopConfig config)
	{
		var updater = updateResult.MakeConfigUpdater( );

		//[General]
		updater.Set("enabled", true);
		updater.Set("redirect_output_log", true);
		updater.Set("ignore_disable_switch", false);
		updater.Set("target_assembly", config.TargetAssembly);

		//[UnityMono]
		updater.Set("dll_search_path_override", config.DllSearchPathOverride);
#if DEBUG
		updater.Set("debug_enabled", true);
#else
		updater.Set("debug_enabled", false);
#endif
		updater.Set("debug_address", config.DebugAddress.ToString( ));
		updater.Set("debug_suspend", false);

		return Task.Run(
			async ( ) =>
			{
				await using var stream = new FileStream(_dllFile.FullName.ToString( ), FileMode.Create, FileAccess.Write);
				await Task.WhenAll(
					updateResult.dllData.CopyToAsync(stream),
					File.WriteAllLinesAsync(_configFile.FullName.ToString( ), updateResult.configData));
			});
	}

	/*
	//NAME_VERSION_.zip
	private static ReadOnlySpan<char> ExtractVersion(ReadOnlySpan<char> fileName)
	{
		var start = fileName.LastIndexOf('_') + 1;
		var end = fileName.LastIndexOf('.');
		var version = fileName.Slice(start, end - start);
		return version;
	}
	*/

	public static async Task<string> GetDownloadUrl( )
	{
		var gitClient = new GitHubClient(new ProductHeaderValue(DateTime.Now.Ticks.ToString( )));

		var doorstopRelease = await gitClient.Repository.Release.GetLatest("NeighTools", "UnityDoorstop");
		var doorstopReleaseAsset = doorstopRelease.Assets.First(r => r.Name.Contains("win") && r.Name.Contains(releaseType));

		return doorstopReleaseAsset.BrowserDownloadUrl;
	}
}
