using System.Diagnostics;
using System.IO.Compression;
using Installer.Helpers;

namespace Installer.Objects;

internal class DoorstopHolder : IAsyncDisposable
{
	public const string ReleaseType =
#if DEBUG
			"verbose"
#else
			"release"
#endif
		;

	private readonly SimpleFileInfo _configFile, _dllFile;

	private readonly AutoDisposeList<IDisposable> _updateSources;

	private Stream _dllData;
	private IList<string> _configData;

	public DoorstopHolder(ReadOnlySpan<char> gameDirectory)
	{
		_updateSources = new(1);
		_configFile = new(PathEx.Combine(gameDirectory, "doorstop_config.ini"), 19, 4);
		_dllFile = new(PathEx.Combine(gameDirectory, "winhttp.dll"), 11, 4);
	}

	public DoorstopHolder(DirectoryInfo gameDirectory)
		: this(gameDirectory.FullName)
	{
	}

	public DoorstopHolder(SimpleDirectoryInfo gameDirectory)
		: this(gameDirectory.FullName)
	{
	}

	public async ValueTask DisposeAsync( )
	{
		await using var fileStream = new FileStream(_dllFile.FullName.ToString( ), FileMode.Create, FileAccess.Write);
		await _dllData.CopyToAsync(fileStream);

		await File.WriteAllLinesAsync(_configFile.FullName.ToString( ), _configData);

		//_configData = null;
		await _dllData.DisposeAsync( );
		_updateSources.Dispose( );
	}

	public async Task Update(ZipArchive archive, string architecture)
	{
		_updateSources.Add(archive);

		var dllFileChecked = false;
		var configFileChecked = false;

		foreach (var entry in archive.Entries.Where(e => e.Length != 0 && e.FullName.StartsWith(architecture, StringComparison.OrdinalIgnoreCase)))
		{
			if (!dllFileChecked)
			{
				if (TryOpenEntry(entry, _dllFile, out _dllData))
				{
					if (AllDone(configFileChecked, ref dllFileChecked))
						break;
					continue;
				}
			}

			if (!configFileChecked)
			{
				if (TryOpenEntry(entry, _configFile, out var stream))
				{
					using var reader = new StreamReader(stream);
					var lines = await reader.ReadToEndAsync( );
					_configData = lines.Split(Environment.NewLine /*, StringSplitOptions.RemoveEmptyEntries*/);

					if (AllDone(dllFileChecked, ref configFileChecked))
						break;
					continue;
				}
			}
		}

		Debug.Assert(dllFileChecked && _dllData != null);
		Debug.Assert(configFileChecked && _configData != null);

		// ReSharper disable once RedundantAssignment
		bool AllDone(bool other, ref bool current)
		{
			if (other)
			{
#if DEBUG
				current = true;
#endif
				return true;
			}

			current = true;
			return false;
		}

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

	public void Write(ReadOnlySpan<char> targetAssemblyPath, ReadOnlySpan<char> dllSearchPath)
	{
		var usedIndex = new bool[_configData.Count];

		const string trueStr = "true";
		const string falseStr = "false";

		//[General]
		UpdateConfig("enabled", trueStr);
		UpdateConfig("redirect_output_log", trueStr);
		UpdateConfig("ignore_disable_switch", falseStr);
		UpdateConfig("target_assembly", targetAssemblyPath);

		//[UnityMono]
		UpdateConfig("dll_search_path_override", dllSearchPath);
#if DEBUG
		UpdateConfig("debug_enabled", trueStr);
#else
		UpdateConfig("debug_enabled", falseStr);
#endif
		UpdateConfig("debug_address", "127.0.0.1:10000");
		UpdateConfig("debug_suspend", falseStr);

		void UpdateConfig(string key, ReadOnlySpan<char> value)
		{
			Debug.Assert(_configData.Count(s => s.StartsWith(key)) == 1, "Multiple keys found");

			for (var index = 0; index != _configData.Count; ++index)
			{
				if (usedIndex[index])
					continue;
				if (!_configData[index].StartsWith(key))
					continue;
#if true
				var data = new char[key.Length + 1 + value.Length];
				key.CopyTo(data);
				data[key.Length] = '=';
				value.CopyTo(data.AsSpan(key.Length + 1));
				_configData[index] = new(data);
#else
				_configData[index] = $"{key}={value}";
#endif
				usedIndex[index] = true;
				break;
			}
		}
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
}
