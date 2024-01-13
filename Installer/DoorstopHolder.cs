using PeanutButter.INI;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;

namespace Installer;

public class DoorstopHolder : IDisposable
{
	private readonly INIFile _cfg;

	private DoorstopHolder(string path)
	{
		_cfg = new(path);
	}

	public DoorstopHolder(FileInfo dir)
		: this(dir.FullName)
	{
	}

	public DoorstopHolder(DirectoryInfo dir)
		: this(Path.Combine(dir.FullName, ConfigName))
	{
	}

	private static bool EqualDebug(string str, FileVersionInfo info)
	{
		//info.IsPreRelease
		if (str.Contains(info.IsDebug ? "verbose" : "release"))
			return true;

#if DEBUG
		return str.Contains("verbose");
#else
		return str.Contains("release");
#endif
	}

	public const string ConfigName = "doorstop_config.ini";
	public const string DllName = "winhttp.dll";

	public void Dispose( )
	{
		_cfg.WrapValueInQuotes = false;
		_cfg.Persist( );
	}

	//await using var st = entry.Open( );
	// await using var file = File.Create(Path.Combine(dir.FullName, entry.Name));
	// await st.CopyToAsync(file);

	public void Write(Stream source)
	{
		using var archive = new ZipArchive(source);
		var entries = archive.Entries.Where(e => e.FullName.Contains("x86")).ToArray( ); //todo: auto detect x64/x86

		var dll = entries.First(e => e.Name.EndsWith(".dll"));
		Debug.Assert(dll.Name == DllName);
		var cfg = entries.First(e => e.Name.EndsWith(".ini"));
		Debug.Assert(cfg.Name == ConfigName);
		Debug.Assert(_cfg.Path.EndsWith(ConfigName));
		var dllPath = string.Concat(_cfg.Path.AsSpan(0, _cfg.Path.Length - ConfigName.Length), DllName);
		dll.ExtractToFile(dllPath, true);
		Debug.Assert(FileVersionInfo.GetVersionInfo(dllPath).ProductMajorPart >= 4, "Outdated UnityDoorstop");

		var cfgExsists = File.Exists(_cfg.Path);
		cfg.ExtractToFile(_cfg.Path, cfgExsists);
		if (cfgExsists)
			_cfg.Merge(_cfg.Path, MergeStrategies.OnlyAddIfMissing);
		else
			_cfg.Reload( );
	}

	public void Write(bool isDebug, FileInfo targetAssembly, DirectoryInfo dllSearchPath)
	{
		const string trueStr = "true";
		const string falseStr = "false";

		var isDebugStr = isDebug ? trueStr : falseStr;

		var general = _cfg["General"];
		general["enabled"] = trueStr;
		general["redirect_output_log"] = isDebugStr;
		general["ignore_disable_switch"] = falseStr;
		general["target_assembly"] = targetAssembly.FullName;

		var mono = _cfg["UnityMono"];
		mono["dll_search_path_override"] = dllSearchPath.FullName;
		mono["debug_enabled"] = isDebugStr;
		mono["debug_address"] = "127.0.0.1:10000";
		mono["debug_suspend"] = falseStr;
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
