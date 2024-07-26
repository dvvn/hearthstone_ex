namespace Installer.Helpers;

internal class SimpleFileInfo : SimpleFileSystemInfo
{
	private readonly int _nameOffset, _extensionOffset;
	private readonly string _source;

	public override ReadOnlySpan<char> FullName => _source;
	public SimpleDirectoryInfo Directory => new SimpleDirectoryInfoExternalSource(_source, FullName.Slice(0, _nameOffset - 1));
	public override ReadOnlySpan<char> Name => FullName.Slice(_nameOffset /*, _extensionOffset - _nameOffset*/);
	public ReadOnlySpan<char> Extension => FullName.Slice(_extensionOffset);

	public SimpleFileInfo(string fullName, int nameLength, int extensionLength)
	{
		_source = fullName;

		_nameOffset = fullName.Length - nameLength;
		_extensionOffset = fullName.Length - extensionLength;
	}

	public SimpleFileInfo(string fullName, ReadOnlySpan<char> fileName)
		: this(fullName, fileName.Length, Path.GetExtension(fileName).Length)
	{
	}

	public SimpleFileInfo(string fullName)
		: this(fullName, Path.GetFileName(fullName.AsSpan( )))
	{
	}
}
