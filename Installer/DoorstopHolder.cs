using System.Diagnostics;
using System.IO.Compression;
using Installer.Extensions;
using Installer.Helpers;

namespace Installer;

internal class DoorstopHolder : IAsyncDisposable
{
	public const string ReleaseType =
#if DEBUG
			"verbose"
#else
			"release"
#endif
		;

	private readonly SimpleFileInfo _configSimpleFile, _dllSimpleFile;

	private Stream _dllData;
	private IList<string> _configData;

	public DoorstopHolder(string gameDirectory)
	{
		_configSimpleFile = new(CombinePath("doorstop_config.ini"));
		_dllSimpleFile = new(CombinePath("winhttp.dll"));

		return;

		string CombinePath(string fileName) => Path.Combine(gameDirectory, fileName);
	}

	public DoorstopHolder(ReadOnlySpan<char> gameDirectory)
		: this(gameDirectory.ToString( ))
	{
	}

	public async ValueTask DisposeAsync( )
	{
		await using var fileStream = new FileStream(_dllSimpleFile.FullName, FileMode.Create, FileAccess.Write);
		await _dllData.CopyToAsync(fileStream);

		await File.WriteAllLinesAsync(_configSimpleFile.FullName, _configData);

		_configData = null;
		await _dllData.DisposeAsync( );
	}

	public async Task Update(ZipArchive archive, string architecture)
	{
		var entries = archive.Entries.Where(e => e.Length != 0 && e.FullName.StartsWith(architecture)).ToArray( );

		_dllData = OpenEntry(_dllSimpleFile);

		using var reader = new StreamReader(OpenEntry(_configSimpleFile));
		var lines = await reader.ReadToEndAsync( );
		_configData = lines.Split(Environment.NewLine /*, StringSplitOptions.RemoveEmptyEntries*/);

		Stream OpenEntry(SimpleFileInfo info)
		{
			var targetEntry = entries.First(e => e.HasExtension(info.Extension));
			Debug.Assert(targetEntry.Name == info.Name);
			return targetEntry.Open( );
		}
	}

	public void Write(string targetAssemblyPath, string dllSearchPath)
	{
		//[General]
		UpdateConfig("enabled", true);
		UpdateConfig("redirect_output_log", true);
		UpdateConfig("ignore_disable_switch", false);
		UpdateConfig("target_assembly", targetAssemblyPath);

		//[UnityMono]
		UpdateConfig("dll_search_path_override", dllSearchPath);
#if DEBUG
		UpdateConfig("debug_enabled", true);
#else
		ConfigUpdate("debug_enabled", false);
#endif
		UpdateConfig("debug_address", "127.0.0.1:10000");
		UpdateConfig("debug_suspend", false);

		void UpdateConfig(string key, object value)
		{
			Debug.Assert(_configData.Count(s => s.StartsWith(key)) == 1, "Multiple keys found");

			for (var index = 0; index != _configData.Count; ++index)
			{
				if (_configData[index].StartsWith(key))
				{
					_configData[index] = $"{key}={value}";
					break;
				}
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
