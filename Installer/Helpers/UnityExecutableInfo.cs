using System.Diagnostics;

namespace Installer.Helpers;

internal readonly struct UnityExecutableInfo
{
	private readonly int _buildTypeLength;
	private readonly int _buildTypeOffset;

#if DEBUG
	private readonly FileVersionInfo _versionInfo;
	private ReadOnlySpan<char> _product => _versionInfo.ProductVersion;
#else
	private readonly string _productVersion;
	private ReadOnlySpan<char> _product => _productVersion;
#endif

	public ReadOnlySpan<char> FileVersion => _product.Slice(0, _buildTypeOffset);
	public ReadOnlySpan<char> ProductVersion => _product.Slice(0, _buildTypeOffset + _buildTypeLength);
	public ReadOnlySpan<char> BuildType => _product.Slice(_buildTypeOffset, _buildTypeLength);

	public ReadOnlySpan<char> ProductCode
	{
		get
		{
			const string codeOpen = " (";
			const string codeClose = ")";
			var start = _product.Slice(_buildTypeOffset + _buildTypeLength + codeOpen.Length);
			return start.Slice(0, start.Length - codeClose.Length);
		}
	}

	public Uri EditorSetupUrl => new($"https://download.unity3d.com/download_unity/{ProductCode}/Windows64EditorInstaller/UnitySetup64-{ProductVersion}.exe");

	public UnityExecutableInfo(string filePath)
	{
		var versionInfo = FileVersionInfo.GetVersionInfo(filePath);

#if DEBUG
		_versionInfo = versionInfo;
#else
		_productVersion = versionInfo.ProductVersion;
#endif

		var codeIndex = _product.IndexOf(' ');
		var lastPartIndex = _product.Slice(0, codeIndex).LastIndexOf('.') + 1;
		var lastPart = _product.Slice(lastPartIndex, codeIndex - lastPartIndex);
		// a == alpha
		// b == beta
		// r == rc == release candidate
		// f == final
		_buildTypeLength = lastPart.LastIndexOfAnyExcept("abrf") - 1;
		_buildTypeOffset = lastPartIndex + (lastPart.Length - _buildTypeLength);
	}
}
