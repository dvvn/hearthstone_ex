using System.Diagnostics;

namespace Installer.Helpers;

internal readonly struct UnityVersion
{
	public readonly FileVersionInfo Raw;
	private readonly int _fullOffset;
	public ReadOnlySpan<char> Full => Raw.ProductVersion.AsSpan(0, _fullOffset);
	private readonly int _numberOffset;
	public ReadOnlySpan<char> Number => Full.Slice(0, _numberOffset);

	public UnityVersion(FileVersionInfo info)
	{
		Raw = info;

		var rawVersion = info.ProductVersion.AsSpan( );

		var space = rawVersion.IndexOf(' ');
		_fullOffset = space != -1 ? space : rawVersion.Length;
		_numberOffset = Full.EndsWith("f1") ? _fullOffset - 2 : _fullOffset;
	}
}
