using System.Diagnostics;
using System.IO.Compression;
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

	private MemoryStream _dllData;
	private List<string> _configData;

	private void ConfigUpdate(string key, object value)
	{
		Debug.Assert(_configData.Count(s => s.StartsWith(key)) == 1, "Multiple keys found");
		var index = _configData.FindIndex(s => s.StartsWith(key));
		_configData[index] = $"{key}={value}";
	}

	public DoorstopHolder(string gameDirectory)
	{
		_configSimpleFile = new(Path.Combine(gameDirectory, "doorstop_config.ini"));
		_dllSimpleFile = new(Path.Combine(gameDirectory, "winhttp.dll"));
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
	}

	private async Task UpdateDllFile(IEnumerable<ZipArchiveEntry> entries)
	{
		var targetEntry = entries.First(e => e.HasExtension(_dllSimpleFile.Extension));
		Debug.Assert(targetEntry.Name == _dllSimpleFile.Name);

		_dllData = await targetEntry.WriteToMemory( );
	}

	private async Task UpdateConfigData(IEnumerable<ZipArchiveEntry> entries)
	{
		var targetEntry = entries.First(e => e.HasExtension(_configSimpleFile.Extension));
		Debug.Assert(targetEntry.Name == _configSimpleFile.Name);

		using var reader = new StreamReader(await targetEntry.WriteToMemory( ));
		var lines = await reader.ReadToEndAsync( );
		_configData = new(lines.Split(Environment.NewLine /*, StringSplitOptions.RemoveEmptyEntries*/));
	}

	public async Task Update(ZipArchive archive, string architecture)
	{
		var entries = archive.Entries.Where(e => e.Length != 0 && e.FullName.StartsWith(architecture)).ToArray( );
		await Task.WhenAll(UpdateDllFile(entries), UpdateConfigData(entries));
	}

	public void Write(string targetAssemblyPath, string dllSearchPath)
	{
		//[General]
		ConfigUpdate("enabled", true);
		ConfigUpdate("redirect_output_log", true);
		ConfigUpdate("ignore_disable_switch", false);
		ConfigUpdate("target_assembly", targetAssemblyPath);

		//[UnityMono]
		ConfigUpdate("dll_search_path_override", dllSearchPath);
#if DEBUG
		ConfigUpdate("debug_enabled", true);
#else
		ConfigUpdate("debug_enabled", false);
#endif
		ConfigUpdate("debug_address", "127.0.0.1:10000");
		ConfigUpdate("debug_suspend", false);
	}

	//NAME_VERSION_.zip
	private static ReadOnlySpan<char> ExtractVersion(ReadOnlySpan<char> fileName)
	{
		var start = fileName.LastIndexOf('_') + 1;
		var end = fileName.LastIndexOf('.');
		var version = fileName.Slice(start, end - start);
		return version;
	}
}
