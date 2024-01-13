using PeanutButter.INI;
using System.Diagnostics;
using System.IO;
using System.Text;
using ByteSizeLib;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

namespace Installer;

internal class DoorstopHolder : IDisposable, IAsyncDisposable
{
	public const string ConfigName = "doorstop_config.ini";
	public const string DllName = "winhttp.dll";

	public const string ReleaseType =
#if DEBUG
			"verbose"
#else
			"release"
#endif
		;

	private readonly DirectoryInfo _dir;
	private readonly List<string> _lines;

	private void ConfigUpdate(string key, object value)
	{
		Debug.Assert(_lines.Count(s => s.StartsWith(key)) == 1, "Multiple keys found");
		var index = _lines.FindIndex(s => s.StartsWith(key));
		_lines[index] = $"{key}={value}";
	}

	public DoorstopHolder(DirectoryInfo dir)
		: this(Path.Combine(dir.FullName, ConfigName))
	{
		_dir = dir;
		_lines = new( );
	}

	public void Dispose( )
	{
		File.WriteAllLines(Path.Combine(_dir.FullName, ConfigName), _lines);
	}

	public async ValueTask DisposeAsync( )
	{
		await File.WriteAllLinesAsync(Path.Combine(_dir.FullName, ConfigName), _lines);
	}

	public async Task Update(ZipArchive archive)
	{
		var extractionOptions = new ExtractionOptions { ExtractFullPath = false, Overwrite = true };

		var entries = archive.Entries.Where(e => e.Size != 0 && e.Key.Contains("x86")).ToArray( ); //todo: auto detect x64/x86

		var dllEntry = entries.First(e => e.Key.EndsWith(".dll"));
		Debug.Assert(dllEntry.Key.EndsWith(DllName));
		var configEntry = entries.First(e => e.Key.EndsWith(".ini"));
		Debug.Assert(configEntry.Key.EndsWith(ConfigName));

		dllEntry.WriteToFile(Path.Combine(_dir.FullName, DllName), extractionOptions);

		StreamReader reader;
		var configFile = _dir.EnumerateFiles( ).FirstOrDefault(i => i.Name == ConfigName);
		if (configFile == default)
		{
			var stream = new MemoryStream((int)configEntry.Size);
			configEntry.WriteTo(stream);
			stream.Seek(0, SeekOrigin.Begin);
			reader = new(stream);
		}
		else
		{
			var text = configFile.OpenRead( );
			reader = new(text);
		}

		using (reader)
		{
			for (;;)
			{
				var line = await reader.ReadLineAsync( );
				if (line == null)
					break;
				_lines.Add(line);
			}
		}
	}

	public void Write(FileInfo targetAssembly, DirectoryInfo dllSearchPath)
	{
		//[General]
		ConfigUpdate("enabled", true);
		ConfigUpdate("redirect_output_log", true);
		ConfigUpdate("ignore_disable_switch", false);
		ConfigUpdate("target_assembly", targetAssembly.FullName);

		//[UnityMono]
		ConfigUpdate("dll_search_path_override", dllSearchPath.FullName);
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

	public bool CheckVersion(Uri url)
	{
		var dir = new FileInfo(_cfg.Path).Directory;
		var dllPath = Path.Combine(dir.FullName, DllName);

		if (!File.Exists(dllPath))
			return false;

		var fileName = Path.GetFileName(url.LocalPath);
		var dllInfo = FileVersionInfo.GetVersionInfo(dllPath);

		return EqualDebug(fileName, dllInfo) && ExtractVersion(fileName).SequenceEqual(dllInfo.ProductVersion);
	}
}
