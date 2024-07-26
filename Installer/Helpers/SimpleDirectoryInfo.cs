using System.Diagnostics;

namespace Installer.Helpers;

internal class SimpleDirectoryInfo : SimpleFileSystemInfo
{
	private readonly string _source;
	private readonly int _nameOffset;

	public override ReadOnlySpan<char> FullName => _source;
	public override ReadOnlySpan<char> Name => FullName.Slice(_nameOffset);

	public SimpleDirectoryInfo Parent
	{
		get
		{
			if (Path.GetPathRoot(FullName).SequenceEqual(FullName))
				throw new InvalidOperationException("The root directory does not have a parent.");
			return new SimpleDirectoryInfoExternalSource(_source, Path.GetDirectoryName(FullName));
		}
	}

	public IEnumerable<SimpleFileInfo> EnumerateFiles( )
	{
		return Directory.EnumerateFiles(FullName.ToString( )).Select(filePath => new SimpleFileInfo(filePath));
	}

	protected SimpleDirectoryInfo(string fullName, int nameOffset)
	{
		_source = fullName;
		_nameOffset = nameOffset;
	}

	public SimpleDirectoryInfo(string fullName)
		: this(fullName, fullName.Length - Path.GetFileName(fullName.AsSpan( )).Length)
	{
	}
}

internal class SimpleDirectoryInfoExternalSource : SimpleDirectoryInfo
{
	private readonly int _fullNameLength;

	public override ReadOnlySpan<char> FullName => base.FullName.Slice(0, _fullNameLength);

	public SimpleDirectoryInfoExternalSource(string fullName, int fullNameLength, int nameLength)
		: base(fullName, fullNameLength - nameLength)
	{
		_fullNameLength = fullNameLength;
	}

	public SimpleDirectoryInfoExternalSource(string fullName, ReadOnlySpan<char> currentName)
		: this(fullName, currentName.Length, Path.GetFileName(currentName).Length)
	{
		Debug.Assert(Path.IsPathFullyQualified(currentName));
	}

	public SimpleDirectoryInfoExternalSource(string fullName)
		: this(fullName, Path.GetDirectoryName(fullName.AsSpan( )))
	{
	}
}
