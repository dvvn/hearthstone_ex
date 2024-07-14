namespace Installer.Helpers;

internal class SimpleFileInfo
{
	private readonly int _nameOffset, _extensionOffset;

	public readonly string FullName;
	public ReadOnlySpan<char> Directory => FullName.AsSpan( ).Slice(0, _nameOffset);
	public ReadOnlySpan<char> Name => FullName.AsSpan( ).Slice(_nameOffset /*, _extensionOffset - _nameOffset*/);
	public ReadOnlySpan<char> Extension => FullName.AsSpan( ).Slice(_extensionOffset);

	public SimpleFileInfo(string fullName, int nameLength, int extensionLength)
	{
		FullName = fullName;

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
